using System.IO;
using System.Linq;
using NUnit.Framework;

// EditMode tests for the saved-gesture storage layer.
// Run: Window > General > Test Runner > EditMode > Run All.
// They use a temp folder, so your real saved gestures are never touched.
public class GestureLibraryTests
{
    string tempRoot;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "UGTests_" + System.Guid.NewGuid().ToString("N"));
        GestureLibrary.RootOverride = tempRoot;
        GestureLibrary.ClearCache();
    }

    [TearDown]
    public void TearDown()
    {
        GestureLibrary.RootOverride = null;
        GestureLibrary.ClearCache();
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
    }

    // Writes a small recording in the existing JSON format and registers it.
    static void Record(GestureRecord g, bool negative, int frames = 3)
    {
        string path = GestureLibrary.NextRecordingPath(g.id, negative);
        var lines = Enumerable.Range(0, frames).Select(i =>
            $"    {{\"confidence\":{(negative ? 0 : 1)},\"handData\":[{string.Join(",", Enumerable.Repeat("0.5", g.settings.featureCount))}]}}");
        File.WriteAllText(path, "[\n" + string.Join(",\n", lines) + "\n]");
        GestureLibrary.AddRecording(g.id, path, negative, frames);
    }

    static void SimulateRestart() => GestureLibrary.ClearCache();

    [Test]
    public void TwoGestures_SurviveRestart_WithNamesRecordingsAndSettings()
    {
        var rock = GestureLibrary.Create("Rockstar", new GestureSettings { handMode = HandMode.RightHand });
        var heart = GestureLibrary.Create("Heart Hands", new GestureSettings { handMode = HandMode.TwoHands });
        Record(rock, false); Record(rock, true); Record(heart, false);
        rock.settings.thresholdConfidenceLevel = 0.65f;
        GestureLibrary.Save(rock);

        SimulateRestart();

        var r = GestureLibrary.Get(rock.id);
        var h = GestureLibrary.Get(heart.id);
        Assert.AreEqual("Rockstar", r.displayName);
        Assert.AreEqual("Heart Hands", h.displayName);
        Assert.AreEqual(1, r.PositiveCount);
        Assert.AreEqual(1, r.NegativeCount);
        Assert.AreEqual(0.65f, r.settings.thresholdConfidenceLevel, 1e-6);
        Assert.AreEqual(HandMode.TwoHands, h.settings.handMode);
        Assert.AreEqual(44, h.settings.featureCount);
        Assert.IsTrue(GestureLibrary.RecordingPaths(rock.id).All(File.Exists));
    }

    [Test]
    public void UntrainedGesture_IsSaved()
    {
        var g = GestureLibrary.Create("Wave", null);
        SimulateRestart();
        Assert.IsNotNull(GestureLibrary.Get(g.id));
        Assert.IsFalse(GestureLibrary.Get(g.id).IsTrained);
    }

    [Test]
    public void Recordings_UseNegMarker_AndNeverReuseNumbers()
    {
        var g = GestureLibrary.Create("Peace", null);
        Record(g, false); Record(g, true);
        string first = GestureLibrary.Get(g.id).recordings[0].file;
        GestureLibrary.DeleteRecording(g.id, first);
        Record(g, false);

        var files = GestureLibrary.Get(g.id).recordings.Select(r => Path.GetFileName(r.file)).ToList();
        CollectionAssert.AreEquivalent(new[] { "001_neg.json", "002.json" }, files);
    }

    [Test]
    public void Rename_KeepsIdRecordingsAndModel()
    {
        var g = GestureLibrary.Create("Rokstar", null);
        Record(g, false);
        GestureLibrary.SaveTrainedModel(g.id, new byte[] { 1, 2, 3 });

        GestureLibrary.Rename(g.id, "Rockstar");
        SimulateRestart();

        var r = GestureLibrary.Get(g.id);
        Assert.AreEqual("Rockstar", r.displayName);
        Assert.AreEqual(1, r.recordings.Count);
        Assert.IsTrue(r.IsTrained);
        Assert.IsTrue(File.Exists(GestureLibrary.ModelPath(r)));
    }

    [Test]
    public void DuplicateNames_AreRejected_CaseInsensitive()
    {
        GestureLibrary.Create("Rockstar", null);
        Assert.IsFalse(GestureLibrary.ValidateDisplayName("rockstar", null, out _));
        Assert.Throws<System.ArgumentException>(() => GestureLibrary.Create("ROCKSTAR ", null));
    }

    [Test]
    public void TrainingTwoGestures_DoesNotOverwriteModels()
    {
        var a = GestureLibrary.Create("A", null);
        var b = GestureLibrary.Create("B", null);
        string pa = GestureLibrary.SaveTrainedModel(a.id, new byte[] { 1 });
        string pb = GestureLibrary.SaveTrainedModel(b.id, new byte[] { 2 });

        Assert.AreNotEqual(pa, pb);
        Assert.AreEqual(1, File.ReadAllBytes(pa)[0]);
        Assert.AreEqual(2, File.ReadAllBytes(pb)[0]);
    }

    [Test]
    public void Retraining_ReplacesOnlyThatGesturesOldModel()
    {
        var a = GestureLibrary.Create("A", null);
        string first = GestureLibrary.SaveTrainedModel(a.id, new byte[] { 1 });
        string second = GestureLibrary.SaveTrainedModel(a.id, new byte[] { 9 });
        Assert.IsFalse(File.Exists(first));
        Assert.IsTrue(File.Exists(second));
    }

    [Test]
    public void Delete_LeavesOtherGesturesAndSharedModel()
    {
        var a = GestureLibrary.Create("A", null);
        var b = GestureLibrary.Create("B", null);
        Record(b, false);
        string modelPath = GestureLibrary.SaveTrainedModel(a.id, new byte[] { 7 });

        // B shares A's model (e.g. a future multi-class model)
        var bRec = GestureLibrary.Get(b.id);
        bRec.model = new ModelRef { file = GestureLibrary.Get(a.id).model.file, outputIndex = 1 };
        GestureLibrary.Save(bRec);

        GestureLibrary.Delete(a.id);
        SimulateRestart();

        Assert.IsNull(GestureLibrary.Get(a.id));
        Assert.IsNotNull(GestureLibrary.Get(b.id));
        Assert.AreEqual(1, GestureLibrary.Get(b.id).recordings.Count);
        Assert.IsTrue(File.Exists(modelPath), "shared model must survive");

        GestureLibrary.Delete(b.id);
        Assert.IsFalse(File.Exists(modelPath), "model is removed once nothing uses it");
    }

    [Test]
    public void SavedRecord_UsesOnlyRelativePaths()
    {
        var g = GestureLibrary.Create("Fist", null);
        Record(g, false);
        GestureLibrary.SaveTrainedModel(g.id, new byte[] { 1 });

        string json = File.ReadAllText(Path.Combine(tempRoot, "gestures", g.id, "gesture.json"));
        StringAssert.DoesNotContain(tempRoot.Replace('\\', '/'), json.Replace("\\\\", "/"));
        StringAssert.Contains($"\"gestures/{g.id}/recordings/000.json\"", json);
        StringAssert.Contains("\"models/m_", json);
    }

    [Test]
    public void MissingFiles_AreDroppedOnLoad_NotCrashing()
    {
        var g = GestureLibrary.Create("Gun", null);
        Record(g, false);
        GestureLibrary.SaveTrainedModel(g.id, new byte[] { 1 });
        File.Delete(GestureLibrary.ModelPath(GestureLibrary.Get(g.id)));
        File.Delete(GestureLibrary.RecordingPaths(g.id).First());

        SimulateRestart();

        var r = GestureLibrary.Get(g.id);
        Assert.AreEqual(0, r.recordings.Count);
        Assert.IsFalse(r.IsTrained);
    }
}
