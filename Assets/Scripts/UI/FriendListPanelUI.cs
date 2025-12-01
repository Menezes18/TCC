using UnityEngine;

public class FriendListPanelUI : MonoBehaviour
{
    [SerializeField] private HUDSO HUDSO;
    [SerializeField] private GameObject _mainContainer;

    private void Awake()
    {
        if (_mainContainer != null)
            _mainContainer.SetActive(false);

        if (HUDSO != null)
        {
            HUDSO.EventOnShowFriendListPanel += OnShowPanel;
            HUDSO.EventOnHideFriendListPanel += OnHidePanel;
        }
    }

    private void OnDestroy()
    {
        if (HUDSO != null)
        {
            HUDSO.EventOnShowFriendListPanel -= OnShowPanel;
            HUDSO.EventOnHideFriendListPanel -= OnHidePanel;
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
