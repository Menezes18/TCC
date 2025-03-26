using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewProjetilData", menuName = "ProjetilData")]
public class ProjetilData : ScriptableObject
{
    public float tempoRecarga = 0.5f;
    public GameObject projetilPrefab;

    [Header("Atributos do Tiro")]
    [Tooltip("Velocidade inicial (magnitude) do projétil.")]
    public float velocidadeInicial = 30f;
    [Tooltip("Ângulo de lançamento em graus (0 = horizontal, 90 = vertical).")]
    public float anguloTiro = 45f;
    [Tooltip("Gravidade (negativa). Normal: -9.81.")]
    public float gravidade = -9.81f;
    [Tooltip("Multiplicador de altura da parábola.")]
    public float multiplicadorAltura = 1.5f;
    public float velocidadeProjetil = 1f;
    
    [Header("Gizmo de trajetória")]
    public float tempoMaximoTrajetoria = 3f;
    public int passosTrajetoria = 30;
}
