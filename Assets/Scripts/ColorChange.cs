using UnityEngine;
using Mirror;
using System.Linq;

public class ColorChange : NetworkBehaviour
{
    public Material redMaterial;
    public Material blueMaterial;
    public Material greenMaterial;

    [SyncVar(hook = nameof(OnColorChanged))]
    private PlayerColor currentColor = PlayerColor.None;

    private void OnColorChanged(PlayerColor oldColor, PlayerColor newColor)
    {
        ApplyMaterial(newColor);
    }

    public bool TrySetColor(PlayerColor newColor)
    {
        if (IsColorTaken(newColor)) return false;

        currentColor = newColor;
        return true;
    }

    private bool IsColorTaken(PlayerColor color)
    {
        if (color == PlayerColor.None) return false;

        return FindObjectsOfType<ColorChange>().Any(player =>
            player != this && player.currentColor == color
        );
    }

    private void ApplyMaterial(PlayerColor color)
    {
        Material targetMaterial = null;

        switch (color)
        {
            case PlayerColor.Red: targetMaterial = redMaterial; break;
            case PlayerColor.Blue: targetMaterial = blueMaterial; break;
            case PlayerColor.Green: targetMaterial = greenMaterial; break;
            default: return;
        }

        Transform meshChild = transform.Find("MacacoAnimacoes");
        if (meshChild == null)
        {
            Debug.LogWarning("Mesh 'MacacoAnimacoes' não encontrada.");
            return;
        }

        int count = 0;
        foreach (Transform bodyPart in meshChild)
        {
            if (count >= 4) break;

            Renderer rend = bodyPart.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(targetMaterial);
                count++;
            }
        }
    }

    public PlayerColor GetCurrentColor()
    {
        return currentColor;
    }
}
