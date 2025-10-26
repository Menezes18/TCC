using System;
using System.Collections.Generic;
using UnityEngine;

public class KiteAmbientAnimator : MonoBehaviour
{
    [Header("Anchor/Área")]
    public Transform anchor;
    public float radius = 8f;
    public Vector2 heightRange = new Vector2(6f, 14f);
    public float smoothTime = 0.6f;

    [Header("Caminho")]
    public float pathSpeed = 0.45f;
    public float pathYaw = 0f; // rotação do desenho no plano XZ

    [Header("Vento")]
    public Vector3 baseWind = new Vector3(1f, 0.2f, 0.6f);
    public float windStrength = 1.6f;
    public float gustStrength = 2.2f;
    public float gustSpeed = 0.4f;

    [Header("Balanço")]
    public float rollAmplitude = 18f;   // inclinação lateral (Z)
    public float pitchAmplitude = 12f;  // bico para cima/baixo (X)
    public float swaySpeed = 1.8f;

    [Header("Cauda (opcional)")]
    public List<Transform> tailSegments = new List<Transform>();
    public float tailWaveAmplitude = 15f;
    public float tailWaveLength = 0.35f;
    public float tailWaveSpeed = 2.2f;
    public float tailTightness = 10f;

    [Header("Linha (opcional)")]
    public LineRenderer stringRenderer;
    public int lineSegments = 10;
    public float stringSag = 0.7f;

    private Vector3 dampVel;
    private float t;
    private Vector3 lastPos;
    private Vector3 anchorInitial;
    private float nXOff, nZOff, gustOff;
    private System.Random rng;

    [Header("Aleatorização")]
    [Tooltip("Aleatoriza valores ao iniciar para variar cada pipa.")]
    public bool randomizeOnStart = true;
    [Tooltip("Usa uma semente fixa para resultados reproduzíveis.")]
    public bool useCustomSeed = false;
    [Tooltip("Semente customizada quando 'useCustomSeed' estiver ativo.")]
    public int customSeed = 0;
    [Tooltip("Se verdadeiro, usa a posição inicial do anchor como referência. Evita drift quando o anchor é filho da pipa.")]
    public bool useAnchorInitialPosition = true;
    private bool anchorMovesWithKite;

    void Start()
    {
        lastPos = transform.position;
        anchorInitial = anchor ? anchor.position : transform.position; // referência fixa
        anchorMovesWithKite = anchor && (anchor == transform || anchor.IsChildOf(transform));

        if (randomizeOnStart)
        {
            Randomize();
        }
    }

    void Update()
    {
        t += Time.deltaTime * Mathf.Max(0.01f, pathSpeed);

        // Posição base do "ponto de ancoragem"
        // Se o anchor for a própria pipa (ou filho), fixa na posição inicial para evitar subir infinito.
        Vector3 aPos;
        if (anchor)
        {
            bool useFixed = useAnchorInitialPosition || anchorMovesWithKite;
            aPos = useFixed ? anchorInitial : anchor.position;
        }
        else
        {
            aPos = anchorInitial;
        }

        // Ruído leve para evitar repetição perfeita
        float nX = Mathf.PerlinNoise(t * 0.35f + nXOff, 12.3f) - 0.5f;
        float nZ = Mathf.PerlinNoise(34.7f, t * 0.35f + nZOff) - 0.5f;

        // Trajetória em forma de "8" com leve aleatoriedade
        Vector3 fig = new Vector3(
            Mathf.Sin(t) + nX * 0.6f,
            0f,
            0.5f * Mathf.Sin(t * 2f) + nZ * 0.6f
        ) * radius;

        if (Mathf.Abs(pathYaw) > 0.001f)
            fig = Quaternion.Euler(0f, pathYaw, 0f) * fig;

        // Altura oscilando numa faixa
        float yOff = Mathf.Lerp(heightRange.x, heightRange.y, 0.5f + 0.5f * Mathf.Sin(t * 0.6f));
        yOff += Mathf.Sin(t * 3.1f) * 0.5f;

        Vector3 targetPos = aPos + new Vector3(fig.x, yOff, fig.z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref dampVel, smoothTime);

        // Direção de movimento e vento
        Vector3 moveDir = (transform.position - lastPos);
        Vector3 gust = new Vector3(
            Mathf.PerlinNoise(0.1f + gustOff, t * gustSpeed),
            Mathf.PerlinNoise(0.2f + gustOff, t * gustSpeed) * 0.4f,
            Mathf.PerlinNoise(0.3f + gustOff, t * gustSpeed)
        );
        gust = (gust - Vector3.one * 0.5f) * 2f * gustStrength;

        Vector3 wind = baseWind.normalized * windStrength + gust * 0.5f;
        Vector3 dir = moveDir + wind * Time.deltaTime;
        if (dir.sqrMagnitude < 1e-6f) dir = transform.forward;

        // Inclinação e rotação natural da pipa
        Vector3 lookDir = new Vector3(dir.x, Mathf.Clamp(dir.y, -0.2f, 0.4f), dir.z);
        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        float roll = Mathf.Sin(t * swaySpeed * 2.2f) * rollAmplitude + (gust.x - gust.z) * 0.8f;
        float pitch = -Mathf.Cos(t * swaySpeed * 1.3f) * pitchAmplitude + -wind.y * 6f;

        Quaternion sway = Quaternion.Euler(pitch, 0f, roll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot * sway, Time.deltaTime * 2f);

        UpdateTail();
        UpdateLine(aPos);

        lastPos = transform.position;
    }

