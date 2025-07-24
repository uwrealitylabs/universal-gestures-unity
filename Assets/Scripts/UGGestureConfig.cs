using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all information for a specific gesture
/// </summary>
[System.Serializable]
public class GestureInfo
{
    public string name; // Internal gesture name (e.g., "thumbs_up")
    public string displayName; // Human-readable name (e.g., "Thumbs Up")
    public Color color; // Color for UI visualization

    public GestureInfo(string name, string displayName, Color color)
    {
        this.name = name;
        this.displayName = displayName;
        this.color = color;
    }
}

/// <summary>
/// Shared configuration and constants for Universal Gestures system
/// Contains gesture mappings, labels, and other shared data used across multiple scripts
/// </summary>
public static class UGGestureConfig
{
    /// <summary>
    /// Mapping of gesture names to their corresponding class indices in the ML model
    /// This is kept separate since it's the primary mapping from string to int
    /// </summary>
    public static readonly Dictionary<string, int> GestureLabelMap = new Dictionary<string, int>()
    {
        { "closed_fist", 0 },
        { "finger_gun", 1 },
        { "peace_sign", 2 },
        { "thumbs_up", 3 },
    };

    /// <summary>
    /// Complete gesture information indexed by class index
    /// Contains name, display name, and color for each gesture
    /// </summary>
    public static readonly Dictionary<int, GestureInfo> GestureInfoMap = new Dictionary<
        int,
        GestureInfo
    >()
    {
        { 0, new GestureInfo("closed_fist", "Closed Fist", Color.red) },
        { 1, new GestureInfo("finger_gun", "Finger Gun", Color.blue) },
        { 2, new GestureInfo("peace_sign", "Peace Sign", Color.green) },
        { 3, new GestureInfo("thumbs_up", "Thumbs Up", Color.yellow) },
    };

    /// <summary>
    /// Get the number of gesture classes
    /// </summary>
    public static int NumGestureClasses => GestureInfoMap.Count;

    /// <summary>
    /// Get gesture name from class index
    /// </summary>
    /// <param name="index">Class index from model output</param>
    /// <returns>Gesture name or "unknown" if index is invalid</returns>
    public static string GetGestureName(int index)
    {
        return GestureInfoMap.TryGetValue(index, out GestureInfo info) ? info.name : "unknown";
    }

    /// <summary>
    /// Get display name for gesture by index
    /// </summary>
    /// <param name="index">Class index from model output</param>
    /// <returns>Human-readable display name</returns>
    public static string GetDisplayName(int index)
    {
        return GestureInfoMap.TryGetValue(index, out GestureInfo info)
            ? info.displayName
            : "Unknown";
    }

    /// <summary>
    /// Get display name for gesture by name
    /// </summary>
    /// <param name="gestureName">Internal gesture name</param>
    /// <returns>Human-readable display name</returns>
    public static string GetDisplayName(string gestureName)
    {
        if (GestureLabelMap.TryGetValue(gestureName, out int index))
        {
            return GetDisplayName(index);
        }
        return gestureName;
    }

    /// <summary>
    /// Get color for gesture by class index
    /// </summary>
    /// <param name="index">Class index from model output</param>
    /// <returns>Color for UI visualization</returns>
    public static Color GetGestureColor(int index)
    {
        return GestureInfoMap.TryGetValue(index, out GestureInfo info) ? info.color : Color.white;
    }

    /// <summary>
    /// Get color for gesture by name
    /// </summary>
    /// <param name="gestureName">Internal gesture name</param>
    /// <returns>Color for UI visualization</returns>
    public static Color GetGestureColor(string gestureName)
    {
        if (GestureLabelMap.TryGetValue(gestureName, out int index))
        {
            return GetGestureColor(index);
        }
        return Color.white;
    }

    /// <summary>
    /// Get complete gesture information by index
    /// </summary>
    /// <param name="index">Class index from model output</param>
    /// <returns>GestureInfo object or null if index is invalid</returns>
    public static GestureInfo GetGestureInfo(int index)
    {
        return GestureInfoMap.TryGetValue(index, out GestureInfo info) ? info : null;
    }

    /// <summary>
    /// Get complete gesture information by name
    /// </summary>
    /// <param name="gestureName">Internal gesture name</param>
    /// <returns>GestureInfo object or null if name is invalid</returns>
    public static GestureInfo GetGestureInfo(string gestureName)
    {
        if (GestureLabelMap.TryGetValue(gestureName, out int index))
        {
            return GetGestureInfo(index);
        }
        return null;
    }

    /// <summary>
    /// Get gesture index from name
    /// </summary>
    /// <param name="gestureName">Internal gesture name</param>
    /// <returns>Gesture index or -1 if not found</returns>
    public static int GetGestureIndex(string gestureName)
    {
        return GestureLabelMap.TryGetValue(gestureName, out int index) ? index : -1;
    }
}
