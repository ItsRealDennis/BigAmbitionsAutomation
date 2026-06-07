using System;
using System.Collections.Generic;
using BAA.Core.Config;
using BaBot.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace BaBot.UI;

/// <summary>
/// The F8 in-game overlay, built at runtime from Unity's own uGUI components (Canvas/Image/Text/Button).
/// We never AddComponent a type from THIS mod assembly (Unity can't instantiate those at runtime) — only
/// engine components — so the panel always loads. ModMain/BaBotLogic drives show/hide + Refresh from the
/// official UnityLifecycleProvider.OnUpdate hook; no MonoBehaviour of ours is required.
/// </summary>
internal sealed class PanelView
{
    private GameObject _root;
    private Font _font;
    private Text _status;
    private Text _activity;

    private sealed class ToggleRow { public Text Label; public Func<bool> Get; public string Name; }
    private sealed class StepRow { public Text Value; public Func<string> Format; }
    private readonly List<ToggleRow> _toggles = new();
    private readonly List<StepRow> _steppers = new();

    private static readonly Color Slate = new(0.082f, 0.094f, 0.121f, 0.97f);
    private static readonly Color Inset = new(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color Cyan = new(0.36f, 0.80f, 1f);
    private static readonly Color Green = new(0.20f, 0.62f, 0.36f);
    private static readonly Color GreyBtn = new(0.20f, 0.24f, 0.30f);
    private static readonly Color White = new(0.96f, 0.97f, 0.99f);
    private static readonly Color Dim = new(0.68f, 0.74f, 0.82f);

    private const float W = 360f, H = 724f, Pad = 14f;

    public bool Built => _root != null;
    public void SetVisible(bool v) { if (_root != null && _root.activeSelf != v) _root.SetActive(v); }
    public void Destroy() { if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; } }

