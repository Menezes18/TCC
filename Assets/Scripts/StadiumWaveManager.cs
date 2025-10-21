using UnityEngine;

/// <summary>
/// Gerenciador para criar efeito de "onda de estádio" em múltiplos objetos
/// Aplica automaticamente o componente VerticalBounce com offsets sequenciais
/// </summary>
public class StadiumWaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    [Tooltip("Altura do bounce")]
    [SerializeField] private float bounceHeight = 0.5f;
    
    [Tooltip("Velocidade do movimento")]
    [SerializeField] private float bounceSpeed = 2f;
    
    [Tooltip("Delay entre cada objeto para criar a onda")]
    [SerializeField] private float waveDelay = 0.2f;
    
    [Tooltip("Direção da onda")]
    [SerializeField] private WaveDirection direction = WaveDirection.LeftToRight;
    
    [Header("Auto Setup")]
    [Tooltip("Configurar automaticamente ao iniciar")]
    [SerializeField] private bool autoSetup = true;
    
    [Tooltip("Aplicar em filhos diretos apenas")]
    [SerializeField] private bool directChildrenOnly = true;

    public enum WaveDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop,
        CenterOut,
        OutsideIn
    }

    private void Start()
    {
        if (autoSetup)
        {
            SetupWave();
        }
    }

    [ContextMenu("Setup Stadium Wave")]
    public void SetupWave()
    {
        // Obter todos os objetos filhos
        Transform[] children = directChildrenOnly ? 
            GetDirectChildren() : 
            GetComponentsInChildren<Transform>();

        if (children.Length == 0)
        {
            Debug.LogWarning("Nenhum objeto filho encontrado para aplicar o efeito de onda!");
            return;
        }

        // Ordenar objetos baseado na direção
        Transform[] orderedChildren = OrderChildrenByDirection(children);

        // Aplicar VerticalBounce com offset incremental
        for (int i = 0; i < orderedChildren.Length; i++)
        {
            if (orderedChildren[i] == transform) continue; // Pular o próprio objeto

            VerticalBounce bounce = orderedChildren[i].GetComponent<VerticalBounce>();
            
            if (bounce == null)
            {
                bounce = orderedChildren[i].gameObject.AddComponent<VerticalBounce>();
            }

            // Configurar via reflection para acessar campos privados
            var type = typeof(VerticalBounce);
            
            type.GetField("bounceHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(bounce, bounceHeight);
            
            type.GetField("bounceSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(bounce, bounceSpeed);
            
            type.GetField("useWaveEffect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(bounce, true);

            // Calcular offset baseado na posição na sequência
            float offset = i * waveDelay;
            bounce.SetWaveOffset(offset);
        }

        Debug.Log($"Efeito de onda configurado em {orderedChildren.Length} objetos!");
    }

    private Transform[] GetDirectChildren()
    {
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }
        return children;
    }

    private Transform[] OrderChildrenByDirection(Transform[] children)
    {
        System.Array.Sort(children, (a, b) =>
        {
            if (a == transform) return -1;
            if (b == transform) return 1;

            switch (direction)
            {
                case WaveDirection.LeftToRight:
                    return a.position.x.CompareTo(b.position.x);
                
                case WaveDirection.RightToLeft:
                    return b.position.x.CompareTo(a.position.x);
                
                case WaveDirection.TopToBottom:
                    return b.position.y.CompareTo(a.position.y);
                
                case WaveDirection.BottomToTop:
                    return a.position.y.CompareTo(b.position.y);
                
                case WaveDirection.CenterOut:
                    Vector3 center = transform.position;
                    float distA = Vector3.Distance(a.position, center);
                    float distB = Vector3.Distance(b.position, center);
                    return distA.CompareTo(distB);
                
                case WaveDirection.OutsideIn:
                    Vector3 centerIn = transform.position;
                    float distAIn = Vector3.Distance(a.position, centerIn);
                    float distBIn = Vector3.Distance(b.position, centerIn);
                    return distBIn.CompareTo(distAIn);
                
                default:
                    return 0;
            }
        });

        return children;
    }

    [ContextMenu("Remove Wave Effect")]
    public void RemoveWaveEffect()
    {
        VerticalBounce[] bounces = GetComponentsInChildren<VerticalBounce>();
        foreach (var bounce in bounces)
        {
            DestroyImmediate(bounce);
        }
        Debug.Log("Efeito de onda removido!");
    }
}
