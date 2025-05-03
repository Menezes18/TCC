using UnityEngine;

public static class CustomMath
{
    public static float ConvertRange(float value, float originalStart, float originalEnd)
    {
        return (value - originalStart) / (originalEnd - originalStart);
    }

    public static float ConvertRange(float value, float originalStart, float originalEnd, float newStart, float newEnd)
    {
        float t = (value - originalStart) / (originalEnd - originalStart);
        return Mathf.Lerp(newStart, newEnd, t);
    }
    
    public static float Normalized01(float value, float from, float to)
    {
        // de from → to em 0…1, com clamp
        if (Mathf.Approximately(to, from))
            return 0f;
        return Mathf.Clamp01((value - from) / (to - from));
    }
}