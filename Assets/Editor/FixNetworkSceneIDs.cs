using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Mirror;

/// <summary>
/// Utilitário para corrigir SceneIDs de objetos NetworkIdentity em cenas.
/// Este script força a atribuição de SceneIDs válidos para todos os objetos NetworkIdentity
/// que não possuem um SceneID válido.
/// </summary>
public class FixNetworkSceneIDs : EditorWindow
{
    [MenuItem("Tools/Mirror/Fix Network Scene IDs")]
    public static void ShowWindow()
    {
        GetWindow<FixNetworkSceneIDs>("Fix Network Scene IDs");
    }

    private Vector2 scrollPosition;
    private List<string> scenesToFix = new List<string>();
    private HashSet<string> scenesInBuild = new HashSet<string>();
    private bool isProcessing = false;
    private bool onlyCheckBuildScenes = true;
    private bool autoFixOnFind = false;
    private bool buildAfterFix = false;

    private void OnGUI()
    {
        GUILayout.Label("Correção de SceneIDs do Mirror", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Este utilitário encontra e corrige objetos NetworkIdentity sem SceneIDs válidos.\n\n" +
            "A solução mais simples é abrir cada cena problemática e salvá-la (Ctrl+S).\n\n" +
            "Este utilitário pode ajudar a identificar quais cenas precisam ser corrigidas.",
            MessageType.Info);

        GUILayout.Space(10);

        // Opção para verificar apenas cenas no Build Settings
        onlyCheckBuildScenes = EditorGUILayout.Toggle(
            new GUIContent("Verificar apenas cenas no Build Settings", 
                "Se marcado, verifica apenas as cenas configuradas nas Build Settings. " +
                "Se desmarcado, verifica todas as cenas do projeto."),
            onlyCheckBuildScenes);

        GUILayout.Space(5);

        // Opção para corrigir automaticamente
        autoFixOnFind = EditorGUILayout.Toggle(
            new GUIContent("Corrigir automaticamente ao buscar", 
                "Se marcado, corrige automaticamente todas as cenas encontradas ao buscar problemas. " +
                "Se desmarcado, apenas lista as cenas com problemas para correção manual."),
            autoFixOnFind);

        GUILayout.Space(5);

        // Opção para fazer build após correção
        buildAfterFix = EditorGUILayout.Toggle(
            new GUIContent("Fazer build automaticamente após correção", 
                "Se marcado, inicia o build automaticamente após a correção completa ser concluída."),
            buildAfterFix);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Buscar Cenas com Problemas", GUILayout.Height(30)))
        {
            FindProblematicScenes();
        }
        
        if (GUILayout.Button("Buscar e Corrigir Tudo", GUILayout.Height(30), GUILayout.Width(180)))
        {
            FindAndFixAll();
        }
        
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Botão para abrir e salvar todas as cenas (método mais confiável)
        EditorGUILayout.HelpBox(
            "Método mais confiável: Abre e salva todas as cenas, forçando o Unity a atribuir SceneIDs automaticamente.\n\n" +
            "⚠️ IMPORTANTE: Use este botão ANTES de fazer build para garantir que todas as cenas estejam corretas!",
            MessageType.Warning);

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🔄 Abrir e Salvar Todas as Cenas", GUILayout.Height(35)))
        {
            OpenAndSaveAllScenes();
        }
        
        if (GUILayout.Button("🔧 Forçar Correção Completa", GUILayout.Height(35), GUILayout.Width(200)))
        {
            ForceCompleteFix();
        }
        
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (scenesToFix.Count > 0)
        {
            GUILayout.Label($"Cenas com problemas encontradas: {scenesToFix.Count}", EditorStyles.boldLabel);
            
            if (scenesInBuild.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"ℹ️ {scenesInBuild.Count} cena(s) estão configuradas no Build Settings",
                    MessageType.Info);
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Ordenar: cenas no build primeiro
            var sortedScenes = scenesToFix.OrderByDescending(s => scenesInBuild.Contains(s)).ToList();
            
            foreach (string scenePath in sortedScenes)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool isInBuild = scenesInBuild.Contains(scenePath);
                string displayPath = scenePath;
                
                if (isInBuild)
                {
                    // Destacar cenas no build
                    GUI.color = new Color(1f, 0.8f, 0.4f); // Laranja/amarelo
                    displayPath = "🔨 " + scenePath;
                }
                
                EditorGUILayout.LabelField(displayPath, GUILayout.Width(400));
                GUI.color = Color.white;
                
                if (GUILayout.Button("Abrir e Corrigir", GUILayout.Width(120)))
                {
                    FixScene(scenePath);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Corrigir Todas as Cenas", GUILayout.Height(30)))
            {
                FixAllScenes();
            }
        }
        else if (!isProcessing)
        {
            EditorGUILayout.HelpBox("Nenhuma cena com problemas encontrada. Clique em 'Buscar Cenas com Problemas' para verificar.", MessageType.Info);
        }
    }

    private bool IsSceneInPackage(string scenePath)
    {
        // Verificar se a cena está em um pacote (Packages/...)
        // Cenas em pacotes são somente leitura e não devem ser modificadas
        return scenePath.StartsWith("Packages/");
    }

    private HashSet<string> GetScenesInBuildSettings()
    {
        HashSet<string> buildScenes = new HashSet<string>();
        
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
            {
                buildScenes.Add(scene.path);
            }
        }
        
        return buildScenes;
    }

