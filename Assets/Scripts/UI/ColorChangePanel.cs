using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Scripting;

public class ColorChangePanel : MonoBehaviour{
    private PlayerList playerList => PlayerList.singleton;
    
    [SerializeField] Database db;
    [SerializeField] HUDSO HUDSO;
    [SerializeField] GameObject _mainContainer;
    [SerializeField] Transform _gridRoot;

    [SerializeField] Transform _customButtonPrefab;
    [SerializeField] private List<CustomButton> buttons = new List<CustomButton>();

    private void Awake()
    {
        Debug.Log($"📂 [DB] Ref = {db}"); 
        Debug.Log($"📂 [DB] Total de cores = {db.playerColors.Count}");
        CreateButtons();
    }

    private IEnumerator Start()
    {
        // espera o Mirror spawnar o PlayerList
        while (PlayerList.singleton == null || PlayerList.singleton.players == null)
            yield return null;

        PlayerList pl = PlayerList.singleton;           // agora já existe
        pl.players.Callback += PlayersOnCallback;

        HUDSO.EventOnShowColorChangePanel += HUDSOOnEventOnShowColorChangePanel;
        HUDSO.EventOnHideColorChangePanel += HUDSOOnEventOnHideColorChangePanel;
    }
    public void CreateButtons()
    {
        for (int i = 0; i < db.playerColors.Count; i++)
        {
            Transform instance = Instantiate(_customButtonPrefab, _gridRoot);
            CustomButton cb = instance.GetComponentInChildren<CustomButton>();
            
            cb.Sprite.color = db.playerColors[i].color;
            buttons.Add(cb);
        }
    }

    private void OnDestroy()
    {
        playerList.players.Callback -= PlayersOnCallback;
        
        HUDSO.EventOnShowColorChangePanel -= HUDSOOnEventOnShowColorChangePanel;
        HUDSO.EventOnHideColorChangePanel -= HUDSOOnEventOnHideColorChangePanel;
    }

    public void Refresh()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool occupied = buttons[i].interactable = playerList.ColorsAvailable.Contains(i) == true;
            buttons[i].interactable = occupied;


            
        }
    }
    
    private void PlayersOnCallback(SyncList<PlayerData>.Operation op, int itemindex, PlayerData olditem, PlayerData newitem)
    {
        Refresh();
    }
    
    private void HUDSOOnEventOnShowColorChangePanel()
    {
        _mainContainer.SetActive(true);
        Refresh();
    }
    
    private void HUDSOOnEventOnHideColorChangePanel()
    {
        _mainContainer.SetActive(false);
        Refresh();
    }


}
