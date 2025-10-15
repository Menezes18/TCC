using UnityEngine;

/// <summary>
/// Script helper para criar attachment points automaticamente no player.
/// Use este script para setup rápido dos pontos de anexo.
/// </summary>
[ExecuteInEditMode]
public class CustomizationAttachmentSetup : MonoBehaviour
{
    [Header("Setup Automático")]
    [SerializeField] private bool autoFindHead = true;
    [SerializeField] private bool autoCreatePoints = true;

    [Header("Manual References (opcional)")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform bodyTransform;

    [Header("Offset Settings")]
    [SerializeField] private Vector3 hatOffset = new Vector3(0, 0.15f, 0);
    [SerializeField] private Vector3 glassesOffset = new Vector3(0, 0.05f, 0.08f);
    [SerializeField] private Vector3 shirtOffset = Vector3.zero;

    [Header("Generated Points (Read Only)")]
    public Transform hatAttachPoint;
    public Transform glassesAttachPoint;
    public Transform shirtAttachPoint;

    #if UNITY_EDITOR
    [ContextMenu("Setup Attachment Points")]
    public void SetupAttachmentPoints()
    {
        Debug.Log("🔧 [Setup] Iniciando configuração de attachment points...");

        // Tenta encontrar cabeça automaticamente
        if (autoFindHead && headTransform == null)
        {
            headTransform = FindHeadTransform();
        }

        // Tenta encontrar corpo automaticamente
        if (bodyTransform == null)
        {
            bodyTransform = FindBodyTransform();
        }

        if (autoCreatePoints)
        {
            CreateAttachmentPoints();
        }

        Debug.Log("✅ [Setup] Attachment points configurados!");
    }

    /// <summary>
    /// Tenta encontrar o transform da cabeça automaticamente
    /// </summary>
    private Transform FindHeadTransform()
    {
        // Procura por nomes comuns de head bone
        string[] headNames = { "Head", "head", "HEAD", "Cabeca", "cabeca" };
        
        foreach (string name in headNames)
        {
            Transform found = FindDeepChild(transform, name);
            if (found != null)
            {
                Debug.Log($"🎯 [Setup] Head encontrado: {found.name}");
                return found;
            }
        }

        // Se não encontrar, usa o transform raiz
        Debug.LogWarning("⚠️ [Setup] Head não encontrado, usando transform raiz");
        return transform;
    }

    /// <summary>
    /// Tenta encontrar o transform do corpo
    /// </summary>
    private Transform FindBodyTransform()
    {
        string[] bodyNames = { "Spine", "spine", "Body", "body", "Torso", "torso" };
        
        foreach (string name in bodyNames)
        {
            Transform found = FindDeepChild(transform, name);
            if (found != null)
            {
                Debug.Log($"🎯 [Setup] Body encontrado: {found.name}");
                return found;
            }
        }

        Debug.LogWarning("⚠️ [Setup] Body não encontrado, usando transform raiz");
        return transform;
    }

    /// <summary>
    /// Busca recursiva por transform filho
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Cria os attachment points
    /// </summary>
    private void CreateAttachmentPoints()
    {
        // Hat Attach Point
        if (hatAttachPoint == null)
        {
            GameObject hatObj = new GameObject("HatAttachPoint");
            hatObj.transform.SetParent(headTransform != null ? headTransform : transform);
            hatObj.transform.localPosition = hatOffset;
            hatObj.transform.localRotation = Quaternion.identity;
            hatAttachPoint = hatObj.transform;
            Debug.Log("➕ [Setup] HatAttachPoint criado");
        }

        // Glasses Attach Point
        if (glassesAttachPoint == null)
        {
            GameObject glassesObj = new GameObject("GlassesAttachPoint");
            glassesObj.transform.SetParent(headTransform != null ? headTransform : transform);
            glassesObj.transform.localPosition = glassesOffset;
            glassesObj.transform.localRotation = Quaternion.identity;
            glassesAttachPoint = glassesObj.transform;
            Debug.Log("➕ [Setup] GlassesAttachPoint criado");
        }

        // Shirt Attach Point
        if (shirtAttachPoint == null)
        {
            GameObject shirtObj = new GameObject("ShirtAttachPoint");
            shirtObj.transform.SetParent(bodyTransform != null ? bodyTransform : transform);
            shirtObj.transform.localPosition = shirtOffset;
            shirtObj.transform.localRotation = Quaternion.identity;
            shirtAttachPoint = shirtObj.transform;
            Debug.Log("➕ [Setup] ShirtAttachPoint criado");
        }

        // Salva mudanças
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }

    [ContextMenu("Clear Attachment Points")]
    public void ClearAttachmentPoints()
    {
        if (hatAttachPoint != null) DestroyImmediate(hatAttachPoint.gameObject);
        if (glassesAttachPoint != null) DestroyImmediate(glassesAttachPoint.gameObject);
        if (shirtAttachPoint != null) DestroyImmediate(shirtAttachPoint.gameObject);

        hatAttachPoint = null;
        glassesAttachPoint = null;
        shirtAttachPoint = null;

        Debug.Log("🧹 [Setup] Attachment points removidos");
    }

    [ContextMenu("Auto Configure CustomizationApplier")]
    public void AutoConfigureApplier()
    {
        CustomizationApplier applier = GetComponent<CustomizationApplier>();
        
        if (applier == null)
        {
            applier = gameObject.AddComponent<CustomizationApplier>();
            Debug.Log("➕ [Setup] CustomizationApplier adicionado");
        }

        // Usa reflection para configurar campos privados
        var type = typeof(CustomizationApplier);
        var hatField = type.GetField("hatAttachPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var glassesField = type.GetField("glassesAttachPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var shirtField = type.GetField("shirtAttachPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (hatField != null) hatField.SetValue(applier, hatAttachPoint);
        if (glassesField != null) glassesField.SetValue(applier, glassesAttachPoint);
        if (shirtField != null) shirtField.SetValue(applier, shirtAttachPoint);

        UnityEditor.EditorUtility.SetDirty(applier);
        Debug.Log("✅ [Setup] CustomizationApplier configurado automaticamente!");
    }

    private void OnDrawGizmos()
    {
        // Desenha pontos de anexo no editor
        if (hatAttachPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hatAttachPoint.position, 0.05f);
            UnityEditor.Handles.Label(hatAttachPoint.position, "🎩 Hat");
        }

        if (glassesAttachPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(glassesAttachPoint.position, 0.03f);
            UnityEditor.Handles.Label(glassesAttachPoint.position, "🕶️ Glasses");
        }

        if (shirtAttachPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(shirtAttachPoint.position, 0.08f);
            UnityEditor.Handles.Label(shirtAttachPoint.position, "👕 Shirt");
        }
    }
    #endif
}
