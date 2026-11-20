using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Saves and loads gestures so they survive closing the app.
///
/// Layout (under Application.persistentDataPath, on PC/Editor and on Quest alike):
///
///   UniversalGestures/
///   ├─ gestures/
///   │  ├─ 7f3a9c21e4b0/                 ← folder = stable gesture ID (not the name)
///   │  │  ├─ gesture.json               ← GestureRecord: name, settings, recordings, model ref
///   │  │  └─ recordings/
///   │  │     ├─ 000.json                ← existing recording JSON format, positive
///   │  │     └─ 001_neg.json            ← negative
///   │  └─ c18d02aa94f1/ ...
///   └─ models/
///      └─ m_20261029-201502_ab12cd.onnx ← trained models, referenced by gesture.json
///
/// All paths stored in gesture.json are relative to the UniversalGestures/ folder.
/// </summary>
public static class GestureLibrary
{
    public const int MaxNameLength = 40;
    const string RecordFileName = "gesture.json";
    static readonly Regex RecordingFile = new Regex(@"^(\d+)(_neg)?\.json$", RegexOptions.Compiled);

    static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
    };

    /// <summary>Fired after anything is created, renamed, deleted, recorded or trained.</summary>
    public static event Action Changed;

    static Dictionary<string, GestureRecord> cache;

    // ------------------------------------------------------------------ Paths

    /// <summary>Tests point this at a temp folder. Leave null in the app.</summary>
    public static string RootOverride;

    public static string Root => RootOverride ?? Path.Combine(Application.persistentDataPath, "UniversalGestures");

    /// <summary>Forget the in-memory copy (next access re-reads disk). Used by tests to simulate a restart.</summary>
    public static void ClearCache() => cache = null;
    static string GesturesDir => Path.Combine(Root, "gestures");
    static string ModelsDir => Path.Combine(Root, "models");
    static string GestureDir(string id) => Path.Combine(GesturesDir, id);
    static string RecordPath(string id) => Path.Combine(GestureDir(id), RecordFileName);

    public static string ToAbsolute(string relative) =>
        Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

    static string ToRelative(string absolute)
    {
        string rel = Path.GetFullPath(absolute).Substring(Path.GetFullPath(Root).Length)
                         .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return rel.Replace('\\', '/'); // always forward slashes in saved JSON
    }

    // ------------------------------------------------------------------ Loading

    public static IReadOnlyList<GestureRecord> All
    {
        get
        {
            EnsureLoaded();
            return cache.Values.OrderBy(g => g.createdAt, StringComparer.Ordinal).ToList();
        }
    }

    public static GestureRecord Get(string id)
    {
        EnsureLoaded();
        return id != null && cache.TryGetValue(id, out var g) ? g : null;
    }

    public static GestureRecord FindByName(string displayName)
    {
        EnsureLoaded();
        string key = displayName?.Trim();
        return cache.Values.FirstOrDefault(g =>
            string.Equals(g.displayName, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Re-reads everything from disk. Called automatically on first use.</summary>
    public static void Reload()
    {
        cache = new Dictionary<string, GestureRecord>();
        if (!Directory.Exists(GesturesDir)) return;

        foreach (string dir in Directory.GetDirectories(GesturesDir))
        {
            string path = Path.Combine(dir, RecordFileName);
            if (!File.Exists(path)) continue;
            try
            {
                var record = JsonConvert.DeserializeObject<GestureRecord>(File.ReadAllText(path), JsonSettings);
                if (record == null || string.IsNullOrEmpty(record.id)) continue;
                record.id = Path.GetFileName(dir); // folder name is the source of truth for the ID
                record.recordings ??= new List<RecordingRef>();
                record.settings ??= new GestureSettings();

                // Drop references to files that were deleted outside the app
                record.recordings.RemoveAll(r => !File.Exists(ToAbsolute(r.file)));
                if (record.model != null && !File.Exists(ToAbsolute(record.model.file)))
                {
                    Debug.LogWarning($"GestureLibrary: model for \"{record.displayName}\" is missing; marking untrained.");
                    record.model = null;
                }
                cache[record.id] = record;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GestureLibrary: skipped unreadable gesture at {path}: {e.Message}");
            }
        }
        Debug.Log($"GestureLibrary: loaded {cache.Count} gesture(s) from {Root}");
    }

    static void EnsureLoaded()
    {
        if (cache == null) Reload();
    }

    // ------------------------------------------------------------------ Names

    public static bool ValidateDisplayName(string name, string excludeId, out string error)
    {
        error = null;
        string trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed)) { error = "Enter a gesture name."; return false; }
        if (trimmed.Length > MaxNameLength) { error = $"Max {MaxNameLength} characters."; return false; }
        var existing = FindByName(trimmed);
        if (existing != null && existing.id != excludeId)
        {
            error = $"\"{existing.displayName}\" already exists.";
            return false;
        }
        return true;
    }

    // ------------------------------------------------------------------ Create / rename / delete

    /// <summary>Creates and saves a new, untrained gesture.</summary>
    public static GestureRecord Create(string displayName, GestureSettings settings)
    {
        if (!ValidateDisplayName(displayName, null, out string error))
            throw new ArgumentException(error);

        string now = Now();
        var record = new GestureRecord
        {
            id = NewId(),
            displayName = displayName.Trim(),
            createdAt = now,
            updatedAt = now,
            settings = settings ?? new GestureSettings(),
        };
        record.settings.featureCount =
            GestureSettings.FeatureCountFor(record.settings.handMode, record.settings.useTransformData);

        Directory.CreateDirectory(Path.Combine(GestureDir(record.id), "recordings"));
        Save(record);
        return record;
    }

    /// <summary>Changes only the display name. ID, folder, recordings and model are untouched.</summary>
    public static void Rename(string id, string newDisplayName)
    {
        var record = Require(id);
        if (!ValidateDisplayName(newDisplayName, id, out string error))
            throw new ArgumentException(error);
        record.displayName = newDisplayName.Trim();
        Save(record);
    }

    /// <summary>
    /// Deletes one gesture's folder (record + recordings). Its model file is deleted only if no
    /// other gesture still references it.
    /// </summary>
    public static void Delete(string id)
    {
        var record = Require(id);
        string modelFile = record.model?.file;

        string dir = GestureDir(id);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        cache.Remove(id);

        ReleaseModelIfUnused(modelFile);
        Changed?.Invoke();
    }

    /// <summary>Saves a record after changing its settings (or anything else) in code.</summary>
    public static void Save(GestureRecord record)
    {
        EnsureLoaded();
        record.updatedAt = Now();
        Directory.CreateDirectory(GestureDir(record.id));
        WriteAtomic(RecordPath(record.id), JsonConvert.SerializeObject(record, JsonSettings));
        cache[record.id] = record;
        Changed?.Invoke();
    }

    // ------------------------------------------------------------------ Recordings

    /// <summary>
    /// Returns the absolute path the data writer should write the next recording to,
    /// e.g. .../recordings/004_neg.json. Numbers are never reused.
    /// </summary>
    public static string NextRecordingPath(string id, bool isNegative)
    {
        Require(id);
        string dir = Path.Combine(GestureDir(id), "recordings");
        Directory.CreateDirectory(dir);

        int max = -1;
        foreach (string f in Directory.GetFiles(dir, "*.json"))
        {
            var m = RecordingFile.Match(Path.GetFileName(f));
            if (m.Success) max = Math.Max(max, int.Parse(m.Groups[1].Value));
        }
        return Path.Combine(dir, (max + 1).ToString("D3") + (isNegative ? "_neg" : "") + ".json");
    }

    /// <summary>Registers a finished recording file with its gesture.</summary>
    public static RecordingRef AddRecording(string id, string absolutePath, bool isNegative, int frames)
    {
        var record = Require(id);
        if (!File.Exists(absolutePath) || frames <= 0) return null;

        var rec = new RecordingRef
        {
            file = ToRelative(absolutePath),
            label = isNegative ? RecordingLabel.Negative : RecordingLabel.Positive,
            frames = frames,
            recordedAt = Now(),
        };
        record.recordings.RemoveAll(r => r.file == rec.file);
        record.recordings.Add(rec);
        Save(record);
        return rec;
    }

    public static void DeleteRecording(string id, string relativeFile)
    {
        var record = Require(id);
        string path = ToAbsolute(relativeFile);
        if (File.Exists(path)) File.Delete(path);
        record.recordings.RemoveAll(r => r.file == relativeFile);
        Save(record);
    }

    public static IEnumerable<string> RecordingPaths(string id) =>
        Require(id).recordings.Select(r => ToAbsolute(r.file));

    // ------------------------------------------------------------------ Models

    /// <summary>
    /// Stores a freshly trained binary model for one gesture under a unique file name
    /// (so training another gesture never overwrites it) and points the gesture at it.
    /// </summary>
    public static string SaveTrainedModel(string id, byte[] onnxBytes)
    {
        var record = Require(id);
        Directory.CreateDirectory(ModelsDir);

        string fileName = $"m_{DateTime.Now:yyyyMMdd-HHmmss}_{NewId().Substring(0, 6)}.onnx";
        string absolute = Path.Combine(ModelsDir, fileName);
        File.WriteAllBytes(absolute, onnxBytes);

        string previous = record.model?.file;
        record.model = new ModelRef
        {
            file = ToRelative(absolute),
            trainedAt = Now(),
            outputType = "binary",
            outputIndex = 0,
            classLabels = new List<string> { record.id },
            trainedOnRecordings = record.recordings.Select(r => r.file).ToList(),
        };
        Save(record);

        if (previous != record.model.file) ReleaseModelIfUnused(previous);
        return absolute;
    }

    public static string ModelPath(GestureRecord record) =>
        record?.IsTrained == true ? ToAbsolute(record.model.file) : null;

    /// <summary>True if recordings were added/removed since the model was trained.</summary>
    public static bool NeedsRetraining(GestureRecord record)
    {
        if (!record.IsTrained) return record.recordings.Count > 0;
        var now = new HashSet<string>(record.recordings.Select(r => r.file));
        return !now.SetEquals(record.model.trainedOnRecordings ?? new List<string>());
    }

    static void ReleaseModelIfUnused(string relativeModelFile)
    {
        if (string.IsNullOrEmpty(relativeModelFile)) return;
        bool stillUsed = cache.Values.Any(g => g.model != null && g.model.file == relativeModelFile);
        if (stillUsed) return;
        string path = ToAbsolute(relativeModelFile);
        if (File.Exists(path)) File.Delete(path);
    }

    // ------------------------------------------------------------------ Helpers

    static GestureRecord Require(string id) =>
        Get(id) ?? throw new KeyNotFoundException($"No saved gesture with id \"{id}\".");

    static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 12);

    static string Now() => DateTime.Now.ToString("o");

    static void WriteAtomic(string path, string contents)
    {
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, contents);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }
}
