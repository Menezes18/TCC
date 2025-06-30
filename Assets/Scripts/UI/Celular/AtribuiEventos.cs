using UnityEngine;

public class AtribuiEventos : MonoBehaviour
{
    void Start()
    {
        this.gameObject.GetComponent<ButtonToggle>().onActivated.AddListener(() => MyNetworkManager.manager.AdicionarMiniGames(gameObject.name));
        this.gameObject.GetComponent<ButtonToggle>().onDeactivated.AddListener(() => MyNetworkManager.manager.tirarMiniGames(gameObject.name));
    }
}
