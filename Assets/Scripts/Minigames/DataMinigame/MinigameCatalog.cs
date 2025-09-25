using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MinigameCatalog", menuName = "Minigame/Catalog")]
public class MinigameCatalog : ScriptableObject
{
    [Serializable]
    public class MinigameEntry
    {
        [Tooltip("Unique identifier used by UI toggles and code.")]
        public string id;

        [Tooltip("Scene reference used when loading the minigame.")]
        public SceneReference scene = new SceneReference();

        [Tooltip("Friendly name shown to players.")]
        public string displayName;

        public SettingsMiniGameData settings;

        public Sprite icon;

        [Tooltip("Include this minigame when the list is reset.")]
        public bool enabledByDefault = true;

        public string SceneIdentifier => scene?.ScenePathOrName;
        public bool HasValidScene => scene != null && scene.IsValid;
    }

    [SerializeField] private SceneReference victoryScene = new SceneReference();

    [SerializeField] private List<MinigameEntry> entries = new();

    public IReadOnlyList<MinigameEntry> Entries => entries;

    public string VictorySceneName => victoryScene?.SceneName;

    public string VictorySceneIdentifier => victoryScene?.ScenePathOrName;

    public IEnumerable<MinigameEntry> GetDefaultEntries() =>
        entries.Where(entry => entry.enabledByDefault && entry.HasValidScene);

    public bool TryGetEntry(string id, out MinigameEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(id))
            return false;

        entry = entries.FirstOrDefault(e => string.Equals(e.id, id, StringComparison.OrdinalIgnoreCase));
        return entry != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        victoryScene?.Validate();
        foreach (var entry in entries)
            entry?.scene?.Validate();

        var duplicated = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.id))
            .GroupBy(e => e.id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicated.Length > 0)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                Debug.LogWarning($"[MinigameCatalog] Duplicate minigame ids detected: {string.Join(", ", duplicated)}", this);
            };
        }
    }
#endif
}
