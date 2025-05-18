using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Briefing", menuName = "Minigame/Briefing")]
public class BriefingScreenSO : ScriptableObject
{
    public Sprite image;
    public string title;
    [TextArea] public string[] tips;
}
