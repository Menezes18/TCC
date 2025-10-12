using UnityEngine;

public class MinigamePanelActions : MonoBehaviour
{
    [SerializeField] private HUDSO HUDSO;

    public void Close()
    {
        if (HUDSO != null)
            HUDSO.HideMinigameSelectionPanel();
    }
}

