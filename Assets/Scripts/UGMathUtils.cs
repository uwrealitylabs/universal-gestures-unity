using System.Linq;
using UnityEngine;

public static class UGMathUtils
{
    public static float[] ApplySoftmax(float[] logits)
    {
        if (logits.Length == 0)
            return logits;

        // Find max value for numerical stability
        float maxLogit = logits.Max();

        // Compute exponentials
        float[] exponentials = new float[logits.Length];
        float sumExp = 0f;

        for (int i = 0; i < logits.Length; i++)
        {
            exponentials[i] = Mathf.Exp(logits[i] - maxLogit);
            sumExp += exponentials[i];
        }

        // Normalize to get probabilities
        for (int i = 0; i < exponentials.Length; i++)
        {
            exponentials[i] /= sumExp;
        }

        return exponentials;
    }

    public static int ArgMax(float[] values)
    {
        if (values.Length == 0)
            return -1;

        int maxIndex = 0;
        float maxValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > maxValue)
            {
                maxValue = values[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    public static float GetMaxConfidence(float[] probabilities)
    {
        return probabilities.Length > 0 ? probabilities.Max() : 0f;
    }
}
