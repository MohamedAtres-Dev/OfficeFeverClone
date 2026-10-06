#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot, idempotent builder for the compact office: materials, avatar prefabs, environment props, layout of the
/// existing gameplay objects and the seated worker on the OfficeObject prefab. Re-running replaces the previous result.
/// </summary>
public static class OfficeEnvironmentBuilder
{
    const string MatDir = "Assets/Materials/Environment";
    const string AvatarDir = "Assets/Prefabs/Avatars";
    const string EnvName = "Environment";

    // layout constants (metres, world space)
    const float RoomHalfW = 6f, RoomFront = -8f, RoomBack = 15f;
    const float DeskTop = 0.85f;

    static Material floorM, outsideM, wallM, wallTrimM, baseM, glassM, frameM, matNavyM, matNavy2M, sandM, blueM, woodM, woodDarkM,
        partM, capM, cabM, cabTopM, handleM, potM, leafM, leaf2M, binM, metalM, screenM, deskBackM, whiteM, book1, book2, book3, book4,
        skinM, pantsM, chairM, seatAccentM, eyeM, tieM, counterM, counterTopM;

    [MenuItem("Tools/Office/Build Environment")]
    public static void Build()
    {
        EnsureDir(MatDir); EnsureDir(AvatarDir);
        MakeMaterials();

        var seatOrange = BuildAvatarPrefab("Avatar_Worker_Orange", H("#F28C38"), H("#3A2A20"), OfficeAvatar.Role.Worker, true, false);
        var seatGreen = BuildAvatarPrefab("Avatar_Worker_Green", H("#4DB87A"), H("#D9A441"), OfficeAvatar.Role.Worker, true, false);
        var reception = BuildAvatarPrefab("Avatar_Receptionist", H("#EE6F8C"), H("#2B1B16"), OfficeAvatar.Role.Receptionist, false, false);
        var manager = BuildAvatarPrefab("Avatar_Manager", H("#6C5CE0"), H("#22262E"), OfficeAvatar.Role.Manager, true, true);

        var old = GameObject.Find(EnvName);
        if (old != null) Object.DestroyImmediate(old);
        var env = new GameObject(EnvName).transform;

        BuildShell(Group(env, "Shell"));
        BuildZones(Group(env, "Zones"));
        BuildFurniture(Group(env, "Props"));
        BuildPeopleAndDesks(Group(env, "Characters"), reception, manager);

        MarkStatic(env.gameObject);
        // characters animate (they are not static)
        foreach (var a in env.GetComponentsInChildren<OfficeAvatar>(true)) SetStaticRecursive(a.gameObject, false);

        LayoutGameplayObjects();
        AddWorkersToOfficePrefab(seatOrange);
        SetupLighting();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Office environment built.");
    }

    // ------------------------------------------------------------------------------------------------ materials

    static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

