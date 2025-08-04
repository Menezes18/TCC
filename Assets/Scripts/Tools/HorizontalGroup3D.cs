
using UnityEngine;

[ExecuteInEditMode]
public class LineAlign3D : MonoBehaviour
{
    public Vector3 direction = Vector3.right; // eixo da linha
    public float spacing = 1f;                 // distância entre centros
    public bool useRendererSize = true;       // usa tamanho do mesh pra somar ao spacing
    public bool alignRotation = true;         // força todos a mesma rotação
    public Vector3 lockedEuler = Vector3.zero;// rotação que quer travar (ex: (0,0,0))
    public bool includeInactive = false;
    public bool autoUpdate = true;

    [ContextMenu("Alinhar Agora")]
    public void Align()
    {
        var children = GetComponentsInChildren<Transform>(includeInactive);
        // 0 é o próprio pai
        Vector3 dirNorm = direction.normalized;
        float cursor = 0f;

        for (int i = 1; i < children.Length; i++)
        {
            var t = children[i];
            float size = useRendererSize ? GetSizeAlongDir(t, dirNorm) : 0f;

            // posição local ao longo do eixo
            Vector3 localPos = t.localPosition;
            Vector3 axisOnly = dirNorm * (cursor + size * 0.5f);
            // Zera componentes fora do eixo (projeta)
            localPos = Vector3.Project(localPos, dirNorm); // mantém só o que já tinha no eixo
            localPos = axisOnly;                           // substitui
            t.localPosition = localPos;

            if (alignRotation)
                t.localEulerAngles = lockedEuler;

            cursor += size + spacing;
        }
    }

    float GetSizeAlongDir(Transform t, Vector3 dirNorm)
    {
        var r = t.GetComponentInChildren<Renderer>();
        if (r == null) return 0f;
        // projeta o bounds size no eixo
        Vector3 size = r.bounds.size;
        // pega o maior entre x/y/z projetado na direção
        // método mais seguro: mede extents com dot
        Vector3 extents = r.bounds.extents * 2f;
        // aproximação boa: componente absoluta do dot com cada axis * size
        return Mathf.Abs(Vector3.Dot(dirNorm, t.right)) * size.x +
               Mathf.Abs(Vector3.Dot(dirNorm, t.up))    * size.y +
               Mathf.Abs(Vector3.Dot(dirNorm, t.forward))* size.z;
    }

    void OnValidate()
    {
        if (autoUpdate) Align();
    }

    void Update()
    {
        if (autoUpdate && !Application.isPlaying) Align();
    }
}
