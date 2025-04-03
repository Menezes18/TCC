using UnityEngine;
using UnityEditor;
using System.Linq;

public class ScoreboardEditorWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private GUIStyle headerStyle;
    private GUIStyle cellStyle;
    private Color headerColor = new Color(0.2f, 0.2f, 0.2f);
    private Color rowEvenColor = new Color(0.3f, 0.3f, 0.3f, 0.2f);
    private Color rowOddColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

    [MenuItem("Window/Game/Scoreboard")]
    public static void ShowWindow()
    {
        GetWindow<ScoreboardEditorWindow>("Scoreboard");
    }

    private void OnEnable()
    {
        // Configurar estilos
        headerStyle = new GUIStyle();
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.padding = new RectOffset(5, 5, 5, 5);

        cellStyle = new GUIStyle();
        cellStyle.alignment = TextAnchor.MiddleLeft;
        cellStyle.normal.textColor = Color.white;
        cellStyle.padding = new RectOffset(5, 5, 5, 5);
    }

    private void OnGUI()
    {
        var networkManager = MyNetworkManager.manager;
        if (networkManager == null)
        {
            EditorGUILayout.HelpBox("Network Manager não encontrado na cena.", MessageType.Warning);
            return;
        }

        // Título
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Game Scoreboard", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Status do Servidor
        string serverStatus = networkManager.isNetworkActive ? "Online" : "Offline";
        EditorGUILayout.LabelField($"Server Status: {serverStatus}");
        EditorGUILayout.LabelField($"Connected Players: {networkManager.allClients.Count}");
        EditorGUILayout.Space(10);

        // Cabeçalho da tabela
        DrawTableHeader();

        // Área de rolagem para a lista de jogadores
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Lista de jogadores
        if (networkManager.scoreboard != null && networkManager.scoreboard.players != null)
        {
            var sortedPlayers = networkManager.scoreboard.players.OrderByDescending(p => p.points).ToList();
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                DrawPlayerRow(sortedPlayers[i], i);
            }
        }

        EditorGUILayout.EndScrollView();

        // Auto-refresh da janela
        Repaint();
    }

    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        var headerRect = EditorGUILayout.GetControlRect();
        EditorGUI.DrawRect(headerRect, headerColor);
        
        float rankWidth = 50;
        float nameWidth = position.width * 0.4f;
        float steamIDWidth = position.width * 0.3f;
        float pointsWidth = position.width * 0.2f;

        headerRect.width = rankWidth;
        EditorGUI.LabelField(headerRect, "Rank", headerStyle);
        
        headerRect.x += rankWidth;
        headerRect.width = nameWidth;
        EditorGUI.LabelField(headerRect, "Player Name", headerStyle);
        
        headerRect.x += nameWidth;
        headerRect.width = steamIDWidth;
        EditorGUI.LabelField(headerRect, "Steam ID", headerStyle);
        
        headerRect.x += steamIDWidth;
        headerRect.width = pointsWidth;
        EditorGUI.LabelField(headerRect, "Points", headerStyle);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPlayerRow(PlayerData player, int index)
    {
        var rowRect = EditorGUILayout.GetControlRect(false, 25);
        EditorGUI.DrawRect(rowRect, index % 2 == 0 ? rowEvenColor : rowOddColor);

        float rankWidth = 50;
        float nameWidth = position.width * 0.4f;
        float steamIDWidth = position.width * 0.3f;
        float pointsWidth = position.width * 0.2f;

        // Rank
        var cellRect = new Rect(rowRect.x, rowRect.y, rankWidth, rowRect.height);
        EditorGUI.LabelField(cellRect, (index + 1).ToString(), cellStyle);

        // Player Name
        cellRect = new Rect(rowRect.x + rankWidth, rowRect.y, nameWidth, rowRect.height);
        EditorGUI.LabelField(cellRect, player.playerName, cellStyle);

        // Steam ID
        cellRect = new Rect(rowRect.x + rankWidth + nameWidth, rowRect.y, steamIDWidth, rowRect.height);
        EditorGUI.LabelField(cellRect, player.steamID.ToString(), cellStyle);

        // Points
        cellRect = new Rect(rowRect.x + rankWidth + nameWidth + steamIDWidth, rowRect.y, pointsWidth, rowRect.height);
        EditorGUI.LabelField(cellRect, player.points.ToString(), cellStyle);
    }
}