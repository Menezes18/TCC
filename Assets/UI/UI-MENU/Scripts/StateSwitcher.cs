using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StateSwitcher : MonoBehaviour
{
    public TextMeshProUGUI titleButton;  
    public CanvasType ChangeCanvasTo;

    StateManager stateManager;
    Button menuButton;

    private void Start()
    {
        menuButton = GetComponent<Button>();
        menuButton.onClick.AddListener(OnButtonClicked);
        stateManager = StateManager.GetInstance();
        if(titleButton != null) titleButton.text = ChangeCanvasTo.title;
    }

    public void OnButtonClicked()
    {
       
        StartCoroutine(stateManager.PlayNextCanvasAnimation(ChangeCanvasTo));
            
    }
}