    public void Build(AutomationConfig cfg, Action runNow)
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) { try { _font = Font.CreateDynamicFontFromOSFont("Arial", 14); } catch { } }

        _root = new GameObject("BA BOT Canvas");
        UnityEngine.Object.DontDestroyOnLoad(_root);
        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        var window = Img(_root.transform, "Window", 24, 24, W, H, Slate).transform;

        float y = Pad;
        MkText(window, "Title", Pad, y, W - 2 * Pad, 28, 22, White, TextAnchor.MiddleLeft, FontStyle.Bold).text = "BA BOT";
        y += 30;
        MkText(window, "Sub", Pad, y, W - 2 * Pad, 14, 11, Dim, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("AUTOMATION CONTROL");
        y += 22;

        Img(window, "StatusBg", Pad, y, W - 2 * Pad, 86, Inset);
        _status = MkText(window, "Status", Pad + 12, y + 8, W - 2 * Pad - 24, 70, 13, White, TextAnchor.UpperLeft, FontStyle.Normal);
        y += 86 + 12;

        AddToggle(window, ref y, "AUTOMATION (MASTER)", () => cfg.MasterEnabled, v => cfg.MasterEnabled = v);
        AddToggle(window, ref y, "FINANCE AUTO-PAY", () => cfg.FinanceEnabled, v => cfg.FinanceEnabled = v);
        AddToggle(window, ref y, "EMPLOYEES", () => cfg.EmployeesEnabled, v => cfg.EmployeesEnabled = v);
        AddToggle(window, ref y, "LOGISTICS", () => cfg.LogisticsEnabled, v => cfg.LogisticsEnabled = v);
        AddToggle(window, ref y, "AUTO-RESTOCK", () => cfg.RestockEnabled, v => cfg.RestockEnabled = v);
        AddToggle(window, ref y, "AUTO-WELLBEING", () => cfg.WellbeingEnabled, v => cfg.WellbeingEnabled = v);
        AddToggle(window, ref y, "SERVICE FEE", () => cfg.ServiceFeeEnabled, v => cfg.ServiceFeeEnabled = v);
        AddToggle(window, ref y, "LIVE MODE", () => cfg.LiveWrites, v => cfg.LiveWrites = v);
        y += 6;

        AddStepper(window, ref y, () => $"{Loc.T("RESERVE FLOOR")}  ${cfg.CashReserveFloor:N0}",
            () => cfg.CashReserveFloor = Math.Max(0m, cfg.CashReserveFloor - 500m), () => cfg.CashReserveFloor += 500m);
        AddStepper(window, ref y, () => $"{Loc.T("RESTOCK TARGET")}  {cfg.RestockTarget}",
            () => cfg.RestockTarget = Math.Max(1, cfg.RestockTarget - 5), () => cfg.RestockTarget += 5);
        AddStepper(window, ref y, () => $"{Loc.T("FEE / RUN")}  ${cfg.ServiceFeePerRun:N0}",
            () => cfg.ServiceFeePerRun = Math.Max(0m, cfg.ServiceFeePerRun - 50m), () => cfg.ServiceFeePerRun += 50m);
        y += 6;

        Btn(window, "RunNow", Pad, y, W - 2 * Pad, 28, Cyan, Loc.T("RUN NOW"), 13, () => { try { runNow(); } catch { } }, out _);
        y += 34;
        float hw = (W - 2 * Pad - 8) / 2f;
        Btn(window, "Cash", Pad, y, hw, 26, GreyBtn, "+$1,000", 12, () => GameActions.AddMoney(1000f), out _);
        Btn(window, "Energy", Pad + hw + 8, y, hw, 26, Green, Loc.T("ENERGY 100%"), 12, GameActions.RefillEnergy, out _);
        y += 32;

        MkText(window, "ActLbl", Pad, y, W - 2 * Pad, 13, 11, Dim, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("ACTIVITY");
        y += 16;
        float logH = H - y - Pad;
        Img(window, "ActBg", Pad, y, W - 2 * Pad, logH, Inset);
        _activity = MkText(window, "Act", Pad + 8, y + 6, W - 2 * Pad - 16, logH - 12, 11, Dim, TextAnchor.UpperLeft, FontStyle.Normal);
    }

    public void Refresh(AutomationConfig cfg, GameSnapshot s)
    {
        if (_root == null) return;
        try
        {
            _status.text = s.HasSave
                ? $"<b>${s.Money:N0}</b>\n" +
                  $"{Loc.T("DAY")} {s.Day}  {s.Hour:00}:{(int)s.Minute:00}   {Loc.T("NET")} ${s.NetWorth:N0}\n" +
                  $"{Loc.T("Shops")} {s.PlayerBusinesses}   {Loc.T("Energy")} {s.Energy:0}   {Loc.T("Happy")} {s.Happiness:0}\n" +
                  $"{Loc.T("Tax due")} ${s.TaxDue:N0}   {Loc.T("Loans")} {s.Loans}   {Loc.T("Staff")} {s.Employees}"
                : Loc.T("NO SAVE LOADED");

            foreach (var t in _toggles)
            {
                bool on = false; try { on = t.Get(); } catch { }
                t.Label.text = $"{Loc.T(t.Name)}    <b>{(on ? Loc.T("ON") : Loc.T("OFF"))}</b>";
                t.Label.color = on ? Cyan : Dim;
            }
            foreach (var st in _steppers) st.Value.text = st.Format();

            var lines = Activity.Recent();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count && i < 7; i++) sb.AppendLine(lines[i]);
            _activity.text = sb.ToString();
        }
        catch { }
    }

    private void AddToggle(Transform parent, ref float y, string name, Func<bool> get, Action<bool> set)
    {
        Btn(parent, name, Pad, y, W - 2 * Pad, 26, GreyBtn, name, 12, () => { try { set(!get()); } catch { } }, out var label);
        label.alignment = TextAnchor.MiddleLeft;
        var rt = label.rectTransform; rt.offsetMin = new Vector2(12, 0); rt.offsetMax = new Vector2(-10, 0);
        _toggles.Add(new ToggleRow { Label = label, Get = get, Name = name });
        y += 30;
    }

    private void AddStepper(Transform parent, ref float y, Func<string> format, Action minus, Action plus)
    {
        var val = MkText(parent, "StepVal", Pad + 4, y + 4, W - 2 * Pad - 80, 22, 13, White, TextAnchor.MiddleLeft, FontStyle.Bold);
        Btn(parent, "Minus", Pad + W - 2 * Pad - 70, y, 32, 26, GreyBtn, "-", 16, () => { try { minus(); } catch { } }, out _);
        Btn(parent, "Plus", Pad + W - 2 * Pad - 34, y, 32, 26, GreyBtn, "+", 16, () => { try { plus(); } catch { } }, out _);
        _steppers.Add(new StepRow { Value = val, Format = format });
        y += 30;
    }

    private Image Img(Transform parent, string name, float x, float y, float w, float h, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
        var img = go.AddComponent<Image>();
        img.color = col;
        return img;
    }

    private Text MkText(Transform parent, string name, float x, float y, float w, float h, int size, Color col, TextAnchor anchor, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
        var t = go.AddComponent<Text>();
        t.font = _font; t.fontSize = size; t.color = col; t.alignment = anchor; t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        t.supportRichText = true; t.raycastTarget = false;
        return t;
    }

    private void Btn(Transform parent, string name, float x, float y, float w, float h, Color bg, string label, int fontSize,
        UnityEngine.Events.UnityAction onClick, out Text labelText)
    {
        var img = Img(parent, name, x, y, w, h, bg);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = new Color(bg.r + 0.08f, bg.g + 0.08f, bg.b + 0.10f, 1f);
        c.pressedColor = new Color(bg.r * 0.8f, bg.g * 0.8f, bg.b * 0.8f, 1f);
        btn.colors = c;
        btn.onClick.AddListener(onClick);
        labelText = MkText(img.transform, "Label", 0, 0, w, h, fontSize, White, TextAnchor.MiddleCenter, FontStyle.Bold);
        var rt = labelText.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.anchoredPosition = Vector2.zero;
    }
}
