using TMPro;
using UnityEngine;
using DG.Tweening;

public class SoccerHUD : MonoBehaviour
{
    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreAText; // Azul
    [SerializeField] private TMP_Text scoreBText; // Vermelho

    [Header("Goal Banner")] 
    [SerializeField] private CanvasGroup goalBannerGroup;
    [SerializeField] private TMP_Text goalBannerText;
    [SerializeField] private float bannerShowDuration = 1.5f;
    [SerializeField] private float bannerFadeDuration = 0.5f;

    Tween _bannerTween;

    public void SetScore(int scoreA, int scoreB)
    {
        if (scoreAText != null) scoreAText.text = scoreA.ToString();
        if (scoreBText != null) scoreBText.text = scoreB.ToString();
    }

    public void ShowGoal(int team, string scorerAlias)
    {
        string teamName = team == 0 ? "Azul" : team == 1 ? "Vermelho" : "";
        string who = string.IsNullOrWhiteSpace(scorerAlias) ? string.Empty : $" (Autor: {scorerAlias})";
        string msg = $"GOL do time {teamName}!\n{who}";

        if (goalBannerText != null) goalBannerText.text = msg;
        if (goalBannerGroup == null) return;

        _bannerTween?.Kill();
        goalBannerGroup.DOKill();
        goalBannerGroup.alpha = 0f;
        goalBannerGroup.gameObject.SetActive(true);

        _bannerTween = DOTween.Sequence()
            .Append(goalBannerGroup.DOFade(1f, 0.1f))
            .AppendInterval(bannerShowDuration)
            .Append(goalBannerGroup.DOFade(0f, bannerFadeDuration))
            .OnComplete(() => goalBannerGroup.gameObject.SetActive(false));
    }
}

