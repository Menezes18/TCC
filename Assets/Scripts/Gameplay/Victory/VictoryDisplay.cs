using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class VictoryDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image colorIndicator;
    [SerializeField] private GameObject displayPanel;
    
    [Header("Settings")]
    [SerializeField] private bool autoUpdateOnStart = true;
    [SerializeField] private float updateDelay = 1f;
    
    private VictoryPlayerData _currentWinnerData;
    
    private void Start()
    {
        
    }
    
    private void OnDestroy()
    {
        if (VictoryDataManager.Instance != null)
        {
            VictoryDataManager.Instance.OnWinnerDataReady -= OnWinnerDataReceived;
        }
    }
    

    private void OnWinnerDataReceived(VictoryPlayerData winnerData)
    {
        if (winnerData != null)
        {
            UpdateDisplay(winnerData);
        }
    }
    
   
    public void UpdateDisplay()
    {
        if (VictoryDataManager.Instance == null)
        {
            Debug.LogWarning("⚠️ [VictoryDisplay] VictoryDataManager.Instance é null. Aguardando...");
            Invoke(nameof(UpdateDisplay), 2f);
            return;
        }
        
        var winnerData = VictoryDataManager.Instance.GetWinnerData();
        if (winnerData != null)
        {
            UpdateDisplay(winnerData);
        }
        else
        {
            Debug.LogWarning("⚠️ [VictoryDisplay] Dados do vencedor não disponíveis ainda. Tentando novamente...");
            Invoke(nameof(UpdateDisplay), 1f);
        }
    }
    
    
    public void UpdateDisplay(VictoryPlayerData playerData)
    {
        if (playerData == null)
        {
            Debug.LogWarning("⚠️ [VictoryDisplay] Tentativa de atualizar display com dados nulos");
            return;
        }
        
        if (playerData.steamId == 0 || string.IsNullOrWhiteSpace(playerData.playerName))
        {
            Debug.LogWarning($"⚠️ [VictoryDisplay] Dados de jogador inválidos: SteamID={playerData.steamId}, Nome='{playerData.playerName}'");
            return;
        }
        
        _currentWinnerData = playerData;
        
        Debug.Log($"🖼️ [VictoryDisplay] Atualizando display:");
        Debug.Log($"   → Nome: {playerData.playerName}");
        Debug.Log($"   → Score: {playerData.finalScore}");
        Debug.Log($"   → Cor: {playerData.playerColor}");
        
        if (playerNameText != null)
        {
            playerNameText.text = playerData.playerName;
            Debug.Log($"   ✅ Nome aplicado à UI: '{playerData.playerName}'");
        }
        else
        {
            Debug.LogWarning("   ⚠️ playerNameText é NULL - configure no Inspector");
        }
        
        if (scoreText != null)
        {
            scoreText.text = $"{playerData.finalScore} pontos";
            Debug.Log($"   ✅ Score aplicado à UI: {playerData.finalScore}");
        }
        else
        {
            Debug.LogWarning("   ⚠️ scoreText é NULL - configure no Inspector");
        }
        
        if (colorIndicator != null)
        {
            colorIndicator.color = playerData.playerColor;
            Debug.Log($"   ✅ Cor aplicada à UI: {playerData.playerColor}");
        }
        else
        {
            Debug.LogWarning("   ⚠️ colorIndicator é NULL - configure no Inspector");
        }
        
        if (displayPanel != null && !displayPanel.activeSelf)
        {
            displayPanel.SetActive(true);
            Debug.Log("   ✅ Display panel ativado");
        }
        
        Debug.Log($"✅ [VictoryDisplay] Display atualizado com sucesso para: {playerData.playerName}");
    }
    

    public void HideDisplay()
    {
        if (displayPanel != null)
        {
            displayPanel.SetActive(false);
        }
    }
    

    public void ShowDisplay()
    {
        if (displayPanel != null)
        {
            displayPanel.SetActive(true);
        }
        
        
    }
    
    
    public void ShowDisplay(VictoryPlayerData playerData)
    {
        Debug.Log($"═══ [VictoryDisplay] ShowDisplay chamado ═══");
        
        if (playerData == null)
        {
            Debug.LogWarning("⚠️ [VictoryDisplay] ShowDisplay chamado com dados nulos");
            HideDisplay();
            return;
        }
        
        if (playerData.steamId == 0)
        {
            Debug.LogWarning($"⚠️ [VictoryDisplay] Jogador com SteamID inválido (0): '{playerData.playerName}'");
            HideDisplay();
            return;
        }
        
        Debug.Log($"📺 [VictoryDisplay] Exibindo jogador:");
        Debug.Log($"   → Nome: {playerData.playerName}");
        Debug.Log($"   → SteamID: {playerData.steamId}");
        Debug.Log($"   → Score: {playerData.finalScore}");
        
        if (displayPanel != null)
        {
            displayPanel.SetActive(true);
            Debug.Log("   ✅ Display panel ativado");
        }
        else
        {
            Debug.LogWarning("   ⚠️ displayPanel é NULL - configure no Inspector");
        }

        UpdateDisplay(playerData);
        
        Debug.Log($"✅ [VictoryDisplay] Display configurado com sucesso para: {playerData.playerName}");

    }
    

}

