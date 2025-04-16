using UnityEngine;
using Mirror;

public enum PlayerColor
{
    None,
    Red,
    Blue,
    Green
}

public class TriggerColorZone : NetworkBehaviour
{
    [SerializeField] private PlayerColor colorToApply = PlayerColor.Red;

    

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return; // Só pra verificar o status

        if (other.CompareTag("Player")) //Verificação padrão pro meu trigger
        {
            ColorChange colorChange = other.GetComponent<ColorChange>(); // pegar o script
            if (colorChange != null)
            {
                bool sucesso = colorChange.TrySetColor(colorToApply);
                if(!sucesso)Debug.Log("Cor em uso por" + colorToApply);
            }
        }
    }
}
