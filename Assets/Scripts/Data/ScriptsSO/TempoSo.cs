using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tempo/TempoSo")]
public class TempoSo : ScriptableObject
{
    [Tooltip("Tempo em segundos para a contagem inicial (ex: 3)")]
    public float tempoInicial;
    
    [Tooltip("Tempo em segundos para o timer principal (ex: 300 para 5 minutos)")]
    public float tempoPrincipal;
}
