using UnityEngine;


public class VerticalBounce : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Altura máxima do movimento (em unidades)")]
    [SerializeField] private float bounceHeight = 0.5f;
    
    [Tooltip("Velocidade do movimento")]
    [SerializeField] private float bounceSpeed = 2f;
    
    [Header("Wave Settings (Efeito Estádio)")]
    [Tooltip("Ativar efeito de onda (como em estádio)")]
    [SerializeField] private bool useWaveEffect = false;
    
    [Tooltip("Delay/offset para criar efeito de onda (usado com múltiplos objetos)")]
    [SerializeField] private float waveOffset = 0f;
    
    [Header("Random Settings")]
    [Tooltip("Usar velocidade aleatória")]
    [SerializeField] private bool randomSpeed = false;
    
    [Tooltip("Variação da velocidade (min/max)")]
    [SerializeField] private Vector2 speedRange = new Vector2(1f, 3f);
    
    [Tooltip("Usar altura aleatória")]
    [SerializeField] private bool randomHeight = false;
    
    [Tooltip("Variação da altura (min/max)")]
    [SerializeField] private Vector2 heightRange = new Vector2(0.3f, 0.7f);


    private Vector3 startPosition;
    private float currentSpeed;
    private float currentHeight;
    private float timeOffset;

    private void Awake()
    {
        startPosition = transform.localPosition;
        
        currentSpeed = randomSpeed ? Random.Range(speedRange.x, speedRange.y) : bounceSpeed;
        currentHeight = randomHeight ? Random.Range(heightRange.x, heightRange.y) : bounceHeight;
        
        timeOffset = useWaveEffect ? waveOffset : Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float yOffset = Mathf.Sin((Time.time * currentSpeed) + timeOffset) * currentHeight;
        
        transform.localPosition = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);
    }

    public void SetWaveOffset(float offset)
    {
        waveOffset = offset;
        timeOffset = offset;
    }


    public void ResetPosition()
    {
        transform.localPosition = startPosition;
    }


    public void Pause()
    {
        enabled = false;
    }


    public void Resume()
    {
        enabled = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.localPosition;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.parent != null ? 
            transform.parent.TransformPoint(pos + Vector3.up * bounceHeight) : 
            pos + Vector3.up * bounceHeight, 0.1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.parent != null ? 
            transform.parent.TransformPoint(pos - Vector3.up * bounceHeight) : 
            pos - Vector3.up * bounceHeight, 0.1f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            transform.parent != null ? transform.parent.TransformPoint(pos + Vector3.up * bounceHeight) : pos + Vector3.up * bounceHeight,
            transform.parent != null ? transform.parent.TransformPoint(pos - Vector3.up * bounceHeight) : pos - Vector3.up * bounceHeight
        );
    }
#endif
}
