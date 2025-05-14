using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ColorChangePanel : MonoBehaviour{
    private PlayerList playerList => PlayerList.singleton;
    
    [SerializeField] Database db;
    [SerializeField] HUDSO HUDSO;
    [SerializeField] GameObject _mainContainer;
    [SerializeField] Transform _gridRoot;

    [SerializeField] Transform _customButtonPrefab;
    private List<CustomButton> buttons = new List<CustomButton>();

    private void Start()
    {
        //
        playerList.players.Callback += PlayersOnCallback;
        
        HUDSO.EventOnShowColorChangePanel += HUDSOOnEventOnShowColorChangePanel;
        HUDSO.EventOnHideColorChangePanel += HUDSOOnEventOnHideColorChangePanel;
        
        //
        for (int i = 0; i < db.playerColors.Count; i++)
        {
            Transform instance = Instantiate(_customButtonPrefab, _gridRoot);
            CustomButton cb = instance.GetComponent<CustomButton>();
            
            cb.Sprite.color = db.playerColors[i].color;
        }
        
        //
        playerList.players.Callback += PlayersOnCallback;
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
    }
    
    private void HUDSOOnEventOnHideColorChangePanel()
    {
        _mainContainer.SetActive(false);
    }


}
