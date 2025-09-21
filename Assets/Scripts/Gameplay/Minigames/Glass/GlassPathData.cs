using System.Collections.Generic;
using UnityEngine;

public enum GlassSide { Left = 0, Right = 1 }

[CreateAssetMenu(fileName = "GlassPath", menuName = "Minigame/Glass Path")]
public class GlassPathData : ScriptableObject
{
    [Tooltip("Lista de lados seguros por linha (0 = Left, 1 = Right)")]
    public List<GlassSide> rows = new List<GlassSide>();

    public int GetSafeSide(int rowIndex)
    {
        if (rows == null || rows.Count == 0) return 0;
        int idx = Mathf.Clamp(rowIndex, 0, rows.Count - 1);
        return (int)rows[idx];
    }
}

