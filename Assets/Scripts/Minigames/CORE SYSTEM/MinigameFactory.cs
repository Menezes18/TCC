using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class MinigameFactory
{
    private Dictionary<string, Type> _registeredMinigames = new Dictionary<string, Type>();

    public void RegisterMinigameType<T>(string key) where T : IMinigame
    {
        if (!_registeredMinigames.ContainsKey(key))
        {
            _registeredMinigames.Add(key, typeof(T));
        }
    }

    public IMinigame CreateMinigame(string key)
    {
        if (_registeredMinigames.TryGetValue(key, out Type minigameType))
        {
            return (IMinigame)Activator.CreateInstance(minigameType);
        }
        
        Debug.LogError($"Minigame type not registered: {key}");
        return null;
    }
}
