using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private Image pushFillImage;
    [SerializeField] private Image throwFillImage;

    public PlayerScript player;

    public void Init(PlayerScript target)
    {
        player = target;
    }

    void Start()
    {
        if (player == null && NetworkClient.localPlayer != null)
            player = NetworkClient.localPlayer.GetComponent<PlayerScript>();
    }

    void Update()
    {
        if (player == null)
            return;

        if (pushFillImage != null)
            pushFillImage.fillAmount = player.PushCooldownNormalized;

        if (throwFillImage != null)
            throwFillImage.fillAmount = player.ThrowCooldownNormalized;
    }
}
