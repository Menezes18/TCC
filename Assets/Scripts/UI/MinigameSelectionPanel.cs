using UnityEngine;

public class MinigameSelectionPanel : MonoBehaviour
{
    [SerializeField] private HUDSO HUDSO;
    [SerializeField] private GameObject _mainContainer;

    private void Awake()
    {
        if (_mainContainer != null)
            _mainContainer.SetActive(false);

        // Assina já no Awake para funcionar mesmo se este GO estiver desativado
        if (HUDSO != null)
        {
            HUDSO.EventOnShowMinigameSelectionPanel += OnShowPanel;
            HUDSO.EventOnHideMinigameSelectionPanel += OnHidePanel;
        }
    }

    private void OnDestroy()
    {
        if (HUDSO != null)
        {
            HUDSO.EventOnShowMinigameSelectionPanel -= OnShowPanel;
            HUDSO.EventOnHideMinigameSelectionPanel -= OnHidePanel;
        }
    }

    private void OnShowPanel()
    {
        if (_mainContainer != null)
            _mainContainer.SetActive(true);
    }

    private void OnHidePanel()
    {
        if (_mainContainer != null)
            _mainContainer.SetActive(false);
    }
}
