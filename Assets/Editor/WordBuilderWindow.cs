using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

public class WordBuilderWindow : EditorWindow
{
    string word = "HELLO";
    DefaultAsset folder;
    Transform parent;
    float extraSpacing = 0.1f;
    float fallbackWidth = 1f;
    float spaceWidth = 0.5f;
    float uniformScale = 1f;
    bool useBoundsWidth = true;
    bool clearParentChildren = false;
    bool reverseOrder = false;
    enum Align { Left, Center, Right }
    Align align = Align.Left;
    enum Axis { X, Y, Z }
    Axis axis = Axis.X;

    [MenuItem("Tools/Word Builder")]
    static void Open()
    {
        var win = GetWindow<WordBuilderWindow>();
        win.titleContent = new GUIContent("Word Builder");
        win.minSize = new Vector2(320, 240);
    }

    void OnGUI()
    {
        word = EditorGUILayout.TextField("Palavra", word);
        folder = (DefaultAsset)EditorGUILayout.ObjectField("Pasta de prefabs", folder, typeof(DefaultAsset), false);
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        align = (Align)EditorGUILayout.EnumPopup("Alinhamento", align);
        axis = (Axis)EditorGUILayout.EnumPopup("Eixo", axis);
        useBoundsWidth = EditorGUILayout.Toggle("Usar largura por Bounds", useBoundsWidth);
        fallbackWidth = EditorGUILayout.FloatField("Largura padrão", fallbackWidth);
        extraSpacing = EditorGUILayout.FloatField("Espaçamento extra", extraSpacing);
        spaceWidth = EditorGUILayout.FloatField("Largura do espaço", spaceWidth);
        uniformScale = EditorGUILayout.FloatField("Escala", uniformScale);
        reverseOrder = EditorGUILayout.Toggle("Inverter ordem", reverseOrder);
        clearParentChildren = EditorGUILayout.Toggle("Limpar filhos do parent", clearParentChildren);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(word) || folder == null))
        {
            if (GUILayout.Button("Construir")) BuildWord();
        }
    }

    string ExtractToken(string nameUpper)
    {
        const string tag = "LETTER_";
        int idx = nameUpper.IndexOf(tag);
        if (idx < 0) return null;
        int start = idx + tag.Length;
        int end = nameUpper.Length;
        for (int i = start; i < nameUpper.Length; i++)
        {
            char c = nameUpper[i];
            if (c == '_')
            {
                bool digits = true;
                for (int j = i + 1; j < nameUpper.Length; j++)
                {
                    if (!char.IsDigit(nameUpper[j])) { digits = false; break; }
                }
                if (digits) { end = i; break; }
            }
        }
        if (end <= start) return null;
        return nameUpper.Substring(start, end - start);
    }

    Dictionary<char, GameObject> BuildCharMap(string folderPath)
    {
        var map = new Dictionary<char, GameObject>();
        var tokens = new Dictionary<string, GameObject>();
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fnameU = System.IO.Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
            var token = ExtractToken(fnameU);
            if (string.IsNullOrEmpty(token)) continue;
            if (!tokens.ContainsKey(token))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) tokens[token] = prefab;
            }
        }
        foreach (var kv in tokens)
        {
            var t = kv.Key;
            var prefab = kv.Value;
            if (t.Length == 1)
            {
                char c = t[0];
                if (!map.ContainsKey(c)) map[c] = prefab;
                continue;
            }
            if (t == "AND" && !map.ContainsKey('&')) map['&'] = prefab;
            else if (t == "APOSTROPHE" && !map.ContainsKey('\'')) map['\''] = prefab;
            else if (t == "COLON" && !map.ContainsKey(':')) map[':'] = prefab;
            else if (t == "DOT" && !map.ContainsKey('.')) map['.'] = prefab;
            else if (t == "EXCLAMATION" && !map.ContainsKey('!')) map['!'] = prefab;
            else if (t == "QUESTION" && !map.ContainsKey('?')) map['?'] = prefab;
            else if (t == "MONEY" && !map.ContainsKey('$')) map['$'] = prefab;
            else if (t == "BRACKET")
            {
                if (!map.ContainsKey('(')) map['('] = prefab;
                if (!map.ContainsKey(')')) map[')'] = prefab;
            }
            else if (t == "COMMA" && !map.ContainsKey(',')) map[','] = prefab;
            else if (t == "SEMICOLON" && !map.ContainsKey(';')) map[';'] = prefab;
            else if (t == "DASH" && !map.ContainsKey('-')) map['-'] = prefab;
            else if (t == "UNDERSCORE" && !map.ContainsKey('_')) map['_'] = prefab;
            else if (t == "PLUS" && !map.ContainsKey('+')) map['+'] = prefab;
            else if (t == "EQUALS" && !map.ContainsKey('=')) map['='] = prefab;
            else if (t == "SLASH" && !map.ContainsKey('/')) map['/'] = prefab;
            else if (t == "BACKSLASH" && !map.ContainsKey('\\')) map['\\'] = prefab;
            else if (t == "STAR" && !map.ContainsKey('*')) map['*'] = prefab;
            else if (t == "HASH" && !map.ContainsKey('#')) map['#'] = prefab;
            else if (t == "AT" && !map.ContainsKey('@')) map['@'] = prefab;
            else if (t == "PERCENT" && !map.ContainsKey('%')) map['%'] = prefab;
            else if (t == "CARET" && !map.ContainsKey('^')) map['^'] = prefab;
            else if (t == "TILDE" && !map.ContainsKey('~')) map['~'] = prefab;
            else if (t == "PIPE" && !map.ContainsKey('|')) map['|'] = prefab;
            else if (t == "QUOTE" && !map.ContainsKey('"')) map['"'] = prefab;
            else if (t == "LESS" && !map.ContainsKey('<')) map['<'] = prefab;
            else if (t == "GREATER" && !map.ContainsKey('>')) map['>'] = prefab;
            else if (t == "OPEN" || t == "CLOSE")
            {
                if (!map.ContainsKey('(')) map['('] = prefab;
                if (!map.ContainsKey(')')) map[')'] = prefab;
                if (!map.ContainsKey('[')) map['['] = prefab;
                if (!map.ContainsKey(']')) map[']'] = prefab;
            }
        }
        return map;
    }

    string NormalizeWord(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var formD = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    float GetPrefabWidth(GameObject prefab)
    {
        if (!useBoundsWidth || prefab == null) return fallbackWidth;
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return fallbackWidth;
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        switch (axis)
        {
            case Axis.X: return bounds.size.x * uniformScale;
            case Axis.Y: return bounds.size.y * uniformScale;
            case Axis.Z: return bounds.size.z * uniformScale;
        }
        return fallbackWidth;
    }

    Vector3 AxisDirection()
    {
        switch (axis)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            case Axis.Z: return Vector3.forward;
        }
        return Vector3.right;
    }

    void BuildWord()
    {
        var folderPath = AssetDatabase.GetAssetPath(folder);
        if (string.IsNullOrEmpty(folderPath)) return;
        var charMap = BuildCharMap(folderPath);
        var normalized = NormalizeWord(word ?? "").ToUpperInvariant();
        var seq = reverseOrder ? normalized.Reverse().ToArray() : normalized.ToCharArray();
        var instances = new List<Transform>();
        var widths = new List<float>();
        var dir = AxisDirection();
        Transform parentToUse = parent;
        if (parentToUse == null)
        {
            var go = new GameObject("Word_" + word);
            parentToUse = go.transform;
            Undo.RegisterCreatedObjectUndo(go, "Create Word Parent");
        }
        else if (clearParentChildren)
        {
            var childs = new List<GameObject>();
            for (int i = parentToUse.childCount - 1; i >= 0; i--) childs.Add(parentToUse.GetChild(i).gameObject);
            foreach (var ch in childs) Undo.DestroyObjectImmediate(ch);
        }
        foreach (var ch in seq)
        {
            if (ch == ' ')
            {
                widths.Add(spaceWidth);
                instances.Add(null);
                continue;
            }
            charMap.TryGetValue(ch, out var prefab);
            if (prefab == null)
            {
                widths.Add(spaceWidth);
                instances.Add(null);
                continue;
            }
            float w = GetPrefabWidth(prefab) + extraSpacing;
            widths.Add(w);
            var inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(inst, "Instantiate Letter");
            inst.transform.SetParent(parentToUse, false);
            inst.transform.localScale = Vector3.one * uniformScale;
            instances.Add(inst.transform);
        }
        float total = widths.Sum();
        float offset = 0f;
        if (align == Align.Center) offset = -total * 0.5f;
        else if (align == Align.Right) offset = -total;
        float cursor = 0f;
        for (int i = 0; i < instances.Count; i++)
        {
            float w = widths[i];
            var t = instances[i];
            if (t != null)
            {
                var pos = (cursor + offset + w * 0.5f - extraSpacing * 0.5f) * dir;
                t.localPosition = pos;
            }
            cursor += w;
        }
        Selection.activeTransform = parentToUse;
    }
}
