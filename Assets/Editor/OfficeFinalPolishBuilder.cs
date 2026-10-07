#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Idempotent builder for the final polish pass, layered on top of OfficeEnvironmentBuilder / OfficeUIBuilder:
/// the upgrade room with its two world-space upgrade pads, the animated paper machines, the worker sleep bubble,
/// the unlock confetti + chime, and a few environment details. Re-running replaces the previous result.
/// </summary>
public static class OfficeFinalPolishBuilder
{
    const string MatDir = "Assets/Materials/Environment";
    const string UiDir = "Assets/Textures/UI";
    const string CelebratePath = "Assets/Audio/Clips/SFX/Celebrate.wav";
    const string ConfettiPath = "Assets/VFX/Prefabs/ConfettiVFX.prefab";
    const string OfficePrefabPath = "Assets/Prefabs/OfficeObject.prefab";

    // main room right wall (see OfficeEnvironmentBuilder: RoomHalfW 6, RoomFront -8, RoomBack 15)
    const float WallX = 6.2f, WallZMin = -8.4f, WallZMax = 15.4f;
    // doorway into the upgrade room, right next to the paper area the player visits all the time
    const float DoorZ0 = 0f, DoorZ1 = 1.8f;
    // upgrade room interior
    const float AnnexX0 = 6.4f, AnnexX1 = 11.6f, AnnexZ0 = -2.4f, AnnexZ1 = 4.3f;
    // the upgrade stations are the focus of the room, so they are built big enough to read from the follow camera
    const float PadScale = 1.35f;

    static readonly Color Navy = new Color32(0x2B, 0x3A, 0x55, 255);
    static readonly Color Blue = new Color32(0x4D, 0xA3, 0xFF, 255);
    static readonly Color Orange = new Color32(0xFF, 0xA0, 0x2A, 255);

    static Material whiteM, metalM, slateM, cabTopM, counterTopM, floorM, woodM, woodDarkM, matNavyM, potM, leafM, leaf2M, glassM, baseM,
        wallM, book1, book3, padTopM, indicatorM, sofaM, sofaCushionM;
    static Sprite circle, pill, ring, iconStack, iconBolt, coin;
    static AudioClip celebrateClip, deniedClip;

    [MenuItem("Tools/Office/Build Final Polish")]
    public static void Build()
    {
        LoadMaterials();
        MakeSprites();
        celebrateClip = MakeCelebrateClip();
        deniedClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Clips/SFX/slide.aif");
        var confetti = MakeConfettiPrefab();

        AddSleepBubble("Assets/Prefabs/Avatars/Avatar_Worker_Orange.prefab");
        AddSleepBubble("Assets/Prefabs/Avatars/Avatar_Worker_Green.prefab");
        SetupOfficePrefab(confetti);

        var env = GameObject.Find("Environment").transform;
        CutDoorway(env.Find("Shell"));
        BuildAnnex(Fresh(env, "Annex"));
        BuildPaperMachines(Fresh(env, "PaperMachines"));
        BuildDetails(Fresh(env, "Details"));
        RemoveHudUpgradeCards();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Office final polish built.");
    }

    // ------------------------------------------------------------------------------------------------ assets

    static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

