using System.Collections.Generic;
using UnityEngine;

namespace LevelEditor
{
    [CreateAssetMenu(fileName = "HexagonLevel", menuName = "Level Editor/Hexagon Level Data")]
    public class HexagonLevelData : ScriptableObject
    {
        [System.Serializable]
        public class HexagonData
        {
            public Vector2Int gridPosition;
            public Vector3 worldPosition;
            public Quaternion rotation;
            public Vector3 scale = Vector3.one;

            public HexagonData(Vector2Int gridPos, Vector3 worldPos)
            {
                gridPosition = gridPos;
                worldPosition = worldPos;
                // Rotação é definida no editor, não aqui (para manter consistência)
                rotation = Quaternion.Euler(-90f, 0f, 0f);
                scale = Vector3.one; // Escala base, será multiplicada pelo hexSize no editor
            }
        }

        [Header("Level Settings")]
        public string levelName = "New Level";
        public GameObject hexagonPrefab;
        public float hexSize = 1f;
        public int circleRadius = 5;

        [Header("Level Data")]
        public List<HexagonData> hexagons = new List<HexagonData>();

        public void AddHexagon(Vector2Int gridPos, Vector3 worldPos)
        {
            // Verifica se já existe um hexágono nessa posição
            if (!HasHexagonAt(gridPos))
            {
                hexagons.Add(new HexagonData(gridPos, worldPos));
            }
        }

        public void RemoveHexagon(Vector2Int gridPos)
        {
            hexagons.RemoveAll(h => h.gridPosition == gridPos);
        }

        public bool HasHexagonAt(Vector2Int gridPos)
        {
            return hexagons.Exists(h => h.gridPosition == gridPos);
        }

        public void Clear()
        {
            hexagons.Clear();
        }

        public HexagonData GetHexagonAt(Vector2Int gridPos)
        {
            return hexagons.Find(h => h.gridPosition == gridPos);
        }
    }
}
