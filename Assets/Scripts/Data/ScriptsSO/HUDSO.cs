using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database", menuName = "Player/HUDSO")]
public class HUDSO : ScriptableObject{

    public event Action EventOnShowColorChangePanel;

    public void ShowColorChangePanel()
    {
        this.EventOnShowColorChangePanel?.Invoke();
    }
    
    public event Action EventOnHideColorChangePanel;
    
    public void HideColorChangePanel(){
        this.EventOnHideColorChangePanel?.Invoke();
    }
    
    //
    public event Action<float> EventOnSetBlindAlpha;

    public void SetBlindAlpha(float alpha)
    {
        this.EventOnSetBlindAlpha?.Invoke(alpha);
    }

    public event Action<float> EventOnFreezeTimerUpdated;
    
    public void FreezeTimerUpdated(float value) {this.EventOnFreezeTimerUpdated?.Invoke(value);}
    public event Action<float> EventOnPrepareTimerUpdated;
    
    public void PrepareTimerUpdate(float value) {this.EventOnPrepareTimerUpdated?.Invoke(value);}
    
    public event Action<float> EventOnMatchTimerUpdated;
    
    public void MatchTimerUpdate(float value) {this.EventOnMatchTimerUpdated?.Invoke(value);}
    
    public event Action<float> EventOnVotingTimerUpdated;
    
    public void VotingTimerUpdate(float value) {this.EventOnVotingTimerUpdated?.Invoke(value);}
    
    public event Action<float> EventOnRespawnTimerUpdated;
    public void RespawnTimerUpdate(float value) {this.EventOnRespawnTimerUpdated?.Invoke(value);}
    
    public event Action<string> EventOnPotatoHolderUpdated;
    public void PotatoHolderUpdate(string value) { this.EventOnPotatoHolderUpdated?.Invoke(value); }
    //
    
    public event Action<string> EventOnGameOver;
    public void GameOver(string value) {this.EventOnGameOver?.Invoke(value);}    

    // Minigame Selection Panel
    public event Action EventOnShowMinigameSelectionPanel;
    public event Action EventOnHideMinigameSelectionPanel;

    public void ShowMinigameSelectionPanel()
    {
        this.EventOnShowMinigameSelectionPanel?.Invoke();
    }

    public void HideMinigameSelectionPanel()
    {
        this.EventOnHideMinigameSelectionPanel?.Invoke();
    }

    public event Action EventOnShowCustomizationPanel;
    public event Action EventOnHideCustomizationPanel;

    public void ShowCustomizationPanel()
    {
        this.EventOnShowCustomizationPanel?.Invoke();
    }

    public void HideCustomizationPanel()
    {
        this.EventOnHideCustomizationPanel?.Invoke();
    }

    public event Action EventOnShowFriendListPanel;
    public event Action EventOnHideFriendListPanel;

    public void ShowFriendListPanel()
    {
        this.EventOnShowFriendListPanel?.Invoke();
    }

    public void HideFriendListPanel()
    {
        this.EventOnHideFriendListPanel?.Invoke();
    }

    // Interaction hint (local-only)
    public event Action<string> EventOnShowInteractHint;
    public event Action EventOnHideInteractHint;

    public void ShowInteractHint(string message)
    {
        this.EventOnShowInteractHint?.Invoke(message);
    }

    public void HideInteractHint()
    {
        this.EventOnHideInteractHint?.Invoke();
    }

    // Menu Panel (Celular)
    public event Action EventOnShowMenuPanel;
    public event Action EventOnHideMenuPanel;

    public void ShowMenuPanel()
    {
        this.EventOnShowMenuPanel?.Invoke();
    }

    public void HideMenuPanel()
    {
        this.EventOnHideMenuPanel?.Invoke();
    }

    // Briefing Manager
    public event Action EventOnShowBriefing;
    public event Action EventOnHideBriefing;

    public void ShowBriefing()
    {
        this.EventOnShowBriefing?.Invoke();
    }

    public void HideBriefing()
    {
        this.EventOnHideBriefing?.Invoke();
    }

    // Voting Panel
    public event Action EventOnShowVotingPanel;
    public event Action EventOnHideVotingPanel;

    public void ShowVotingPanel()
    {
        this.EventOnShowVotingPanel?.Invoke();
    }

    public void HideVotingPanel()
    {
        this.EventOnHideVotingPanel?.Invoke();
    }

    // Cooldown UI (local-only)
    public event Action<float> EventOnPushCooldownUpdated;
    public event Action<float> EventOnThrowCooldownUpdated;

    public void UpdatePushCooldown(float normalizedValue)
    {
        this.EventOnPushCooldownUpdated?.Invoke(normalizedValue);
    }

    public void UpdateThrowCooldown(float normalizedValue)
    {
        this.EventOnThrowCooldownUpdated?.Invoke(normalizedValue);
    }

    // Ability Block UI (local-only)
    public event Action<bool> EventOnAbilityBlockUpdated;
    public void SetAbilityBlock(bool blocked)
    {
        this.EventOnAbilityBlockUpdated?.Invoke(blocked);
    }

    // Score UI (local-only)
    public event Action<int> EventOnScoreUpdated;

    public void UpdateScore(int scoreValue)
    {
        this.EventOnScoreUpdated?.Invoke(scoreValue);
    }

    // Spectator Mode (local-only)
    public event Action<bool> EventOnSpectatorModeChanged;

    public void SetSpectatorMode(bool isSpectating)
    {
        this.EventOnSpectatorModeChanged?.Invoke(isSpectating);
    }
    
    /// <summary>
    /// Limpa todos os eventos registrados para evitar referências a objetos destruídos.
    /// </summary>
    public void ClearAllEvents()
    {
        EventOnShowColorChangePanel = null;
        EventOnHideColorChangePanel = null;
        EventOnSetBlindAlpha = null;
        EventOnFreezeTimerUpdated = null;
        EventOnPrepareTimerUpdated = null;
        EventOnMatchTimerUpdated = null;
        EventOnVotingTimerUpdated = null;
        EventOnRespawnTimerUpdated = null;
        EventOnPotatoHolderUpdated = null;
        EventOnGameOver = null;
        EventOnShowMinigameSelectionPanel = null;
        EventOnHideMinigameSelectionPanel = null;
        EventOnShowCustomizationPanel = null;
        EventOnHideCustomizationPanel = null;
        EventOnShowFriendListPanel = null;
        EventOnHideFriendListPanel = null;
        EventOnShowInteractHint = null;
        EventOnHideInteractHint = null;
        EventOnShowMenuPanel = null;
        EventOnHideMenuPanel = null;
        EventOnShowBriefing = null;
        EventOnHideBriefing = null;
        EventOnShowVotingPanel = null;
        EventOnHideVotingPanel = null;
        EventOnPushCooldownUpdated = null;
        EventOnThrowCooldownUpdated = null;
        EventOnScoreUpdated = null;
        EventOnSpectatorModeChanged = null;
        
        Debug.Log("[HUDSO] All events cleared");
    }
    
    private void OnEnable()
    {
        #if UNITY_EDITOR
        ClearAllEvents();
        #endif
    }
}