using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;


public class SpecOverlayController : MonoBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private GameObject rootPanel; 
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Button btnPrev;
    [SerializeField] private Button btnNext;

    [Header("Alvo Observado (opcional)")]
    [SerializeField] private TextMeshProUGUI observingText;

    private void Awake()
    {
        
        SetActive(false);

        if (btnPrev) btnPrev.onClick.AddListener(OnPrevClicked);
        if (btnNext) btnNext.onClick.AddListener(OnNextClicked);
    }

    private void OnEnable()
    {
        if (SpectatorManager.Instance == null) return;
        SpectatorManager.Instance.OnLocalSpectatorStateChanged += HandleLocalSpecState;
        SpectatorManager.Instance.OnLocalSpectatorTargetChanged += HandleTargetChanged;

        var isSpec = SpectatorManager.Instance.LocalSpectator != null;
        HandleLocalSpecState(isSpec);
        HandleTargetChanged(SpectatorManager.Instance.CurrentTarget);
    }

    private void OnDisable()
    {
        if (SpectatorManager.Instance == null) return;
        SpectatorManager.Instance.OnLocalSpectatorStateChanged -= HandleLocalSpecState;
        SpectatorManager.Instance.OnLocalSpectatorTargetChanged -= HandleTargetChanged;
    }

    private void HandleLocalSpecState(bool isSpectating)
    {
        SetActive(isSpectating);
        if (isSpectating && headerText != null)
        {
            headerText.text = "Você está como Espectador";
        }
    }

    private void HandleTargetChanged(PlayerScript target)
    {
        if (observingText == null) return;
        if (target == null)
        {
            observingText.text = "";
            return;
        }
        var pd = target.GetComponent<PlayerData>();
        var name = pd != null ? (string.IsNullOrEmpty(pd.alias) ? pd.playerInfo.username : pd.alias) : target.name;
        observingText.text = $"observando {name}";
    }


    private void SetActive(bool active)
    {
        if (rootPanel != null) rootPanel.SetActive(active);
        if (btnPrev != null) btnPrev.interactable = active;
        if (btnNext != null) btnNext.interactable = active;
    }

    private void OnPrevClicked()
    {

        var spec = SpectatorManager.Instance?.LocalSpectator;
        if (spec != null)
        {

            spec.SpectatePreviousTarget();
        }
    }

    private void OnNextClicked()
    {
        var spec = SpectatorManager.Instance?.LocalSpectator;
        if (spec != null)
        {
            spec.SpectateNextTarget();
        }
    }
}
