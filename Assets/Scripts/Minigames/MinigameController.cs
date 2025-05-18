using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public abstract class MinigameController : NetworkBehaviour, IScoreRule
{
    [SerializeField] private BriefingScreenSO briefingData;
    public BriefingScreenSO GetBriefingData() => briefingData;
    public event Action OnMatchStarted;
    public event Action OnMatchEnded;

    [Server]
    public void StartMatch()
    {
        OnMatchStarted?.Invoke();
        BriefingManager.singleton.ShowBriefing(briefingData);
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