    [ContextMenu("Randomize Now")]
    public void Randomize(int? seedOverride = null)
    {
        int seed = seedOverride ?? (useCustomSeed ? customSeed : (gameObject.GetInstanceID() ^ (int)(DateTime.Now.Ticks & 0xFFFFFF)));
        rng = new System.Random(seed);

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        int RI(int a, int bInclusive) => rng.Next(a, bInclusive + 1);

        // Movimento e trajetória
        pathSpeed = R(0.3f, 0.85f);
        smoothTime = R(0.45f, 0.9f);
        radius = R(5f, 15f);
        float minH = R(4f, 9f);
        float maxH = minH + R(4f, 10f);
        heightRange = new Vector2(minH, maxH);
        pathYaw = R(-180f, 180f);

        // Vento
        float yaw = R(0f, 360f) * Mathf.Deg2Rad;
        float up = R(0.05f, 0.35f);
        baseWind = new Vector3(Mathf.Cos(yaw), up, Mathf.Sin(yaw));
        windStrength = R(1.0f, 2.4f);
        gustStrength = R(1.2f, 3.2f);
        gustSpeed = R(0.3f, 0.7f);

        // Balanço
        rollAmplitude = R(10f, 26f);
        pitchAmplitude = R(8f, 16f);
        swaySpeed = R(1.2f, 2.6f);

        // Cauda (se houver)
        if (tailSegments != null && tailSegments.Count > 0)
        {
            tailWaveAmplitude = R(10f, 22f);
            tailWaveLength = R(0.25f, 0.55f);
            tailWaveSpeed = R(1.6f, 3.0f);
            tailTightness = R(6f, 14f);
        }

        // Linha (se houver)
        if (stringRenderer != null)
        {
            lineSegments = RI(8, 14);
            stringSag = R(0.5f, 1.1f);
        }

        // Offsets e fase
        nXOff = R(0f, 1000f);
        nZOff = R(0f, 1000f);
        gustOff = R(0f, 1000f);
        t = R(0f, 100f);
    }

    void UpdateTail()
    {
        if (tailSegments == null || tailSegments.Count == 0) return;

        float time = t * tailWaveSpeed;
        int count = tailSegments.Count;
        for (int i = 0; i < count; i++)
        {
            var seg = tailSegments[i];
            if (!seg) continue;

            float offs = i * tailWaveLength;
            float wave = Mathf.Sin(time - offs) * tailWaveAmplitude;
            Quaternion q = Quaternion.AngleAxis(wave, transform.up);
            seg.localRotation = Quaternion.Slerp(
                seg.localRotation,
                q,
                Time.deltaTime * (tailTightness * (1f - (i / Mathf.Max(1f, (float)count))) + 1f)
            );
        }
    }

    void UpdateLine(Vector3 anchorPos)
    {
        if (!stringRenderer || !anchor) return;

        int count = Mathf.Max(2, lineSegments);
        stringRenderer.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            float u = i / (float)(count - 1);
            Vector3 p = Vector3.Lerp(anchorPos, transform.position, u);
            float sag = Mathf.Sin(u * Mathf.PI) * stringSag * radius * 0.2f;
            p += Vector3.down * sag;
            stringRenderer.SetPosition(i, p);
        }
    }
}
