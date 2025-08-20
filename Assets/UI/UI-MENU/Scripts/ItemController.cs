using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.Windows;

public class ItemController : MonoBehaviour
{

    [Header("Item Type")]
    public itemTypes itemType = new itemTypes();
    
    public enum itemTypes
    {
        Button,
        Slider,
        HorizontalSelector,
        Toggle
    }

    



}
