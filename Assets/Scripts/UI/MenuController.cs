using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    public GameObject menuUI;


    private bool menuAberto = false;

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            AlternarMenu();
        }
    }

    void AlternarMenu()
    {
        menuAberto = !menuAberto;
        
        menuUI.SetActive(menuAberto);
        
    }


}
