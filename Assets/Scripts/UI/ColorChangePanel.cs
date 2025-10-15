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
        // Assina eventos do HUD imediatamente para permitir abrir o painel
        if (HUDSO != null)
        {
            HUDSO.EventOnShowColorChangePanel += HUDSOOnEventOnShowColorChangePanel;
            HUDSO.EventOnHideColorChangePanel += HUDSOOnEventOnHideColorChangePanel;
        }

        // Aguarda PlayerList para callbacks de atualização, mas não bloqueia a UI
        while (PlayerList.singleton == null)
            yield return null;

        var list = PlayerList.singleton;
        if (list != null)
        {
            if (list.players != null)
                list.players.Callback += PlayersOnCallback;
            if (list.ColorsAvailable != null)
                list.ColorsAvailable.Callback += ColorsOnCallback;
        }
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
        if (playerList != null)
        {
            if (playerList.players != null)
                playerList.players.Callback -= PlayersOnCallback;
            if (playerList.ColorsAvailable != null)
                playerList.ColorsAvailable.Callback -= ColorsOnCallback;
        }
        
        if (HUDSO != null)
        {
            HUDSO.EventOnShowColorChangePanel -= HUDSOOnEventOnShowColorChangePanel;
            HUDSO.EventOnHideColorChangePanel -= HUDSOOnEventOnHideColorChangePanel;
        }
    }

    public void Refresh()
    {
        if (playerList == null || playerList.ColorsAvailable == null)
            return;

        // Se a lista ainda não sincronizou nada, não bloqueie a seleção no cliente
        if (playerList.ColorsAvailable.Count == 0)
        {
            for (int i = 0; i < buttons.Count; i++)
                buttons[i].interactable = true;
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            bool available = playerList.ColorsAvailable.Contains(i);
            buttons[i].interactable = available;
        }
    }
    
    private void PlayersOnCallback(SyncList<PlayerData>.Operation op, int itemindex, PlayerData olditem, PlayerData newitem)
    {
        Refresh();
    }
    private void ColorsOnCallback(SyncList<int>.Operation op, int itemIndex, int oldItem, int newItem)
    {
        Refresh();
    }
    
    private void HUDSOOnEventOnShowColorChangePanel()
    {
        if (_mainContainer != null) _mainContainer.SetActive(true);
        Refresh();
    }
    
    private void HUDSOOnEventOnHideColorChangePanel()
    {
        if (_mainContainer != null) _mainContainer.SetActive(false);
        Refresh();
    }
    
    public void SetHUD(HUDSO hud)
    {
        if (HUDSO != null)
        {
            HUDSO.EventOnShowColorChangePanel -= HUDSOOnEventOnShowColorChangePanel;
            HUDSO.EventOnHideColorChangePanel -= HUDSOOnEventOnHideColorChangePanel;
        }
        HUDSO = hud;
        if (HUDSO != null)
        {
            HUDSO.EventOnShowColorChangePanel += HUDSOOnEventOnShowColorChangePanel;
            HUDSO.EventOnHideColorChangePanel += HUDSOOnEventOnHideColorChangePanel;
        }
    }
}