    static Material Env(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");

    static Material M(string name, string hex, float gloss = 0.1f, string emissive = null)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.color = H(hex);
        m.SetFloat("_Glossiness", gloss);
        m.SetFloat("_Metallic", 0f);
        if (emissive != null)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", H(emissive));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void LoadMaterials()
    {
        whiteM = Env("Env_White"); metalM = Env("Env_Dark"); slateM = Env("Env_WallTrim"); cabTopM = Env("Env_CabinetTop");
        counterTopM = Env("Env_CounterTop"); floorM = Env("Env_Floor"); woodM = Env("Env_Wood"); woodDarkM = Env("Env_WoodDark");
        matNavyM = Env("Env_MatNavy"); potM = Env("Env_Pot"); leafM = Env("Env_Leaf"); leaf2M = Env("Env_Leaf2");
        glassM = Env("Env_WindowGlass"); baseM = Env("Env_Baseboard"); wallM = Env("Env_Wall"); book1 = Env("Env_BookRed"); book3 = Env("Env_BookYellow");
        padTopM = M("Env_PadTop", "#FFFFFF", 0.25f);                       // tinted per pad with a property block
        indicatorM = M("Env_Indicator", "#3DDC6B", 0.6f, "#3DDC6B");        // colour/emission driven by PaperMachine
        sofaM = M("Env_Sofa", "#3FA6A0", 0.08f);
        sofaCushionM = M("Env_SofaCushion", "#5CC2BB", 0.08f);
    }

    static void MakeSprites()
    {
        circle = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiDir}/UI_Circle.png");
        pill = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiDir}/UI_Pill.png");
        iconBolt = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiDir}/UI_IconBolt.png");
        coin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/money1.png");
        ring = SaveSprite("UI_Ring", RingTexture(128, 0.36f));
        iconStack = SaveSprite("UI_IconPaperStack", PaperStackTexture(128));
    }

    static Texture2D RingTexture(int size, float inner)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude / size;   // 0..~0.7
                float a = Mathf.Clamp01((0.5f - r) * size) * Mathf.Clamp01((r - inner) * size);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        return tex;
    }

    /// <summary>Three sheets fanned up and to the right: reads as "a stack of papers" (carry capacity).</summary>
    static Texture2D PaperStackTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color line = new Color32(0x9A, 0xA7, 0xBA, 255);
        var offsets = new[] { new Vector2(0.11f, 0.11f), new Vector2(0.055f, 0.055f), Vector2.zero }; // back -> front
        const float hw = 0.22f, hh = 0.29f, rad = 0.04f, gap = 0.035f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                float a = 0f; Color c = Color.white;
                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector2 p = uv - (new Vector2(0.44f, 0.44f) + offsets[i]);
                    float px = Mathf.Abs(p.x) - hw + rad, py = Mathf.Abs(p.y) - hh + rad;
                    float d = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - rad;
                    float inside = Mathf.Clamp01(0.5f - d * size);
                    float cut = Mathf.Clamp01(0.5f - (d - gap) * size);   // transparent band around the sheet in front
                    a = Mathf.Lerp(a * (1f - cut), 1f, inside);
                    c = Color.white;
                    if (i == offsets.Length - 1 && inside > 0.5f)
                        foreach (float ly in new[] { -0.12f, 0f, 0.12f })
                            if (Mathf.Abs(p.y - ly) < 0.022f && Mathf.Abs(p.x) < 0.13f) c = line;
                }
                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
            }
        return tex;
    }

    static Sprite SaveSprite(string name, Texture2D tex)
    {
        string path = $"{UiDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>A short rising bell arpeggio (C6 E6 G6 C7), synthesised so the project needs no new audio package.</summary>
    static AudioClip MakeCelebrateClip()
    {
        const int rate = 44100;
        float[] freqs = { 1046.5f, 1318.5f, 1568f, 2093f };
        float[] starts = { 0f, 0.07f, 0.14f, 0.21f };
        int n = (int)(rate * 0.95f);
        var data = new short[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate, s = 0f;
            for (int k = 0; k < freqs.Length; k++)
            {
                float u = t - starts[k];
                if (u < 0f) continue;
                float env = Mathf.Exp(-u * 6.5f) * Mathf.Clamp01(u / 0.004f) * (k == freqs.Length - 1 ? 1.1f : 1f);
                float w = 2f * Mathf.PI * freqs[k] * u;
                s += env * (Mathf.Sin(w) + 0.3f * Mathf.Sin(2f * w) + 0.08f * Mathf.Sin(3f * w));
            }
            data[i] = (short)(Mathf.Clamp(s * 0.22f, -1f, 1f) * short.MaxValue);
        }

        using (var fs = new FileStream(CelebratePath, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (short v in data) w.Write(v);
        }
        AssetDatabase.ImportAsset(CelebratePath, ImportAssetOptions.ForceUpdate);
        var imp = (AudioImporter)AssetImporter.GetAtPath(CelebratePath);
        imp.forceToMono = true;
        var settings = imp.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;   // tiny one-shot SFX
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.6f;
        imp.defaultSampleSettings = settings;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(CelebratePath);
    }

    /// <summary>One burst of ~36 paper-like confetti squares. Pooled through VFXPool like the other one-shot effects.</summary>
    static PooledVFX MakeConfettiPrefab()
    {
        string matPath = "Assets/VFX/Materials/VFX_Confetti.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Particles/Standard Unlit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(mat);

        var go = new GameObject("ConfettiVFX");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 1.1f;
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var palette = new Gradient { mode = GradientMode.Fixed };
        palette.SetKeys(
            new[] { new GradientColorKey(H("#FF5A5F"), 0.2f), new GradientColorKey(H("#FFC531"), 0.4f), new GradientColorKey(H("#3DDC6B"), 0.6f),
                    new GradientColorKey(H("#4DA3FF"), 0.8f), new GradientColorKey(H("#B07CFF"), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(palette) { mode = ParticleSystemGradientMode.RandomColor };

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.3f;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // cone points up

        var limit = ps.limitVelocityOverLifetime;      // air drag: the pieces flutter down instead of falling like stones
        limit.enabled = true;
        limit.limit = 100f;
        limit.drag = 1.4f;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        go.AddComponent<PooledVFX>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, ConfettiPath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<PooledVFX>();
    }

    // ------------------------------------------------------------------------------------------------ prefabs

    /// <summary>Thought bubble with animated "zZz" above a seated worker; OfficeAvatar shows it only while truly idle.</summary>
    static void AddSleepBubble(string path)
    {
        var contents = PrefabUtility.LoadPrefabContents(path);
        var old = contents.transform.Find("SleepBubble");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var bubble = new GameObject("SleepBubble").transform;
        bubble.SetParent(contents.transform, false);
        bubble.localPosition = new Vector3(-0.4f, 2.15f, 0f);

        // white cloud with a navy rim (a slightly larger navy copy drawn first), plus two trailing thought dots
        Color rim = new Color(Navy.r, Navy.g, Navy.b, 0.9f), fill = Color.white;
        SpriteChild(bubble, "CloudRim", circle, new Vector3(0f, 0f, 0.002f), new Vector3(0.66f, 0.44f, 1f), rim, 0);
        SpriteChild(bubble, "Cloud", circle, Vector3.zero, new Vector3(0.6f, 0.38f, 1f), fill, 1);
        SpriteChild(bubble, "DotRim_A", circle, new Vector3(-0.27f, -0.27f, 0.002f), new Vector3(0.14f, 0.14f, 1f), rim, 0);
        SpriteChild(bubble, "Dot_A", circle, new Vector3(-0.27f, -0.27f, 0f), new Vector3(0.1f, 0.1f, 1f), fill, 1);
        SpriteChild(bubble, "DotRim_B", circle, new Vector3(-0.36f, -0.38f, 0.002f), new Vector3(0.09f, 0.09f, 1f), rim, 0);
        SpriteChild(bubble, "Dot_B", circle, new Vector3(-0.36f, -0.38f, 0f), new Vector3(0.055f, 0.055f, 1f), fill, 1);

        var textGo = new GameObject("Zzz", typeof(RectTransform));
        textGo.transform.SetParent(bubble, false);
        var text = textGo.AddComponent<TextMeshPro>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = "zZz";
        text.fontSize = 3.2f;
        text.fontStyle = FontStyles.Bold;
        text.color = H("#5B6BD6");
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        ((RectTransform)textGo.transform).sizeDelta = new Vector2(0.6f, 0.36f);
        textGo.transform.localPosition = new Vector3(0f, 0.01f, -0.01f);
        var tr = text.GetComponent<MeshRenderer>();
        tr.sortingOrder = 2;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        bubble.gameObject.SetActive(false);

        var avatar = contents.GetComponent<OfficeAvatar>();
        var so = new SerializedObject(avatar);
        so.FindProperty("sleepBubble").objectReferenceValue = bubble;
        so.FindProperty("sleepText").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    static void SpriteChild(Transform parent, string name, Sprite sprite, Vector3 pos, Vector3 scale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale / (sprite.rect.width / sprite.pixelsPerUnit);   // scale = size in metres
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
    }

    static void SetupOfficePrefab(PooledVFX confetti)
    {
        var contents = PrefabUtility.LoadPrefabContents(OfficePrefabPath);
        var so = new SerializedObject(contents.GetComponentInChildren<Office>(true));
        so.FindProperty("confettiVFX").objectReferenceValue = confetti;
        so.FindProperty("celebrateSound").objectReferenceValue = celebrateClip;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(contents, OfficePrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    // ------------------------------------------------------------------------------------------------ primitives

    static Transform Fresh(Transform parent, string name)
    {
        var old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        return Group(parent, name);
    }

    static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        return g;
    }

    static GameObject P(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat,
        bool collider = false, Vector3? euler = null, bool shadows = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if (collider) go.AddComponent<BoxCollider>();
        return go;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false, bool shadows = true) =>
        P(parent, PrimitiveType.Cube, name, pos, size, mat, collider, null, shadows);

    static void SetStatic(GameObject root, bool isStatic)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
            flags = isStatic ? flags | StaticEditorFlags.BatchingStatic : flags & ~StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }
    }

    // ------------------------------------------------------------------------------------------------ doorway

    /// <summary>Replaces the solid right wall with two segments and door posts, leaving a 1.8 m opening into the upgrade room.</summary>
    static void CutDoorway(Transform shell)
    {
        for (int i = shell.childCount - 1; i >= 0; i--)
        {
            var c = shell.GetChild(i);
            bool rightSide = c.position.x > 5f;
            if (rightSide && (c.name.StartsWith("Wall_Right") || c.name.StartsWith("WallCap_Side") || c.name.StartsWith("Baseboard_Side") || c.name.StartsWith("DoorPost_Annex")))
                Object.DestroyImmediate(c.gameObject);
        }

        var wallTrim = slateM;
        foreach (var (z0, z1, suffix) in new[] { (WallZMin, DoorZ0, "_S"), (DoorZ1, WallZMax, "_N") })
        {
            float cz = (z0 + z1) * 0.5f, len = z1 - z0;
            Box(shell, "Wall_Right" + suffix, new Vector3(WallX, 0.85f, cz), new Vector3(0.4f, 1.7f, len), wallM, true);
            Box(shell, "WallCap_Side" + suffix, new Vector3(WallX, 1.74f, cz), new Vector3(0.5f, 0.1f, len + 0.1f), wallTrim);
            float b0 = Mathf.Max(z0, -8f), b1 = Mathf.Min(z1, 15f);
            Box(shell, "Baseboard_Side" + suffix, new Vector3(5.98f, 0.12f, (b0 + b1) * 0.5f), new Vector3(0.06f, 0.24f, b1 - b0), baseM);
        }
        foreach (float z in new[] { DoorZ0 - 0.12f, DoorZ1 + 0.12f })
            Box(shell, "DoorPost_Annex", new Vector3(WallX, 0.95f, z), new Vector3(0.52f, 1.9f, 0.26f), wallTrim);

        SetStatic(shell.gameObject, true);
    }

    // ------------------------------------------------------------------------------------------------ upgrade room

    static void BuildAnnex(Transform g)
    {
        var shell = Group(g, "Shell");
        float cx = (AnnexX0 + AnnexX1) * 0.5f, cz = (AnnexZ0 + AnnexZ1) * 0.5f;
        float w = AnnexX1 - AnnexX0, d = AnnexZ1 - AnnexZ0;

        // floor: same tiles as the main room plus a warm parquet inset, so it reads as a different (nicer) room
        // starts where the main slab ends (x 6.4), so the two top faces never overlap
        Box(shell, "Floor", new Vector3(AnnexX0 + (w + 0.4f) * 0.5f, -0.04f, cz), new Vector3(w + 0.4f, 0.1f, d + 0.8f), floorM);
        Box(shell, "Parquet", new Vector3(cx, 0.02f, cz), new Vector3(w - 0.2f, 0.02f, d - 0.2f), woodM);
        Box(shell, "DoorMat", new Vector3(WallX, 0.035f, (DoorZ0 + DoorZ1) * 0.5f), new Vector3(1.5f, 0.02f, 1.5f), matNavyM);

        // walls: east and north full height like the main side walls, south wall low so the camera sees in
        Box(shell, "Wall_East", new Vector3(AnnexX1 + 0.2f, 0.85f, cz), new Vector3(0.4f, 1.7f, d + 0.8f), wallM, true);
        Box(shell, "WallCap_East", new Vector3(AnnexX1 + 0.2f, 1.74f, cz), new Vector3(0.5f, 0.1f, d + 0.9f), slateM);
        Box(shell, "Wall_North", new Vector3(cx, 0.85f, AnnexZ1 + 0.2f), new Vector3(w, 1.7f, 0.4f), wallM, true);
        Box(shell, "WallCap_North", new Vector3(cx, 1.74f, AnnexZ1 + 0.2f), new Vector3(w + 0.1f, 0.1f, 0.5f), slateM);
        Box(shell, "Wall_South", new Vector3(cx, 0.3f, AnnexZ0 - 0.2f), new Vector3(w, 0.6f, 0.4f), wallM, true);
        Box(shell, "WallCap_South", new Vector3(cx, 0.63f, AnnexZ0 - 0.2f), new Vector3(w + 0.9f, 0.08f, 0.5f), slateM);
        Box(shell, "Baseboard_East", new Vector3(AnnexX1 - 0.02f, 0.12f, cz), new Vector3(0.06f, 0.24f, d), baseM);
        Box(shell, "Baseboard_North", new Vector3(cx, 0.12f, AnnexZ1 - 0.02f), new Vector3(w, 0.24f, 0.06f), baseM);
        SetStatic(shell.gameObject, true);

        // the two upgrade stations along the back wall, clear of the walking line through the door
        BuildPad(g, "UpgradePad_Carry", new Vector3(7.95f, 0f, 2.3f), UpgradeManager.UpgradeType.Capacity, Blue, iconStack);
        BuildPad(g, "UpgradePad_Speed", new Vector3(10.35f, 0f, 2.3f), UpgradeManager.UpgradeType.WorkerSpeed, Orange, iconBolt);

        // lounge corner in the front half: a few props, nothing in the walking line
        var props = Group(g, "Props");
        Sofa(props, new Vector3(11.15f, 0f, -0.95f));
        CoffeeTable(props, new Vector3(10.05f, 0f, -0.95f));
        WaterCooler(props, new Vector3(6.85f, 0f, -1.95f));
        Plant(props, new Vector3(11.2f, 0f, -2.05f), 0.85f);
        SetStatic(props.gameObject, true);
    }

    static void BuildPad(Transform parent, string name, Vector3 pos, UpgradeManager.UpgradeType type, Color accent, Sprite icon)
    {
        var root = Group(parent, name);
        root.localPosition = pos;
        root.localScale = Vector3.one * PadScale;

        // physical pad: slate rim + coloured top. The top group is punched on purchase (unit scale, pivot on the floor).
        SetStatic(P(root, PrimitiveType.Cylinder, "Rim", new Vector3(0, 0.03f, 0), new Vector3(1.5f, 0.03f, 1.5f), slateM), true);
        var top = Group(root, "Top");
        var topMesh = P(top, PrimitiveType.Cylinder, "Disc", new Vector3(0, 0.07f, 0), new Vector3(1.3f, 0.04f, 1.3f), padTopM);

        var padCanvas = WorldCanvas(top, "PadCanvas", new Vector3(0, 0.116f, 0), Quaternion.Euler(90f, 0f, 0f), new Vector2(130, 130));
        Img(padCanvas, "Track", ring, new Color(1, 1, 1, 0.35f), Vector2.zero, new Vector2(118, 118));
        var charge = Img(padCanvas, "Charge", ring, Color.white, Vector2.zero, new Vector2(118, 118));
        charge.type = Image.Type.Filled; charge.fillMethod = Image.FillMethod.Radial360;
        charge.fillOrigin = (int)Image.Origin360.Top; charge.fillClockwise = true; charge.fillAmount = 0f;
        var iconImg = Img(padCanvas, "Icon", icon, Color.white, Vector2.zero, new Vector2(66, 66));

        var trigger = root.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0, 0.25f, 0);
        trigger.size = new Vector3(1.1f, 0.5f, 1.1f);

        // sign stand behind the pad: two posts and a board tilted toward the camera
        var stand = Group(root, "Stand");
        stand.localPosition = new Vector3(0, 0, 1.0f);
        foreach (int s in new[] { -1, 1 }) SetStatic(Box(stand, "Post", new Vector3(s * 0.5f, 0.6f, 0.06f), new Vector3(0.05f, 1.2f, 0.05f), metalM), true);
        var standCol = stand.gameObject.AddComponent<BoxCollider>();
        standCol.center = new Vector3(0, 0.8f, 0.05f); standCol.size = new Vector3(1.35f, 1.6f, 0.25f);

        // high enough that a player standing on the pad never hides the price from the follow camera
        var sign = Group(stand, "Sign");
        sign.localPosition = new Vector3(0, 1.22f, 0);
        sign.localRotation = Quaternion.Euler(30f, 0f, 0f);
        Box(sign, "Frame", new Vector3(0, 0, 0.012f), new Vector3(1.36f, 0.8f, 0.04f), slateM);
        Box(sign, "Board", new Vector3(0, 0, -0.005f), new Vector3(1.3f, 0.74f, 0.04f), whiteM);

        var sc = WorldCanvas(sign, "SignCanvas", new Vector3(0, 0, -0.03f), Quaternion.identity, new Vector2(128, 72));
        var header = Img(sc, "Header", pill, accent, new Vector2(0, 21), new Vector2(118, 23));
        header.type = Image.Type.Sliced; header.pixelsPerUnitMultiplier = 5.2f;
        Img(header.rectTransform, "HeaderIcon", icon, Color.white, new Vector2(-47, 0), new Vector2(17, 17));
        var title = Txt(header.rectTransform, "Title", type == UpgradeManager.UpgradeType.Capacity ? "CARRY +10" : "SPEED +25%", 14f, Color.white, TextAlignmentOptions.Center, new Vector2(9, 0), new Vector2(92, 20));

        var row = Rect(sc, "CostRow", new Vector2(0, -4), new Vector2(120, 28));
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter; layout.spacing = 3f;
        layout.childControlWidth = true; layout.childControlHeight = false;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        var coinImg = Img(row, "Coin", coin, Color.white, Vector2.zero, new Vector2(22, 22));
        var le = coinImg.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 22; le.preferredHeight = 22;
        var cost = Txt(row, "Cost", "100", 27f, Navy, TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(60, 28));

        var pips = new Image[2];
        for (int i = 0; i < pips.Length; i++)
            pips[i] = Img(sc, "Pip" + i, circle, H("#CCD3E0"), new Vector2((i - 0.5f) * 15f, -26), new Vector2(10, 10));

        var zone = root.gameObject.AddComponent<UpgradePadZone>();
        var so = new SerializedObject(zone);
        so.FindProperty("type").enumValueIndex = (int)type;
        so.FindProperty("padTop").objectReferenceValue = top;
        so.FindProperty("padTopRenderer").objectReferenceValue = topMesh.GetComponent<Renderer>();
        so.FindProperty("chargeRing").objectReferenceValue = charge;
        so.FindProperty("icon").objectReferenceValue = iconImg.transform;
        so.FindProperty("iconGraphic").objectReferenceValue = iconImg;
        so.FindProperty("sign").objectReferenceValue = sign;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("costText").objectReferenceValue = cost;
        so.FindProperty("coinIcon").objectReferenceValue = coinImg.gameObject;
        so.FindProperty("accentColor").colorValue = accent;
        so.FindProperty("deniedSound").objectReferenceValue = deniedClip;
        var pipsProp = so.FindProperty("pips"); pipsProp.arraySize = pips.Length;
        for (int i = 0; i < pips.Length; i++) pipsProp.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static RectTransform WorldCanvas(Transform parent, string name, Vector3 localPos, Quaternion localRot, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.localPosition = localPos;
        rt.localRotation = localRot;
        rt.localScale = Vector3.one * 0.01f;   // 1 canvas unit = 1 cm
        return rt;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        var im = Rect(parent, name, pos, size).gameObject.AddComponent<Image>();
        im.sprite = sprite; im.color = color; im.preserveAspect = true; im.raycastTarget = false;
        return im;
    }

    static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align, Vector2 pos, Vector2 box)
    {
        var t = Rect(parent, name, pos, box).gameObject.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = FontStyles.Bold;
        t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false;
        return t;
    }

    // ------------------------------------------------------------------------------------------------ paper machines

    static void BuildPaperMachines(Transform g)
    {
        foreach (var zone in Object.FindObjectsByType<PaperCollectZone>(FindObjectsInactive.Include))
        {
            // The shelf stays where it is: the 1.6 m corridor behind it is the route from the desks to the upgrade room.
            // (Restores the original spot in case an earlier version of this builder moved it.)
            var shelf = zone.transform.parent.Find("PaperDesk");
            if (shelf != null) { var p = shelf.position; p.z = ShelfZ; shelf.position = p; }

            // The copier stands on the back edge of the pile's base, flush against the shelf. That base is already a solid
            // block the player cannot step onto, so the machine needs no collider and adds no new obstacle.
            Vector3 pos = new Vector3(zone.transform.position.x, 0f, ShelfFrontZ - MachineDepth * 0.5f);
            BuildMachine(g, zone.transform.position.x < 0 ? "PaperMachine_Left" : "PaperMachine_Right", pos, zone);
        }
    }

    const float ShelfZ = 0.8f, ShelfFrontZ = -0.85f, MachineDepth = 0.42f;

    static void BuildMachine(Transform parent, string name, Vector3 pos, PaperCollectZone zone)
    {
        const float d = MachineDepth, front = -MachineDepth * 0.5f;
        var root = Group(parent, name);
        root.localPosition = pos;

        Box(root, "Plinth", new Vector3(0, 0.04f, 0), new Vector3(1.56f, 0.08f, d + 0.04f), metalM);

        // static body (batches with the office); only the parts collected in "moving" below animate
        var body = Group(root, "Body");
        body.localPosition = new Vector3(0, 0.08f, 0);
        Box(body, "Shell", new Vector3(0, 0.31f, 0), new Vector3(1.5f, 0.62f, d), whiteM);
        Box(body, "Stripe", new Vector3(0, 0.48f, 0), new Vector3(1.52f, 0.06f, d + 0.02f), counterTopM, false, false);
        Box(body, "Lid", new Vector3(0, 0.65f, 0), new Vector3(1.5f, 0.06f, d), cabTopM);
        Box(body, "FeedTray", new Vector3(-0.1f, 0.69f, 0.03f), new Vector3(0.62f, 0.02f, 0.32f), metalM, false, false);
        var feed = Box(body, "FeedPaper", new Vector3(-0.1f, 0.72f, 0.03f), new Vector3(0.52f, 0.05f, 0.27f), whiteM, false, false);
        Box(body, "Panel", new Vector3(0.52f, 0.69f, front + 0.11f), new Vector3(0.36f, 0.03f, 0.18f), metalM, false, false);
        P(body, PrimitiveType.Cylinder, "ButtonA", new Vector3(0.42f, 0.71f, front + 0.11f), new Vector3(0.05f, 0.012f, 0.05f), book1, false, null, false);
        P(body, PrimitiveType.Cylinder, "ButtonB", new Vector3(0.5f, 0.71f, front + 0.11f), new Vector3(0.05f, 0.012f, 0.05f), book3, false, null, false);
        var light = P(body, PrimitiveType.Sphere, "StatusLight", new Vector3(0.62f, 0.72f, front + 0.11f), new Vector3(0.09f, 0.09f, 0.09f), indicatorM, false, null, false);
        Box(body, "Slot", new Vector3(0, 0.28f, front), new Vector3(1.0f, 0.1f, 0.03f), metalM, false, false);

        // rollers in the output slot (cylinders laid along X, ribbed so the spin is visible)
        var rollers = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            var r = Group(body, "Roller" + i);
            r.localPosition = new Vector3(0, 0.31f - i * 0.06f, front - 0.015f);
            r.localRotation = Quaternion.Euler(0f, 0f, 90f);
            P(r, PrimitiveType.Cylinder, "Drum", Vector3.zero, new Vector3(0.065f, 0.45f, 0.065f), cabTopM, false, null, false);
            Box(r, "Rib", new Vector3(0.033f, 0, 0), new Vector3(0.012f, 0.88f, 0.02f), metalM, false, false);   // one rib is enough to see the spin
            rollers[i] = r;
        }

        // a gear on the front face, left of the slot and above the tray, turning with the rollers
        var gear = Group(body, "Gear");
        gear.localPosition = new Vector3(-0.6f, 0.37f, front - 0.01f);
        gear.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        P(gear, PrimitiveType.Cylinder, "Disc", Vector3.zero, new Vector3(0.17f, 0.012f, 0.17f), book3, false, null, false);
        Box(gear, "Spoke", new Vector3(0, 0.014f, 0), new Vector3(0.15f, 0.012f, 0.035f), metalM, false, false);

        // output tray sloping toward the pile, and the sheet that slides out for every produced paper
        Box(root, "OutputTray", new Vector3(0, 0.27f, front - 0.17f), new Vector3(1.0f, 0.025f, 0.34f), cabTopM).transform.localEulerAngles = new Vector3(-14f, 0f, 0f);
        Vector3 sheetStart = new Vector3(0, 0.36f, front + 0.08f), sheetEnd = new Vector3(0, 0.29f, front - 0.3f);
        var sheet = Box(root, "OutputSheet", sheetStart, new Vector3(0.42f, 0.012f, 0.3f), whiteM, false, false).transform;
        sheet.localEulerAngles = new Vector3(-14f, 0f, 0f);

        SetStatic(root.gameObject, true);
        foreach (var moving in new[] { rollers[0], rollers[1], gear, light.transform, sheet, feed.transform })
            SetStatic(moving.gameObject, false);

        var machine = root.gameObject.AddComponent<PaperMachine>();
        var so = new SerializedObject(machine);
        so.FindProperty("source").objectReferenceValue = zone;
        so.FindProperty("feedPaper").objectReferenceValue = feed.transform;
        so.FindProperty("gear").objectReferenceValue = gear;
        so.FindProperty("statusLight").objectReferenceValue = light.GetComponent<Renderer>();
        so.FindProperty("outputSheet").objectReferenceValue = sheet;
        so.FindProperty("sheetStart").vector3Value = sheetStart;
        so.FindProperty("sheetEnd").vector3Value = sheetEnd;
        var rp = so.FindProperty("rollers"); rp.arraySize = rollers.Length;
        for (int i = 0; i < rollers.Length; i++) rp.GetArrayElementAtIndex(i).objectReferenceValue = rollers[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------------------------------------ props

    static void Plant(Transform g, Vector3 p, float s)
    {
        var root = Group(g, "Plant");
        root.localPosition = p;
        P(root, PrimitiveType.Cylinder, "Pot", new Vector3(0, 0.3f * s, 0), new Vector3(0.55f, 0.3f, 0.55f) * s, potM, true);
        P(root, PrimitiveType.Sphere, "Leaf_A", new Vector3(0, 0.95f * s, 0), new Vector3(0.75f, 0.75f, 0.75f) * s, leafM);
        P(root, PrimitiveType.Sphere, "Leaf_B", new Vector3(0.22f * s, 1.3f * s, 0.05f * s), new Vector3(0.5f, 0.5f, 0.5f) * s, leaf2M);
        P(root, PrimitiveType.Sphere, "Leaf_C", new Vector3(-0.2f * s, 1.15f * s, -0.12f * s), new Vector3(0.45f, 0.45f, 0.45f) * s, leafM);
    }

    /// <summary>Sofa against the east wall, facing into the room (-x).</summary>
    static void Sofa(Transform g, Vector3 p)
    {
        var root = Group(g, "Sofa");
        root.localPosition = p;
        Box(root, "Base", new Vector3(0, 0.22f, 0), new Vector3(0.75f, 0.3f, 1.7f), sofaM, true);
        Box(root, "Back", new Vector3(0.3f, 0.55f, 0), new Vector3(0.18f, 0.5f, 1.7f), sofaM);
        foreach (int s in new[] { -1, 1 }) Box(root, "Arm", new Vector3(0, 0.42f, s * 0.8f), new Vector3(0.75f, 0.3f, 0.14f), sofaM);
        foreach (int s in new[] { -1, 1 }) Box(root, "Cushion", new Vector3(-0.06f, 0.42f, s * 0.37f), new Vector3(0.58f, 0.1f, 0.68f), sofaCushionM, false, false);
        Box(root, "Pillow", new Vector3(0.14f, 0.58f, 0.5f), new Vector3(0.12f, 0.26f, 0.3f), book3, false, false).transform.localEulerAngles = new Vector3(0, 0, 12f);
    }

    static void CoffeeTable(Transform g, Vector3 p)
    {
        var root = Group(g, "CoffeeTable");
        root.localPosition = p;
        Box(root, "Top", new Vector3(0, 0.36f, 0), new Vector3(0.6f, 0.05f, 0.9f), woodM, true);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            Box(root, "Leg", new Vector3(sx * 0.25f, 0.17f, sz * 0.4f), new Vector3(0.05f, 0.34f, 0.05f), woodDarkM, false, false);
        Box(root, "Magazine", new Vector3(-0.05f, 0.395f, 0.18f), new Vector3(0.22f, 0.02f, 0.3f), book1, false, false).transform.localEulerAngles = new Vector3(0, 14f, 0);
        P(root, PrimitiveType.Cylinder, "Mug", new Vector3(0.1f, 0.43f, -0.22f), new Vector3(0.08f, 0.05f, 0.08f), whiteM, false, null, false);
    }

    static void WaterCooler(Transform g, Vector3 p)
    {
        var root = Group(g, "WaterCooler");
        root.localPosition = p;
        Box(root, "Body", new Vector3(0, 0.45f, 0), new Vector3(0.36f, 0.9f, 0.36f), whiteM, true);
        Box(root, "Tap", new Vector3(0, 0.7f, -0.19f), new Vector3(0.12f, 0.06f, 0.04f), book1, false, false);
        P(root, PrimitiveType.Cylinder, "Bottle", new Vector3(0, 1.1f, 0), new Vector3(0.28f, 0.2f, 0.28f), glassM);
    }

    /// <summary>Small finishing details in the main office (kept few on purpose).</summary>
    static void BuildDetails(Transform g)
    {
        // entrance: plants and bins already frame the door; a water cooler (left) and a waiting bench (right) fill the empty corners
        WaterCooler(g, new Vector3(-5.5f, 0f, -6.1f));
        Bench(g, new Vector3(5.55f, 0f, -5.9f));   // between the corner plant and the bin
        SetStatic(g.gameObject, true);
    }

    /// <summary>Waiting bench along the right wall (seat runs along z).</summary>
    static void Bench(Transform g, Vector3 p)
    {
        var root = Group(g, "Bench");
        root.localPosition = p;
        Box(root, "Seat", new Vector3(0, 0.42f, 0), new Vector3(0.45f, 0.06f, 1.0f), woodM, true);
        Box(root, "Back", new Vector3(0.2f, 0.68f, 0), new Vector3(0.05f, 0.34f, 1.0f), woodM);
        foreach (int s in new[] { -1, 1 })
        {
            Box(root, "Leg", new Vector3(0, 0.2f, s * 0.42f), new Vector3(0.4f, 0.4f, 0.05f), metalM, false, false);
            Box(root, "BackPost", new Vector3(0.2f, 0.55f, s * 0.42f), new Vector3(0.05f, 0.3f, 0.05f), metalM, false, false);
        }
    }

    // ------------------------------------------------------------------------------------------------ HUD

    static void RemoveHudUpgradeCards()
    {
        var root = GameObject.Find("HudCanvas/Root");
        if (root == null) return;
        foreach (var n in new[] { "Upgrade_Capacity", "Upgrade_Speed" })
        {
            var t = root.transform.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
    }
}
#endif
