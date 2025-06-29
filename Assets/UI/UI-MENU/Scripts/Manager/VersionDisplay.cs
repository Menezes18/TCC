using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class VersionDisplay : MonoBehaviour
{
    public TextMeshProUGUI versionText;

    void Start()
    {
        versionText.text = $"{Application.version}<color=#00FF00>v Prototipo</color>";
    }
}