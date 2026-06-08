using System;
using System.Collections.Generic;
using BAA.Core.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaBot.UI;

/// <summary>
/// The F8 in-game overlay — a large, rounded, Big-Ambitions-styled panel built at runtime from Unity's
/// own uGUI components (so it always instantiates, unlike a mod-defined MonoBehaviour). Two-column
/// toggle grid keeps it short enough to scale up large. Hover tooltips via Unity's EventTrigger.
/// </summary>
internal sealed class PanelView
{
    private GameObject _root;
    private Font _font;
    private Text _status;
    private Text _log;
    private GameObject _tip;
    private Text _tipText;
    private RectTransform _winRect;
    private Canvas _canvas;
    private AutomationConfig _cfg;
    private Action _runNow, _onClose;
    private const float MinScale = 0.5f, MaxScale = 1.7f;

    private sealed class ToggleRow { public Image Pill; public Text PillText; public Text Label; public Func<bool> Get; }
    private sealed class StepRow { public Text Label; public Func<string> Format; }
    private readonly List<ToggleRow> _toggles = new();
    private readonly List<StepRow> _steppers = new();
    private static readonly Dictionary<int, Sprite> _sprites = new();

    private static readonly Color Slate   = new(0.137f, 0.165f, 0.212f, 0.98f);
    private static readonly Color Chrome  = new(0.85f, 0.87f, 0.90f, 1f);
    private static readonly Color Inset   = new(0.082f, 0.10f, 0.13f, 1f);
    private static readonly Color RowBg   = new(0.18f, 0.22f, 0.28f, 1f);
    private static readonly Color Blue    = new(0.24f, 0.53f, 0.95f, 1f);
    private static readonly Color Cyan    = new(0.40f, 0.78f, 1f, 1f);
    private static readonly Color Green   = new(0.30f, 0.72f, 0.42f, 1f);
    private static readonly Color Red     = new(0.91f, 0.30f, 0.24f, 1f);
    private static readonly Color GreyOff = new(0.30f, 0.35f, 0.42f, 1f);
    private static readonly Color White   = new(0.96f, 0.97f, 0.99f, 1f);
    private static readonly Color Dim     = new(0.66f, 0.73f, 0.82f, 1f);
    private static readonly Color TipBg   = new(0.04f, 0.06f, 0.09f, 0.98f);

    private const float W = 540f, H = 924f, Pad = 22f;

