using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CreditsGenerator : MonoBehaviour
{
    public GameObject titlePrefab;
    public GameObject sectionPrefab;
    public GameObject empty;
    
    public RectTransform content;
    
    public List<CreditEntry> entries;
    public float lastCreditY;
    void Start()
    {
        Generate();
    }

    void Generate()
    {
        foreach (Transform c in content) Destroy(c.gameObject);

        foreach (var e in entries)
        {
            switch (e.entryType)
            {
                case CreditEntryType.Section:
                    var section = Instantiate(sectionPrefab, content);
                    var sectionUI = section.GetComponent<CreditSectionUI>();
                    sectionUI.Setup(e);
                    break;
                case CreditEntryType.Empty:
                    var emptyUI = Instantiate(empty, content);
                    break;
                case CreditEntryType.Title:
                    var title = Instantiate(titlePrefab, content);
                    var titleUI = title.GetComponent<CreditTitleUI>();
                    titleUI.Setup(e);
                    break;
            }
        }
    }
}