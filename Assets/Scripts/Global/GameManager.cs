using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager manager;
    public int jogadoresAtuais;
    
    void Awake()
    {
        manager = this;
    }

    public void addJogadores(int quantidade)
    {
        jogadoresAtuais += quantidade;
    }

    public void removeJogadores(int quantidade)
    {
        jogadoresAtuais -= quantidade;
        if (jogadoresAtuais < 0)
            jogadoresAtuais = 0;
    }
}