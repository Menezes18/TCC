using UnityEngine;

namespace LevelEditor
{
    /// <summary>
    /// Representa um tile hexagonal no level
    /// </summary>
    public class HexagonTile : MonoBehaviour
    {
        [Header("Hexagon Settings")]
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color selectedColor = Color.green;

        private Material material;

        public Vector2Int GridPosition
        {
            get => gridPosition;
            set => gridPosition = value;
        }

        private void Awake()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (meshRenderer != null && meshRenderer.material != null)
            {
                material = meshRenderer.material;
            }
        }

        public void SetColor(Color color)
        {
            if (material != null)
            {
                material.color = color;
            }
        }

        public void SetNormalState()
        {
            SetColor(normalColor);
        }

        public void SetHighlightState()
        {
            SetColor(highlightColor);
        }

        public void SetSelectedState()
        {
            SetColor(selectedColor);
        }

        // Calcula a posição world do hexágono baseado na posição de grid
        // Hexágonos pointy-topped (deitados olhando pra cima) no plano XZ
        public static Vector3 HexToWorldPosition(Vector2Int hexCoord, float hexSize)
        {
            // Pointy-topped hexagon layout - hexágonos grudados lado a lado
            float x = hexSize * Mathf.Sqrt(3) * (hexCoord.x + hexCoord.y / 2f);
            float z = hexSize * 3f / 2f * hexCoord.y;
            return new Vector3(x, 0, z);
        }
        
        // Retorna a rotação padrão para hexágonos deitados (olhando pra cima)
        public static Quaternion GetHexagonRotation()
        {
            // Rotação -90° no X para deitar no chão olhando pra cima
            return Quaternion.Euler(-90f, 0f, 0f);
        }

        // Calcula a coordenada de grid mais próxima de uma posição world
        // Ajustado para hexágonos pointy-topped no plano XZ
        public static Vector2Int WorldToHexPosition(Vector3 worldPos, float hexSize)
        {
            float q = (Mathf.Sqrt(3) / 3f * worldPos.x - 1f / 3f * worldPos.z) / hexSize;
            float r = (2f / 3f * worldPos.z) / hexSize;

            return HexRound(q, r);
        }

        private static Vector2Int HexRound(float q, float r)
        {
            float s = -q - r;

            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float q_diff = Mathf.Abs(rq - q);
            float r_diff = Mathf.Abs(rr - r);
            float s_diff = Mathf.Abs(rs - s);

            if (q_diff > r_diff && q_diff > s_diff)
                rq = -rr - rs;
            else if (r_diff > s_diff)
                rr = -rq - rs;

            return new Vector2Int(rq, rr);
        }

        // Retorna os 6 vizinhos de um hexágono
        public static Vector2Int[] GetNeighbors(Vector2Int hexCoord)
        {
            return new Vector2Int[]
            {
                new Vector2Int(hexCoord.x + 1, hexCoord.y),
                new Vector2Int(hexCoord.x + 1, hexCoord.y - 1),
                new Vector2Int(hexCoord.x, hexCoord.y - 1),
                new Vector2Int(hexCoord.x - 1, hexCoord.y),
                new Vector2Int(hexCoord.x - 1, hexCoord.y + 1),
                new Vector2Int(hexCoord.x, hexCoord.y + 1)
            };
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}
