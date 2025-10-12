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
        // Garante que listas dinâmicas (como a seleção de minigames) sejam (re)geradas ao abrir
        if (_mainContainer != null)
        {
            var selectors = _mainContainer.GetComponentsInChildren<MinigameSelectorUI>(true);
            foreach (var sel in selectors)
            {
                try { sel.Rebuild(); }
                catch (System.Exception e) { Debug.LogWarning($"[MinigameSelectionPanel] Falha ao rebuild do selector: {e.Message}", sel); }
            }
        }
    }

    private void OnHidePanel()
    {
        if (_mainContainer != null)
            _mainContainer.SetActive(false);
    }
}
