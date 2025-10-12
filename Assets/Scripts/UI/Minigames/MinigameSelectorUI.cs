using System;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class MinigameSelectorUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MiniggameCatalogProvider catalogProvider; // optional helper to find catalog
    [SerializeField] private MinigameCatalog catalog; // if not set, tries provider

    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private MinigameSelectorItem itemPrefab;
    [SerializeField] private TMP_Text headerText;

    private readonly List<MinigameSelectorItem> _spawned = new();

    private void Awake()
    {
        if (catalog == null && catalogProvider != null)
            catalog = catalogProvider.GetCatalog();
    }

    private void OnEnable()
    {
        Rebuild();
    }

    public void SetCatalog(MinigameCatalog newCatalog)
    {
        catalog = newCatalog;
        Rebuild();
    }

    public void Rebuild()
    {
        if (contentRoot == null || itemPrefab == null) return;
        foreach (var it in _spawned)
            if (it != null) Destroy(it.gameObject);
        _spawned.Clear();

        if (catalog == null || catalog.Entries == null)
        {
            if (headerText != null) headerText.text = "Nenhum catálogo atribuído";
            return;
        }

        if (headerText != null)
            headerText.text = "Seleção de Minigames";

        foreach (var entry in catalog.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !entry.HasValidScene)
                continue;

            var go = Instantiate(itemPrefab, contentRoot);
            go.gameObject.SetActive(true);
            bool isOn = IsEntryActive(entry);
            go.Bind(entry, isOn, OnItemToggle);
            _spawned.Add(go);
        }
    }

    private bool IsEntryActive(MinigameCatalog.MinigameEntry entry)
    {
        // If NetworkManager exists and has rotation, assume active when its scene is in rotation
        var mgr = MyNetworkManager.manager;
        if (mgr != null && mgr.SceneRotation != null)
        {
            string sceneId = entry.SceneIdentifier;
            foreach (var s in mgr.SceneRotation)
            {
                if (string.Equals(s, sceneId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        // Fallback to entry default
        return entry.enabledByDefault;
    }

    private void OnItemToggle(MinigameSelectorItem item, bool isOn)
    {
        var entry = item.Entry;
        if (entry == null || string.IsNullOrWhiteSpace(entry.id)) return;

        var mgr = MyNetworkManager.manager;
        if (mgr == null)
        {
            Debug.LogWarning("[MinigameSelectorUI] MyNetworkManager.manager não encontrado.");
            return;
        }

        // Only host/server can mutate rotation
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[MinigameSelectorUI] Apenas o host pode alterar a lista de minigames.");
            // revert UI to current state from rotation
            bool shouldBeOn = IsEntryActive(entry);
            item.Bind(entry, shouldBeOn, OnItemToggle);
            return;
        }

        if (isOn)
            mgr.AdicionarMiniGames(entry.id);
        else
            mgr.tirarMiniGames(entry.id);
    }
}

// Optional provider if you want to drag a ScriptableObject holder instead of the asset directly
public class MiniggameCatalogProvider : MonoBehaviour
{
    [SerializeField] private MinigameCatalog catalog;
    public MinigameCatalog GetCatalog() => catalog;
}