    public bool Built => _root != null;
    public void SetVisible(bool v) { if (_root != null && _root.activeSelf != v) { _root.SetActive(v); if (!v && _tip != null) _tip.SetActive(false); } }
    public void Destroy() { if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; } }

    public void Build(AutomationConfig cfg, Action runNow, Action onClose)
    {
        _cfg = cfg; _runNow = runNow; _onClose = onClose;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) { try { _font = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { } }

        _root = new GameObject("BA BOT Canvas");
        UnityEngine.Object.DontDestroyOnLoad(_root);
        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1480, 980); // taller ref (panel now includes the activity log) so it fits on screen; use + to enlarge
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        // Legacy uGUI Text rasterises into a dynamic font atlas; a high pixels-per-unit makes that atlas
        // render at much higher resolution so the text stays sharp when the canvas + window scale it up.
        scaler.dynamicPixelsPerUnit = 4f;
        scaler.referencePixelsPerUnit = 100f;
        _canvas = canvas;
        _root.AddComponent<GraphicRaycaster>();

        var win = Panel(_root.transform, "Window", 26, 26, W, H, Slate, 16).transform;
        _winRect = (RectTransform)win;
        _winRect.localScale = Vector3.one * Mathf.Clamp(UiPrefs.Scale, MinScale, MaxScale);
        _winRect.anchoredPosition = new Vector2(UiPrefs.PosX, UiPrefs.PosY);

        // Transparent header handle: drag anywhere on the top bar (except the buttons) to move the panel.
        var handle = Panel(win, "DragHandle", 0, 0, W, 64, new Color(1f, 1f, 1f, 0f), 16);
        AddDrag(handle.gameObject);

        var chrome = Panel(win, "Chrome", 0, 0, W, 12, Chrome, 16); chrome.raycastTarget = false;
        MkText(win, "Title", Pad, 20, W - 2 * Pad - 184, 34, 27, White, TextAnchor.MiddleLeft, FontStyle.Bold).text = "BA BOT";
        MkText(win, "Sub", Pad, 54, W - 2 * Pad - 184, 18, 13, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("AUTOMATION CONTROL");
        Btn(win, "Lang", W - Pad - 160, 22, 38, 30, RowBg, White, Loc.Current == Lang.Da ? "DA" : "EN", 14, ToggleLanguage, "Switch language: English / Dansk");
        Btn(win, "SizeDown", W - Pad - 118, 22, 34, 30, RowBg, White, "-", 20, () => ChangeScale(-0.1f), "Make the panel smaller");
        Btn(win, "SizeUp", W - Pad - 80, 22, 34, 30, RowBg, White, "+", 20, () => ChangeScale(0.1f), "Make the panel bigger");
        Btn(win, "Close", W - Pad - 34, 22, 34, 30, Red, White, "X", 16, () => { try { onClose(); } catch { } }, "Close the panel (or press F8). Drag the title bar to move it; use - / + to resize.");

        float y = 90;
        Panel(win, "StatusBg", Pad, y, W - 2 * Pad, 112, Inset, 10);
        _status = MkText(win, "Status", Pad + 16, y + 12, W - 2 * Pad - 32, 90, 16, White, TextAnchor.UpperLeft, FontStyle.Normal);
        _status.lineSpacing = 1.2f;
        y += 112 + 14;

        // 2-column toggle grid (short labels; full descriptions in the tooltip)
        var defs = new (string disp, Func<bool> g, Action<bool> s, string tip)[]
        {
            ("MASTER",    () => cfg.MasterEnabled,    v => cfg.MasterEnabled = v,    "Master switch. Must be ON for anything else to run. Off = the mod does nothing."),
            ("FINANCE",   () => cfg.FinanceEnabled,   v => cfg.FinanceEnabled = v,   "Auto-pays your taxes the moment they come due - the one money chore the game won't do for you."),
            ("EMPLOYEES", () => cfg.EmployeesEnabled, v => cfg.EmployeesEnabled = v, "Morale bonus to unhappy staff when the game allows one, and finishes completed training."),
            ("LOGISTICS", () => cfg.LogisticsEnabled, v => cfg.LogisticsEnabled = v, "Sets up a repeating weekly import for any product running low so stock keeps flowing."),
            ("RESTOCK",   () => cfg.RestockEnabled,   v => cfg.RestockEnabled = v,   "Buys products back up to your target when shelves run low."),
            ("PRICING",   () => cfg.PricingEnabled,   v => cfg.PricingEnabled = v,   "Auto-sets each product's price to the game's own optimal price for its neighborhood, keeping price-satisfaction high. Previews unless Live mode is on."),
            ("WELLBEING", () => cfg.WellbeingEnabled, v => cfg.WellbeingEnabled = v, "Automatically refills your energy and tops up happiness so you never stop to rest."),
            ("FEE",       () => cfg.ServiceFeeEnabled,v => cfg.ServiceFeeEnabled = v,"Optional challenge: charges cash each time the bot does work for you. Off = free."),
            ("LIVE MODE", () => cfg.LiveWrites,       v => cfg.LiveWrites = v,       "OFF (default) = automation only PREVIEWS what it would do (safe). ON = it actually pays taxes, restocks and gives bonuses. Turn on only while watching."),
            ("TURBO",     () => GameActions.TurboOn,  v => GameActions.SetTurbo(v, cfg.TurboPercent), "AFK accelerator: speeds game time to TURBO SPEED (below) so days pass fast and the daily automation runs while you're away. No cash, no risk. Turn off for normal speed."),
        };
        float colW = (W - 2 * Pad - 14) / 2f;
        float gy = y;
        for (int i = 0; i < defs.Length; i++)
        {
            int col = i % 2, rowIdx = i / 2;
            float x = Pad + col * (colW + 14);
            float yy = gy + rowIdx * 52;
            MakeToggle(win, x, yy, colW, defs[i].disp, defs[i].g, defs[i].s, defs[i].tip);
        }
        y = gy + ((defs.Length + 1) / 2) * 52 + 8;

        AddStepper(win, ref y, () => $"{Loc.T("RESERVE FLOOR")}   ${cfg.CashReserveFloor:N0}",
            () => cfg.CashReserveFloor = Math.Max(0m, cfg.CashReserveFloor - 500m), () => cfg.CashReserveFloor += 500m,
            "Automation never spends below this cash cushion.");
        AddStepper(win, ref y, () => $"{Loc.T("RESTOCK TARGET")}   {cfg.RestockTarget}",
            () => cfg.RestockTarget = Math.Max(1, cfg.RestockTarget - 5), () => cfg.RestockTarget += 5,
            "Auto-restock fills each product up to this many units.");
        AddStepper(win, ref y, () => $"{Loc.T("FEE / RUN")}   ${cfg.ServiceFeePerRun:N0}",
            () => cfg.ServiceFeePerRun = Math.Max(0m, cfg.ServiceFeePerRun - 50m), () => cfg.ServiceFeePerRun += 50m,
            "Cash charged per automation run when Service Fee is on.");
        AddStepper(win, ref y, () => $"{Loc.T("TURBO SPEED")}   {cfg.TurboPercent}%",
            () => { cfg.TurboPercent = Math.Max(100, cfg.TurboPercent - 50); if (GameActions.TurboOn) GameActions.SetTurbo(true, cfg.TurboPercent); },
            () => { cfg.TurboPercent = Math.Min(1000, cfg.TurboPercent + 50); if (GameActions.TurboOn) GameActions.SetTurbo(true, cfg.TurboPercent); },
            "How fast the TURBO accelerator runs, as a percent of normal. 300 = 3x.");
        y += 12;

        Btn(win, "RunNow", Pad, y, W - 2 * Pad, 46, Blue, White, Loc.T("RUN NOW"), 18, () => { try { runNow(); } catch { } },
            "Runs one automation pass now. Turn on MASTER + the features you want first. Previews unless Live mode is on.");
        y += 56;
        float tw = (W - 2 * Pad - 24) / 3f;
        Btn(win, "Cash", Pad, y, tw, 40, Green, White, "+$1,000", 15, () => GameActions.AddMoney(1000f), "Instantly add $1,000 cash (handy for testing).");
        Btn(win, "Energy", Pad + tw + 12, y, tw, 40, Blue, White, Loc.T("ENERGY"), 15, GameActions.RefillEnergy, "Instantly refill your energy to full.");
        Btn(win, "Skip", Pad + 2 * (tw + 12), y, tw, 40, RowBg, White, Loc.T("SKIP DAY"), 15, GameActions.SkipToMorning, "Fast-forward to the next morning (08:00). Crossing midnight runs the daily automation, so the bot works while you skip.");

        // Activity log (newest first) - shows what the bot is doing/previewing
        y += 50;
        MkText(win, "LogHdr", Pad + 2, y, W - 2 * Pad, 18, 13, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("ACTIVITY");
        y += 22;
        Panel(win, "LogBg", Pad, y, W - 2 * Pad, 110, Inset, 10);
        _log = MkText(win, "Log", Pad + 14, y + 9, W - 2 * Pad - 28, 92, 13, Dim, TextAnchor.UpperLeft, FontStyle.Normal);
        _log.lineSpacing = 1.1f;

        // Tooltip box (hidden until hover)
        var tipImg = Panel(_root.transform, "Tip", 0, 0, 360, 96, TipBg, 8);
        tipImg.raycastTarget = false;
        _tip = tipImg.gameObject;
        _tipText = MkText(_tip.transform, "tt", 0, 0, 360, 96, 14, White, TextAnchor.UpperLeft, FontStyle.Normal);
        _tipText.lineSpacing = 1.15f;
        var trt = _tipText.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.pivot = new Vector2(0.5f, 0.5f);
        trt.offsetMin = new Vector2(12, 10); trt.offsetMax = new Vector2(-12, -10);
        _tip.SetActive(false);
    }

    public void Refresh(AutomationConfig cfg, GameSnapshot s)
    {
        if (_root == null) return;
        try
        {
            _status.text = s.HasSave
                ? $"<size=27><b>${s.Money:N0}</b></size>\n" +
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

            if (_log != null)
            {
                var lines = Diagnostics.Activity.Recent();
                int n = Math.Min(lines.Count, 8);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < n; i++) sb.Append(lines[i]).Append('\n');
                _log.text = n > 0 ? sb.ToString() : Loc.T("No activity yet");
            }
        }
        catch { }
    }

    // ---- builders ----

    private void MakeToggle(Transform win, float x, float y, float w, string disp, Func<bool> get, Action<bool> set, string tip)
    {
        var row = Panel(win, "tg_" + disp, x, y, w, 46, RowBg, 9);
        var btn = row.gameObject.AddComponent<Button>();
        btn.targetGraphic = row; ApplyBtnColors(btn, RowBg);
        btn.onClick.AddListener(() => { try { set(!get()); } catch { } });
        AddTip(row.gameObject, tip);

        var label = MkText(row.transform, "lbl", 14, 0, w - 66, 46, 14, Dim, TextAnchor.MiddleLeft, FontStyle.Bold);
        label.text = disp;
        var pill = Panel(row.transform, "pill", w - 56, 11, 46, 24, GreyOff, 12);
        pill.raycastTarget = false;
        var pillTxt = MkText(pill.transform, "pt", 0, 0, 46, 24, 11, White, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(pillTxt.rectTransform);

        _toggles.Add(new ToggleRow { Pill = pill, PillText = pillTxt, Label = label, Get = get });
    }

    private void AddStepper(Transform win, ref float y, Func<string> format, Action minus, Action plus, string tip)
    {
        var lbl = MkText(win, "steplbl", Pad + 2, y + 7, W - 2 * Pad - 108, 30, 16, White, TextAnchor.MiddleLeft, FontStyle.Bold);
        Btn(win, "Minus", W - Pad - 94, y, 44, 34, RowBg, White, "-", 24, () => { try { minus(); } catch { } }, tip);
        Btn(win, "Plus", W - Pad - 46, y, 44, 34, RowBg, White, "+", 24, () => { try { plus(); } catch { } }, tip);
        _steppers.Add(new StepRow { Label = lbl, Format = format });
        y += 44;
    }

    // ---- tooltips ----

    private void AddTip(GameObject go, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var et = go.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(d => ShowTip(text, d));
        et.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => { if (_tip != null) _tip.SetActive(false); });
        et.triggers.Add(exit);
    }

    private void ShowTip(string text, BaseEventData data)
    {
        if (_tip == null) return;
        _tipText.text = Loc.T(text);
        int lines = Mathf.Max(1, Mathf.CeilToInt(_tipText.text.Length / 38f));
        float w = 360f, h = lines * 22f + 22f;
        var rt = (RectTransform)_tip.transform;
        rt.sizeDelta = new Vector2(w, h);
        rt.pivot = new Vector2(0f, 1f);
        // Scale the tooltip to match the panel's current size (it lives outside the scaled window).
        float scale = Mathf.Clamp(UiPrefs.Scale, MinScale, MaxScale);
        rt.localScale = Vector3.one * scale;
        float sw = w * scale, sh = h * scale;
        var ped = data as PointerEventData;
        Vector2 p = ped != null ? ped.position : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float x = p.x + 16f, top = p.y - 14f;
        if (x + sw > Screen.width) x = p.x - 16f - sw;
        if (top - sh < 0f) top = sh + 8f;
        if (x < 4f) x = 4f;
        rt.position = new Vector3(x, top, 0f);
        _tip.SetActive(true);
        _tip.transform.SetAsLastSibling();
    }

    // ---- move + resize ----

    private void AddDrag(GameObject go)
    {
        var et = go.AddComponent<EventTrigger>();
        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(OnDrag);
        et.triggers.Add(drag);
    }

    private void OnDrag(BaseEventData data)
    {
        var ped = data as PointerEventData;
        if (ped == null || _winRect == null) return;
        float sf = (_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
        _winRect.anchoredPosition += ped.delta / sf;
        UiPrefs.PosX = _winRect.anchoredPosition.x;
        UiPrefs.PosY = _winRect.anchoredPosition.y;
    }

    private void ChangeScale(float delta)
    {
        UiPrefs.Scale = Mathf.Clamp(UiPrefs.Scale + delta, MinScale, MaxScale);
        if (_winRect != null) _winRect.localScale = Vector3.one * UiPrefs.Scale;
    }

    // ---- language ----

    private void ToggleLanguage()
    {
        Loc.Current = Loc.Current == Lang.En ? Lang.Da : Lang.En;
        if (_cfg != null) _cfg.Language = Loc.Current == Lang.Da ? "da" : "en"; // persisted by the 1s autosave
        Rebuild();
    }

    // Rebuild the whole panel so every label (static + dynamic) picks up the new language. Size/position
    // live in persisted UiPrefs so they survive, and we restore the current visibility.
    private void Rebuild()
    {
        if (_cfg == null) return;
        bool wasVisible = _root != null && _root.activeSelf;
        var cfg = _cfg; var run = _runNow; var close = _onClose;
        Destroy();
        _toggles.Clear();
        _steppers.Clear();
        Build(cfg, run, close);
        SetVisible(wasVisible);
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

    private void Btn(Transform parent, string name, float x, float y, float w, float h, Color bg, Color fg, string label, float fontSize, UnityEngine.Events.UnityAction onClick, string tip = null)
    {
        var img = Panel(parent, name, x, y, w, h, bg, 9);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img; ApplyBtnColors(btn, bg);
        btn.onClick.AddListener(onClick);
        var t = MkText(img.transform, "Label", 0, 0, w, h, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
        t.text = label;
        Stretch(t.rectTransform);
        AddTip(img.gameObject, tip);
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
