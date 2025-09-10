using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using UnityEditor;
using UnityEngine.SceneManagement;
public class PopupManager : MonoBehaviour
{
    public static PopupManager instance;

    [Header("UI References")]
    public GameObject popUp; // painel geral// para fade
    public RectTransform popupRect; // para scale/ shake
    public TMP_Text titleText;
    public GameObject celular;
    public CanvasGroup celularCanvasGroup;

    public GameObject[] popupsMenus;
    
    public CanvasType menuType;

    private void Awake()
    {
        instance = this;
        popUp.SetActive(false);
    }

    public void Popup_Show(string title, bool shake = false, bool shakeloop = false)
    {
        foreach (var popups in popupsMenus){
            popups.SetActive(false);
        }
    
        StateManager.GetInstance().DesativarStates();
        titleText.text = title;
        
        popUp.SetActive(true);
    }
    

    public void Popup_Close()
    {
        popUp.SetActive(false);
        StateManager.GetInstance().GoToNextCanvas(menuType);
    }
}