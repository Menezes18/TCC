using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct CustomizationItem
{
    public string name;
    public GameObject prefab;
    public Sprite icon;
    public bool isUnlocked;
}


[CreateAssetMenu(fileName = "CustomizationDatabase", menuName = "Player/Customization Database")]
public class CustomizationDatabase : ScriptableObject
{
    [Header("Chapéus")]
    public List<CustomizationItem> hats = new List<CustomizationItem>();

    [Header("Óculos")]
    public List<CustomizationItem> glasses = new List<CustomizationItem>();

    [Header("Blusas")]
    public List<CustomizationItem> shirts = new List<CustomizationItem>();


    public CustomizationItem? GetHat(int index)
    {
        if (index < 0 || index >= hats.Count)
            return null;
        return hats[index];
    }


    public CustomizationItem? GetGlasses(int index)
    {
        if (index < 0 || index >= glasses.Count)
            return null;
        return glasses[index];
    }

    public CustomizationItem? GetShirt(int index)
    {
        if (index < 0 || index >= shirts.Count)
            return null;
        return shirts[index];
    }


    public bool IsValidHatIndex(int index)
    {
        return index >= -1 && index < hats.Count;
    }


    public bool IsValidGlassesIndex(int index)
    {
        return index >= -1 && index < glasses.Count;
    }

    public bool IsValidShirtIndex(int index)
    {
        return index >= -1 && index < shirts.Count;
    }
}
