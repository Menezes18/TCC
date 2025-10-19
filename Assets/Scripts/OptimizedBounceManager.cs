using UnityEngine;
using System.Collections.Generic;


public class OptimizedBounceManager : MonoBehaviour
{
    [Header("Performance")]
    [Tooltip("Objetos que terão o efeito de bounce")]
    [SerializeField] private List<Transform> bouncingObjects = new List<Transform>();
    
    [Header("Movement Settings")]
    [Tooltip("Altura máxima do movimento (em unidades)")]
    [SerializeField] private float bounceHeight = 0.5f;
    
    [Tooltip("Velocidade do movimento")]
    [SerializeField] private float bounceSpeed = 2f;
    
    [Header("Wave Settings (Efeito Estádio)")]
    [Tooltip("Ativar efeito de onda sequencial")]
    [SerializeField] private bool useWaveEffect = true;
    
    [Tooltip("Delay entre cada objeto na onda")]
    [SerializeField] private float waveDelay = 0.1f;
    
    [Tooltip("Direção da onda")]
    [SerializeField] private WaveDirection waveDirection = WaveDirection.Sequential;
    
    [Header("Randomization")]
    [Tooltip("Adicionar variação aleatória na velocidade")]
    [SerializeField] private bool randomizeSpeed = false;
    
    [SerializeField] private Vector2 speedVariation = new Vector2(0.8f, 1.2f);
    
    [Header("Auto Setup")]
    [Tooltip("Preencher lista automaticamente com filhos")]
    [SerializeField] private bool autoFindChildren = true;

    public enum WaveDirection
    {
        Sequential,
        Simultaneous,
        LeftToRight,
        Random
    }

    private Vector3[] startPositions;
    private float[] timeOffsets;
    private float[] speeds;
    private Transform[] objectsArray;
    private int objectCount;

    private void Awake()
    {
        InitializeSystem();
    }

    private void Start()
    {
        if (autoFindChildren && (bouncingObjects == null || bouncingObjects.Count == 0))
        {
            AutoPopulateObjects();
        }
        
        CacheObjectData();
    }


    private void InitializeSystem()
    {
        if (bouncingObjects == null)
        {
            bouncingObjects = new List<Transform>();
        }
    }


    [ContextMenu("Auto Find Children")]
    public void AutoPopulateObjects()
    {
        bouncingObjects.Clear();
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            bouncingObjects.Add(child);
        }
        
        Debug.Log($"[OptimizedBounceManager] Encontrados {bouncingObjects.Count} objetos filhos");
        
        if (Application.isPlaying)
        {
            CacheObjectData();
        }
    }


    private void CacheObjectData()
    {
        objectCount = bouncingObjects.Count;
        
        if (objectCount == 0)
        {
            Debug.LogWarning("[OptimizedBounceManager] Nenhum objeto na lista!");
            enabled = false;
            return;
        }

        objectsArray = new Transform[objectCount];
        startPositions = new Vector3[objectCount];
        timeOffsets = new float[objectCount];
        speeds = new float[objectCount];

        Transform[] orderedObjects = OrderObjects();

        for (int i = 0; i < objectCount; i++)
        {
            objectsArray[i] = orderedObjects[i];
            startPositions[i] = orderedObjects[i].localPosition;
            
            if (useWaveEffect && waveDirection != WaveDirection.Simultaneous)
            {
                timeOffsets[i] = i * waveDelay;
            }
            else if (waveDirection == WaveDirection.Random)
            {
                timeOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            else
            {
                timeOffsets[i] = 0f;
            }
            
            speeds[i] = randomizeSpeed ? 
                bounceSpeed * Random.Range(speedVariation.x, speedVariation.y) : 
                bounceSpeed;
        }

        Debug.Log($"[OptimizedBounceManager] Sistema inicializado com {objectCount} objetos");
    }


    private Transform[] OrderObjects()
    {
        Transform[] ordered = bouncingObjects.ToArray();

        if (waveDirection == WaveDirection.LeftToRight)
        {
            System.Array.Sort(ordered, (a, b) => a.position.x.CompareTo(b.position.x));
        }
        else if (waveDirection == WaveDirection.Random)
        {
            for (int i = 0; i < ordered.Length; i++)
            {
                int randomIndex = Random.Range(i, ordered.Length);
                Transform temp = ordered[i];
                ordered[i] = ordered[randomIndex];
                ordered[randomIndex] = temp;
            }
        }

        return ordered;
    }

    private void Update()
    {
        if (objectsArray == null || objectCount == 0) return;

        float time = Time.time;

        for (int i = 0; i < objectCount; i++)
        {
            if (objectsArray[i] == null) continue;

            float yOffset = Mathf.Sin((time * speeds[i]) + timeOffsets[i]) * bounceHeight;
            
            Vector3 newPos = startPositions[i];
            newPos.y += yOffset;
            objectsArray[i].localPosition = newPos;
        }
    }


    public void AddObject(Transform obj)
    {
        if (!bouncingObjects.Contains(obj))
        {
            bouncingObjects.Add(obj);
            
            if (Application.isPlaying)
            {
                CacheObjectData();
            }
        }
    }


    public void RemoveObject(Transform obj)
    {
        if (bouncingObjects.Remove(obj))
        {
            if (Application.isPlaying)
            {
                CacheObjectData();
            }
        }
    }


    public void PauseAll()
    {
        enabled = false;
    }


    public void ResumeAll()
    {
        enabled = true;
    }


    [ContextMenu("Reset All Positions")]
    public void ResetAllPositions()
    {
        if (startPositions == null || objectsArray == null) return;

        for (int i = 0; i < objectCount; i++)
        {
            if (objectsArray[i] != null)
            {
                objectsArray[i].localPosition = startPositions[i];
            }
        }
    }


    public void SetBounceHeight(float height)
    {
        bounceHeight = height;
    }


    public void SetBounceSpeed(float speed)
    {
        bounceSpeed = speed;
        

        if (speeds != null)
        {
            for (int i = 0; i < speeds.Length; i++)
            {
                speeds[i] = randomizeSpeed ? 
                    speed * Random.Range(speedVariation.x, speedVariation.y) : 
                    speed;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {

        if (bouncingObjects != null)
        {
            bouncingObjects.RemoveAll(obj => obj == null);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (bouncingObjects == null || bouncingObjects.Count == 0) return;

        Gizmos.color = Color.cyan;
        
        foreach (var obj in bouncingObjects)
        {
            if (obj == null) continue;

            Vector3 pos = obj.position;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos + Vector3.up * bounceHeight, 0.05f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos - Vector3.up * bounceHeight, 0.05f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos + Vector3.up * bounceHeight, pos - Vector3.up * bounceHeight);
        }
    }
#endif
}
