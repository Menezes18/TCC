using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    // Configurações de Escala
    [Header("Configurações de Escala")]
    [Tooltip("Escala final do botão quando estiver 'hovering'")]
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
    [Tooltip("Escala do botão no momento do clique (durante o 'Pointer Down')")]
    public Vector3 clickScale = new Vector3(1.2f, 1.2f, 1.2f);
    [Tooltip("Duração da animação de escala (em segundos)")]
    public float scaleDuration = 0.1f;

    // Configurações de Wiggle
    [Header("Configurações de Wiggle")]
    [Tooltip("Amplitude máxima do wiggle (em euler angles)")]
    public float wiggleAmplitude = 2f;
    [Tooltip("Velocidade do wiggle (em ciclos por segundo)")]
    public float wiggleSpeed = 10f;

    private Vector3 defaultScale;
    private Quaternion defaultRotation;
    private Coroutine scaleCoroutine;
    private Coroutine wiggleCoroutine;
    private bool isHovering = false;

    // AWAKE é chamado mesmo se o objeto começar desativado, garantindo o defaultScale correto.
    void Awake()
    {
        // Garante que o objeto tem um RectTransform
        if (GetComponent<RectTransform>() == null)
        {
            Debug.LogError("O script ButtonHoverAnimator requer um RectTransform no GameObject.");
            enabled = false;
            return;
        }

        // Guarda a escala e rotação iniciais
        defaultScale = transform.localScale;
        defaultRotation = transform.localRotation;
    }

    // ON DISABLE garante que o botão resete o estado ao ser desativado/oculto
    private void OnDisable()
    {
        // Força o reset para o estado normal (escala e rotação)
        ResetToDefaultState();
    }

    // Método para ser chamado quando precisar de reset imediato
    private void ResetToDefaultState()
    {
        // 1. Para as Coroutines em andamento
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }

        // 2. Reseta o estado de 'hovering'
        isHovering = false;

        // 3. Define a escala e rotação para os valores iniciais imediatamente
        transform.localScale = defaultScale;
        transform.localRotation = defaultRotation;
    }

    // Chamado quando o mouse entra no botão
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleToTarget(hoverScale, scaleDuration));

        isHovering = true;
        if (wiggleCoroutine == null)
        {
            wiggleCoroutine = StartCoroutine(WiggleAnimation());
        }
    }

    // Chamado quando o mouse sai do botão
    public void OnPointerExit(PointerEventData eventData)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleToTarget(defaultScale, scaleDuration));

        isHovering = false;
    }

    // Chamado quando o botão do mouse é pressionado (clicado)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        // Escala para o estado de clique, mais rápido para feedback instantâneo
        scaleCoroutine = StartCoroutine(ScaleToTarget(clickScale, scaleDuration * 0.5f));
    }


    // Coroutine para a animação suave de escala
    IEnumerator ScaleToTarget(Vector3 targetScale, float duration)
    {
        Vector3 initialScale = transform.localScale;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            // Lerp para interpolação suave
            transform.localScale = Vector3.Lerp(initialScale, targetScale, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;

        // Se a escala alvo for a default, garante que a rotação também esteja normal
        if (targetScale == defaultScale)
        {
            transform.localRotation = defaultRotation;
        }
    }

    // Coroutine para o efeito de "wiggle"
    IEnumerator WiggleAnimation()
    {
        float time = 0f;
        while (isHovering)
        {
            time += Time.deltaTime * wiggleSpeed;
            // Usa Mathf.Sin para criar um movimento oscilatório suave
            float angle = Mathf.Sin(time) * wiggleAmplitude;

            // Aplica a rotação no eixo Z
            transform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        // Garante que o botão volte para a rotação normal quando o hover terminar
        transform.localRotation = defaultRotation;
        wiggleCoroutine = null;
    }
}