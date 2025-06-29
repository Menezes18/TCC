// HorizontalSelector.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HorizontalSelector : MonoBehaviour
{
    public int defaultValueIndex = 0;

    public List<string> data = new List<string>();


    public event Action<int> OnValueChanged;

    public TextMeshProUGUI text;
    private int m_Index;

    /// <summary>
    /// Altera o índice, atualiza o texto e dispara o evento.
    /// </summary>
    public int index {
        get => m_Index;
        set {
            if (text == null) 
                return;
            
            if (data == null) 
                return;
            
            if (data.Count == 0) 
                return;
            

            m_Index = Mathf.Clamp(value, 0, data.Count - 1);
            text.text = data[m_Index];
            OnValueChanged?.Invoke(m_Index);
        }
    }

    
    public string value => data[m_Index];

    void Awake()
    {
        text = transform.Find("txt_text").GetComponent<TextMeshProUGUI>();
        transform.Find("btn_left").GetComponent<Button>()
            .onClick.AddListener(OnLeftClicked);
        transform.Find("btn_right").GetComponent<Button>()
            .onClick.AddListener(OnRightClicked);
        
        
    }

    void Start()
    {
        index = defaultValueIndex;
    }

    void OnLeftClicked()
    {
        index = (index == 0) ? data.Count - 1 : index - 1;
    }

    void OnRightClicked()
    {
        index = (index + 1 >= data.Count) ? 0 : index + 1;
    }
}