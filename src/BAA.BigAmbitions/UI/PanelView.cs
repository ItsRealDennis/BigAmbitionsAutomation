using System;
using System.Collections.Generic;
using BAA.Core.Config;
using BaBot.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace BaBot.UI;

/// <summary>
/// The F8 in-game overlay — a rounded, slate/cyan, game-matching panel built at runtime from Unity's
/// own uGUI components (so it always instantiates, unlike a mod-defined MonoBehaviour). Driven by
/// BaBotLogic via UnityLifecycleProvider.OnUpdate. Rounded corners come from a 9-sliced sprite tinted
/// per element; the canvas scales by height so it reads well on ultrawide.
/// </summary>
internal sealed class PanelView
{
    private GameObject _root;
    private Font _font;
    private Text _status;
    private Text _activity;

    private sealed class ToggleRow { public Image Pill; public Text PillText; public Text Label; public Func<bool> Get; public string Name; }
    private sealed class StepRow { public Text Label; public Func<string> Format; }
    private readonly List<ToggleRow> _toggles = new();
    private readonly List<StepRow> _steppers = new();
    private static readonly Dictionary<int, Sprite> _sprites = new();

    private static readonly Color Slate   = new(0.137f, 0.165f, 0.212f, 0.98f); // BA dark blue-grey body
    private static readonly Color Chrome  = new(0.85f, 0.87f, 0.90f, 1f);        // light window top strip
    private static readonly Color Inset   = new(0.082f, 0.10f, 0.13f, 1f);
    private static readonly Color RowBg   = new(0.18f, 0.22f, 0.28f, 1f);
    private static readonly Color Blue    = new(0.24f, 0.53f, 0.95f, 1f);        // BA primary blue
    private static readonly Color Cyan    = new(0.40f, 0.78f, 1f, 1f);
    private static readonly Color Green   = new(0.30f, 0.72f, 0.42f, 1f);
    private static readonly Color Red     = new(0.91f, 0.30f, 0.24f, 1f);
    private static readonly Color GreyOff = new(0.30f, 0.35f, 0.42f, 1f);
    private static readonly Color White   = new(0.96f, 0.97f, 0.99f, 1f);
    private static readonly Color Dim     = new(0.66f, 0.73f, 0.82f, 1f);
    private static readonly Color Dark    = new(0.05f, 0.10f, 0.14f, 1f);

    private const float W = 480f, H = 900f, Pad = 20f;

