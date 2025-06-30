using UnityEngine;

public class LinkOpener : MonoBehaviour
{
    public string url = "https://linktr.ee/100IDEIASOFC";


    public void AbrirLink()
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning("URL não foi definida!");
        }
    }
}