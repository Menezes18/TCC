using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class ResultsRow : MonoBehaviour
{
    [Header("Referências da linha")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text gainText;
    [SerializeField] TMP_Text totalText;
    [SerializeField] TMP_Text positionText;
    [SerializeField] Image colorBadge;

    [Header("Formatação")]
    [SerializeField] bool showPlusOnGain = true;

    [Header("Animações de Texto")]
    [SerializeField] float popScale = 1.12f;
    [SerializeField] float popUpDuration = 0.10f;
    [SerializeField] float popDownDuration = 0.08f;
    [SerializeField] float fadeOutDuration = 0.18f;
    [SerializeField] float positionPopScale = 1.08f;
    [SerializeField] float positionPopUpDuration = 0.09f;
    [SerializeField] float positionPopDownDuration = 0.08f;
    [SerializeField] float countEasePower = 2f; 
    [SerializeField] CanvasGroup _gainCG;

    int _targetGain;
    int _finalTotal;

    public void SetData(string playerName, int gain, int total, int position, Color color)
    {
        if (nameText) nameText.text = playerName;
        if (gainText) gainText.text = showPlusOnGain ? $"+{gain}" : gain.ToString();
        if (totalText) totalText.text = total.ToString();
        if (positionText) positionText.text = position.ToString();
        if (colorBadge) colorBadge.color = color;
    }

    public void SetupForAnimation(string playerName, int gain, int finalTotal, int position, Color color)
    {
        _targetGain = gain;
        _finalTotal = finalTotal;

        if (nameText) nameText.text = playerName;
        if (positionText) positionText.text = position.ToString();
        if (colorBadge) colorBadge.color = color;

        if (gainText)
        {
            gainText.text = FormatGain(0);
            // _gainCG = EnsureCanvasGroup(gainText);
            // Se não há ganho, já inicia invisível
            Debug.LogError("IGNORE");
            _gainCG.alpha = (_targetGain == 0) ? 0f : 1f;
        }
        if (totalText)
            totalText.text = (_finalTotal - _targetGain).ToString();;
        
        if (positionText) positionText.alpha = 1f;
    }

    public IEnumerator PlayNumberSequence(float gainDuration, float waitAfterGain, float totalDuration)
    {
        // Pop simples no positionText
        if (positionText)
            yield return PopTMP(positionText, positionPopScale, positionPopUpDuration, positionPopDownDuration);

        // Anima ganho: 0 -> _targetGain
        if (gainText)
        {
            if (_targetGain != 0)
            {
                // Garante visível para contar
                if (_gainCG == null) _gainCG = EnsureCanvasGroup(gainText);
                _gainCG.alpha = 1f;
                yield return AnimateInt(0, _targetGain, gainDuration, v => gainText.text = FormatGain(v));
                // Pop e fade-out do ganho
                yield return PopTMP(gainText, popScale, popUpDuration, popDownDuration);
                yield return FadeOutCanvasGroup(_gainCG, fadeOutDuration);
            }
            else
            {
                // Sem ganho: mantém invisível
                if (_gainCG == null) _gainCG = EnsureCanvasGroup(gainText);
                _gainCG.alpha = 0f;
            }
        }

        if (waitAfterGain > 0f)
            yield return new WaitForSeconds(waitAfterGain);

        // Anima total: (final - ganho) -> final
        if (totalText)
        {
            int start = _finalTotal - _targetGain;
            int end = _finalTotal;
            yield return AnimateInt(start, end, totalDuration, v => totalText.text = v.ToString());
            // Pop do total (mantém visível, não faz fade-out)
            yield return PopTMP(totalText, popScale, popUpDuration, popDownDuration);
        }
    }

    private IEnumerator AnimateInt(int from, int to, float duration, System.Action<int> onStep)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            // Ease-in com potência para parecer que "multiplica" no final
            if (countEasePower > 0.999f)
                k = Mathf.Pow(k, countEasePower);
            int val = Mathf.RoundToInt(Mathf.Lerp(from, to, k));
            onStep?.Invoke(val);
            yield return null;
        }
        onStep?.Invoke(to);
    }

    private string FormatGain(int v)
    {
        if (!showPlusOnGain) return v.ToString();
        return v >= 0 ? $"+{v}" : v.ToString();
    }

    private IEnumerator PopTMP(TMP_Text text, float scale, float upDur, float downDur)
    {
        var rt = text ? text.transform as RectTransform : null;
        if (rt == null)
        {
            yield break;
        }
        var baseScale = rt.localScale;
        LeanTween.scale(rt, baseScale * Mathf.Max(1f, scale), Mathf.Max(0f, upDur)).setEaseOutBack();
        if (upDur > 0f) yield return new WaitForSeconds(upDur);
        LeanTween.scale(rt, baseScale, Mathf.Max(0f, downDur)).setEaseInQuad();
        if (downDur > 0f) yield return new WaitForSeconds(downDur);
    }

    private CanvasGroup EnsureCanvasGroup(Component c)
    {
        if (c == null) return null;
        var cg = c.GetComponent<CanvasGroup>();
        return cg;
    }

    private IEnumerator FadeOutCanvasGroup(CanvasGroup cg, float duration)
    {
        if (cg == null) yield break;
        float from = cg.alpha;
        LeanTween.value(cg.gameObject, from, 0f, Mathf.Max(0f, duration)).setOnUpdate((float a) => cg.alpha = a).setEaseInQuad();
        if (duration > 0f) yield return new WaitForSeconds(duration);
        cg.alpha = 0f;
    }


    
}
