using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager manager;
    public int jogadoresAtuais;

    public int minJogadores = 1;
    
    void Awake()
    {
        manager = this;
        DontDestroyOnLoad(gameObject);
    }

    public void atualizaJogadores(int jogadores)
    {
        jogadoresAtuais = jogadores;
        Debug.Log("Conectados" + jogadoresAtuais);
        if(jogadoresAtuais >= minJogadores) iniciaContador();
    }

    public void iniciaContador(){
        ContadorTempo temp = GameObject.Find("Temporizador").GetComponent<ContadorTempo>();
        temp.IniciarContador();
    }
}