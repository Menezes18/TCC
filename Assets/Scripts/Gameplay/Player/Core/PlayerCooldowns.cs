using System;
using UnityEngine;

public enum PlayerCooldownType
{
    Push,
    Throw,
    Roll,
    Blind
}

// Fase 2: Wrapper de cooldowns para reduzir floats soltos no PlayerScript.
// Responsável apenas por contagem e consulta (SRP). Não aplica efeitos/estado.
public class PlayerCooldowns
{
    private readonly float[] _remaining = new float[Enum.GetValues(typeof(PlayerCooldownType)).Length];

    public void Tick(float delta)
    {
        for (int i = 0; i < _remaining.Length; i++)
        {
            if (_remaining[i] > 0f)
                _remaining[i] -= delta;
        }
    }

    public void Start(PlayerCooldownType type, float duration)
    {
        if (duration <= 0f) return;
        _remaining[(int)type] = duration;
    }

    public bool IsReady(PlayerCooldownType type) => _remaining[(int)type] <= 0f;
    public float GetRemaining(PlayerCooldownType type) => Mathf.Max(0f, _remaining[(int)type]);

    public float GetNormalized(PlayerCooldownType type, float totalDuration)
    {
        if (totalDuration <= 0f) return 0f;
        return Mathf.Clamp01(GetRemaining(type) / totalDuration);
    }

    public void ResetAll()
    {
        for (int i = 0; i < _remaining.Length; i++)
            _remaining[i] = 0f;
    }
}