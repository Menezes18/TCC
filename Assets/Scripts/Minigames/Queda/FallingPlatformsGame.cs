using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingPlatformsGame : MonoBehaviour
{
    [Header("Platform Settings")]
    public List<GameObject> platforms = new List<GameObject>();
    public string platformTag = "FallingPlatform";
    public float timeBetweenPlatformFalls = 2f;
    public float fallDelay = 1f;
    public float platformRespawnTime = 5f;
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.5f;

    [Header("Falling Objects Settings")]
    public GameObject[] fallingObjectPrefabs;
    public Transform[] spawnPoints;
    public float minObjectSpawnTime = 3f;
    public float maxObjectSpawnTime = 8f;
    public float objectFallSpeed = 5f;

    [Header("Game Duration")]
    public float gameDuration = 60f;

    private List<GameObject> activePlatforms = new List<GameObject>();
    private List<Coroutine> fallingRoutines = new List<Coroutine>();
    private bool gameActive = false;

    void Start()
    {
        FindAllPlatforms();
        StartCoroutine(GameLoop());
    }

    void FindAllPlatforms()
    {
        platforms.Clear();
        GameObject[] platformObjects = GameObject.FindGameObjectsWithTag(platformTag);
        platforms.AddRange(platformObjects);
        activePlatforms.AddRange(platformObjects);
    }

    IEnumerator GameLoop()
    {
        gameActive = true;
        
        // Inicia a queda aleatória de plataformas
        Coroutine platformFallRoutine = StartCoroutine(RandomPlatformFalls());
        fallingRoutines.Add(platformFallRoutine);
        
        // Inicia a queda de objetos
        Coroutine objectFallRoutine = StartCoroutine(SpawnFallingObjects());
        fallingRoutines.Add(objectFallRoutine);
        
        // Tempo de duração do jogo
        yield return new WaitForSeconds(gameDuration);
        
        // Finaliza o jogo
        gameActive = false;
        foreach (Coroutine routine in fallingRoutines)
        {
            if (routine != null) StopCoroutine(routine);
        }
    }

    IEnumerator RandomPlatformFalls()
    {
        while (gameActive)
        {
            yield return new WaitForSeconds(timeBetweenPlatformFalls);
            
            if (activePlatforms.Count > 0)
            {
                int randomIndex = Random.Range(0, activePlatforms.Count);
                GameObject platformToFall = activePlatforms[randomIndex];
                
                StartCoroutine(FallPlatform(platformToFall));
            }
        }
    }

    IEnumerator FallPlatform(GameObject platform)
    {
        // Remove da lista de plataformas ativas
        activePlatforms.Remove(platform);
        
        // Efeito de tremer
        Vector3 originalPosition = platform.transform.position;
        float shakeTimer = 0f;
        
        while (shakeTimer < shakeDuration)
        {
            platform.transform.position = originalPosition + Random.insideUnitSphere * shakeIntensity;
            shakeTimer += Time.deltaTime;
            yield return null;
        }
        
        platform.transform.position = originalPosition;
        
        // Espera o delay antes de cair
        yield return new WaitForSeconds(fallDelay);
        
        // Adiciona Rigidbody para fazer a plataforma cair
        Rigidbody rb = platform.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = platform.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        
        // Reseta a plataforma após um tempo
        yield return new WaitForSeconds(platformRespawnTime);
        
        if (platform != null)
        {
            ResetPlatform(platform);
        }
    }

    void ResetPlatform(GameObject platform)
    {
        Rigidbody rb = platform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        platform.transform.rotation = Quaternion.identity;
        activePlatforms.Add(platform);
    }

    IEnumerator SpawnFallingObjects()
    {
        while (gameActive)
        {
            float waitTime = Random.Range(minObjectSpawnTime, maxObjectSpawnTime);
            yield return new WaitForSeconds(waitTime);
            
            if (spawnPoints.Length > 0 && fallingObjectPrefabs.Length > 0)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                GameObject fallingObjectPrefab = fallingObjectPrefabs[Random.Range(0, fallingObjectPrefabs.Length)];
                
                GameObject fallingObject = Instantiate(fallingObjectPrefab, spawnPoint.position, Quaternion.identity);
                Rigidbody rb = fallingObject.GetComponent<Rigidbody>();
                
                if (rb != null)
                {
                    rb.velocity = Vector3.down * objectFallSpeed;
                }
                
                // Destroi o objeto após algum tempo
                Destroy(fallingObject, 10f);
            }
        }
    }

    // Método para ser chamado quando um objeto atinge uma plataforma
    public void PlatformHit(GameObject platform)
    {
        if (activePlatforms.Contains(platform))
        {
            StartCoroutine(FallPlatform(platform));
        }
    }
}
