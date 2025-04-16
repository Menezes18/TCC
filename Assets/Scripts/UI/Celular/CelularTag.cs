using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CelularTag : MonoBehaviour
{
    public TMP_Text username_text, money_text;
    public Image pfp_Image;

    public void UpdateTagCelular(string username, string money) 
    {
        username_text.text = username;
        money_text.text = money;
    }

    public void UpdatePFP(Sprite icon)
    {
        pfp_Image.sprite = icon;
    }
}