using UnityEngine;

// Centraliza nomes de parâmetros/Triggers do Animator para evitar typos.
// Fase 1 Refactor: somente substituição de literais existentes ("push", "throw").
// Adicione novos aqui conforme for migrando.
public static class AnimatorParams
{
    // Triggers
    public const string Push = "push";
    public const string Throw = "throw";

    // Integers
    public const string State = "state";
    public const string Status = "status";

    // Floats
    public const string MoveX = "MoveX";
    public const string MoveY = "MoveY";
    public const string AimWeight = "animweight"; // usado em PlayerScript
}