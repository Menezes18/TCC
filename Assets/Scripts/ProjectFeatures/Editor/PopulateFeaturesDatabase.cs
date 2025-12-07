using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ProjectFeatures.Editor
{
    /// <summary>
    /// Popula o database com todas as features do projeto automaticamente.
    /// Menu: Window → TCC → Populate Features Database
    /// </summary>
    public class PopulateFeaturesDatabase : EditorWindow
    {
        private FeaturesDatabase database;
        private Vector2 scrollPosition;
        private bool createTestFeatures = true;

        [MenuItem("Window/TCC/Populate Features Database")]
        public static void ShowWindow()
        {
            var window = GetWindow<PopulateFeaturesDatabase>("Populate Features");
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Header
            EditorGUILayout.BeginVertical("Box");
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🚀 Populate Features Database", titleStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Database selection
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("💾 Database", EditorStyles.boldLabel);

            database = (FeaturesDatabase)EditorGUILayout.ObjectField("Features Database:", database, typeof(FeaturesDatabase), false);

            if (database == null)
            {
                EditorGUILayout.HelpBox("Selecione um database ou crie um novo.", MessageType.Warning);

                if (GUILayout.Button("🔍 Buscar Database"))
                {
                    FindDatabase();
                }

                if (GUILayout.Button("➕ Criar Novo Database"))
                {
                    CreateNewDatabase();
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Database atual tem {database.features.Count} features.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Info
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("📋 O que será criado:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• 15 Features de Programação");
            EditorGUILayout.LabelField("• 10 Features de Mecânicas");
            EditorGUILayout.LabelField("• 5 Features de Arte");
            EditorGUILayout.LabelField("• 3 Features de VFX");
            EditorGUILayout.LabelField("• 8 Features Gerais");
            EditorGUILayout.LabelField("• 3 Features de Ferramentas");
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Total: ~40 features!", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Action button
            EditorGUI.BeginDisabledGroup(database == null);

            if (GUILayout.Button("🚀 Adicionar Todas as Features Automaticamente", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirmar",
                    $"Isso criará ~40 features no database.\n\nContinuar?",
                    "Sim, Criar!", "Cancelar"))
                {
                    PopulateDatabase();
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }

        private void FindDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:FeaturesDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<FeaturesDatabase>(path);
                Debug.Log($"[Populate] Database encontrado: {path}");
            }
            else
            {
                EditorUtility.DisplayDialog("Não encontrado", "Nenhum FeaturesDatabase encontrado no projeto.", "OK");
            }
        }

        private void CreateNewDatabase()
        {
            var db = ScriptableObject.CreateInstance<FeaturesDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/FeaturesDatabase.asset");
            AssetDatabase.SaveAssets();
            database = db;
            Debug.Log("[Populate] Database criado: Assets/FeaturesDatabase.asset");
            EditorUtility.DisplayDialog("Sucesso", "Database criado em Assets/FeaturesDatabase.asset", "OK");
        }

        private void PopulateDatabase()
        {
            Debug.Log("[Populate] ========== Iniciando população do database ==========");

            // Garante pasta Features
            if (!AssetDatabase.IsValidFolder("Assets/Features"))
            {
                AssetDatabase.CreateFolder("Assets", "Features");
            }

            int count = 0;

            // Cria todas as features
            count += CreateProgramacaoFeatures();
            count += CreateMecanicaFeatures();
            count += CreateArteFeatures();
            count += CreateVFXFeatures();
            count += CreateGeralFeatures();
            count += CreateFerramentasFeatures();

            // Salva
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Populate] ========== {count} features criadas! ==========");

            EditorUtility.DisplayDialog(
                "Sucesso!",
                $"{count} features adicionadas ao database!\n\n" +
                "Abra Window → TCC → Features Manager para ver todas.",
                "OK"
            );
        }

        private int CreateProgramacaoFeatures()
        {
            var features = new[]
            {
                ("Sistema de Multiplayer P2P com Mirror", "Sistema de Rede", "Gerencia conexão P2P, sincronização de players, cenas multiplayer e replicação de estado usando Mirror Networking.", "Mirror, Networking, Multiplayer, P2P"),
                ("Integração com Steamworks", "Sistema", "Integração completa com Steam incluindo lobbies, lista de amigos, avatares e convites para partidas.", "Steam, Steamworks, Social"),
                ("Sistema de Votação de Próximo Minigame", "Sistema", "Sistema de votação server-authoritative onde jogadores votam entre 2-3 minigames. Suporta empates, desconexões e sincronização em rede.", "Votação, Minigame, Mirror"),
                ("Scene Transition Manager", "Manager", "Gerencia transições entre cenas de forma sincronizada em multiplayer, com loading screens e validações de carregamento.", "Scenes, Loading, Network"),
                ("Match Manager", "Manager", "Controla o fluxo da partida, incluindo início, fim, pontuação e transições entre minigames.", "Match, Gameplay, Flow"),
                ("Chat Manager", "Sistema de Comunicação", "Sistema de chat em tempo real sincronizado via Mirror para comunicação entre jogadores.", "Chat, Communication, Mirror"),
                ("Audio Manager", "Manager", "Gerenciamento centralizado de música, efeitos sonoros e mixagem com transições suaves.", "Audio, Sound, Music, SFX"),
                ("UI Manager", "Manager", "Gerenciador central de todas as interfaces do jogo, incluindo menus, HUD e popups.", "UI, Interface, Menu"),
                ("Sistema de Customização de Personagens", "Sistema", "Permite customização visual de personagens com salvamento de preferências e sincronização multiplayer.", "Customização, Characters, Save"),
                ("HUD Manager", "UI", "Interface in-game mostrando pontuação, timer, posição dos jogadores e informações relevantes em tempo real.", "HUD, UI, Gameplay"),
                ("Popup Manager", "UI", "Gerenciador de popups modais para confirmações, avisos e mensagens ao jogador.", "Popup, UI, Dialog"),
                ("Lobby Controller", "Sistema", "Controla lobby multiplayer, status ready dos jogadores e início de partida sincronizado.", "Lobby, Multiplayer, Party"),
                ("Spectator Manager", "Sistema", "Permite jogadores eliminados assistirem a partida em andamento como espectadores.", "Spectator, Gameplay"),
                ("Victory Podium Manager", "Sistema", "Gerencia tela de vitória com pódio animado mostrando top 3 jogadores e estatísticas finais.", "Victory, Podium, Results"),
                ("Sistema de Ranking de Contribuições GitHub", "Sistema", "Exibe ranking de contribuidores do projeto buscando dados da API do GitHub com fotos e estatísticas.", "GitHub, API, Integration")
            };

            int count = 0;
            foreach (var (titulo, tipo, descricao, tags) in features)
            {
                CreateFeature(titulo, FeatureCategory.Programacao, tipo, descricao, tags);
                count++;
            }

            return count;
        }

        private int CreateMecanicaFeatures()
        {
            var features = new[]
            {
                ("Sistema de Catálogo de Minigames", "Sistema", "Gerencia todos os minigames disponíveis, suas configurações e rotação durante a partida.", "Minigame, Catalog"),
                ("Minigame Rotation State", "Sistema", "Controla quais minigames já foram jogados na partida para evitar repetições.", "Minigame, Rotation"),
                ("Minigame - Floor Breaking", "Minigame", "Minigame onde jogadores precisam quebrar pisos antes de cair. Último sobrevivente ganha.", "Minigame, Competitivo"),
                ("Minigame - Kart Racing", "Minigame", "Corrida de kart com controles de veículo, física e sistema de colisões.", "Minigame, Racing, Vehicle"),
                ("Briefing Manager", "Sistema", "Exibe instruções e regras de cada minigame antes de começar com animações e timer.", "Briefing, Tutorial, Minigame"),
                ("Sistema de Combate", "Sistema", "Mecânicas de ataque, defesa e interações de combate entre jogadores.", "Combat, Fight, Gameplay"),
                ("Sistema de Movimento do Jogador", "Controles", "Sistema de movimento com controles responsivos, física e sincronização multiplayer.", "Movement, Player, Controls"),
                ("Sistema de Perigos/Hazards", "Sistema", "Obstáculos e perigos dinâmicos que afetam os jogadores durante os minigames.", "Hazards, Obstacles, Gameplay"),
                ("Sistema de Pontuação", "Sistema", "Calcula e distribui pontos baseado em performance, posição e objetivos dos minigames.", "Score, Points, Ranking"),
                ("Sistema de Resultados de Minigame", "Sistema", "Exibe resultados após cada minigame com rankings, pontos ganhos e estatísticas.", "Results, Scoreboard, Minigame")
            };

            int count = 0;
            foreach (var (titulo, tipo, descricao, tags) in features)
            {
                CreateFeature(titulo, FeatureCategory.Mecanica, tipo, descricao, tags);
                count++;
            }

            return count;
        }

        private int CreateArteFeatures()
        {
            var features = new[]
            {
                ("Modelagem 3D dos Personagens Jogáveis", "Modelagem 3D", "Criação de personagens 3D estilizados com rigging e preparação para animações.", "Characters, 3D, Modeling"),
                ("Sistema de Attachments de Customização", "Sistema Visual", "Sistema de anexos visuais para customização de personagens (chapéus, acessórios, cores).", "Customization, Visuals, Art"),
                ("Modelagem do Estádio e Ambiente", "Cenário", "Criação do cenário principal do estádio com torcida animada e elementos visuais.", "Environment, Stadium, 3D"),
                ("Design Visual de Menus e Interface", "UI/UX", "Criação de assets visuais, ícones e layouts para todos os menus e interfaces do jogo.", "UI, Design, Graphics"),
                ("Cenários dos Minigames", "Modelagem 3D", "Criação de cenários únicos para cada minigame (pistas de corrida, arenas, etc).", "Scenarios, Minigame, 3D")
            };

            int count = 0;
            foreach (var (titulo, tipo, descricao, tags) in features)
            {
                CreateFeature(titulo, FeatureCategory.Arte, tipo, descricao, tags);
                count++;
            }

            return count;
        }

        private int CreateVFXFeatures()
        {
            var features = new[]
            {
                ("Sistema de Partículas e Efeitos Visuais", "Sistema", "Efeitos de partículas para impactos, habilidades, itens e feedback visual de ações.", "Particles, VFX, Effects"),
                ("Sistema de Bounce Otimizado", "Efeito", "Sistema otimizado de bounce/quique com efeitos visuais e físicas responsivas.", "Bounce, Physics, VFX"),
                ("Animação de Torcida no Estádio", "Animação", "Animação procedural de torcida fazendo ola no estádio para ambientação.", "Animation, Stadium, Ambient")
            };

            int count = 0;
            foreach (var (titulo, tipo, descricao, tags) in features)
            {
                CreateFeature(titulo, FeatureCategory.VFX, tipo, descricao, tags);
                count++;
            }

            return count;
        }

        private int CreateGeralFeatures()
        {
            var features = new[]
            {
                ("Menu Principal do Jogo", "UI", "Menu inicial com opções de jogar solo, multiplayer, configurações, créditos e sair.", "Menu, UI, Main"),
                ("State Manager de Menus", "Sistema", "Gerencia estados e transições entre diferentes telas e menus do jogo.", "State, Menu, UI"),
                ("Menu de Configurações", "UI", "Tela de configurações com opções de audio, vídeo, controles e acessibilidade.", "Settings, Config, UI"),
                ("Party Menu UI Manager", "UI", "Interface de gerenciamento de party/grupo com lista de jogadores, ready status e chat.", "Party, Lobby, UI"),
                ("Inspect Manager", "UI", "Permite jogadores inspecionarem customizações de outros personagens no lobby.", "Inspect, Preview, UI"),
                ("Hint Manager", "UI", "Exibe dicas e tutoriais contextuais durante o jogo para auxiliar novos jogadores.", "Hints, Tutorial, Help"),
                ("UI Input Manager", "Sistema", "Gerencia input de UI com suporte para teclado, mouse e gamepad.", "Input, Controls, UI"),
                ("Sistema de Exibição de Features do Projeto", "Ferramenta", "Sistema que documenta e exibe todas as funcionalidades desenvolvidas com filtros por categoria.", "Documentation, TCC, Features")
            };

            int count = 0;
            foreach (var (titulo, tipo, descricao, tags) in features)
            {
                CreateFeature(titulo, FeatureCategory.Geral, tipo, descricao, tags);
                count++;
            }

            return count;
        }

        private int CreateFerramentasFeatures()
        {
            var features = new[]
            {
                ("Editor de Níveis com Tiles Hexagonais", "Ferramenta", "Editor customizado para criar níveis usando sistema de tiles hexagonais.", "Editor, Tools, LevelDesign"),
                ("Editor de Configurações de Minigames", "Ferramenta", "Janela de editor customizada para configurar parâmetros de minigames facilmente.", "Editor, Tools, Minigame"),
                ("Cast Visualizer (Debug)", "Ferramenta", "Ferramenta de debug para visualizar raycasts e colisões durante desenvolvimento.", "Debug, Tools, Raycast")
            };

            int count = 0;
            foreach (var (titulo, tipo, descricao, tags) in features)
            {
                CreateFeature(titulo, FeatureCategory.Programacao, tipo, descricao, tags);
                count++;
            }

            return count;
        }

        private void CreateFeature(string titulo, FeatureCategory categoria, string tipo, string descricao, string tagsString)
        {
            // Cria o FeatureEntry
            FeatureEntry entry = ScriptableObject.CreateInstance<FeatureEntry>();
            entry.titulo = titulo;
            entry.categoria = categoria;
            entry.tipo = tipo;
            entry.descricaoCurta = descricao;
            entry.responsavel = "Equipe 100 Ideias"; // Você pode mudar depois
            entry.status = FeatureStatus.Concluido;

            // Parse tags
            if (!string.IsNullOrEmpty(tagsString))
            {
                entry.tags = tagsString.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToArray();
            }

            // Salva asset
            string path = $"Assets/Features/{titulo}.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(entry, path);

            // Adiciona ao database
            database.features.Add(entry);

            Debug.Log($"[Populate] ✅ Feature criada: {titulo}");
        }
    }
}

