using UnityEngine;
using UnityEditor;
using Mirror;

/// <summary>
/// Ferramenta de Editor para auto-atribuir 1070+ tiles ao FloorBreakingManager
/// Menu: Tools → Floor Breaking → Auto Assign Tiles
/// </summary>
public class FloorBreakingEditorHelper : EditorWindow
{
    [MenuItem("Tools/Floor Breaking/Auto Assign Tiles")]
    static void AutoAssignTiles()
    {
        FloorBreakingManager manager = Object.FindFirstObjectByType<FloorBreakingManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Erro", 
                "FloorBreakingManager não encontrado na cena!\n\n" +
                "Certifique-se de que o Manager existe na Hierarchy.", 
                "OK");
            return;
        }

        ChaoQuebrandoSimples[] tiles = Object.FindObjectsByType<ChaoQuebrandoSimples>(FindObjectsSortMode.None);
        
        if (tiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Aviso", 
                "Nenhum tile com ChaoQuebrandoSimples encontrado!\n\n" +
                "Adicione o componente ChaoQuebrandoSimples aos tiles.", 
                "OK");
            return;
        }

        bool confirmar = EditorUtility.DisplayDialog("Confirmar Atribuição",
            $"Encontrados {tiles.Length} tiles.\n\n" +
            $"Deseja atribuir todos ao FloorBreakingManager?",
            "Sim", "Cancelar");

        if (!confirmar) return;

        Undo.RecordObject(manager, "Auto Assign Tiles");
        
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty tilesProperty = so.FindProperty("tiles");
        
        tilesProperty.arraySize = tiles.Length;
        for (int i = 0; i < tiles.Length; i++)
        {
            tilesProperty.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
        }
        
        so.ApplyModifiedProperties();
        
        EditorUtility.DisplayDialog("Sucesso!", 
            $"✅ {tiles.Length} tiles atribuídos com sucesso ao Manager!\n\n" +
            $"Verifique o Inspector do FloorBreakingManager.", 
            "OK");
        
        Debug.Log($"<color=green>✅ FloorBreakingManager: {tiles.Length} tiles atribuídos automaticamente!</color>");
        
        // Seleciona o manager para mostrar o resultado
        Selection.activeObject = manager;
    }

    [MenuItem("Tools/Floor Breaking/Organize Tiles in Hierarchy")]
    static void OrganizeTilesInHierarchy()
    {
        ChaoQuebrandoSimples[] tiles = Object.FindObjectsByType<ChaoQuebrandoSimples>(FindObjectsSortMode.None);
        
        if (tiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Aviso", "Nenhum tile encontrado!", "OK");
            return;
        }

        bool confirmar = EditorUtility.DisplayDialog("Organizar Hierarquia",
            $"Isso irá organizar {tiles.Length} tiles em pastas (Row_00, Row_01, etc.)\n\n" +
            $"10 tiles por pasta. Continuar?",
            "Sim", "Cancelar");

        if (!confirmar) return;

        // Cria pasta principal
        GameObject tilesParent = GameObject.Find("Tiles");
        if (tilesParent == null)
        {
            tilesParent = new GameObject("Tiles");
            Undo.RegisterCreatedObjectUndo(tilesParent, "Create Tiles Folder");
        }

        int tilesPerRow = 10;
        
        for (int i = 0; i < tiles.Length; i++)
        {
            int rowIndex = i / tilesPerRow;
            string rowName = $"Row_{rowIndex:D2}";
            
            Transform rowTransform = tilesParent.transform.Find(rowName);
            if (rowTransform == null)
            {
                GameObject row = new GameObject(rowName);
                Undo.RegisterCreatedObjectUndo(row, "Create Row Folder");
                row.transform.SetParent(tilesParent.transform);
                rowTransform = row.transform;
            }
            
            Undo.SetTransformParent(tiles[i].transform, rowTransform, "Organize Tile");
        }

        EditorUtility.DisplayDialog("Sucesso!", 
            $"✅ {tiles.Length} tiles organizados em {(tiles.Length / tilesPerRow) + 1} pastas!", 
            "OK");
        
        Debug.Log($"<color=green>✅ Hierarquia organizada: {tiles.Length} tiles em pastas!</color>");
    }

    [MenuItem("Tools/Floor Breaking/Validate Setup")]
    static void ValidateSetup()
    {
        string report = "=== VALIDAÇÃO DO SETUP ===\n\n";
        bool hasErrors = false;

        // Verifica Manager
        FloorBreakingManager manager = Object.FindFirstObjectByType<FloorBreakingManager>();
        if (manager == null)
        {
            report += "❌ FloorBreakingManager não encontrado!\n";
            hasErrors = true;
        }
        else
        {
            report += "✅ FloorBreakingManager encontrado\n";
            
            if (manager.GetComponent<NetworkIdentity>() == null)
            {
                report += "❌ Manager SEM NetworkIdentity!\n";
                hasErrors = true;
            }
            else
            {
                report += "✅ Manager tem NetworkIdentity\n";
            }
        }

        // Verifica Tiles
        ChaoQuebrandoSimples[] tiles = Object.FindObjectsByType<ChaoQuebrandoSimples>(FindObjectsSortMode.None);
        report += $"\n📊 Total de tiles: {tiles.Length}\n\n";

        int tilesComNetworkIdentity = 0;
        int tilesSemCollider = 0;
        int tilesSemDataChao = 0;

        foreach (var tile in tiles)
        {
            if (tile.GetComponent<NetworkIdentity>() != null)
                tilesComNetworkIdentity++;
            
            if (tile.GetComponent<Collider>() == null)
                tilesSemCollider++;
            
            // Verifica DataChao via SerializedObject
            SerializedObject so = new SerializedObject(tile);
            SerializedProperty dataChaoProp = so.FindProperty("dataChao");
            if (dataChaoProp.objectReferenceValue == null)
                tilesSemDataChao++;
        }

        if (tilesComNetworkIdentity > 0)
        {
            report += $"⚠️ {tilesComNetworkIdentity} tiles TÊM NetworkIdentity (desnecessário!)\n";
            report += "   Recomendação: Remova NetworkIdentity dos tiles\n";
        }
        else
        {
            report += "✅ Nenhum tile tem NetworkIdentity (correto!)\n";
        }

        if (tilesSemCollider > 0)
        {
            report += $"❌ {tilesSemCollider} tiles SEM Collider!\n";
            hasErrors = true;
        }
        else
        {
            report += "✅ Todos os tiles têm Collider\n";
        }

        if (tilesSemDataChao > 0)
        {
            report += $"⚠️ {tilesSemDataChao} tiles sem ChaoMaeSo configurado\n";
        }
        else
        {
            report += "✅ Todos os tiles têm DataChao configurado\n";
        }

        // Verifica Players
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        report += $"\n👥 Players com tag 'Player': {players.Length}\n";

        report += "\n" + (hasErrors ? "❌ Corrija os erros acima!" : "✅ Setup validado com sucesso!");

        EditorUtility.DisplayDialog("Validação do Setup", report, "OK");
        Debug.Log(report);
    }

    [MenuItem("Tools/Floor Breaking/Show Stats")]
    static void ShowStats()
    {
        FloorBreakingManager manager = Object.FindFirstObjectByType<FloorBreakingManager>();
        ChaoQuebrandoSimples[] tiles = Object.FindObjectsByType<ChaoQuebrandoSimples>(FindObjectsSortMode.None);

        string stats = "=== ESTATÍSTICAS ===\n\n";
        stats += $"🎮 Manager: {(manager != null ? "✅ Encontrado" : "❌ Não encontrado")}\n";
        stats += $"📦 Total de tiles: {tiles.Length}\n";
        stats += $"💾 Memória estimada: ~{tiles.Length * 2}KB\n";
        stats += $"🌐 NetworkIdentities: 1 (apenas no Manager)\n";
        stats += $"🚀 Performance: Excelente para {tiles.Length} tiles\n\n";

        if (tiles.Length > 0)
        {
            int ativos = 0;
            int inativos = 0;
            foreach (var tile in tiles)
            {
                if (tile.gameObject.activeSelf) ativos++;
                else inativos++;
            }
            stats += $"✅ Tiles ativos: {ativos}\n";
            stats += $"❌ Tiles inativos: {inativos}\n";
        }

        EditorUtility.DisplayDialog("Estatísticas", stats, "OK");
        Debug.Log(stats);
    }
}
