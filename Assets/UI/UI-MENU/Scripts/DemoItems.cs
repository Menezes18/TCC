using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoItems : MonoBehaviour
{
    
    public HorizontalSelector colorSelector;
    
    public Slider slider;
    

    public Image box;

    private void Start()
    {
        //This is for Slider to control image opacity.
        box.color = new Color(box.color.r, box.color.g, box.color.b, slider.value);
    }
    private void Update()
    {

        //This is for Horizontal Selector to control color of the Image.
        switch (colorSelector.value)
        {
            case "RED":
                box.color = new Color(1, 0.224f, 0.224f, slider.value);
                break;
            case "ORANGE":
                box.color = new Color(1, 0.639f, 0.149f, slider.value);
                break;
            case "YELLOW":
                box.color = new Color(1, 0.973f, 0.192f, slider.value);
                break;
            case "GREEN":
                box.color = new Color(0.153f, 0.788f, 0.173f, slider.value);
                break;
            case "BLUE":
                box.color = new Color(0.184f, 0.686f, 1, slider.value);
                break;
            case "PURPLE":
                box.color = new Color(0.804f, 0.271f, 1, slider.value);
                break;
            case "BLACK":
                box.color = new Color(0, 0, 0, slider.value);
                break;
            case "WHITE":
                box.color = new Color(1, 1, 1, slider.value);
                break;
        }
    }

    //This is for toggle to active or deactive Image.
    public void ActiveDeactiveObject()
    {
        if (box.gameObject.activeSelf == true)
        {
            box.gameObject.SetActive(false);
        }
        else
        {
            box.gameObject.SetActive(true);
        }
    }
}
