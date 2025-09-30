using System;
using UnityEngine;

public class AtribuiEventos : MonoBehaviour
{
    [SerializeField] private string minigameId;

    private ButtonToggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<ButtonToggle>();

        if (_toggle == null)
        {
            Debug.LogWarning("[AtribuiEventos] Nenhum ButtonToggle encontrado para atribuir eventos.", this);
            return;
        }

        _toggle.onActivated.AddListener(OnActivated);
        _toggle.onDeactivated.AddListener(OnDeactivated);
    }

    private void OnDestroy()
    {
        if (_toggle == null)
            return;

        _toggle.onActivated.RemoveListener(OnActivated);
        _toggle.onDeactivated.RemoveListener(OnDeactivated);
    }

    private string ResolveMinigameId() => string.IsNullOrWhiteSpace(minigameId) ? gameObject.name : minigameId;

    private void OnActivated()
    {
        MyNetworkManager.manager?.AdicionarMiniGames(ResolveMinigameId());
    }

    private void OnDeactivated()
    {
        MyNetworkManager.manager?.tirarMiniGames(ResolveMinigameId());
    }
}
