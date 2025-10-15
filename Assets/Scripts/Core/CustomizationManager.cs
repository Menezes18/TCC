using System;
using UnityEngine;


public class CustomizationManager : MonoBehaviour
{
    private static CustomizationManager _instance;
    public static CustomizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CustomizationManager");
                _instance = go.AddComponent<CustomizationManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private const string PLAYER_ID_KEY = "PlayerID";
    private const string CUSTOMIZATION_KEY = "PlayerCustomization";

    private string _playerId;
    private PlayerCustomizationData _currentCustomization;

    public event Action<PlayerCustomizationData> OnCustomizationChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePlayerID();
        LoadCustomization();
    }

    private void InitializePlayerID()
    {
        if (PlayerPrefs.HasKey(PLAYER_ID_KEY))
        {
            _playerId = PlayerPrefs.GetString(PLAYER_ID_KEY);
            Debug.Log($"🎮 [CustomizationManager] PlayerID carregado: {_playerId}");
        }
        else
        {
            // Gera novo GUID
            _playerId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PLAYER_ID_KEY, _playerId);
            PlayerPrefs.Save();
            Debug.Log($"🎮 [CustomizationManager] Novo PlayerID gerado: {_playerId}");
        }
    }

    private void LoadCustomization()
    {
        if (PlayerPrefs.HasKey(CUSTOMIZATION_KEY))
        {
            string json = PlayerPrefs.GetString(CUSTOMIZATION_KEY);
            _currentCustomization = JsonUtility.FromJson<PlayerCustomizationData>(json);
            Debug.Log($"✅ [CustomizationManager] Customização carregada: {_currentCustomization}");
        }
        else
        {
            _currentCustomization = new PlayerCustomizationData(_playerId);
            Debug.Log($"🆕 [CustomizationManager] Nova customização criada para: {_playerId}");
        }
    }

    public void SaveCustomization()
    {
        if (_currentCustomization == null)
        {
            Debug.LogWarning("⚠️ [CustomizationManager] Tentativa de salvar customização nula");
            return;
        }

        string json = JsonUtility.ToJson(_currentCustomization);
        PlayerPrefs.SetString(CUSTOMIZATION_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"💾 [CustomizationManager] Customização salva: {_currentCustomization}");
        OnCustomizationChanged?.Invoke(_currentCustomization);
        

        SendCustomizationToServer();
    }
    

    private void SendCustomizationToServer()
    {
        var playerData = FindAnyObjectByType<PlayerData>();
        if (playerData != null && playerData.isLocalPlayer)
        {
            playerData.SendCustomizationToServer();
        }
    }


    public PlayerCustomizationData GetCurrentCustomization()
    {
        return _currentCustomization?.Clone();
    }


    public void SetHat(int hatIndex)
    {
        if (_currentCustomization == null) return;
        _currentCustomization.hatIndex = hatIndex;
        SaveCustomization();
    }


    public void SetGlasses(int glassesIndex)
    {
        if (_currentCustomization == null) return;
        _currentCustomization.glassesIndex = glassesIndex;
        SaveCustomization();
    }


    public void SetShirt(int shirtIndex)
    {
        if (_currentCustomization == null) return;
        _currentCustomization.shirtIndex = shirtIndex;
        SaveCustomization();
    }


    public void ResetCustomization()
    {
        _currentCustomization = new PlayerCustomizationData(_playerId);
        SaveCustomization();
        Debug.Log("🔄 [CustomizationManager] Customização resetada");
    }

    public string GetPlayerId() => _playerId;
}
