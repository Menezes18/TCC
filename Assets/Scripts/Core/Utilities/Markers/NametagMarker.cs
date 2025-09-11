using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NametagMarker : Marker
{
    public TMP_Text username_text, ready_text;
    public Image ready_image, pfp_Image;

    public void UpdateTag(string username, bool isReady) 
    {
        username_text.text = username;
        var mm = MainMenu.instance; // soft fallback
        if (mm == null)
        {
            Debug.LogWarning("[NametagMarker] MainMenu.instance null; usando cores padrão.");
            ready_image.color = isReady ? Color.green : Color.red;
            ready_text.text = isReady ? "Ready" : "Not Ready";
            ready_text.color = ready_image.color;
            return;
        }
        ready_image.color = isReady ? mm.readyColor : mm.notReadyColor;
        ready_text.text = isReady ? "Ready" : "Not Ready";
        ready_text.color = isReady ? mm.readyColor : mm.notReadyColor;
    }

    public void UpdatePFP(Sprite icon)
    {
        pfp_Image.sprite = icon;
    }
}
