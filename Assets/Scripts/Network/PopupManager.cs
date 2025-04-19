using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
public class PopupManager : MonoBehaviour
{
    public static PopupManager instance;

    [Header("UI References")]
    public GameObject popUp; // painel geral
    public CanvasGroup canvasGroup; // para fade
    public RectTransform popupRect; // para scale/ shake
    public TMP_Text titleText;
    public GameObject celular;
    public CanvasGroup celularCanvasGroup;

    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float scaleDuration = 0.3f;
    public float shakeStrength = 20f;
    public int vibrato = 10;

    private void Awake()
    {
        instance = this;
        canvasGroup.alpha = 0;
        popUp.SetActive(false);
    }

    public void Popup_Show(string title, bool shake = false, bool shakeloop = false)
    {
        celularCanvasGroup.alpha = 0;
        Sequence seq = DOTween.Sequence();
        seq.Append(celular.transform.DOLocalMoveY(-865, 0.2f).SetEase(Ease.OutCubic));
        seq.Append(celular.transform.DOLocalMoveY(-865, 0.25f).SetEase(Ease.InOutCubic));

        
        titleText.text = title;
        popUp.SetActive(true);

        canvasGroup.alpha = 0;
        popupRect.localScale = Vector3.zero;

        canvasGroup.DOFade(1, fadeDuration);
        popupRect.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack);
        if (shakeloop)
            Popup_ShowShakeLoop(title);
        else if (shake)
            popupRect.DOShakeAnchorPos(0.4f, shakeStrength, vibrato, 90, false, true);
    }
    Tween shakeTween;

    public void Popup_ShowShakeLoop(string title)
    {
        titleText.text = title;
        popUp.SetActive(true);

        canvasGroup.alpha = 0;
        popupRect.localScale = Vector3.zero;

        canvasGroup.DOFade(1, fadeDuration);
        popupRect.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack);

        
        shakeTween?.Kill();
        shakeTween = popupRect.DOShakeAnchorPos(
            2f, 
            shakeStrength,
            vibrato,
            90,
            true,
            true
        ).SetLoops(-1, LoopType.Restart); 
    }

    public void Popup_Close()
    {
        celularCanvasGroup.alpha = 1;
        
        Vector3 targetPos = new Vector3(celular.transform.localPosition.x, 429, celular.transform.localPosition.z);
            celular.transform.DOLocalMove(targetPos, 0.2f).SetEase(Ease.InOutQuad);
        shakeTween?.Kill(); 
        shakeTween = null;
       
        Sequence closeSeq = DOTween.Sequence();
        closeSeq.Append(canvasGroup.DOFade(0, fadeDuration));
        closeSeq.Join(popupRect.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack));
        closeSeq.OnComplete(() =>
        {
            popUp.SetActive(false);
            
        });
    }
}