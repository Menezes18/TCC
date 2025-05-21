using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public abstract class MinigameController : NetworkBehaviour, IScoreRule
{
    public UnityEvent OnStartGame;
    public event Action OnMatchStarted;
    public event Action OnMatchEnded;

    [Server]
    public void SetupMiniGame()
    {
        BriefingManager.singleton.TriggerBriefing();
    }
    
    [Server]
    public void StartMatch()
    {
        OnMatchStarted?.Invoke();
        OnStartGame?.Invoke();

    }

    [Server]
    public void EndMatch()
    {
        OnMatchEnded?.Invoke();
        AssignFinalPoints();
    }
    
    public abstract void UpdateScores();
    public abstract void AssignFinalPoints();
    public abstract Dictionary<ulong, int> GetResults();
}