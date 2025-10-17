using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Adiciona as interfaces para hover e click
public class ButtonHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    // Configurações de Escala
    [Header("Configurações de Escala")]
    [Tooltip("Escala final do botão quando estiver 'hovering'")]
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
    [Tooltip("Escala do botão no momento do clique (durante o 'Pointer Down')")]
    public Vector3 clickScale = new Vector3(1.2f, 1.2f, 1.2f); // Novo: Escala um pouco maior que a hover
    [Tooltip("Duração da animação de escala (em segundos)")]
    public float scaleDuration = 0.1f; // Reduzi a duração para um clique mais responsivo

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

    void Start()
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

    // Chamado quando o mouse entra no botão
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleToTarget(hoverScale, scaleDuration));

        isHovering = true;
        // Inicia o Wiggle se ainda não estiver rodando (garantia)
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
        // Interrompe a coroutine de wiggle. O WiggleAnimation também se encerra por causa do 'isHovering'.
        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }

        // Garante que a rotação e escala voltem ao normal imediatamente, se o Lerp não for executado por completo.
        transform.localRotation = defaultRotation;
    }

    // NOVO: Chamado quando o botão do mouse é pressionado (clicado)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        // Aplica a animação de escala para o clique (mais rápido, mais feedback)
        scaleCoroutine = StartCoroutine(ScaleToTarget(clickScale, scaleDuration * 0.5f));

        // NOTA: Para retornar do "clickScale", você precisaria de IPointerUpHandler.
        // Se você não o usar, a OnPointerExit (se o mouse sair) ou OnPointerEnter (se o clique for rápido
        // e o mouse permanecer) se encarregarão de restaurar a escala para 'hoverScale' ou 'defaultScale'.
        // Geralmente, o próprio sistema de UI do Unity se encarrega de disparar a OnPointerEnter/Exit
        // ou o OnPointerClick (que dispara Up) para gerenciar o estado.

        // Se você quiser que ele *volte* para a hoverScale após o clique, mas o mouse ainda está em cima,
        // você precisaria de IPointerUpHandler para ScaleToTarget(hoverScale).
        // Por enquanto, vamos manter simples: OnPointerExit/Enter gerenciam a transição.
    }


    // Coroutine para a animação suave de escala
    IEnumerator ScaleToTarget(Vector3 targetScale, float duration)
    {
        Vector3 initialScale = transform.localScale;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            transform.localScale = Vector3.Lerp(initialScale, targetScale, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;

        // Se retornar à escala default, garante que a rotação também está normal
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
            // Usa MathF.Sin para criar um movimento oscilatório suave
            float angle = Mathf.Sin(time) * wiggleAmplitude;

            // Aplica a rotação (wiggle) no eixo Z (ou o que for mais apropriado para a sua UI)
            transform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        // Garante que o botão volte para a rotação normal quando o hover terminar
        transform.localRotation = defaultRotation;
        wiggleCoroutine = null;
    }
}