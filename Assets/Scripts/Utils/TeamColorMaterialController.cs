using UnityEngine;


[DisallowMultipleComponent]
public class TeamColorMaterialController : MonoBehaviour
{
    [Header("Renderer (alvo)")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private bool replaceMaterial = true;
    [SerializeField] private Material materialTemplate;

    [Header("Cores do Futebol")] 
    [SerializeField] private Color teamBlue = new Color(0.2f, 0.45f, 1.0f, 1f);
    [SerializeField] private Color teamRed  = new Color(1.0f, 0.2f, 0.2f, 1f);
    [SerializeField, Tooltip("Alpha quando estiver NO futebol")] private float alphaInSoccer = 1f;
    [SerializeField, Tooltip("Alpha quando NÃO for futebol")] private float alphaOutside = 0f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private Material runtimeMat;

    private Material EnsureSetup()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>(true);
        if (targetRenderer == null)
            return null;

        var mats = targetRenderer.materials; 
        if (mats == null || mats.Length == 0)
            return null;

        int idx = 1;
        if (replaceMaterial && materialTemplate != null)
        {
            var inst = new Material(materialTemplate);
            mats[idx] = inst;
            targetRenderer.materials = mats;
            runtimeMat = inst;
        }
        else
        {
            runtimeMat = mats[idx];
        }
        return runtimeMat;
    }


    public void ApplyForTeam(int team)
    {
        var mat = runtimeMat != null ? runtimeMat : EnsureSetup();
        if (mat == null) return;

        Color teamColor = (team == 1) ? teamRed : teamBlue;
        teamColor.a = alphaInSoccer;
        SetColor(mat, teamColor);
    }


    public void SetInvisible()
    {
        var mat = runtimeMat != null ? runtimeMat : EnsureSetup();
        if (mat == null) return;
        var c = GetCurrentColor(mat);
        c.a = alphaOutside;
        SetColor(mat, c);
    }

    private static Color GetCurrentColor(Material mat)
    {
        if (mat.HasProperty(BaseColorId)) return mat.GetColor(BaseColorId);
        return mat.color;
    }
    private static void SetColor(Material mat, Color c)
    {
        if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, c);
        else mat.color = c;
    }
}

