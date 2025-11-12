using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ParticleSystemAdjustTool : EditorWindow
{
    private GameObject targetParent;
    private string targetName = "BubbleStream";
    private bool usePartialMatch = true;
    private bool includeInactive = true;
    private bool includeChildren = true;

    private float scaleFactor = 0.5f; // Default: reduce by half

    private bool affectStartSize = true;
    private bool affectStartSpeed = true;
    private bool affectStartLifetime = true;
    private bool affectEmission = true;
    private bool affectMaxParticles = false;
    private bool affectGravityModifier = false;

    private Vector2 scroll;

    private readonly List<ParticleSystem> foundRoots = new List<ParticleSystem>();
    private readonly List<ParticleSystem> affectedList = new List<ParticleSystem>();
    private readonly Dictionary<ParticleSystem, bool> selection = new Dictionary<ParticleSystem, bool>();

    [MenuItem("Tools/Particle System Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<ParticleSystemAdjustTool>("Particle Adjuster");
    }

    private void OnGUI()
    {
        GUILayout.Label("Particle System Adjuster", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetParent = (GameObject)EditorGUILayout.ObjectField("Target Parent", targetParent, typeof(GameObject), true);
        targetName = EditorGUILayout.TextField("Object/Prefab Name", targetName);
        usePartialMatch = EditorGUILayout.Toggle("Use Partial Match", usePartialMatch);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Scale Factor (0.1 = 10%, 1 = no change)");
        scaleFactor = EditorGUILayout.Slider(scaleFactor, 0.01f, 1f);

        EditorGUILayout.Space();

        GUILayout.Label("Affect These Properties", EditorStyles.boldLabel);
        affectStartSize = EditorGUILayout.Toggle("Start Size", affectStartSize);
        affectStartSpeed = EditorGUILayout.Toggle("Start Speed", affectStartSpeed);
        affectStartLifetime = EditorGUILayout.Toggle("Start Lifetime", affectStartLifetime);
        affectEmission = EditorGUILayout.Toggle("Emission (rate, bursts)", affectEmission);
        affectMaxParticles = EditorGUILayout.Toggle("Max Particles", affectMaxParticles);
        affectGravityModifier = EditorGUILayout.Toggle("Gravity Modifier", affectGravityModifier);

        EditorGUILayout.Space();

        if (GUILayout.Button("Find Particle Systems", GUILayout.Height(28)))
        {
            FindParticleSystems();
        }

        if (affectedList.Count > 0)
        {
            int selected = selection.Count(k => k.Value);
            EditorGUILayout.LabelField($"Found {foundRoots.Count} root objects", EditorStyles.boldLabel);
            if (includeChildren)
            {
                EditorGUILayout.LabelField($"Total ParticleSystems (incl. children): {affectedList.Count}");
            }
            EditorGUILayout.LabelField($"Selected: {selected}");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                SetAll(true);
            }
            if (GUILayout.Button("Deselect All"))
            {
                SetAll(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(260));
            foreach (var ps in includeChildren ? affectedList : foundRoots)
            {
                if (ps == null) continue;
                EditorGUILayout.BeginHorizontal();
                bool sel = selection.ContainsKey(ps) && selection[ps];
                bool newSel = EditorGUILayout.Toggle(sel, GUILayout.Width(20));
                if (newSel != sel) selection[ps] = newSel;

                EditorGUILayout.ObjectField(ps.gameObject, typeof(GameObject), true);
                EditorGUILayout.LabelField($"PS: {ps.name}", GUILayout.Width(180));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Scaling", GUILayout.Height(36)))
            {
                ApplyScaling();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1) Selecione o objeto pai (ou deixe vazio para toda a cena).\n" +
            "2) Informe parte ou todo o nome do objeto/prefab.\n" +
            "3) Clique em 'Find Particle Systems'.\n" +
            "4) Ajuste o 'Scale Factor' para diminuir a intensidade.\n" +
            "5) Clique em 'Apply Scaling' para aplicar nos selecionados.",
            MessageType.Info);
    }

    private void SetAll(bool value)
    {
        var list = includeChildren ? affectedList : foundRoots;
        foreach (var ps in list)
        {
            if (ps == null) continue;
            selection[ps] = value;
        }
    }

    private void FindParticleSystems()
    {
        foundRoots.Clear();
        affectedList.Clear();
        selection.Clear();

        IEnumerable<GameObject> searchRoots = null;
        if (targetParent != null)
        {
            var transforms = targetParent.GetComponentsInChildren<Transform>(includeInactive);
            searchRoots = transforms.Select(t => t.gameObject);
        }
        else
        {
            if (includeInactive)
            {
                searchRoots = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(o => o.scene.IsValid());
            }
            else
            {
                var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                searchRoots = all.Where(o => o.scene.IsValid());
            }
        }

        if (searchRoots == null) return;

        foreach (var go in searchRoots)
        {
            bool match = string.IsNullOrEmpty(targetName) ||
                         (usePartialMatch ? go.name.Contains(targetName) : go.name == targetName);

            if (!match) continue;

            // Root can be the GO itself if it has a PS
            var rootPs = go.GetComponents<ParticleSystem>();
            foreach (var ps in rootPs)
            {
                if (!foundRoots.Contains(ps))
                {
                    foundRoots.Add(ps);
                    affectedList.Add(ps);
                    selection[ps] = true;
                }
            }

            if (includeChildren)
            {
                var childPs = go.GetComponentsInChildren<ParticleSystem>(includeInactive);
                foreach (var ps in childPs)
                {
                    if (!affectedList.Contains(ps))
                    {
                        affectedList.Add(ps);
                        selection[ps] = true;
                    }
                }
            }
        }

        Debug.Log($"Particle Adjuster: Found {foundRoots.Count} root objects; total {affectedList.Count} ParticleSystems.");
    }

    private void ApplyScaling()
    {
        var selected = selection.Where(k => k.Value).Select(k => k.Key).Where(ps => ps != null).ToList();
        if (selected.Count == 0)
        {
            EditorUtility.DisplayDialog("Particle Adjuster", "No Particle Systems selected.", "OK");
            return;
        }

        Undo.RecordObjects(selected.Cast<Object>().ToArray(), "Scale Particle Systems");

        int edited = 0;
        foreach (var ps in selected)
        {
            try
            {
                var main = ps.main;

                if (affectStartSize)
                {
                    if (!main.startSize3D)
                    {
                        var c = main.startSize;
                        c = ScaleCurve(c, scaleFactor);
                        main.startSize = c;
                    }
                    else
                    {
                        var cx = main.startSizeX; cx = ScaleCurve(cx, scaleFactor); main.startSizeX = cx;
                        var cy = main.startSizeY; cy = ScaleCurve(cy, scaleFactor); main.startSizeY = cy;
                        var cz = main.startSizeZ; cz = ScaleCurve(cz, scaleFactor); main.startSizeZ = cz;
                    }
                }

                if (affectStartSpeed)
                {
                    var c = main.startSpeed;
                    c = ScaleCurve(c, scaleFactor);
                    main.startSpeed = c;
                }

                if (affectStartLifetime)
                {
                    var c = main.startLifetime;
                    c = ScaleCurve(c, scaleFactor);
                    main.startLifetime = c;
                }

                if (affectMaxParticles)
                {
                    main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(main.maxParticles * scaleFactor));
                }

                if (affectGravityModifier)
                {
                    var c = main.gravityModifier;
                    c = ScaleCurve(c, scaleFactor);
                    main.gravityModifier = c;
                }

                if (affectEmission)
                {
                    var em = ps.emission;
                    var rot = em.rateOverTime; rot = ScaleCurve(rot, scaleFactor); em.rateOverTime = rot;
                    var rod = em.rateOverDistance; rod = ScaleCurve(rod, scaleFactor); em.rateOverDistance = rod;

                    int burstCount = em.burstCount;
                    if (burstCount > 0)
                    {
                        var bursts = new ParticleSystem.Burst[burstCount];
                        em.GetBursts(bursts);
                        for (int i = 0; i < bursts.Length; i++)
                        {
                            var b = bursts[i];
                            var count = b.count;
                            count = ScaleCurve(count, scaleFactor);
                            b.count = count;
                            bursts[i] = b;
                        }
                        em.SetBursts(bursts);
                    }
                }

                EditorUtility.SetDirty(ps);
                edited++;
            }
            catch (System.SystemException e)
            {
                Debug.LogWarning($"Particle Adjuster: Failed on '{ps?.name}': {e.Message}");
            }
        }

        SceneView.RepaintAll();
        EditorUtility.DisplayDialog("Particle Adjuster", $"Scaled {edited} ParticleSystems.", "OK");
    }

    private static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve c, float mul)
    {
        // Handle constants
        c.constant *= mul;
        c.constantMin *= mul;
        c.constantMax *= mul;

        // Curves use multiplier; safer than editing keys directly
        c.curveMultiplier *= mul;
        return c;
    }
}

