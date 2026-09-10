using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public abstract class MinigameController : NetworkBehaviour, IScoreRule, ISubject
{
    public UnityEvent OnStartGame;
    public event Action OnMatchStarted;
    public event Action OnMatchEnded;
    private readonly List<IObserver> _observers = new();
    
    public virtual bool UseAliveStatusOnScoreboard => false;
    public virtual bool UseTeamColorsOnScoreboard => false;
    public virtual bool UsePercentageOnScoreboard => false;
    public virtual int GetScoreboardTeam(ulong playerId) => -1;
    // Quando true, o MatchManager não fará teleporte inicial; o minigame cuidará do spawn
    public virtual bool HandlesInitialSpawns => false;
    
    
    [Server]
    public virtual void SetupMiniGame()
    {
        BriefingManager.singleton.TriggerBriefing();
    }
    
    [Server]
    public virtual void StartMatch()
    {
        OnMatchStarted?.Invoke();
        OnStartGame?.Invoke();
    }

    [Server]
    public virtual void EndMatch()
    {
        OnMatchEnded?.Invoke();
        Debug.LogWarning("[MinigameController] EndMatch chamado – delegando resultados ao MatchManager");
    }
    [ClientRpc]
    public void RpcUpdateScoreboard(string[] names, int[] points, int[] colors, bool[] aliveStates, ulong[] steamIds, int[] teamIds)
    {
        // relay to UI on clients via runtime lookup
        var ui = FindAnyObjectByType<ScoreboardUI>();
        if (ui != null)
            ui.UpdateUI(names, points, colors, aliveStates, steamIds, teamIds);
    }
    
    
    public abstract void UpdateScores();
    public abstract void AssignFinalPoints();
    public abstract Dictionary<ulong, int> GetResults();
    public abstract Dictionary<ulong, int> GetLiveScores();
    
    public void Adicionar(IObserver observer) => _observers.Add(observer);
    public void Retira(IObserver observer)    => _observers.Remove(observer);
    public void Notifica()
    {
        foreach (var obs in _observers) 
            obs.Atualizacao(this);
    }
}
