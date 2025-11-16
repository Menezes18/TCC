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
    [SerializeField] private RectTransform goalBannerTransform;
    
    [Header("Team Announcement")]
    [SerializeField] private CanvasGroup teamAnnouncementGroup;
    [SerializeField] private TMP_Text teamAnnouncementText;
    [SerializeField] private RectTransform teamAnnouncementTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float goalTextDuration = 1.5f;
    [SerializeField] private float teamTextDuration = 1.2f;
    [SerializeField] private float scorerTextDuration = 1.5f;
    [SerializeField] private float bannerFadeDuration = 0.8f;
    [SerializeField] private float teamAnnouncementDuration = 4.0f;

    [Header("Colors")]
    [SerializeField] private Color blueTeamColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color redTeamColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color goalTextColor = Color.yellow;

    Sequence _bannerSequence;
    Sequence _teamAnnouncementSequence;

    public void SetScore(int scoreA, int scoreB)
    {
        if (scoreAText != null) 
        {
            scoreAText.text = scoreA.ToString();
            // Animação de pulse no score
            scoreAText.transform.DOKill();
            scoreAText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
        }
        
        if (scoreBText != null) 
        {
            scoreBText.text = scoreB.ToString();
            // Animação de pulse no score
            scoreBText.transform.DOKill();
            scoreBText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
        }
    }

    public void ShowGoal(int team, string scorerAlias)
    {
        string teamName = team == 0 ? "AZUL" : team == 1 ? "VERMELHO" : "";
        Color teamColor = team == 0 ? blueTeamColor : redTeamColor;
        string scorerName = string.IsNullOrWhiteSpace(scorerAlias) ? "Jogador Desconhecido" : scorerAlias;

        if (goalBannerGroup == null) return;

        // Mata animações anteriores
        _bannerSequence?.Kill();
        goalBannerGroup.DOKill();
        if (goalBannerTransform != null) goalBannerTransform.DOKill();
        if (goalBannerText != null) goalBannerText.transform.DOKill();

        // Setup inicial
        goalBannerGroup.alpha = 0f;
        goalBannerGroup.gameObject.SetActive(true);

        if (goalBannerText != null)
        {
            goalBannerText.transform.localScale = Vector3.zero;
            goalBannerText.alpha = 0f;
        }

        _bannerSequence = DOTween.Sequence();

        _bannerSequence.Append(goalBannerGroup.DOFade(1f, 0.2f));

        if (goalBannerText != null)
        {
            _bannerSequence.AppendCallback(() => {
                goalBannerText.text = "GOOOOOOOL!";
                goalBannerText.color = goalTextColor;
            });
            
            _bannerSequence.Append(goalBannerText.DOFade(1f, 0.1f));
            _bannerSequence.Join(goalBannerText.transform.DOScale(1.8f, 0.5f)
                .SetEase(Ease.OutBack, 2f));
            
            _bannerSequence.Append(goalBannerText.transform.DOPunchScale(Vector3.one * 0.4f, 0.6f, 10, 0.8f));
            _bannerSequence.Join(goalBannerText.transform.DOPunchRotation(new Vector3(0, 0, 15), 0.6f, 8, 0.7f));
            
            _bannerSequence.Append(goalBannerText.transform.DOScale(2.0f, 0.4f)
                .SetEase(Ease.InOutSine)
                .SetLoops(2, LoopType.Yoyo));
            
            _bannerSequence.AppendInterval(goalTextDuration);

            _bannerSequence.Append(goalBannerText.transform.DOScale(0.5f, 0.3f)
                .SetEase(Ease.InBack));
            _bannerSequence.Join(goalBannerText.DOFade(0f, 0.3f));
            
            _bannerSequence.AppendCallback(() => {
                goalBannerText.text = $"TIME {teamName}!\n<size=60%>{scorerName}</size>";
                goalBannerText.color = teamColor;
                goalBannerText.transform.localPosition = new Vector3(1200f, goalBannerText.transform.localPosition.y, 0);
            });
            
            _bannerSequence.Append(goalBannerText.DOFade(1f, 0.2f));
            _bannerSequence.Join(goalBannerText.transform.DOLocalMoveX(0f, 0.7f)
                .SetEase(Ease.OutBack, 1.5f));
            _bannerSequence.Join(goalBannerText.transform.DOScale(1.4f, 0.7f)
                .SetEase(Ease.OutBack));
            
            _bannerSequence.Append(goalBannerText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 8, 0.6f));
            
            _bannerSequence.AppendInterval(teamTextDuration + scorerTextDuration);
        }

        if (goalBannerTransform != null)
        {
            _bannerSequence.Append(goalBannerTransform.DOScale(1.8f, bannerFadeDuration)
                .SetEase(Ease.InBack));
        }
        
        _bannerSequence.Join(goalBannerGroup.DOFade(0f, bannerFadeDuration)
            .SetEase(Ease.InQuad));
        
        _bannerSequence.OnComplete(() => {
            if (goalBannerGroup != null)
            {
                goalBannerGroup.gameObject.SetActive(false);
                
                // Reset transforms
                if (goalBannerTransform != null)
                    goalBannerTransform.localScale = Vector3.one;
                if (goalBannerText != null)
                {
                    goalBannerText.transform.localScale = Vector3.one;
                    goalBannerText.transform.localRotation = Quaternion.identity;
                    goalBannerText.transform.localPosition = Vector3.zero;
                }
            }
        });
    }

    public void ShowTeamAnnouncement(string blueTeamNames, string redTeamNames)
    {
        if (teamAnnouncementGroup == null || teamAnnouncementText == null) return;

        // Mata animações anteriores
        _teamAnnouncementSequence?.Kill();
        teamAnnouncementGroup.DOKill();
        if (teamAnnouncementTransform != null) teamAnnouncementTransform.DOKill();
        teamAnnouncementText.transform.DOKill();

        // Setup inicial
        teamAnnouncementGroup.alpha = 0f;
        teamAnnouncementGroup.gameObject.SetActive(true);
        teamAnnouncementText.transform.localScale = Vector3.zero;
        teamAnnouncementText.alpha = 0f;

        // Formata o texto com cores e tamanhos
        string formattedText = "<size=120%><b>TIMES SORTEADOS!</b></size>\n\n" +
                              $"<color=#{ColorUtility.ToHtmlStringRGB(blueTeamColor)}><size=100%><b>TIME AZUL:</b></size></color>\n" +
                              $"<color=#{ColorUtility.ToHtmlStringRGB(blueTeamColor)}>{blueTeamNames}</color>\n\n" +
                              $"<color=#{ColorUtility.ToHtmlStringRGB(redTeamColor)}><size=100%><b>TIME VERMELHO:</b></size></color>\n" +
                              $"<color=#{ColorUtility.ToHtmlStringRGB(redTeamColor)}>{redTeamNames}</color>";

        teamAnnouncementText.text = formattedText;

        _teamAnnouncementSequence = DOTween.Sequence();

        // Fade in do background
        _teamAnnouncementSequence.Append(teamAnnouncementGroup.DOFade(1f, 0.3f));

        // Fade in e scale do texto
        _teamAnnouncementSequence.Join(teamAnnouncementText.DOFade(1f, 0.3f));
        _teamAnnouncementSequence.Join(teamAnnouncementText.transform.DOScale(1.2f, 0.5f)
            .SetEase(Ease.OutBack, 1.7f));

        // Punch animation para dar ênfase
        _teamAnnouncementSequence.Append(teamAnnouncementText.transform.DOPunchScale(Vector3.one * 0.15f, 0.5f, 8, 0.7f));

        // Aguarda o tempo de exibição
        _teamAnnouncementSequence.AppendInterval(teamAnnouncementDuration);

        // Scale up e fade out
        if (teamAnnouncementTransform != null)
        {
            _teamAnnouncementSequence.Append(teamAnnouncementTransform.DOScale(1.5f, 0.5f)
                .SetEase(Ease.InBack));
        }
        _teamAnnouncementSequence.Join(teamAnnouncementGroup.DOFade(0f, 0.5f)
            .SetEase(Ease.InQuad));

        // Cleanup ao terminar
        _teamAnnouncementSequence.OnComplete(() => {
            if (teamAnnouncementGroup != null)
            {
                teamAnnouncementGroup.gameObject.SetActive(false);
                
                // Reset transforms
                if (teamAnnouncementTransform != null)
                    teamAnnouncementTransform.localScale = Vector3.one;
                teamAnnouncementText.transform.localScale = Vector3.one;
                teamAnnouncementText.transform.localRotation = Quaternion.identity;
            }
        });
    }
}

