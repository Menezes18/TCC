using System.Collections;
using UnityEngine;

public class Credits : MonoBehaviour
{
    [Header("Velocidade de Scroll")]
    public float speed = 100f;

    [Header("Posição Y inicial (off-screen)")]
    public float startY = -825f;

    [Header("Posição Y final (on-screen)")]
    public float endY = 825f;

    [Header("Loopar automaticamente?")]
    public bool isLooping = false;

    private RectTransform creditsRect;

    void OnEnable()
    {
        creditsRect = GetComponent<RectTransform>();
        Vector2 pos = creditsRect.anchoredPosition;
        creditsRect.anchoredPosition = new Vector2(pos.x, startY);
        StartCoroutine(AutoScroll());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator AutoScroll()
    {
        while (true)
        {
            // Move no eixo Y
            creditsRect.anchoredPosition += Vector2.up * speed * Time.deltaTime;

            // Verifica se passou do fim
            if (creditsRect.anchoredPosition.y >= endY)
            {
                if (isLooping)
                {
                    // Reinicia apenas o Y
                    Vector2 pos = creditsRect.anchoredPosition;
                    creditsRect.anchoredPosition = new Vector2(pos.x, startY);
                }
                else
                {
                    yield break;
                }
            }

            yield return null;
        }
    }
}