    static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    static Material M(string name, string hex, float gloss = 0.1f, string emissive = null, Texture tex = null, Vector2? tiling = null)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.color = H(hex);
        m.SetFloat("_Glossiness", gloss);
        m.SetFloat("_Metallic", 0f);
        if (tex != null) { m.mainTexture = tex; if (tiling.HasValue) m.mainTextureScale = tiling.Value; }
        if (emissive != null) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", H(emissive)); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void MakeMaterials()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Textures/white-square-tiled-texture-background.jpg");
        floorM = M("Env_Floor", "#C3CCD8", 0.15f, null, tile, new Vector2(1.6f, 4.2f));
        outsideM = M("Env_Outside", "#8FA3B8", 0.02f);
        wallM = M("Env_Wall", "#F2F4F8", 0.05f);
        wallTrimM = M("Env_WallTrim", "#5C6B80", 0.1f);
        baseM = M("Env_Baseboard", "#C7D0DC", 0.1f);
        glassM = M("Env_WindowGlass", "#BFE4FF", 0.6f, "#4F7FA0");
        frameM = M("Env_WindowFrame", "#FFFFFF", 0.1f);
        matNavyM = M("Env_MatNavy", "#34507A", 0.05f);
        matNavy2M = M("Env_MatNavyLight", "#4D6C99", 0.05f);
        sandM = M("Env_CarpetSand", "#E3CD9E", 0.04f);
        blueM = M("Env_CarpetBlue", "#9CC4DE", 0.04f);
        woodM = M("Env_Wood", "#C99A66", 0.2f);
        woodDarkM = M("Env_WoodDark", "#7A5436", 0.2f);
        partM = M("Env_Partition", "#8FB4CC", 0.08f);
        capM = M("Env_PartitionCap", "#FFFFFF", 0.1f);
        cabM = M("Env_Cabinet", "#8C98A8", 0.15f);
        cabTopM = M("Env_CabinetTop", "#B5BFCC", 0.15f);
        handleM = M("Env_Handle", "#323A47", 0.3f);
        potM = M("Env_Pot", "#E5E9EF", 0.2f);
        leafM = M("Env_Leaf", "#4FAE5E", 0.08f);
        leaf2M = M("Env_Leaf2", "#3B8F4D", 0.08f);
        binM = M("Env_Bin", "#6E7A8C", 0.2f);
        metalM = M("Env_Dark", "#2B323D", 0.25f);
        screenM = M("Env_Screen", "#7CC4FF", 0.3f, "#2E6FA8");
        deskBackM = M("Env_MonitorBack", "#444C58", 0.2f);
        whiteM = M("Env_White", "#F7F8FA", 0.1f);
        book1 = M("Env_BookRed", "#E5584F", 0.05f);
        book2 = M("Env_BookBlue", "#4A82D6", 0.05f);
        book3 = M("Env_BookYellow", "#F2C14E", 0.05f);
        book4 = M("Env_BookTeal", "#3FB5A3", 0.05f);
        skinM = M("Env_Skin", "#F3CBA8", 0.1f);
        pantsM = M("Env_Pants", "#3C4658", 0.08f);
        chairM = M("Env_Chair", "#3A4250", 0.15f);
        seatAccentM = M("Env_ChairAccent", "#5B6678", 0.15f);
        eyeM = M("Env_Eye", "#1B1E24", 0.3f);
        tieM = M("Env_Tie", "#E5584F", 0.1f);
        counterM = M("Env_Counter", "#F7F8FA", 0.15f);
        counterTopM = M("Env_CounterTop", "#4DB7A8", 0.2f);
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------------------------------------ primitives

    static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        return g;
    }

    static GameObject P(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, bool collider = false, Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        if (collider) go.AddComponent<BoxCollider>();
        return go;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false) =>
        P(parent, PrimitiveType.Cube, name, pos, size, mat, collider);

    static void MarkStatic(GameObject root) => SetStaticRecursive(root, true);

    static void SetStaticRecursive(GameObject go, bool isStatic)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
            flags = isStatic ? flags | StaticEditorFlags.BatchingStatic : flags & ~StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }
    }

    // ------------------------------------------------------------------------------------------------ shell

    static void BuildShell(Transform g)
    {
        float depth = RoomBack - RoomFront;
        float cz = (RoomBack + RoomFront) * 0.5f;
        float width = RoomHalfW * 2f;

        // floor slab (ground collider stays the original plane)
        Box(g, "Floor", new Vector3(0, -0.04f, cz), new Vector3(width + 0.8f, 0.1f, depth + 0.8f), floorM);

        // back wall + trim
        Box(g, "Wall_Back", new Vector3(0, 1.7f, RoomBack + 0.2f), new Vector3(width + 0.8f, 3.4f, 0.4f), wallM, true);
        Box(g, "Baseboard_Back", new Vector3(0, 0.12f, RoomBack - 0.02f), new Vector3(width, 0.24f, 0.06f), baseM);
        Box(g, "WallCap_Back", new Vector3(0, 3.43f, RoomBack + 0.2f), new Vector3(width + 0.9f, 0.1f, 0.5f), wallTrimM);

        // side walls (cut-away height so the camera always sees into the room)
        foreach (int s in new[] { -1, 1 })
        {
            Box(g, s < 0 ? "Wall_Left" : "Wall_Right", new Vector3(s * (RoomHalfW + 0.2f), 0.85f, cz), new Vector3(0.4f, 1.7f, depth + 0.8f), wallM, true);
            Box(g, "WallCap_Side", new Vector3(s * (RoomHalfW + 0.2f), 1.74f, cz), new Vector3(0.5f, 0.1f, depth + 0.9f), wallTrimM);
            Box(g, "Baseboard_Side", new Vector3(s * (RoomHalfW - 0.02f), 0.12f, cz), new Vector3(0.06f, 0.24f, depth), baseM);
        }

        // front wall with entrance gap (low: the camera looks over it)
        float gap = 1.7f;
        foreach (int s in new[] { -1, 1 })
        {
            float len = RoomHalfW + 0.4f - gap;
            float cx = s * (gap + len * 0.5f);
            Box(g, "Wall_Front", new Vector3(cx, 0.3f, RoomFront - 0.2f), new Vector3(len, 0.6f, 0.4f), wallM, true);
            Box(g, "WallCap_Front", new Vector3(cx, 0.63f, RoomFront - 0.2f), new Vector3(len, 0.08f, 0.5f), wallTrimM);
        }
        // door posts
        foreach (int s in new[] { -1, 1 })
            Box(g, "DoorPost", new Vector3(s * (gap + 0.1f), 0.5f, RoomFront - 0.2f), new Vector3(0.28f, 1.0f, 0.5f), wallTrimM);
        // invisible barrier so the player cannot walk out of the entrance
        var barrier = new GameObject("EntranceBarrier");
        barrier.transform.SetParent(g, false);
        barrier.transform.localPosition = new Vector3(0, 1f, RoomFront - 0.9f);
        barrier.AddComponent<BoxCollider>().size = new Vector3(gap * 2f + 1f, 2f, 0.3f);

        // windows on the back wall
        foreach (float x in new[] { -3f, 0f, 3f }) Window(g, new Vector3(x, 2.1f, RoomBack - 0.02f));
    }

    static void Window(Transform g, Vector3 c)
    {
        Box(g, "WindowGlass", c, new Vector3(2.0f, 1.4f, 0.05f), glassM);
        Box(g, "Frame_T", c + new Vector3(0, 0.74f, -0.02f), new Vector3(2.2f, 0.1f, 0.12f), frameM);
        Box(g, "Frame_B", c + new Vector3(0, -0.74f, -0.05f), new Vector3(2.3f, 0.12f, 0.2f), frameM);
        Box(g, "Frame_L", c + new Vector3(-1.05f, 0, -0.02f), new Vector3(0.1f, 1.5f, 0.12f), frameM);
        Box(g, "Frame_R", c + new Vector3(1.05f, 0, -0.02f), new Vector3(0.1f, 1.5f, 0.12f), frameM);
        Box(g, "Mullion", c + new Vector3(0, 0, -0.02f), new Vector3(0.06f, 1.4f, 0.1f), frameM);
    }

    // ------------------------------------------------------------------------------------------------ zones

    static void Carpet(Transform g, string name, float cx, float cz, float w, float d, Material m, float y = 0.02f)
    {
        Box(g, name, new Vector3(cx, y, cz), new Vector3(w, 0.02f, d), m);
    }

    static void BuildZones(Transform g)
    {
        // entrance mat: darker outer + lighter inner so it reads as a welcome mat
        Carpet(g, "EntranceMat_Outer", 0, -7.1f, 3.4f, 1.9f, matNavyM, 0.02f);
        Carpet(g, "EntranceMat_Inner", 0, -7.1f, 2.9f, 1.4f, matNavy2M, 0.03f);
        // paper source
        Carpet(g, "PaperZone_Carpet", 0, -1.2f, 11.0f, 6.6f, sandM);
        // workstations
        Carpet(g, "WorkZone_Carpet", 0, 6.4f, 7.6f, 7.8f, blueM);
        // reception / manager parquet
        Carpet(g, "ManagerZone_Wood", 0, 13.15f, 11.6f, 3.6f, woodM);
        // paper area edge stripe + manager rug
        Carpet(g, "ManagerRug", -2.6f, 13.1f, 3.4f, 2.4f, matNavy2M, 0.03f);
    }

    // ------------------------------------------------------------------------------------------------ props

    static void BuildFurniture(Transform g)
    {
        // plants
        Plant(g, new Vector3(-5.2f, 0, -7.0f), 1.0f); Plant(g, new Vector3(5.2f, 0, -7.0f), 1.0f);
        Plant(g, new Vector3(-5.35f, 0, 3.0f), 0.9f); Plant(g, new Vector3(5.35f, 0, 3.0f), 0.9f);
        Plant(g, new Vector3(-5.4f, 0, 11.7f), 1.0f); Plant(g, new Vector3(5.4f, 0, 14.2f), 1.1f);

        // paper area: filing cabinets along both side walls
        foreach (int s in new[] { -1, 1 })
        {
            Cabinet(g, new Vector3(s * 5.45f, 0, -3.2f), s);
            Cabinet(g, new Vector3(s * 5.45f, 0, -2.2f), s);
        }
        Bin(g, new Vector3(-5.2f, 0, -5.2f)); Bin(g, new Vector3(5.2f, 0, -5.2f));

        // low partitions that split the paper area from the workstations
        foreach (int s in new[] { -1, 1 })
            Partition(g, new Vector3(s * 4.85f, 0, 2.5f), 2.3f);
        // workstation side shelves
        Shelf(g, new Vector3(-5.5f, 0, 6.0f), true, 0.5f, 1.7f);
        Shelf(g, new Vector3(5.5f, 0, 6.0f), false, 0.5f, 1.7f);
        Bin(g, new Vector3(-4.7f, 0, 9.4f)); Bin(g, new Vector3(4.7f, 0, 9.4f));
        Cabinet(g, new Vector3(-5.45f, 0, 8.4f), -1);
        Cabinet(g, new Vector3(5.45f, 0, 8.4f), 1);

        // partition between workstations and the manager area
        Partition(g, new Vector3(-3.4f, 0, 11.0f), 4.8f);
        Partition(g, new Vector3(3.4f, 0, 11.0f), 4.8f);

        // manager corner shelf (back wall) + reception side cabinet
        ShelfBack(g, new Vector3(-5.1f, 0, 14.7f), 1.6f, 2.1f);
        Cabinet(g, new Vector3(5.45f, 0, 12.2f), 1);
    }

    static void Plant(Transform g, Vector3 p, float s)
    {
        var root = Group(g, "Plant");
        root.localPosition = p;
        P(root, PrimitiveType.Cylinder, "Pot", new Vector3(0, 0.3f * s, 0), new Vector3(0.55f, 0.3f, 0.55f) * s, potM, true);
        P(root, PrimitiveType.Sphere, "Leaf_A", new Vector3(0, 0.95f * s, 0), new Vector3(0.75f, 0.75f, 0.75f) * s, leafM);
        P(root, PrimitiveType.Sphere, "Leaf_B", new Vector3(0.22f * s, 1.3f * s, 0.05f * s), new Vector3(0.5f, 0.5f, 0.5f) * s, leaf2M);
        P(root, PrimitiveType.Sphere, "Leaf_C", new Vector3(-0.2f * s, 1.15f * s, -0.12f * s), new Vector3(0.45f, 0.45f, 0.45f) * s, leafM);
    }

    static void Bin(Transform g, Vector3 p)
    {
        var root = Group(g, "Bin");
        root.localPosition = p;
        P(root, PrimitiveType.Cylinder, "Body", new Vector3(0, 0.27f, 0), new Vector3(0.42f, 0.27f, 0.42f), binM, true);
        P(root, PrimitiveType.Cylinder, "Rim", new Vector3(0, 0.55f, 0), new Vector3(0.46f, 0.02f, 0.46f), metalM);
    }

    /// <summary>Cabinet against a side wall; facing = +1 means the drawers face +x.</summary>
    static void Cabinet(Transform g, Vector3 p, int wallSide)
    {
        var root = Group(g, "Cabinet");
        root.localPosition = p;
        float face = -wallSide * 0.36f;
        Box(root, "Body", new Vector3(0, 0.55f, 0), new Vector3(0.75f, 1.1f, 0.85f), cabM, true);
        Box(root, "Top", new Vector3(0, 1.12f, 0), new Vector3(0.8f, 0.05f, 0.9f), cabTopM);
        foreach (float y in new[] { 0.3f, 0.62f, 0.94f })
        {
            Box(root, "Handle", new Vector3(face, y, 0), new Vector3(0.04f, 0.04f, 0.3f), handleM);
            Box(root, "Gap", new Vector3(face * 1.02f, y - 0.16f, 0), new Vector3(0.02f, 0.02f, 0.8f), metalM);
        }
    }

    static void Partition(Transform g, Vector3 p, float len)
    {
        var root = Group(g, "Partition");
        root.localPosition = p;
        Box(root, "Panel", new Vector3(0, 0.5f, 0), new Vector3(len, 1.0f, 0.14f), partM, true);
        Box(root, "Cap", new Vector3(0, 1.02f, 0), new Vector3(len + 0.04f, 0.06f, 0.2f), capM);
    }

    /// <summary>Bookshelf against a side wall (sideLeft: wall on -x).</summary>
    static void Shelf(Transform g, Vector3 p, bool sideLeft, float depth, float width)
    {
        var root = Group(g, "Shelf");
        root.localPosition = p;
        root.localEulerAngles = new Vector3(0, sideLeft ? -90f : 90f, 0);
        BuildShelfBody(root, width, 1.55f, depth);
    }

    static void ShelfBack(Transform g, Vector3 p, float width, float height)
    {
        var root = Group(g, "Shelf_Back");
        root.localPosition = p;
        BuildShelfBody(root, width, height, 0.5f);
    }

    static void BuildShelfBody(Transform root, float w, float h, float d)
    {
        Box(root, "Frame", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), woodDarkM, true);
        int rows = Mathf.Max(2, Mathf.RoundToInt(h / 0.5f));
        float rowH = (h - 0.1f) / rows;
        var mats = new[] { book1, book2, book3, book4, whiteM };
        for (int r = 0; r < rows; r++)
        {
            float y = 0.05f + rowH * r + rowH * 0.5f;
            Box(root, "Inset", new Vector3(0, y, -d * 0.5f + 0.01f), new Vector3(w - 0.12f, rowH - 0.06f, 0.02f), woodM);
            float x = -w * 0.5f + 0.14f;
            int i = r * 3;
            while (x < w * 0.5f - 0.14f)
            {
                float bw = 0.07f + ((i * 37) % 5) * 0.015f;
                float bh = rowH * (0.62f + ((i * 53) % 4) * 0.08f);
                Box(root, "Book", new Vector3(x + bw * 0.5f, y - rowH * 0.5f + 0.04f + bh * 0.5f, -d * 0.5f + 0.1f), new Vector3(bw, bh, d * 0.45f), mats[(i + r) % mats.Length]);
                x += bw + 0.012f; i++;
            }
        }
    }

    // ------------------------------------------------------------------------------------------------ people / desks

    static void BuildPeopleAndDesks(Transform g, GameObject reception, GameObject manager)
    {
        // manager desk: wood top, side panels, modesty panel. Avatar sits behind it facing the camera.
        var md = Group(g, "ManagerDesk");
        md.localPosition = new Vector3(-2.6f, 0, 13.1f);
        Box(md, "Top", new Vector3(0, 0.8f, 0), new Vector3(2.5f, 0.08f, 0.95f), woodM, true);
        foreach (int s in new[] { -1, 1 }) Box(md, "Side", new Vector3(s * 1.12f, 0.38f, 0), new Vector3(0.1f, 0.76f, 0.85f), woodDarkM);
        Box(md, "Front", new Vector3(0, 0.45f, -0.38f), new Vector3(2.2f, 0.6f, 0.06f), woodDarkM);
        Box(md, "PaperStack", new Vector3(-0.8f, 0.87f, -0.1f), new Vector3(0.3f, 0.07f, 0.4f), whiteM);
        Box(md, "NamePlate", new Vector3(0.6f, 0.86f, -0.28f), new Vector3(0.5f, 0.05f, 0.1f), metalM);
        Place(manager, g, new Vector3(-2.6f, 0, 14.0f), "Manager");

        // reception counter
        var rc = Group(g, "ReceptionCounter");
        rc.localPosition = new Vector3(3.5f, 0, 12.5f);
        Box(rc, "Front", new Vector3(0, 0.45f, 0), new Vector3(2.8f, 0.9f, 0.4f), counterM, true);
        Box(rc, "Top", new Vector3(0, 0.93f, 0.05f), new Vector3(3.0f, 0.07f, 0.7f), counterTopM);
        Box(rc, "ReturnL", new Vector3(-1.35f, 0.45f, 0.6f), new Vector3(0.3f, 0.9f, 0.9f), counterM, true);
        Box(rc, "Bell", new Vector3(0.9f, 1.0f, 0), new Vector3(0.14f, 0.06f, 0.14f), metalM);
        Box(rc, "Monitor", new Vector3(-0.4f, 1.22f, 0.25f), new Vector3(0.7f, 0.42f, 0.05f), deskBackM);
        Box(rc, "MonitorStand", new Vector3(-0.4f, 1.0f, 0.25f), new Vector3(0.1f, 0.1f, 0.1f), deskBackM);
        Place(reception, g, new Vector3(3.5f, 0, 13.45f), "Receptionist");
    }

    static void Place(GameObject prefab, Transform parent, Vector3 pos, string name)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, 180f, 0);   // faces the camera
    }

    // ------------------------------------------------------------------------------------------------ avatar prefab

    static GameObject BuildAvatarPrefab(string name, Color shirt, Color hair, OfficeAvatar.Role role, bool monitor, bool tie)
    {
        var shirtM = new Material(Shader.Find("Standard")) { color = shirt };
        shirtM.SetFloat("_Glossiness", 0.1f);
        string shirtPath = $"{MatDir}/Shirt_{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(shirtPath);
        if (existing != null) { existing.color = shirt; shirtM = existing; } else AssetDatabase.CreateAsset(shirtM, shirtPath);
        string hairPath = $"{MatDir}/Hair_{name}.mat";
        var hairM = AssetDatabase.LoadAssetAtPath<Material>(hairPath);
        if (hairM == null) { hairM = new Material(Shader.Find("Standard")); hairM.SetFloat("_Glossiness", 0.1f); AssetDatabase.CreateAsset(hairM, hairPath); }
        hairM.color = hair;

        var root = new GameObject(name).transform;

        // chair
        var chair = Group(root, "Chair");
        P(chair, PrimitiveType.Cylinder, "Base", new Vector3(0, 0.05f, 0), new Vector3(0.55f, 0.03f, 0.55f), metalM);
        P(chair, PrimitiveType.Cylinder, "Post", new Vector3(0, 0.3f, -0.03f), new Vector3(0.07f, 0.22f, 0.07f), metalM);
        Box(chair, "Seat", new Vector3(0, 0.55f, -0.02f), new Vector3(0.66f, 0.1f, 0.62f), chairM);
        Box(chair, "Back", new Vector3(0, 0.95f, -0.33f), new Vector3(0.6f, 0.66f, 0.09f), seatAccentM);

        // legs
        foreach (int s in new[] { -1, 1 })
        {
            P(root, PrimitiveType.Capsule, "Thigh", new Vector3(s * 0.14f, 0.66f, 0.25f), new Vector3(0.18f, 0.27f, 0.18f), pantsM, false, new Vector3(90, 0, 0));
            P(root, PrimitiveType.Capsule, "Shin", new Vector3(s * 0.14f, 0.32f, 0.5f), new Vector3(0.16f, 0.3f, 0.16f), pantsM);
        }

        // torso (pivot at the hips so leaning looks right)
        var torso = Group(root, "TorsoPivot");
        torso.localPosition = new Vector3(0, 0.62f, -0.02f);
        P(torso, PrimitiveType.Capsule, "Torso", new Vector3(0, 0.3f, 0), new Vector3(0.46f, 0.32f, 0.4f), shirtM);
        if (tie) Box(torso, "Tie", new Vector3(0, 0.38f, 0.19f), new Vector3(0.08f, 0.3f, 0.03f), tieM);

        var headPivot = Group(torso, "HeadPivot");
        headPivot.localPosition = new Vector3(0, 0.66f, 0);
        P(headPivot, PrimitiveType.Sphere, "Head", new Vector3(0, 0.2f, 0), new Vector3(0.54f, 0.54f, 0.54f), skinM);
        P(headPivot, PrimitiveType.Sphere, "Hair", new Vector3(0, 0.29f, -0.04f), new Vector3(0.57f, 0.4f, 0.57f), hairM);
        foreach (int s in new[] { -1, 1 })
            P(headPivot, PrimitiveType.Sphere, "Eye", new Vector3(s * 0.09f, 0.19f, 0.245f), new Vector3(0.055f, 0.065f, 0.04f), eyeM);

        Transform MakeArm(string n, int s)
        {
            var pivot = Group(torso, n);
            pivot.localPosition = new Vector3(s * 0.29f, 0.55f, 0);
            P(pivot, PrimitiveType.Capsule, "Arm", new Vector3(0, -0.2f, 0), new Vector3(0.13f, 0.21f, 0.13f), shirtM);
            P(pivot, PrimitiveType.Sphere, "Hand", new Vector3(0, -0.42f, 0), new Vector3(0.14f, 0.14f, 0.14f), skinM);
            return pivot;
        }
        var armL = MakeArm("ArmLeft", -1);
        var armR = MakeArm("ArmRight", 1);

        if (monitor)
        {
            // desk-top gear in front of the seat; the camera sees the back of the monitor
            var gear = Group(root, "DeskGear");
            Box(gear, "Keyboard", new Vector3(0, DeskTop + 0.02f, 0.62f), new Vector3(0.42f, 0.03f, 0.15f), whiteM);
            P(gear, PrimitiveType.Cylinder, "Stand", new Vector3(0, DeskTop + 0.1f, 0.95f), new Vector3(0.09f, 0.1f, 0.09f), deskBackM);
            Box(gear, "ScreenBack", new Vector3(0, DeskTop + 0.38f, 0.96f), new Vector3(0.78f, 0.46f, 0.05f), deskBackM);
            Box(gear, "ScreenFace", new Vector3(0, DeskTop + 0.38f, 0.93f), new Vector3(0.72f, 0.4f, 0.01f), screenM);
        }

        var av = root.gameObject.AddComponent<OfficeAvatar>();
        var so = new SerializedObject(av);
        so.FindProperty("role").enumValueIndex = (int)role;
        so.FindProperty("torso").objectReferenceValue = torso;
        so.FindProperty("head").objectReferenceValue = headPivot;
        so.FindProperty("armLeft").objectReferenceValue = armL;
        so.FindProperty("armRight").objectReferenceValue = armR;
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = $"{AvatarDir}/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        Object.DestroyImmediate(root.gameObject);
        return prefab;
    }

    // ------------------------------------------------------------------------------------------------ gameplay layout

    static void LayoutGameplayObjects()
    {
        var factory = Object.FindFirstObjectByType<OfficeFactory>();
        var canvas = GameObject.Find("WorldSpaceCanvas");

        // workstation spawn points: first one is the unlocked desk, second one is the locked desk behind it
        var pts = factory.spawnPoints;
        pts[0].position = new Vector3(-0.3f, 0.05f, 4.6f);
        pts[1].position = new Vector3(-0.3f, 0.05f, 8.8f);
        if (pts.Length > 2)
        {
            for (int i = 2; i < pts.Length; i++) if (pts[i] != null) Object.DestroyImmediate(pts[i].gameObject);
            var so = new SerializedObject(factory);
            so.FindProperty("spawnPoints").arraySize = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // paper source: both generators side by side, centre corridor kept open
        MoveGenerator(canvas.transform.Find("PaperGeneratorObject"), -2.8f);
        MoveGenerator(canvas.transform.Find("PaperGeneratorObject (1)"), 2.8f);

        // the old oversized ground becomes the exterior; the room slab sits on top of it
        var ground = GameObject.Find("Map/Ground");
        if (ground != null) ground.GetComponent<Renderer>().sharedMaterial = outsideM;

        var player = GameObject.Find("Player");
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            bool was = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            player.transform.position = new Vector3(0, 0, -5.4f);
            if (cc != null) cc.enabled = was;
        }
        var cam = Camera.main;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) cam.transform.position = new Vector3(0, 0, -5.4f) + follow.offset;
    }

    static void MoveGenerator(Transform gen, float x)
    {
        if (gen == null) return;
        // zone row (collect zone) is the anchor: its z lands at -3.6
        var zone = gen.Find("PaperCollectZone");
        var delta = new Vector3(x - gen.position.x, 0, -3.6f - zone.position.z);
        gen.position += delta;
    }

    // ------------------------------------------------------------------------------------------------ office prefab

    static void AddWorkersToOfficePrefab(GameObject avatarPrefab)
    {
        const string path = "Assets/Prefabs/OfficeObject.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);
        var worker = contents.GetComponentInChildren<OfficeWorker>(true);
        var wt = worker.transform;

        var oldSeat = wt.Find("WorkerSeat");
        if (oldSeat != null) Object.DestroyImmediate(oldSeat.gameObject);

        // prefab space is the canvas space: x right, local y = world z, local -z = world up, 1 unit = 0.08 m
        const float unit = 0.08f;
        var toLocal = Quaternion.Euler(-90, 0, 0);
        Vector3 worldOffset = new Vector3(0, 0, 1.1f);    // behind the desk (world +z), relative to the office root
        Vector3 local = toLocal * worldOffset / unit - wt.localPosition;

        var seat = (GameObject)PrefabUtility.InstantiatePrefab(avatarPrefab, wt);
        seat.name = "WorkerSeat";
        seat.transform.localPosition = local;
        seat.transform.localRotation = toLocal * Quaternion.Euler(0, 180f, 0);
        seat.transform.localScale = Vector3.one / unit;

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    // ------------------------------------------------------------------------------------------------ lighting

    static void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = H("#C9D6E6");
        RenderSettings.ambientEquatorColor = H("#AEB7C4");
        RenderSettings.ambientGroundColor = H("#8A8580");
        RenderSettings.fog = false;

        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = H("#A9BAD0");

        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            var l = sun.GetComponent<Light>();
            l.color = H("#FFF1D9");
            l.intensity = 1.05f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        }
    }
}
#endif
