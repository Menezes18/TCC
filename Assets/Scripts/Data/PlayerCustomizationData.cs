using System;
using UnityEngine;


[System.Serializable]
public class PlayerCustomizationData
{
    public string playerId;
    public int hatIndex = -1;
    public int glassesIndex = -1;
    public int shirtIndex = -1;

    public PlayerCustomizationData()
    {
        playerId = string.Empty;
        hatIndex = -1;
        glassesIndex = -1;
        shirtIndex = -1;
    }

    public PlayerCustomizationData(string playerId)
    {
        this.playerId = playerId;
        hatIndex = -1;
        glassesIndex = -1;
        shirtIndex = -1;
    }


    public PlayerCustomizationData Clone()
    {
        return new PlayerCustomizationData(playerId)
        {
            hatIndex = this.hatIndex,
            glassesIndex = this.glassesIndex,
            shirtIndex = this.shirtIndex
        };
    }

    public override string ToString()
    {
        return $"PlayerCustomization [ID: {playerId}, Hat: {hatIndex}, Glasses: {glassesIndex}, Shirt: {shirtIndex}]";
    }
}
