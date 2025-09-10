using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class MenuController : MonoBehaviour
{
    public static MenuController singleton;

    [Header("UI Menus")]
    [SerializeField] GameObject mainMenu;

    private bool isMenuOpen = false;
    private void Awake()
    {
        if (singleton == null) singleton = this;
        else Destroy(gameObject);
    }

    public void OpenMenu(bool isOpen)
    {
        mainMenu.SetActive(isOpen);
    }
}