    private void FindProblematicScenes()
    {
        isProcessing = true;
        scenesToFix.Clear();
        scenesInBuild.Clear();

        // Obter cenas no Build Settings
        scenesInBuild = GetScenesInBuildSettings();
        
        if (onlyCheckBuildScenes && scenesInBuild.Count == 0)
        {
            Debug.LogWarning("⚠️ Nenhuma cena está configurada no Build Settings!");
            isProcessing = false;
            return;
        }

        // Buscar cenas para verificar
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        
        int skippedPackages = 0;
        int skippedNotInBuild = 0;
        
        foreach (string guid in guids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            
            // Ignorar cenas em pacotes (somente leitura)
            if (IsSceneInPackage(scenePath))
            {
                skippedPackages++;
                continue;
            }
            
            // Se a opção estiver marcada, verificar apenas cenas no build
            if (onlyCheckBuildScenes && !scenesInBuild.Contains(scenePath))
            {
                skippedNotInBuild++;
                continue;
            }
            
            try
            {
                // Abrir a cena temporariamente para verificar
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                // Buscar todos os NetworkIdentities na cena
                NetworkIdentity[] identities = Resources.FindObjectsOfTypeAll<NetworkIdentity>()
                    .Where(identity => 
                        identity.gameObject.scene == scene &&
                        !EditorUtility.IsPersistent(identity.gameObject) &&
                        identity.sceneId == 0)
                    .ToArray();

                if (identities.Length > 0)
                {
                    scenesToFix.Add(scenePath);
                    string buildStatus = scenesInBuild.Contains(scenePath) ? " [NO BUILD]" : "";
                    Debug.Log($"⚠️ Cena '{scenePath}'{buildStatus} tem {identities.Length} objetos NetworkIdentity sem SceneID válido");
                }
            }
            catch (System.Exception e)
            {
                // Ignorar cenas que não podem ser abertas (ex: pacotes somente leitura)
                Debug.LogWarning($"⚠️ Não foi possível verificar a cena '{scenePath}': {e.Message}");
                skippedPackages++;
            }
        }

        isProcessing = false;
        
        if (skippedPackages > 0)
        {
            Debug.Log($"ℹ️ {skippedPackages} cenas em pacotes foram ignoradas (somente leitura)");
        }
        
        if (onlyCheckBuildScenes && skippedNotInBuild > 0)
        {
            Debug.Log($"ℹ️ {skippedNotInBuild} cenas fora do Build Settings foram ignoradas");
        }
        
        if (scenesToFix.Count == 0)
        {
            string scope = onlyCheckBuildScenes ? " no Build Settings" : "";
            Debug.Log($"✅ Nenhuma cena{scope} com problemas encontrada!");
        }
        else
        {
            int inBuildCount = scenesToFix.Count(s => scenesInBuild.Contains(s));
            string buildInfo = onlyCheckBuildScenes ? "" : $" ({inBuildCount} no Build Settings)";
            Debug.Log($"⚠️ Encontradas {scenesToFix.Count} cenas com problemas de SceneID{buildInfo}");
            
            // Se a opção de correção automática estiver marcada, corrigir todas
            if (autoFixOnFind)
            {
                Debug.Log("🔧 Iniciando correção automática...");
                FixAllScenes();
            }
        }
    }

