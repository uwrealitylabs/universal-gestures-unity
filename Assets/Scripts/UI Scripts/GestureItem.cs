using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestureItem : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI confidenceText;

    [SerializeField]
    private Image overlay;

    private static float threshold;
    private float confidence;

    private static readonly float fadeDuration = 0.1f;

    // manage text colours
    private static readonly Color defaultTextColor = new Color(0.5843138f, 0.5843138f, 0.5843138f, 1);
    private static readonly Color activatedTextColor = new Color(0.6627451f, 0.9098039f, 0.4784314f, 1);

    private bool glowing;

    public void Start()
    {
        glowing = false;
    }

    public void Update()
    {
        if(confidence >= threshold)
        {
            StartGlowing();
        }else
        {
            StopGlowing();
        }
    }

    // consumes a decimal, sets confidenceText to percentage with 1 decimal place
    public void SetConfidence(float confidence)
    {
        this.confidence = confidence;
        confidenceText.text = string.Format("{0:0.0%}", confidence);
    }

    public void StartGlowing()
    {
        if (!glowing)
        {
            glowing = true;
            StartCoroutine(FadeOverlay(true));
        }
    }

    public void StopGlowing()
    {
        if (glowing)
        {
            glowing = false;
            StartCoroutine(FadeOverlay(false));
        }
    }

    // probably a better name for this but I can't think
    IEnumerator FadeOverlay(bool fadeIn)
    {
        // manage overlay fading
        float startAlpha = overlay.color.a;
        float targetAlpha = fadeIn ? 1.0f : 0.0f;

        // manage text colour
        Color startTextColor = confidenceText.color;
        Color targetTextColor = fadeIn ? activatedTextColor : defaultTextColor;

        float elapsedTime = 0.0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);

            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            overlay.color = new Color(1, 1, 1, newAlpha);

            // manage text
            confidenceText.color = Color.Lerp(startTextColor, targetTextColor, progress);

            yield return null;
        }

        overlay.color = new Color(1, 1, 1, targetAlpha);
        confidenceText.color = targetTextColor;
    }

    public static void SetConfidenceThreshold(float thresh)
    {
        threshold = thresh;
    }
}
