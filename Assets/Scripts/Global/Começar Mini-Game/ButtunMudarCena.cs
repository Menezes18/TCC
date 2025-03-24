using Mirror;

public class CenaHandler : NetworkBehaviour
{
    [Command]
    void CmdPedirTrocarCena(string cena)
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(cena);
        }
    }

    public void OnTrocarCenaButton()
    {
        if (isLocalPlayer)
        {
            CmdPedirTrocarCena("MiniGame");
        }
    }
}