    private void FindAndFixAll()
    {
        Debug.Log("🔍 Buscando e corrigindo todas as cenas...");
        FindProblematicScenes();
        
        // Se não corrigiu automaticamente (porque autoFixOnFind estava desmarcado),
        // corrigir agora mesmo assim
        if (scenesToFix.Count > 0 && !autoFixOnFind)
        {
            Debug.Log("🔧 Corrigindo todas as cenas encontradas...");
            FixAllScenes();
        }
    }

    private void FixScene(string scenePath)
    {
        try
        {
            // Verificar se a cena está em um pacote (não pode ser editada)
            if (IsSceneInPackage(scenePath))
            {
                Debug.LogWarning($"⚠️ Não é possível corrigir a cena '{scenePath}' porque ela está em um pacote somente leitura.");
                EditorUtility.DisplayDialog(
                    "Cena em Pacote Somente Leitura",
                    $"A cena '{scenePath}' está em um pacote somente leitura e não pode ser modificada.\n\n" +
                    "Cenas em pacotes não precisam ser corrigidas, pois são gerenciadas pelo próprio pacote.",
                    "OK");
                return;
            }
            
            Debug.Log($"🔧 Corrigindo cena: {scenePath}");
            
            // Abrir a cena
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            // Buscar todos os NetworkIdentities na cena
            NetworkIdentity[] identities = Resources.FindObjectsOfTypeAll<NetworkIdentity>()
                .Where(identity => 
                    identity.gameObject.scene == scene &&
                    !EditorUtility.IsPersistent(identity.gameObject))
                .ToArray();

            int fixedCount = 0;
            
            foreach (NetworkIdentity identity in identities)
            {
                // Forçar a atribuição de SceneID chamando SetupIDs
                // Isso irá chamar AssignSceneID internamente se necessário
                if (identity.sceneId == 0)
                {
                    // Usar reflection para chamar SetupIDs que é privado
                    var setupIDsMethod = typeof(NetworkIdentity).GetMethod("SetupIDs", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (setupIDsMethod != null)
                    {
                        setupIDsMethod.Invoke(identity, null);
                        fixedCount++;
                    }
                }
            }

            // Marcar a cena como modificada e salvar
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            
            Debug.Log($"✅ Cena '{scenePath}' corrigida! {fixedCount} objetos NetworkIdentity foram atualizados.");
            
            // Atualizar a lista
            FindProblematicScenes();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao corrigir cena '{scenePath}': {e.Message}");
        }
    }

    private void FixAllScenes()
    {
        if (scenesToFix.Count == 0)
            return;

        Debug.Log($"🔧 Iniciando correção de {scenesToFix.Count} cenas...");
        
        foreach (string scenePath in scenesToFix.ToList())
        {
            FixScene(scenePath);
        }
        
        Debug.Log("✅ Todas as cenas foram processadas!");
    }

    /// <summary>
    /// Método mais confiável: Abre e salva todas as cenas, forçando o Unity a atribuir SceneIDs automaticamente.
    /// Este é o método recomendado, pois simplesmente abre e salva cada cena, permitindo que o Unity
    /// processe e atribua os SceneIDs naturalmente através do OnValidate/SetupIDs.
    /// </summary>
    private void OpenAndSaveAllScenes()
    {
        // Obter lista de cenas para processar
        HashSet<string> scenesToProcess = new HashSet<string>();
        
        if (onlyCheckBuildScenes)
        {
            // Usar cenas do Build Settings
            scenesInBuild = GetScenesInBuildSettings();
            foreach (string scenePath in scenesInBuild)
            {
                if (!IsSceneInPackage(scenePath))
                {
                    scenesToProcess.Add(scenePath);
                }
            }
            
            if (scenesToProcess.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Nenhuma Cena no Build",
                    "Nenhuma cena está configurada no Build Settings ou todas estão em pacotes somente leitura.",
                    "OK");
                return;
            }
        }
        else
        {
            // Usar todas as cenas do projeto
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            foreach (string guid in guids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsSceneInPackage(scenePath))
                {
                    scenesToProcess.Add(scenePath);
                }
            }
        }

