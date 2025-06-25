using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Collections;
using TMPro;

public class PoupNetwork : MonoBehaviour
{

    [SerializeField] GameObject popupPanel;
    [SerializeField] TextMeshProUGUI countdownText;
    [SerializeField] int timer;
    private void Start()
    {
        bool steamOk = SteamAPI.Init();
        if (!steamOk)
        {
            ShowSteamMissingPopup();
        }
    }

    private void ShowSteamMissingPopup()
    {
        popupPanel.SetActive(true);
        StartCoroutine(CountdownAndQuit(timer));
    }

    private IEnumerator CountdownAndQuit(int seconds)
    {
        int counter = seconds;
        while (counter > 0)
        {
            countdownText.text = $"Fechando em {counter}s...";
            yield return new WaitForSeconds(1f);
            counter--;
        }
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (SteamAPI.IsSteamRunning())
            SteamAPI.Shutdown();
    }

    private void Update()
    {
        if (SteamAPI.IsSteamRunning())
            SteamAPI.RunCallbacks();
    }
}