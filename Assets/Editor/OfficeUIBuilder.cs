#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Idempotent builder for the gameplay UI style: one rounded white "chip" language, navy bold text, green/red price states.
/// Restyles the existing HUD objects in place (so HUDManager / PausePanel references stay valid).
/// </summary>
public static class OfficeUIBuilder
{
    const string UiDir = "Assets/Textures/UI";

    static readonly Color Navy = new Color32(0x2B, 0x3A, 0x55, 255);
    static readonly Color Panel = new Color(1f, 1f, 1f, 0.94f);
    static readonly Color Shadow = new Color32(0x1B, 0x2A, 0x45, 56);
    static readonly Color Blue = new Color32(0x4D, 0xA3, 0xFF, 255);

    static Sprite pill, card, circle, iconPaper, iconBolt;

    [MenuItem("Tools/Office/Build UI")]
    public static void Build()
    {
        MakeSprites();
        SetupUpgradeManager();
        BuildHud();
        StyleWorldPriceUI();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Office UI built.");
    }

    // ------------------------------------------------------------------------------------------------ sprites

    static void MakeSprites()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Textures/UI")) AssetDatabase.CreateFolder("Assets/Textures", "UI");

        pill = SaveSprite("UI_Pill", RoundedRect(128, 64), new Vector4(60, 60, 60, 60));
        card = SaveSprite("UI_Card", RoundedRect(128, 34), new Vector4(40, 40, 40, 40));
        circle = SaveSprite("UI_Circle", RoundedRect(128, 64), Vector4.zero);
        iconPaper = SaveSprite("UI_IconPaper", PaperIcon(128), Vector4.zero);
        iconBolt = SaveSprite("UI_IconBolt", BoltIcon(128), Vector4.zero);
    }

    static Texture2D RoundedRect(int size, float radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float d = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
        return tex;
    }

    static Texture2D PaperIcon(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color line = new Color32(0x9A, 0xA7, 0xBA, 255);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                // sheet 0.25..0.75 x 0.12..0.88 with rounded corners
                float px = Mathf.Abs(u - 0.5f) - 0.25f + 0.06f, py = Mathf.Abs(v - 0.5f) - 0.38f + 0.06f;
                float d = (new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - 0.06f) * size;
                float a = Mathf.Clamp01(0.5f - d);
                Color c = Color.white;
                foreach (float ly in new[] { 0.32f, 0.47f, 0.62f })
                    if (Mathf.Abs(v - ly) < 0.025f && u > 0.33f && u < 0.67f) c = line;
                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
            }
        return tex;
    }

    static Texture2D BoltIcon(int size)
    {
        var poly = new[] { new Vector2(0.58f, 0.92f), new Vector2(0.24f, 0.46f), new Vector2(0.45f, 0.46f), new Vector2(0.38f, 0.08f), new Vector2(0.76f, 0.58f), new Vector2(0.55f, 0.58f) };
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cover = 0;
                for (int sy = 0; sy < 3; sy++) for (int sx = 0; sx < 3; sx++)
                    if (Inside(poly, new Vector2((x + (sx + 0.5f) / 3f) / size, (y + (sy + 0.5f) / 3f) / size))) cover += 1f / 9f;
                tex.SetPixel(x, y, new Color(1, 1, 1, cover));
            }
        return tex;
    }

    static bool Inside(Vector2[] poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        return inside;
    }

    static Sprite SaveSprite(string name, Texture2D tex, Vector4 border)
    {
        string path = $"{UiDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spriteBorder = border;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ------------------------------------------------------------------------------------------------ helpers

    static RectTransform Rect(Transform parent, string name)
    {
        var existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Anchor(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
        r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
    }

    static Image Img(RectTransform r, Sprite s, Color c, bool sliced, bool raycast = false)
    {
        var im = r.GetComponent<Image>() ?? r.gameObject.AddComponent<Image>();
        im.sprite = s; im.color = c;
        im.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        im.preserveAspect = !sliced;
        im.raycastTarget = raycast;
        return im;
    }

    static TextMeshProUGUI Txt(RectTransform r, string text, float size, Color color, TextAlignmentOptions align)
    {
        var t = r.GetComponent<TextMeshProUGUI>() ?? r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
        t.fontStyle = FontStyles.Bold; t.enableAutoSizing = false; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false;
        return t;
    }

    static void Clear(Transform parent, string name)
    {
        var t = parent.Find(name);
        while (t != null) { Object.DestroyImmediate(t.gameObject); t = parent.Find(name); }
    }

    /// <summary>r already carries the Image; a sibling shadow is placed right behind it (children would draw on top).</summary>
    static Image ChipSibling(RectTransform r, Sprite s, bool sliced, Color body, bool raycast = false, float offset = 8f)
    {
        Clear(r.parent, r.name + "_Shadow");
        var sh = new GameObject(r.name + "_Shadow", typeof(RectTransform)).GetComponent<RectTransform>();
        sh.SetParent(r.parent, false);
        sh.anchorMin = r.anchorMin; sh.anchorMax = r.anchorMax; sh.pivot = r.pivot;
        sh.offsetMin = r.offsetMin; sh.offsetMax = r.offsetMax;
        sh.anchoredPosition = r.anchoredPosition + new Vector2(0, -offset);
        sh.sizeDelta = r.sizeDelta;
        sh.SetSiblingIndex(r.GetSiblingIndex());
        Img(sh, s, Shadow, sliced);
        return Img(r, s, body, sliced, raycast);
    }

    /// <summary>r is a plain container: it gets a stretched Shadow and Body child, so r can be scaled/shaken as one piece.</summary>
    static Image ChipContainer(RectTransform r, Sprite s, bool sliced, Color body, bool raycast = false, float offset = 8f)
    {
        Clear(r, "Shadow"); Clear(r, "Body");
        var sh = Rect(r, "Shadow");
        sh.anchorMin = Vector2.zero; sh.anchorMax = Vector2.one; sh.pivot = new Vector2(0.5f, 0.5f);
        sh.offsetMin = new Vector2(0, -offset); sh.offsetMax = new Vector2(0, -offset);
        Img(sh, s, Shadow, sliced);
        var bd = Rect(r, "Body");
        bd.anchorMin = Vector2.zero; bd.anchorMax = Vector2.one; bd.pivot = new Vector2(0.5f, 0.5f);
        bd.offsetMin = bd.offsetMax = Vector2.zero;
        sh.SetAsFirstSibling(); bd.SetSiblingIndex(1);
        return Img(bd, s, body, sliced, raycast);
    }

    // ------------------------------------------------------------------------------------------------ manager

    static void SetupUpgradeManager()
    {
        var managers = GameObject.Find("Managers");
        var existing = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
        GameObject go = existing != null ? existing.gameObject : new GameObject("UpgradeManager");
        if (existing == null) go.AddComponent<UpgradeManager>();
        if (managers != null) go.transform.SetParent(managers.transform, false);

        var so = new SerializedObject(go.GetComponent<UpgradeManager>());
        so.FindProperty("purchaseSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Clips/SFX/Coin01.aif");
        so.FindProperty("deniedSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Clips/SFX/slide.aif");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------------------------------------ HUD

    static void BuildHud()
    {
        var hud = GameObject.Find("HudCanvas");
        var root = hud.transform.Find("Root");
        var moneyIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/money1.png");
        var pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Icon_PictoIcon_Pause.png");

        // --- money (priority 1)
        var coins = (RectTransform)root.Find("TopBar - Rect/Coins - Rect") ?? (RectTransform)root.Find("Coins - Rect");
        coins.SetParent(root, false);
        // the original layout group would re-position the icon/amount and our shadow sibling
        var hlg = coins.GetComponent<HorizontalLayoutGroup>(); if (hlg != null) Object.DestroyImmediate(hlg);
        var le = coins.Find("Background").GetComponent<LayoutElement>(); if (le != null) Object.DestroyImmediate(le);
        Anchor(coins, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -56), new Vector2(380, 118));
        var bg = coins.Find("Background") as RectTransform;
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
        Clear(bg, "Shadow");
        ChipSibling(bg, pill, true, new Color32(0x2B, 0x3A, 0x55, 245));
        var icon = coins.Find("Icon") as RectTransform;
        Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(92, 92));
        Img(icon, moneyIcon, Color.white, false);
        var amount = coins.Find("Amount") as RectTransform;
        Anchor(amount, new Vector2(0, 0), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
        amount.anchorMin = new Vector2(0, 0); amount.anchorMax = new Vector2(1, 1);
        amount.offsetMin = new Vector2(120, 0); amount.offsetMax = new Vector2(-34, 0);
        Txt(amount, "0", 66, Color.white, TextAlignmentOptions.MidlineRight);

        // --- capacity chip (priority 2.5): carried / max
        Clear(root, "CapacityChip");
        var chipRt = Rect(root, "CapacityChip");
        Anchor(chipRt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -186), new Vector2(236, 72));
        ChipContainer(chipRt, pill, true, Panel, false, 7f);
        var chipIcon = Rect(chipRt, "Icon"); Anchor(chipIcon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(46, 46));
        Img(chipIcon, iconPaper, Blue, false); chipIcon.GetComponent<Image>().color = Color.white;
        var chipIconBg = Rect(chipRt, "IconBg"); chipIconBg.SetSiblingIndex(chipIcon.GetSiblingIndex());
        Anchor(chipIconBg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(56, 56)); Img(chipIconBg, circle, Blue, false);
        chipIcon.SetAsLastSibling();
        var chipTxt = Rect(chipRt, "Label"); chipTxt.anchorMin = Vector2.zero; chipTxt.anchorMax = Vector2.one;
        chipTxt.offsetMin = new Vector2(78, 0); chipTxt.offsetMax = new Vector2(-22, 0);
        var chipLabel = Txt(chipTxt, "0/20", 40, Navy, TextAlignmentOptions.MidlineRight);
        var chipComp = chipRt.GetComponent<CapacityChip>() ?? chipRt.gameObject.AddComponent<CapacityChip>();
        var cso = new SerializedObject(chipComp);
        cso.FindProperty("label").objectReferenceValue = chipLabel;
        cso.FindProperty("root").objectReferenceValue = chipRt;
        cso.ApplyModifiedPropertiesWithoutUndo();

        // --- pause (priority 4): small, secondary
        var pauseBtn = root.Find("LeftBar - Rect/PauseButton") ?? root.Find("PauseButton");
        var pauseRt = (RectTransform)pauseBtn;
        pauseRt.SetParent(root, false);
        Anchor(pauseRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -56), new Vector2(98, 98));
        var pauseImg = pauseRt.GetComponent<Image>();
        pauseImg.sprite = circle; pauseImg.type = Image.Type.Simple; pauseImg.color = new Color(1, 1, 1, 0.9f); pauseImg.raycastTarget = true;
        Clear(pauseRt, "Shadow");
        ChipSibling(pauseRt, circle, false, new Color(1, 1, 1, 0.92f), true, 7f);
        var pGlyph = Rect(pauseRt, "Glyph");
        Anchor(pGlyph, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));
        Img(pGlyph, pauseIcon, Navy, false);
        var pbtn = pauseRt.GetComponent<Button>();
        if (pbtn != null)
        {
            pbtn.targetGraphic = pauseImg; pbtn.transition = Selectable.Transition.ColorTint;
            var cb = pbtn.colors; cb.normalColor = Color.white; cb.highlightedColor = Color.white; cb.pressedColor = new Color(0.82f, 0.86f, 0.92f, 1f); cb.selectedColor = Color.white;
            pbtn.colors = cb;
        }

        // --- upgrades live in the world now (UpgradePadZone stations, see OfficeFinalPolishBuilder): no HUD cards
        foreach (var n in new[] { "Upgrade_Capacity", "Upgrade_Speed" }) Clear(root, n);

        // old empty bars are no longer needed
        foreach (var n in new[] { "TopBar - Rect", "LeftBar - Rect" })
        {
            var bar = root.Find(n);
            if (bar != null && bar.childCount == 0) Object.DestroyImmediate(bar.gameObject);
        }

        // --- joystick: visually secondary
        var visual = root.Find("Circle Joystick Hitbox (LeanJoystick + LeanHitbox)/VisualObject/Visual");
        var handle = root.Find("Circle Joystick Hitbox (LeanJoystick + LeanHitbox)/VisualObject/Handle");
        if (visual != null) { var im = visual.GetComponent<Image>(); im.color = new Color(1, 1, 1, 0.28f); ((RectTransform)visual).sizeDelta = new Vector2(190, 190); }
        if (handle != null) { var im = handle.GetComponent<Image>(); im.color = new Color(1, 1, 1, 0.5f); ((RectTransform)handle).sizeDelta = new Vector2(64, 64); }

        StylePausePanel(root);

        // the chip must sit above the full-screen joystick hitbox
        foreach (var n in new[] { "CapacityChip" })
            root.Find(n).SetAsLastSibling();
        // keep the pause panel on top of everything
        var pp = root.Find("PausePanel"); if (pp != null) pp.SetAsLastSibling();
    }

    static void StylePausePanel(Transform root)
    {
        var bg = root.Find("PausePanel/Root/Root/BG");
        if (bg == null) return;
        var im = bg.GetComponent<Image>() ?? bg.gameObject.AddComponent<Image>();
        im.sprite = card; im.type = Image.Type.Sliced; im.color = new Color32(0xEE, 0xF4, 0xFB, 255);
        // the close button was a bare white square: a round red button with an X
        var close = bg.Find("TopLabel/CloseButton") as RectTransform;
        if (close != null)
        {
            var cim = close.GetComponent<Image>();
            cim.sprite = circle; cim.type = Image.Type.Simple; cim.color = new Color32(0xE5, 0x58, 0x4F, 255);
            var x = close.Find("X") as RectTransform ?? Rect(close, "X");
            x.anchorMin = Vector2.zero; x.anchorMax = Vector2.one; x.offsetMin = x.offsetMax = Vector2.zero; x.localScale = Vector3.one;
            Txt(x, "X", 44, Color.white, TextAlignmentOptions.Center);
        }
        // consistent navy text on the settings rows
        foreach (var t in bg.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.transform.parent.name == "Title - Rect") continue;   // title stays white on the blue flag
            t.color = Navy; t.fontStyle = FontStyles.Bold;
        }
    }

    // ------------------------------------------------------------------------------------------------ world price

    static void StyleWorldPriceUI()
    {
        const string path = "Assets/Prefabs/OfficeObject.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);
        var zone = contents.GetComponentInChildren<OfficeGeneratorZone>(true);
        var zt = zone.transform;
        var moneyIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/money1.png");

        // bubble behind the price: sits in front of the ring, behind the text
        var old = zt.Find("PriceBubble"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var bubble = Rect(zt, "PriceBubble");
        Anchor(bubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(26, 11));
        ChipContainer(bubble, pill, true, new Color(1, 1, 1, 0.97f), false, 1.2f);

        var priceRt = (RectTransform)zt.Find("Text - Price");
        var priceText = priceRt.GetComponent<TextMeshProUGUI>();
        var coin = (RectTransform)zt.Find("Image");

        bubble.SetSiblingIndex(priceRt.GetSiblingIndex());
        coin.SetAsLastSibling(); priceRt.SetAsLastSibling();
        Anchor(coin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-8.6f, 0), new Vector2(7.5f, 7.5f));
        coin.GetComponent<Image>().sprite = moneyIcon;
        Anchor(priceRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(3.5f, 0), new Vector2(15, 9));
        priceText.fontSize = 7.5f; priceText.fontStyle = FontStyles.Bold; priceText.alignment = TextAlignmentOptions.MidlineLeft;
        priceText.textWrappingMode = TextWrappingModes.NoWrap; priceText.overflowMode = TextOverflowModes.Overflow;
        priceText.color = new Color32(0x2F, 0xA8, 0x66, 255);

        // ring: calmer colours, the fill reads as "payment progress"
        var ringMask = zt.Find("Image -Rect/Image - Mask");
        var ringProg = zt.Find("Image -Rect/Image - Mask/Image - Progress");
        if (ringMask != null) ringMask.GetComponent<Image>().color = new Color(1, 1, 1, 0.85f);
        if (ringProg != null) ringProg.GetComponent<Image>().color = new Color32(0xFF, 0xC5, 0x31, 255);

        var so = new SerializedObject(zone);
        so.FindProperty("pulseTarget").objectReferenceValue = bubble;
        so.FindProperty("deniedSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Clips/SFX/slide.aif");
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}
#endif
