using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UGBarGraph : MonoBehaviour
{
    public GameObject barPrefab;
    public List<float> data = new List<float>(); // Will be initialized if empty
    public float maxValue = 1.0f; // Changed from 100 to 1.0 for normalized values
    public float minValue = 0;
    public float barSpacing = 0.1f;
    public float barWidth = 0.1f;
    public float maxBarHeight = 100f; // Maximum height for bars in UI units
    public Color barColor = Color.white;
    public Color barBackgroundColor = Color.gray;
    public Color barBorderColor = Color.black;
    public float barBorderWidth = 1f;

    [Header("Visualization")]
    public bool useGestureSpecificColors = true; // Use different colors for each gesture class
    public bool showGestureLabels = true; // Show gesture names below bars
    public bool showTopGestureOnly = true; // Show only the highest confidence gesture name

    [Header("Gesture Display")]
    public TMPro.TextMeshProUGUI gestureNameText; // Text component to show current top gesture
    public TMPro.TextMeshProUGUI gestureConfidenceText; // Text component to show confidence value

    [Header("Inference Integration")]
    [SerializeField]
    private GameObject inferenceRunnerObject;
    private UGInferenceRunnerScript inferenceRunner;
    public bool useInferenceOutput = true;
    public bool applySoftmax = true;

    // Cache for efficiency
    private List<GameObject> barObjects = new List<GameObject>();
    private List<Image> barImages = new List<Image>();
    private List<RectTransform> barRects = new List<RectTransform>();

    void Awake()
    {
        if (data == null || data.Count == 0)
        {
            data = new List<float> { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f }; // default data for testing
        }
    }

    void Start()
    {
        inferenceRunner = inferenceRunnerObject.GetComponent<UGInferenceRunnerScript>();
        CreateBarsAndLabels();

        // Create a test label directly on the canvas
        GameObject testLabel = new GameObject("TEST_LABEL");
        testLabel.transform.SetParent(transform.parent, false); // Same parent as UGBarGraph
        var testText = testLabel.AddComponent<TMPro.TextMeshProUGUI>();
        testText.text = "TEST VISIBLE";
        testText.fontSize = 1.0f;
        testText.color = Color.red;
    }

    void Update()
    {
        if (useInferenceOutput && inferenceRunner != null)
        {
            if (
                inferenceRunner.inferenceOutput != null
                && inferenceRunner.inferenceOutput.Length > 0
            )
            {
                UpdateBarsWithInferenceData();
            }
            else
            {
                // Debug: Show why inference data isn't available
                if (inferenceRunner.inferenceOutput == null)
                {
                    Debug.LogWarning(
                        "UGBarGraph: inferenceOutput is null - inference may not be running yet"
                    );
                }
                else if (inferenceRunner.inferenceOutput.Length == 0)
                {
                    Debug.LogWarning(
                        "UGBarGraph: inferenceOutput is empty - no inference data available"
                    );
                }
            }
        }
    }

    void CreateBarsAndLabels()
    {
        // Clear existing bars
        foreach (Transform child in transform)
        {
            if (child.gameObject != barPrefab)
                Destroy(child.gameObject);
        }

        // Clear cache
        barObjects.Clear();
        barImages.Clear();
        barRects.Clear();

        // Create bars for expected number of gesture classes or current data
        int numBars = useInferenceOutput ? UGGestureConfig.NumGestureClasses : data.Count;

        float xPos = 0;
        for (int i = 0; i < numBars; i++)
        {
            // Create bar
            GameObject bar = Instantiate(barPrefab, transform);
            RectTransform rectTransform = bar.GetComponent<RectTransform>();
            Image barImage = bar.GetComponent<Image>();

            if (rectTransform == null)
            {
                Debug.LogError("UGBarGraph: Bar prefab must have a RectTransform component");
                Destroy(bar);
                continue;
            }

            // Set up bar positioning (height will be updated later)
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0.5f, 0f); // Bottom center
            rectTransform.anchoredPosition = new Vector2(xPos + barWidth * 0.5f, 0);
            rectTransform.sizeDelta = new Vector2(barWidth, 0); // Start with 0 height

            // Set bar color and name
            if (useGestureSpecificColors && i < UGGestureConfig.NumGestureClasses)
            {
                Color gestureColor = UGGestureConfig.GetGestureColor(i);
                if (barImage != null)
                    barImage.color = gestureColor;
                bar.name = $"Bar_{i}_{UGGestureConfig.GetGestureName(i)}";
            }
            else
            {
                if (barImage != null)
                    barImage.color = barColor;
                bar.name = $"Bar_{i}";
            }

            // Cache components for efficient updates
            barObjects.Add(bar);
            barImages.Add(barImage);
            barRects.Add(rectTransform);

            // Create gesture label if enabled and it's a gesture class
            if (showGestureLabels && i < UGGestureConfig.NumGestureClasses)
            {
                CreateGestureLabel(bar, i);
            }

            xPos += barWidth + barSpacing;
        }

        // Initialize with current data
        if (!useInferenceOutput)
        {
            UpdateBarHeights(data);
        }
    }

    void CreateGestureLabel(GameObject bar, int gestureIndex)
    {
        // Create a text label below the bar
        GameObject labelObj = new GameObject($"Label_{gestureIndex}");
        labelObj.transform.SetParent(bar.transform, false);
        labelObj.layer = bar.layer; // Ensure same layer as parent

        // Add RectTransform
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f); // Top center
        labelRect.anchoredPosition = new Vector2(0, 0); // 10 units below bar (increased)
        labelRect.sizeDelta = new Vector2(Mathf.Max(barWidth * 4f, 1), 0.4f); // Bigger size

        // Add TextMeshPro component
        TMPro.TextMeshProUGUI labelText = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        labelText.text = UGGestureConfig.GetDisplayName(gestureIndex);
        labelText.fontSize = 0.2f; // Increased font size
        labelText.color = Color.white;
        labelText.alignment = TMPro.TextAlignmentOptions.Center;
        labelText.enableWordWrapping = true;

        // Try to assign a font asset (fallback to default)
        labelText.font = TMPro.TMP_Settings.defaultFontAsset;
        if (labelText.font != null)
        {
            labelText.fontSharedMaterial = labelText.font.material; // Ensure material is assigned
        }

        // Ensure text is rendered
        labelText.enabled = true;
        labelText.raycastTarget = false; // Disable raycast to avoid UI blocking

        // Comprehensive debugging
        Debug.Log(
            $"Created label for gesture {gestureIndex}: {UGGestureConfig.GetDisplayName(gestureIndex)}"
        );
        Debug.Log($"  - Local position: {labelRect.anchoredPosition}");
        Debug.Log($"  - World position: {labelObj.transform.position}");
        Debug.Log($"  - Size delta: {labelRect.sizeDelta}");
        Debug.Log($"  - Font: {(labelText.font != null ? labelText.font.name : "NULL")}");
        Debug.Log(
            $"  - Material: {(labelText.fontSharedMaterial != null ? labelText.fontSharedMaterial.name : "NULL")}"
        );
        Debug.Log($"  - Layer: {labelObj.layer}");
        Debug.Log($"  - Parent Canvas: {GetComponentInParent<Canvas>()?.name ?? "NULL"}");

        // Force a text update
        labelText.ForceMeshUpdate();
    }

    void CreateBars()
    {
        // Legacy method - now calls the more efficient version
        CreateBarsAndLabels();
    }

    void UpdateBarsWithInferenceData()
    {
        Debug.Log(
            "inferenceRunner.inferenceOutput.Length: " + inferenceRunner.inferenceOutput.Length
        );
        if (inferenceRunner.inferenceOutput.Length == 0)
            return;

        List<float> inferenceData = new List<float>(inferenceRunner.inferenceOutput);

        inferenceData = new List<float>(UGMathUtils.ApplySoftmax(inferenceData.ToArray()));

        Debug.Log(
            "Inference probabilities: ["
                + string.Join(", ", inferenceData.Select(x => x.ToString("F3")))
                + "]"
        );

        UpdateBarHeights(inferenceData);

        // Update top gesture display
        UpdateTopGestureDisplay(inferenceData);
    }

    void UpdateBarHeights(List<float> dataToUse)
    {
        if (dataToUse == null || dataToUse.Count == 0)
        {
            Debug.LogWarning("UGBarGraph: No data to display");
            return;
        }

        // Update existing bars efficiently
        for (int i = 0; i < barRects.Count && i < dataToUse.Count; i++)
        {
            float value = dataToUse[i];

            // Update bar height
            float normalizedValue = Mathf.Clamp01((value - minValue) / (maxValue - minValue));
            float barHeight = normalizedValue * maxBarHeight;
            barRects[i].sizeDelta = new Vector2(barWidth, barHeight);

            // Update confidence in label if showing all labels
            if (showGestureLabels && !showTopGestureOnly && i < UGGestureConfig.NumGestureClasses)
            {
                Transform labelTransform = barObjects[i].transform.Find($"Label_{i}");
                if (labelTransform != null)
                {
                    TMPro.TextMeshProUGUI labelText =
                        labelTransform.GetComponent<TMPro.TextMeshProUGUI>();
                    if (labelText != null)
                    {
                        labelText.text = UGGestureConfig.GetDisplayName(i) + $"\n{value:F2}";
                    }
                }
            }
        }
    }

    void CreateBarsFromData(List<float> dataToUse)
    {
        // Legacy method - now just updates heights
        UpdateBarHeights(dataToUse);

        if (useInferenceOutput && dataToUse.Count > 0)
        {
            UpdateTopGestureDisplay(dataToUse);
        }
    }

    void UpdateTopGestureDisplay(List<float> dataToUse)
    {
        int maxIndex = UGMathUtils.ArgMax(dataToUse.ToArray());
        float maxConfidence = dataToUse[maxIndex];
        string gestureName = UGGestureConfig.GetGestureName(maxIndex);
        string displayName = UGGestureConfig.GetDisplayName(gestureName);

        // Update UI text components if assigned
        if (gestureNameText != null)
        {
            gestureNameText.text = displayName;
            gestureNameText.color = UGGestureConfig.GetGestureColor(maxIndex);
        }

        if (gestureConfidenceText != null)
        {
            gestureConfidenceText.text = $"{maxConfidence:F3} ({maxConfidence * 100:F1}%)";
        }

        Debug.Log($"Highest confidence: {displayName} ({maxConfidence:F3})");
    }
}
