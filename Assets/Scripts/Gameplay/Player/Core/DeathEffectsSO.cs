using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Death Effects", fileName = "DeathEffectsSO")]
public class DeathEffectsSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public DeathCause cause = DeathCause.Default;

        [Header("VFX")]
        public GameObject vfxPrefab;
        public float vfxLifetime = 2f;
        [Tooltip("Se verdadeiro, o VFX será parentado ao jogador para acompanhar seu transform.")]
        public bool attachToPlayer = false;

        [Tooltip("Atraso antes de ocultar o modelo do player (segundos), para a anima de morte ficar visível.")]
        public float hideModelDelay = 0f;
        [Tooltip("Se verdadeiro, o modelo será ocultado após 'hideModelDelay'.")]
        public bool hideModelAfterDelay = true;


        public AudioClip sfx;
        [Range(0f, 1f)]
        public float sfxVolume = 1f;
    }

    [SerializeField]
    private List<Entry> entries = new List<Entry>();

    public Entry Get(DeathCause cause)
    {
        
        var e = entries.Find(x => x.cause == cause);
        if (e != null) return e;
        return entries.Find(x => x.cause == DeathCause.Default);
    }

    public Entry FindExact(DeathCause cause)
    {
        return entries.Find(x => x.cause == cause);
    }

    public bool TryGetEntry(DeathCause cause, out Entry entry)
    {
        entry = FindExact(cause);
        return entry != null;
    }


    public float GetHideDelay(DeathCause cause, float fallbackDefault = 0f, bool respectHideFlag = true)
    {
        var e = FindExact(cause);
        if (e == null) return fallbackDefault;

        if (respectHideFlag && !e.hideModelAfterDelay)
            return fallbackDefault;

        return Mathf.Max(fallbackDefault, e.hideModelDelay);
    }
}
