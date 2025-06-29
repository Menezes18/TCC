using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintManager : MonoBehaviour
{

    public string[] hints;

    [SerializeField]
    private TextMeshProUGUI hintText;


    private void Start()
    {
        hintText = GetComponent<TextMeshProUGUI>();
    }


    private void OnEnable()
    {
        string randomHint = hints[Random.Range(0, hints.Length)];

        hintText.text = randomHint;
    }
}
