using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PersoManager : NetworkBehaviour
{
    
    [Header("Bonés")]
    public List<GameObject> hats;

    [SyncVar(hook = nameof(OnHatChanged))]
    private int currentHatIndex = 0;


    private void OnHatChanged(int oldIndex, int newIndex)
    {
        ApplyHat(newIndex);
    }

    [ContextMenu("Test Next Hat")] // botão no inspector para testar ainda não funfa
    public void NextHat()
    {
        if (!isServer) return;

        int nextIndex = (currentHatIndex + 1) % hats.Count;
        SetHat(nextIndex);
    }

    public void SetHat(int index)
    {
        if (index < 0 || index >= hats.Count) return;

        currentHatIndex = index;
        ApplyHat(currentHatIndex);
    }

    private void ApplyHat(int index)
    {
        for (int i = 0; i < hats.Count; i++)
        {
            hats[i].SetActive(i == index);
        }
    }
    
    //public int GetCurrentHatIndex()
    //{
    //    return currentHatIndex;
    //}
    //Descobri um jeito mais fácil do que escrever isso :P

    public int GetCurrentHatIndex() => currentHatIndex;

    private void Start()
    {
        //ApplyHat(currentHatIndex); // Caso queira aplicar no spawn acho que dá, mas como é pra personalizar não sei se é o ideal
    }

}