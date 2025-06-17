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
    
    
    [Server]
    public void SetupMiniGame()
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
        AssignFinalPoints();
        Notifica();
        DispatchPoints();
        MyNetworkManager.manager.StoreLastResults(GetResults());
        Debug.LogWarning("Minigame End");
    }
    [Server]
    protected void DispatchPoints()
    {
        foreach (var kv in GetResults())
        {
            ulong playerId = kv.Key;
            int pontos = kv.Value;
            MyNetworkManager.manager.AddPoints(playerId, pontos);
            Debug.Log($"[MinigameController] Enviado {pontos} pontos para {playerId}");
        }
    }
    
    
    public abstract void UpdateScores();
    public abstract void AssignFinalPoints();
    public abstract Dictionary<ulong, int> GetResults();
    public abstract Dictionary<ulong, int> GetLiveScores();
    public void Atualizacao(ISubject subject){}
    
    public void Adicionar(IObserver observer) => _observers.Add(observer);
    public void Retira(IObserver observer)    => _observers.Remove(observer);
    public void Notifica()
    {
        foreach (var obs in _observers) 
            obs.Atualizacao(this);
    }
}