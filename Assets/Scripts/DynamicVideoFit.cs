using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MatchImageSize : MonoBehaviour
{
    public RawImage rawVideo;    // arraste sua RawImage aqui
    public Image referenceImg;   // arraste a Image de referência aqui
    public VideoPlayer vp;       // seu VideoPlayer

    void Start()
    {
        vp.prepareCompleted += _ => CopyRect();
        vp.Prepare();
    }

    void CopyRect()
    {
        RectTransform rtRef   = referenceImg.rectTransform;
        RectTransform rtVideo = rawVideo.rectTransform;

        // 1) Copia anchors e pivot
        rtVideo.anchorMin    = rtRef.anchorMin;
        rtVideo.anchorMax    = rtRef.anchorMax;
        rtVideo.pivot        = rtRef.pivot;

        // 2) Copia posição local dentro do pai
        rtVideo.anchoredPosition = rtRef.anchoredPosition;

        // 3) Copia largura e altura
        rtVideo.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            rtRef.rect.width
        );
        rtVideo.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            rtRef.rect.height
        );

        // 4) Agora aplica a textura e play
        rawVideo.texture = vp.texture;
        vp.Play();
    }
}