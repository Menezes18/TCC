using UnityEngine;

public class InvertYCommand : ISettingCommand
{
    public void Execute(object value)
    {
        bool invertY = (bool)value;
        Debug.Log($"Eixo Y invertido: {(invertY ? "Sim" : "Não")}");
    }
}