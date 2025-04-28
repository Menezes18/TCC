using UnityEngine;
using Mirror;
using System.Linq;

public class ColorChange : NetworkBehaviour
{
    //Materiais
    public Material redMaterial;
    public Material blueMaterial;
    public Material greenMaterial;
    public Material purpleMaterial;

    // Variável sincronizada entre clientes e servidor que guarda a cor atual do jogador.
    // Quando muda, chama automaticamente a função OnColorChanged
    [SyncVar(hook = nameof(OnColorChanged))]
    private PlayerColor currentColor = PlayerColor.None;

    // Função chamada automaticamente quando currentColor muda
    private void OnColorChanged(PlayerColor oldColor, PlayerColor newColor)
    {
        ApplyMaterial(newColor);
    }

  // Tenta definir uma nova cor. Só aplica se a cor ainda não estiver sendo usada
    public bool TrySetColor(PlayerColor newColor)
    {
        if (IsColorTaken(newColor)) return false; // Se a cor já estiver em uso, bloqueia

        currentColor = newColor; // Define a nova cor
        return true;
    }

    // Verifica se a cor desejada já está sendo usada por outro jogador
    private bool IsColorTaken(PlayerColor color)
    {
        if (color == PlayerColor.None) return false; // "None" não conta como cor usada

        // Busca todos os jogadores com ColorChange na cena
        // Verifica se algum deles já está usando a cor desejada
        return FindObjectsOfType<ColorChange>().Any(player =>
            player != this && player.currentColor == color
        );
    }

    // Aplica o material visualmente no personagem com base na cor escolhida
    private void ApplyMaterial(PlayerColor color)
    {
        Material targetMaterial = null;

        // Associa a cor ao material correspondente
        switch (color)
        {
            case PlayerColor.Red: targetMaterial = redMaterial; break;
            case PlayerColor.Blue: targetMaterial = blueMaterial; break;
            case PlayerColor.Green: targetMaterial = greenMaterial; break;
            case PlayerColor.Purple: targetMaterial = purpleMaterial; break;
            default: return; // Se for "None" ou inválida, não faz nada
        }

        // Procura o filho do objeto que contém as partes do corpo do personagem ( no caso tem 4 partes :P)
        Transform meshChild = GameObject.FindWithTag("MacacoAnimacoes").transform;
        if (meshChild == null)
        {
            Debug.LogWarning("Mesh 'MacacoAnimacoes' não encontrada.");
            return;
        }

        // Aplica o material nas primeiras 4 partes do corpo encontradas
        int count = 0;
        foreach (Transform bodyPart in meshChild)
        {
            if (count >= 4) break;

            Renderer rend = bodyPart.GetComponent<Renderer>();
            if (rend != null)
            {
                // Cria uma nova instância do material (evita sobrescrever material global)
                rend.material = new Material(targetMaterial);
                count++;
            }
        }
    }

    // Getter para consultar a cor atual do jogador
    public PlayerColor GetCurrentColor()
    {
        return currentColor;
    }
}
