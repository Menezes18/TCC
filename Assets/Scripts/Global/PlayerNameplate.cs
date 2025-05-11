using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameplate : MonoBehaviour
{
    
    [SerializeField] Database db;
    [SerializeField] TMP_Text _nameplate;
    [SerializeField] Image _sprite;
    
    public void SetNameplate(string playerName)
    {
        _nameplate.text = playerName;
    }

    public void SetColor(int color)
    {
        _sprite.color = db.playerColors[color];
    }

}
