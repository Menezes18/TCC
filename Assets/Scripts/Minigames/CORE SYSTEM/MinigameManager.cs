using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MinigameManager : NetworkBehaviour
{
    private static MinigameManager _instance;
    public static MinigameManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<MinigameManager>();
            return _instance;
        }
    }

    [SyncVar] private int _currentMinigameIndex = -1;
    private List<IMinigame> _availableMinigames = new List<IMinigame>();
    private IMinigame _currentMinigame;
    
    public event Action<IMinigame> OnMinigameStarted;
    public event Action<IMinigame> OnMinigameEnded;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterMinigame(IMinigame minigame)
    {
        if (!_availableMinigames.Contains(minigame))
        {
            _availableMinigames.Add(minigame);
            Debug.Log($"Minigame registered: {minigame.Name}");
        }
    }

    [Server]
    public void StartRandomMinigame()
    {
        if (_availableMinigames.Count == 0)
        {
            Debug.LogError("No minigames available to start");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _availableMinigames.Count);
        StartMinigame(randomIndex);
    }

    [Server]
    public void StartMinigame(int index)
    {
        if (index < 0 || index >= _availableMinigames.Count)
        {
            Debug.LogError($"Invalid minigame index: {index}");
            return;
        }

        if (_currentMinigame != null)
        {
            _currentMinigame.EndGame();
        }

        _currentMinigameIndex = index;
        _currentMinigame = _availableMinigames[index];
        _currentMinigame.Initialize();
        _currentMinigame.StartGame();
        
        RpcMinigameStarted(_currentMinigameIndex);
    }

    [ClientRpc]
    private void RpcMinigameStarted(int index)
    {
        if (isServer) return; // Server already handled this
        
        _currentMinigameIndex = index;
        _currentMinigame = _availableMinigames[index];
        OnMinigameStarted?.Invoke(_currentMinigame);
    }

    [Server]
    public void EndCurrentMinigame()
    {
        if (_currentMinigame == null) return;
        
        _currentMinigame.EndGame();
        RpcMinigameEnded(_currentMinigameIndex);
        _currentMinigame = null;
        _currentMinigameIndex = -1;
    }

    [ClientRpc]
    private void RpcMinigameEnded(int index)
    {
        if (isServer) return; // Server already handled this
        
        var endedMinigame = _availableMinigames[index];
        OnMinigameEnded?.Invoke(endedMinigame);
        _currentMinigame = null;
        _currentMinigameIndex = -1;
    }
}