        if (scenesToProcess.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Nenhuma Cena Encontrada",
                "Nenhuma cena foi encontrada para processar.",
                "OK");
            return;
        }

        // Confirmar ação
        bool confirmed = EditorUtility.DisplayDialog(
            "Abrir e Salvar Todas as Cenas",
            $"Isso irá abrir e salvar {scenesToProcess.Count} cena(s).\n\n" +
            $"Isso força o Unity a atribuir SceneIDs automaticamente.\n\n" +
            $"Deseja continuar?",
            "Sim, Continuar",
            "Cancelar");

        if (!confirmed)
            return;

        Debug.Log($"🔄 Iniciando processo de abrir e salvar {scenesToProcess.Count} cena(s)...");
        
        int successCount = 0;
        int errorCount = 0;
        List<string> processedScenes = new List<string>(scenesToProcess);

        // Processar cada cena
        foreach (string scenePath in processedScenes)
        {
            try
            {
                Debug.Log($"📂 Abrindo cena: {scenePath}");
                
                // Abrir a cena
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                // Forçar o Unity a processar a cena (isso aciona OnValidate nos NetworkIdentities)
                // O Unity automaticamente atribui SceneIDs quando a cena é aberta e processada
                
                // Marcar a cena como modificada (mesmo que não tenha mudanças visíveis,
                // isso força o Unity a processar e salvar os SceneIDs)
                EditorSceneManager.MarkSceneDirty(scene);
                
                // Salvar a cena - isso força o Unity a persistir os SceneIDs
                bool saved = EditorSceneManager.SaveScene(scene);
                
                if (saved)
                {
                    successCount++;
                    Debug.Log($"✅ Cena '{scenePath}' salva com sucesso!");
                }
                else
                {
                    errorCount++;
                    Debug.LogWarning($"⚠️ Não foi possível salvar a cena '{scenePath}'");
                }
            }
            catch (System.Exception e)
            {
                errorCount++;
                Debug.LogError($"❌ Erro ao processar cena '{scenePath}': {e.Message}");
            }
        }

        // Resumo
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"✅ Processo concluído!");
        Debug.Log($"   → {successCount} cena(s) processada(s) com sucesso");
        if (errorCount > 0)
        {
            Debug.LogWarning($"   → {errorCount} cena(s) com erro");
        }
        Debug.Log("═══════════════════════════════════════════════════════");

        // Mostrar diálogo de conclusão
        if (errorCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Processo Concluído",
                $"Todas as {successCount} cena(s) foram processadas com sucesso!\n\n" +
                $"Os SceneIDs foram atribuídos automaticamente pelo Unity.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Processo Concluído com Avisos",
                $"{successCount} cena(s) processada(s) com sucesso.\n" +
                $"{errorCount} cena(s) tiveram problemas.\n\n" +
                $"Verifique o Console para mais detalhes.",
                "OK");
        }
    }

    /// <summary>
    /// Força correção completa de todas as cenas usando o mesmo método do pre-build hook.
    /// Este método é mais agressivo e garante que todos os SceneIDs sejam atribuídos.
    /// </summary>
    private void ForceCompleteFix()
    {
        // Obter todas as cenas do Build Settings
        var buildScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Where(s => !s.path.StartsWith("Packages/"))
            .Select(s => s.path)
            .ToList();

        if (buildScenes.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Nenhuma Cena no Build",
                "Nenhuma cena está configurada no Build Settings.",
                "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Forçar Correção Completa",
            $"Isso irá processar e corrigir {buildScenes.Count} cena(s) do Build Settings.\n\n" +
            $"Este método força a atribuição de SceneIDs de múltiplas formas.\n\n" +
            $"Deseja continuar?",
            "Sim, Continuar",
            "Cancelar");

        if (!confirmed)
            return;

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🔧 [ForceCompleteFix] Iniciando correção completa...");
        
        int totalFixed = 0;
        int totalScenes = 0;
        
        foreach (string scenePath in buildScenes)
        {
            try
            {
                totalScenes++;
                Debug.Log($"📂 [ForceCompleteFix] Processando cena {totalScenes}/{buildScenes.Count}: {scenePath}");
                
                // Abrir a cena
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                // Buscar todos os NetworkIdentities na cena
                NetworkIdentity[] identities = Resources.FindObjectsOfTypeAll<NetworkIdentity>()
                    .Where(identity => 
                        identity.gameObject.scene == scene &&
                        !EditorUtility.IsPersistent(identity.gameObject))
                    .ToArray();

                if (identities.Length == 0)
                {
                    Debug.Log($"ℹ️ [ForceCompleteFix] Cena '{scenePath}' não tem objetos NetworkIdentity.");
                    continue;
                }

                int fixedCount = 0;
                
                // Limpar o dicionário de sceneIds estático para evitar detecção de duplicatas
                var sceneIdsField = typeof(NetworkIdentity).GetField("sceneIds", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (sceneIdsField != null)
                {
                    var sceneIdsDict = sceneIdsField.GetValue(null) as System.Collections.IDictionary;
                    if (sceneIdsDict != null)
                    {
                        sceneIdsDict.Clear();
                    }
                }
                
                foreach (NetworkIdentity identity in identities)
                {
                    // Forçar correção: resetar sceneId para 0 e depois chamar SetupIDs
                    ulong oldSceneId = identity.sceneId;
                    bool needsFix = (oldSceneId == 0);
                    
                    // Verificar se é duplicata usando reflection para acessar o dicionário estático
                    if (!needsFix && sceneIdsField != null)
                    {
                        var sceneIdsDict = sceneIdsField.GetValue(null) as System.Collections.IDictionary;
                        if (sceneIdsDict != null && sceneIdsDict.Contains(oldSceneId))
                        {
                            var existing = sceneIdsDict[oldSceneId] as NetworkIdentity;
                            if (existing != null && !ReferenceEquals(existing, identity))
                            {
                                needsFix = true; // É duplicata!
                                Debug.Log($"⚠️ [ForceCompleteFix] Objeto '{identity.name}' tem sceneId duplicado: {oldSceneId:X}");
                            }
                        }
                    }
                    
                    if (needsFix || oldSceneId == 0)
                    {
                        // Resetar sceneId para forçar nova atribuição
                        var sceneIdField = typeof(NetworkIdentity).GetField("sceneId", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        
                        if (sceneIdField != null)
                        {
                            Undo.RecordObject(identity, "Reset SceneId");
                            sceneIdField.SetValue(identity, (ulong)0);
                        }
                        
                        // Método 1: Chamar SetupIDs via reflection
                        var setupIDsMethod = typeof(NetworkIdentity).GetMethod("SetupIDs", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (setupIDsMethod != null)
                        {
                            setupIDsMethod.Invoke(identity, null);
                        }
                        
                        // Método 2: Forçar OnValidate
                        var onValidateMethod = typeof(NetworkIdentity).GetMethod("OnValidate", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (onValidateMethod != null)
                        {
                            onValidateMethod.Invoke(identity, null);
                        }
                        
                        // Método 3: Marcar como dirty
                        EditorUtility.SetDirty(identity);
                        Undo.FlushUndoRecordObjects();
                        
                        // Verificar se foi corrigido
                        if (identity.sceneId != 0 && identity.sceneId != oldSceneId)
                        {
                            fixedCount++;
                            Debug.Log($"✅ [ForceCompleteFix] Objeto '{identity.name}' corrigido: {oldSceneId:X} -> {identity.sceneId:X}");
                        }
                    }
                }

                // Marcar e salvar a cena
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                
                if (saved)
                {
                    totalFixed += fixedCount;
                    Debug.Log($"✅ [ForceCompleteFix] Cena '{scenePath}' processada! {identities.Length} objetos NetworkIdentity verificados, {fixedCount} corrigido(s).");
                }
                else
                {
                    Debug.LogWarning($"⚠️ [ForceCompleteFix] Não foi possível salvar a cena '{scenePath}'");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [ForceCompleteFix] Erro ao processar cena '{scenePath}': {e.Message}\n{e.StackTrace}");
            }
        }
        
        // Forçar salvamento de assets
        AssetDatabase.SaveAssets();
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"✅ [ForceCompleteFix] Processo concluído!");
        Debug.Log($"   → {totalScenes} cena(s) processada(s)");
        Debug.Log($"   → {totalFixed} objeto(s) NetworkIdentity corrigido(s)");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        string message = $"Processo concluído!\n\n" +
            $"→ {totalScenes} cena(s) processada(s)\n" +
            $"→ {totalFixed} objeto(s) NetworkIdentity corrigido(s)";
        
        if (buildAfterFix)
        {
            EditorUtility.DisplayDialog(
                "Correção Completa Concluída",
                message + "\n\nIniciando build automaticamente...",
                "OK");
            
            // Iniciar build após um pequeno delay para garantir que tudo foi salvo
            EditorApplication.delayCall += () => {
                StartBuild();
            };
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Correção Completa Concluída",
                message + "\n\nAgora você pode tentar fazer o build novamente.",
                "OK");
        }
    }

    /// <summary>
    /// Inicia o build do projeto automaticamente.
    /// </summary>
    private void StartBuild()
    {
        Debug.Log("🚀 [FixNetworkSceneIDs] Iniciando build automaticamente...");
        
        // Obter as configurações de build atuais
        var buildPlayerOptions = new BuildPlayerOptions();
        
        // Obter cenas do Build Settings
        buildPlayerOptions.scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        
        // Obter target e location do último build (ou usar padrões)
        buildPlayerOptions.locationPathName = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.activeBuildTarget);
        if (string.IsNullOrEmpty(buildPlayerOptions.locationPathName))
        {
            // Se não houver localização salva, usar uma padrão
            string projectName = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.Application.dataPath);
            buildPlayerOptions.locationPathName = System.IO.Path.Combine(
                System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                "Builds",
                $"{projectName}_{EditorUserBuildSettings.activeBuildTarget}.exe");
        }
        
        buildPlayerOptions.target = EditorUserBuildSettings.activeBuildTarget;
        buildPlayerOptions.options = BuildOptions.None;
        
        // Iniciar o build
        try
        {
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"✅ [FixNetworkSceneIDs] Build concluído com sucesso! Tempo: {report.summary.totalTime.TotalSeconds:F2}s");
                EditorUtility.DisplayDialog(
                    "Build Concluído",
                    $"Build concluído com sucesso!\n\n" +
                    $"Tempo: {report.summary.totalTime.TotalSeconds:F2}s\n" +
                    $"Tamanho: {report.summary.totalSize / 1024 / 1024:F2} MB",
                    "OK");
            }
            else
            {
                Debug.LogError($"❌ [FixNetworkSceneIDs] Build falhou! Erros: {report.summary.totalErrors}");
                EditorUtility.DisplayDialog(
                    "Build Falhou",
                    $"O build falhou com {report.summary.totalErrors} erro(s).\n\n" +
                    $"Verifique o Console para mais detalhes.",
                    "OK");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [FixNetworkSceneIDs] Erro ao iniciar build: {e.Message}");
            EditorUtility.DisplayDialog(
                "Erro ao Iniciar Build",
                $"Não foi possível iniciar o build:\n\n{e.Message}",
                "OK");
        }
    }
}

