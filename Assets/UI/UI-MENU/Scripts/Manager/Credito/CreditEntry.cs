using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum CreditEntryType
{
    Empty,
    Title,
    Section 
}

[Serializable]
public class CreditEntry
{
    public CreditEntryType entryType;
    
    public string titleText;
    public Color titleTextColor;
    
    public string sectionTitles;
    public List<string> names;
}