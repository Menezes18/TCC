using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public abstract class MinigameBase : NetworkBehaviour, IMinigame
{
    [SyncVar] protected MinigameState _currentState = MinigameState.Uninitialized;
    
    public abstract string Name { get; }
    
    public MinigameState CurrentState => _currentState;
    
    protected virtual void Awake()
    {
        MinigameManager.Instance.RegisterMinigame(this);
    }
    
    public virtual void Initialize()
    {
        _currentState = MinigameState.Initializing;
        // Lógica de inicialização comum
        _currentState = MinigameState.WaitingForPlayers;
    }
    
    public virtual void StartGame()
    {
        _currentState = MinigameState.Starting;
        // Lógica de início comum
        _currentState = MinigameState.InProgress;
    }
    
    public virtual void EndGame()
    {
        _currentState = MinigameState.Ending;
        // Lógica de finalização comum
        _currentState = MinigameState.Finished;
    }
    
    public virtual void ResetGame()
    {
        _currentState = MinigameState.Uninitialized;
        // Lógica de reset comum
    }
}