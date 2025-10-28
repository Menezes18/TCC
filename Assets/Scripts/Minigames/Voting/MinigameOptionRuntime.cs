using System;
using UnityEngine;

/// <summary>
/// Runtime representation of a minigame option for voting.
/// This is a simplified, serializable version of MinigameCatalog.MinigameEntry
/// that can be sent over the network and used by vote input providers.
/// </summary>
[Serializable]
public class MinigameOptionRuntime
{
    public string id;
    public string displayName;
    public string sceneIdentifier;
    
    [NonSerialized]
    public Sprite icon;
    
    // For network serialization of the icon (optional - can be used if needed)
    public string iconAssetPath;

    public MinigameOptionRuntime()
    {
    }

    public MinigameOptionRuntime(MinigameCatalog.MinigameEntry entry)
    {
        if (entry == null)
            return;

        id = entry.id;
        displayName = entry.displayName;
        sceneIdentifier = entry.SceneIdentifier;
        icon = entry.icon;
    }

    /// <summary>
    /// Creates a runtime option from a catalog entry.
    /// </summary>
    public static MinigameOptionRuntime FromCatalogEntry(MinigameCatalog.MinigameEntry entry)
    {
        return new MinigameOptionRuntime(entry);
    }

    public override string ToString()
    {
        return $"MinigameOption[{id}]: {displayName}";
    }
}