/// <summary>
/// Pre-build hook que verifica e corrige SceneIDs automaticamente antes do build começar.
/// Isso garante que todas as cenas tenham SceneIDs válidos antes da validação de cenas executar.
/// </summary>
public class NetworkSceneIDPreBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000; // Executar ANTES de outros processadores (incluindo SceneValidationBuildProcessor)

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🔍 [NetworkSceneIDPreBuildProcessor] Verificando SceneIDs antes do build...");
        
        // Obter todas as cenas do Build Settings
        var buildScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Where(s => !s.path.StartsWith("Packages/"))
            .Select(s => s.path)
            .ToList();

        if (buildScenes.Count == 0)
        {
            Debug.Log("ℹ️ [NetworkSceneIDPreBuildProcessor] Nenhuma cena no Build Settings para verificar.");
            return;
        }

        Debug.Log($"📋 [NetworkSceneIDPreBuildProcessor] Verificando {buildScenes.Count} cena(s) do Build Settings...");
        
        // Processar TODAS as cenas do build, não apenas as com problemas
        // Isso garante que todos os SceneIDs estejam atribuídos
        int totalFixed = 0;
        int totalScenes = 0;
        
        foreach (string scenePath in buildScenes)
        {
            try
            {
                totalScenes++;
                Debug.Log($"📂 [NetworkSceneIDPreBuildProcessor] Processando cena {totalScenes}/{buildScenes.Count}: {scenePath}");
                
                // Abrir a cena
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                // Buscar todos os NetworkIdentities na cena
                NetworkIdentity[] identities = Resources.FindObjectsOfTypeAll<NetworkIdentity>()
                    .Where(identity => 
                        identity.gameObject.scene == scene &&
                        !EditorUtility.IsPersistent(identity.gameObject))
                    .ToArray();

                if (identities.Length == 0)
                {
                    Debug.Log($"ℹ️ [NetworkSceneIDPreBuildProcessor] Cena '{scenePath}' não tem objetos NetworkIdentity.");
                    continue;
                }

                // Limpar o dicionário de sceneIds estático para evitar detecção de duplicatas
                var sceneIdsField = typeof(NetworkIdentity).GetField("sceneIds", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (sceneIdsField != null)
                {
                    var sceneIdsDict = sceneIdsField.GetValue(null) as System.Collections.IDictionary;
                    if (sceneIdsDict != null)
                    {
                        sceneIdsDict.Clear();
                    }
                }
                
                int fixedCount = 0;
                int needsFixCount = 0;
                
                foreach (NetworkIdentity identity in identities)
                {
                    ulong oldSceneId = identity.sceneId;
                    bool needsFix = (oldSceneId == 0);
                    
                    // Verificar se é duplicata
                    if (!needsFix && sceneIdsField != null)
                    {
                        var sceneIdsDict = sceneIdsField.GetValue(null) as System.Collections.IDictionary;
                        if (sceneIdsDict != null && sceneIdsDict.Contains(oldSceneId))
                        {
                            var existing = sceneIdsDict[oldSceneId] as NetworkIdentity;
                            if (existing != null && !ReferenceEquals(existing, identity))
                            {
                                needsFix = true; // É duplicata!
                            }
                        }
                    }
                    
                    if (needsFix || oldSceneId == 0)
                    {
                        needsFixCount++;
                        
                        // Resetar sceneId para forçar nova atribuição
                        var sceneIdField = typeof(NetworkIdentity).GetField("sceneId", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        
                        if (sceneIdField != null)
                        {
                            Undo.RecordObject(identity, "Reset SceneId");
                            sceneIdField.SetValue(identity, (ulong)0);
                        }
                        
                        // Método 1: Chamar SetupIDs via reflection
                        var setupIDsMethod = typeof(NetworkIdentity).GetMethod("SetupIDs", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (setupIDsMethod != null)
                        {
                            setupIDsMethod.Invoke(identity, null);
                        }
                        
                        // Método 2: Forçar OnValidate
                        var onValidateMethod = typeof(NetworkIdentity).GetMethod("OnValidate", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (onValidateMethod != null)
                        {
                            onValidateMethod.Invoke(identity, null);
                        }
                        
                        // Método 3: Marcar como dirty
                        EditorUtility.SetDirty(identity);
                        Undo.FlushUndoRecordObjects();
                        
                        // Verificar se foi corrigido
                        if (identity.sceneId != 0 && identity.sceneId != oldSceneId)
                        {
                            fixedCount++;
                        }
                    }
                }

                if (needsFixCount > 0)
                {
                    Debug.Log($"🔧 [NetworkSceneIDPreBuildProcessor] Cena '{scenePath}': {needsFixCount} objeto(s) precisavam de correção, {fixedCount} corrigido(s).");
                    
                    // Marcar e salvar a cena
                    EditorSceneManager.MarkSceneDirty(scene);
                    bool saved = EditorSceneManager.SaveScene(scene);
                    
                    if (saved)
                    {
                        totalFixed += fixedCount;
                        Debug.Log($"✅ [NetworkSceneIDPreBuildProcessor] Cena '{scenePath}' salva com sucesso!");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [NetworkSceneIDPreBuildProcessor] Não foi possível salvar a cena '{scenePath}'");
                    }
                }
                else
                {
                    Debug.Log($"✅ [NetworkSceneIDPreBuildProcessor] Cena '{scenePath}' já está correta ({identities.Length} objetos NetworkIdentity com SceneIDs válidos).");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [NetworkSceneIDPreBuildProcessor] Erro ao processar cena '{scenePath}': {e.Message}\n{e.StackTrace}");
            }
        }
        
        // Forçar salvamento de assets
        AssetDatabase.SaveAssets();
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"✅ [NetworkSceneIDPreBuildProcessor] Processo concluído!");
        Debug.Log($"   → {totalScenes} cena(s) processada(s)");
        Debug.Log($"   → {totalFixed} objeto(s) NetworkIdentity corrigido(s)");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}

