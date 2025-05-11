using UnityEngine;

public static class CustomMath
{
    public static float ConvertRange(float value, float originalStart, float originalEnd, float newStart, float newEnd)
    {
        float dividend = (originalEnd - originalStart);
        if (dividend == 0) dividend = 1;
        float scale = (newEnd - newStart) / dividend;
        return newStart + ((value - originalStart) * scale);
    }

    public static float ConvertRange(float value, float originalStart, float originalEnd)
    {
        float dividend = (originalEnd - originalStart);
        if (dividend == 0) dividend = 1;
        float scale = 1 / dividend;
        return (value - originalStart) * scale;
    }
    
    public static float Normalized01(float value, float from, float to)
    {
        // de from → to em 0…1, com clamp
        if (Mathf.Approximately(to, from))
            return 0f;
        return Mathf.Clamp01((value - from) / (to - from));
    }
}