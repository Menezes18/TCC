using Mirror;
using UnityEngine;

public class FallGuysDoorRow : NetworkBehaviour
{
    [Tooltip("Se verdadeiro, escolhe exatamente 1 porta verdadeira nesta fileira.")]
    [SerializeField] private bool pickSingleTrue = true;
    [Tooltip("Quantidade de portas verdadeiras se não for apenas uma (0 = todas falsas)")]
    [SerializeField, Min(0)] private int numberOfTrueDoors = 1;

    private FallGuysDoor[] _doors;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _doors = GetComponentsInChildren<FallGuysDoor>(includeInactive: true);
        if (_doors == null || _doors.Length == 0) return;

        foreach (var d in _doors) d.ServerSetReal(false);

        if (pickSingleTrue)
        {
            int idx = Random.Range(0, _doors.Length);
            _doors[idx].ServerSetReal(true);
        }
        else
        {
            int count = Mathf.Clamp(numberOfTrueDoors, 0, _doors.Length);
            // simples: escolhe aleatoriamente 'count' distintos
            var indices = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _doors.Length; i++) indices.Add(i);
            for (int i = 0; i < count; i++)
            {
                if (indices.Count == 0) break;
                int r = Random.Range(0, indices.Count);
                int selected = indices[r];
                indices.RemoveAt(r);
                _doors[selected].ServerSetReal(true);
            }
        }
    }
}

