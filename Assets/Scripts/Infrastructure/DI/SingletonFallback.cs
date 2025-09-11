using UnityEngine;

// Fase 4 - Item 14: utilitário para resolver singletons de maneira segura e logar fallback.
public static class SingletonFallback
{
    public static T Resolve<T>(T serializedRef, System.Func<T> legacyGetter, Object context, string fieldName) where T : class
    {
        if (serializedRef != null) return serializedRef;
        var legacy = legacyGetter != null ? legacyGetter() : null;
        if (legacy == null)
        {
            Debug.LogWarning($"[DI][{context?.name}] Falha ao resolver dependência '{fieldName}' — nem serializada nem singleton.");
        }
        else
        {
            Debug.LogWarning($"[DI][{context?.name}] Usando fallback singleton para '{fieldName}'. Configure via Inspector para remover este aviso.");
        }
        return legacy;
    }
}
