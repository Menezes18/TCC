using UnityEngine;


public class CustomizationInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Força inicialização do CustomizationManager
        var manager = CustomizationManager.Instance;
        Debug.Log("🚀 [CustomizationInitializer] CustomizationManager inicializado");
        
        // Força inicialização do CustomizationSceneManager
        var sceneManager = CustomizationSceneManager.Instance;
        Debug.Log("🚀 [CustomizationInitializer] CustomizationSceneManager inicializado");
    }
}