    public bool Built => _root != null;
    public void SetVisible(bool v) { if (_root != null && _root.activeSelf != v) _root.SetActive(v); }
    public void Destroy() { if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; } }

    public void Build(AutomationConfig cfg, Action runNow, Action onClose)
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) { try { _font = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { } }

        _root = new GameObject("BA BOT Canvas");
        UnityEngine.Object.DontDestroyOnLoad(_root);
        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 960); // lower ref height => larger UI (~1.5x at 1440p)
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // scale by height — consistent on ultrawide
        _root.AddComponent<GraphicRaycaster>();

        var win = Panel(_root.transform, "Window", 28, 28, W, H, Slate, 16).transform;

        // BA-style header: light window-chrome strip, title, red close button
        Panel(win, "Chrome", 0, 0, W, 12, Chrome, 16);
        MkText(win, "Title", Pad, 20, W - 2 * Pad - 44, 30, 24, White, TextAnchor.MiddleLeft, FontStyle.Bold).text = "BA BOT";
        MkText(win, "Sub", Pad, 48, W - 2 * Pad - 44, 18, 12, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("AUTOMATION CONTROL");
        Btn(win, "Close", W - Pad - 30, 20, 30, 28, Red, White, "X", 14, () => { try { onClose(); } catch { } });

        float y = 84;
        Panel(win, "StatusBg", Pad, y, W - 2 * Pad, 104, Inset, 10);
        _status = MkText(win, "Status", Pad + 16, y + 12, W - 2 * Pad - 32, 84, 15, White, TextAnchor.UpperLeft, FontStyle.Normal);
        _status.lineSpacing = 1.15f;
        y += 104 + 16;

        AddToggle(win, ref y, "AUTOMATION (MASTER)", () => cfg.MasterEnabled, v => cfg.MasterEnabled = v);
        AddToggle(win, ref y, "FINANCE AUTO-PAY", () => cfg.FinanceEnabled, v => cfg.FinanceEnabled = v);
        AddToggle(win, ref y, "EMPLOYEES", () => cfg.EmployeesEnabled, v => cfg.EmployeesEnabled = v);
        AddToggle(win, ref y, "LOGISTICS", () => cfg.LogisticsEnabled, v => cfg.LogisticsEnabled = v);
        AddToggle(win, ref y, "AUTO-RESTOCK", () => cfg.RestockEnabled, v => cfg.RestockEnabled = v);
        AddToggle(win, ref y, "AUTO-WELLBEING", () => cfg.WellbeingEnabled, v => cfg.WellbeingEnabled = v);
        AddToggle(win, ref y, "SERVICE FEE", () => cfg.ServiceFeeEnabled, v => cfg.ServiceFeeEnabled = v);
        AddToggle(win, ref y, "LIVE MODE", () => cfg.LiveWrites, v => cfg.LiveWrites = v);
        y += 8;

        AddStepper(win, ref y, () => $"{Loc.T("RESERVE FLOOR")}   ${cfg.CashReserveFloor:N0}",
            () => cfg.CashReserveFloor = Math.Max(0m, cfg.CashReserveFloor - 500m), () => cfg.CashReserveFloor += 500m);
        AddStepper(win, ref y, () => $"{Loc.T("RESTOCK TARGET")}   {cfg.RestockTarget}",
            () => cfg.RestockTarget = Math.Max(1, cfg.RestockTarget - 5), () => cfg.RestockTarget += 5);
        AddStepper(win, ref y, () => $"{Loc.T("FEE / RUN")}   ${cfg.ServiceFeePerRun:N0}",
            () => cfg.ServiceFeePerRun = Math.Max(0m, cfg.ServiceFeePerRun - 50m), () => cfg.ServiceFeePerRun += 50m);
        y += 10;

        Btn(win, "RunNow", Pad, y, W - 2 * Pad, 42, Blue, White, Loc.T("RUN NOW"), 16, () => { try { runNow(); } catch { } });
        y += 50;
        float hw = (W - 2 * Pad - 10) / 2f;
        Btn(win, "Cash", Pad, y, hw, 36, Green, White, "+$1,000", 14, () => GameActions.AddMoney(1000f));
        Btn(win, "Energy", Pad + hw + 10, y, hw, 36, Blue, White, Loc.T("ENERGY 100%"), 14, GameActions.RefillEnergy);
        y += 46;

        MkText(win, "ActLbl", Pad, y, W - 2 * Pad, 16, 12, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("ACTIVITY");
        y += 20;
        float logH = H - y - Pad;
        Panel(win, "ActBg", Pad, y, W - 2 * Pad, logH, Inset, 10);
        _activity = MkText(win, "Act", Pad + 12, y + 10, W - 2 * Pad - 24, logH - 20, 12.5f, Dim, TextAnchor.UpperLeft, FontStyle.Normal);
        _activity.lineSpacing = 1.2f;
    }

    public void Refresh(AutomationConfig cfg, GameSnapshot s)
    {
        if (_root == null) return;
        try
        {
            _status.text = s.HasSave
                ? $"<size=24><b>${s.Money:N0}</b></size>\n" +
                  $"{Loc.T("DAY")} {s.Day}   {s.Hour:00}:{(int)s.Minute:00}      {Loc.T("NET")} ${s.NetWorth:N0}\n" +
                  $"{Loc.T("Shops")} {s.PlayerBusinesses}    {Loc.T("Energy")} {s.Energy:0}    {Loc.T("Happy")} {s.Happiness:0}\n" +
                  $"{Loc.T("Tax due")} ${s.TaxDue:N0}    {Loc.T("Loans")} {s.Loans}    {Loc.T("Staff")} {s.Employees}"
                : Loc.T("NO SAVE LOADED");

            foreach (var t in _toggles)
            {
                bool on = false; try { on = t.Get(); } catch { }
                t.Pill.color = on ? Green : GreyOff;
                t.PillText.text = on ? Loc.T("ON") : Loc.T("OFF");
                t.PillText.color = on ? White : Dim;
                t.Label.color = on ? White : Dim;
            }
            foreach (var st in _steppers) st.Label.text = st.Format();

            var lines = Activity.Recent();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count && i < 9; i++) sb.AppendLine(lines[i]);
            _activity.text = sb.ToString();
        }
        catch { }
    }

    // ---- builders ----

    private void AddToggle(Transform win, ref float y, string name, Func<bool> get, Action<bool> set)
    {
        var row = Panel(win, "row_" + name, Pad, y, W - 2 * Pad, 34, RowBg, 9);
        var btn = row.gameObject.AddComponent<Button>();
        btn.targetGraphic = row; ApplyBtnColors(btn, RowBg);
        btn.onClick.AddListener(() => { try { set(!get()); } catch { } });

        float rowW = W - 2 * Pad;
        var label = MkText(row.transform, "lbl", 16, 0, rowW - 90, 34, 15, Dim, TextAnchor.MiddleLeft, FontStyle.Bold);
        label.text = Loc.T(name);
        var pill = Panel(row.transform, "pill", rowW - 70, 6, 58, 22, GreyOff, 11);
        pill.raycastTarget = false;
        var pillTxt = MkText(pill.transform, "pt", 0, 0, 58, 22, 11, White, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(pillTxt.rectTransform);

        _toggles.Add(new ToggleRow { Pill = pill, PillText = pillTxt, Label = label, Get = get, Name = name });
        y += 38;
    }

    private void AddStepper(Transform win, ref float y, Func<string> format, Action minus, Action plus)
    {
        var lbl = MkText(win, "steplbl", Pad + 2, y + 6, W - 2 * Pad - 100, 30, 16, White, TextAnchor.MiddleLeft, FontStyle.Bold);
        Btn(win, "Minus", W - Pad - 86, y, 40, 32, RowBg, White, "-", 22, () => { try { minus(); } catch { } });
        Btn(win, "Plus", W - Pad - 42, y, 40, 32, RowBg, White, "+", 22, () => { try { plus(); } catch { } });
        _steppers.Add(new StepRow { Label = lbl, Format = format });
        y += 40;
    }

    // ---- primitives ----

    private Image Panel(Transform parent, string name, float x, float y, float w, float h, Color col, int radius)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
        var img = go.AddComponent<Image>();
        img.sprite = RoundedSprite(radius);
        img.type = Image.Type.Sliced;
        img.color = col;
        return img;
    }

    private Text MkText(Transform parent, string name, float x, float y, float w, float h, float size, Color col, TextAnchor anchor, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
        var t = go.AddComponent<Text>();
        t.font = _font; t.fontSize = Mathf.RoundToInt(size); t.color = col; t.alignment = anchor; t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        t.supportRichText = true; t.raycastTarget = false;
        return t;
    }

    private void Btn(Transform parent, string name, float x, float y, float w, float h, Color bg, Color fg, string label, float fontSize, UnityEngine.Events.UnityAction onClick)
    {
        var img = Panel(parent, name, x, y, w, h, bg, 9);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img; ApplyBtnColors(btn, bg);
        btn.onClick.AddListener(onClick);
        var t = MkText(img.transform, "Label", 0, 0, w, h, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
        t.text = label;
        Stretch(t.rectTransform);
    }

    private static void ApplyBtnColors(Button btn, Color bg)
    {
        var c = btn.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(1.12f, 1.12f, 1.14f, 1f);
        c.pressedColor = new Color(0.82f, 0.82f, 0.84f, 1f);
        c.fadeDuration = 0.08f;
        btn.colors = c;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>A white rounded-rect sprite with a 9-slice border, tinted per Image. Cached per radius.</summary>
    private static Sprite RoundedSprite(int radius)
    {
        if (_sprites.TryGetValue(radius, out var cached) && cached != null) return cached;
        int size = radius * 2 + 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        var clear = new Color(1f, 1f, 1f, 0f);
        var solid = Color.white;
        for (int yy = 0; yy < size; yy++)
            for (int xx = 0; xx < size; xx++)
            {
                int cx = xx < radius ? radius : (xx > size - 1 - radius ? size - 1 - radius : xx);
                int cy = yy < radius ? radius : (yy > size - 1 - radius ? size - 1 - radius : yy);
                float dx = xx - cx, dy = yy - cy;
                bool inside = dx * dx + dy * dy <= (radius + 0.5f) * (radius + 0.5f);
                tex.SetPixel(xx, yy, inside ? solid : clear);
            }
        tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp; tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        _sprites[radius] = sp;
        return sp;
    }
}
