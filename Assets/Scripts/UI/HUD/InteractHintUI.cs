using TMPro;
using UnityEngine;

public class InteractHintUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HUDSO hud;
    [SerializeField] private TMP_Text label;
    [SerializeField] private CanvasGroup group;

    [Header("Behavior")]
    [SerializeField] private float fadeDuration = 0.12f;
    [SerializeField] private bool startHidden = true;

    private void Awake()
    {
        if (group == null)
            group = GetComponent<CanvasGroup>();
        if (startHidden)
            SetVisible(false, true);
    }

    private void OnEnable()
    {
        if (hud == null) return;
        hud.EventOnShowInteractHint += OnShowHint;
        hud.EventOnHideInteractHint += OnHideHint;
    }

    private void OnDisable()
    {
        if (hud == null) return;
        hud.EventOnShowInteractHint -= OnShowHint;
        hud.EventOnHideInteractHint -= OnHideHint;
    }

    private void OnShowHint(string message)
    {
        if (label != null)
            label.text = string.IsNullOrEmpty(message) ? "Aperte E para interagir" : message;
        SetVisible(true);
    }

    private void OnHideHint()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible, bool instant = false)
    {
        if (group == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        if (instant)
        {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }
}

