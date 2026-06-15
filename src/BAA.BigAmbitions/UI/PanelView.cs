using System;
using System.Collections.Generic;
using BAA.Core.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaBot.UI;

/// <summary>
/// The F8 in-game overlay — a compact, Big-Ambitions-styled panel built at runtime from Unity's own
/// uGUI components. Organised into four tabs (HOME / AUTO / STAFF / LOG) so only one focused screen
/// shows at a time instead of one long wall of controls. STAFF is a scrollable list of every employee,
/// each with a MAX button to train that person. Hover tooltips via EventTrigger.
/// </summary>
internal sealed class PanelView
{
    private GameObject _root;
    private Font _font;
    private Text _status;
    private Text _sub;
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
    private sealed class SkillRow { public GameObject Row; public Text Name; public Text Pct; public Image Pill; public Text PillText; public PersonSkill Person; }
    private readonly List<ToggleRow> _toggles = new();
    private readonly List<StepRow> _steppers = new();
    private readonly List<SkillRow> _skillRows = new();
    private static readonly Dictionary<int, Sprite> _sprites = new();

    // tabs
    private const int TabHome = 0, TabAuto = 1, TabStaff = 2, TabLog = 3;
    private GameObject[] _tabPanels;
    private Image[] _tabBtns;
    private Text[] _tabLabels;
    private int _activeTab = TabHome;

    // staff scroll list
    private RectTransform _skillContent;
    private Text _skillsEmpty;
    private List<PersonSkill> _skillsCache = new();
    private float _skillsReadAt = -99f;
    private const float SkillRowH = 34f;

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

    private const float W = 500f, Pad = 18f;
    private const float ContentX = Pad, ContentY = 226f, ContentH = 440f;
    private const float ContentW = W - 2 * Pad;
    private const float H = ContentY + ContentH + Pad;

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
        scaler.referenceResolution = new Vector2(1480, 980);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        // High pixels-per-unit renders the dynamic font atlas at much higher resolution so text stays
        // crisp when the canvas + window scale it up.
        scaler.dynamicPixelsPerUnit = 8f;
        scaler.referencePixelsPerUnit = 100f;
        _canvas = canvas;
        _root.AddComponent<GraphicRaycaster>();

        var win = Panel(_root.transform, "Window", 26, 26, W, H, Slate, 16).transform;
        _winRect = (RectTransform)win;
        _winRect.localScale = Vector3.one * Mathf.Clamp(UiPrefs.Scale, MinScale, MaxScale);
        _winRect.anchoredPosition = new Vector2(UiPrefs.PosX, UiPrefs.PosY);

        // Header (drag anywhere on the bar except buttons)
        var handle = Panel(win, "DragHandle", 0, 0, W, 60, new Color(1f, 1f, 1f, 0f), 16);
        AddDrag(handle.gameObject);
        var chrome = Panel(win, "Chrome", 0, 0, W, 10, Chrome, 16); chrome.raycastTarget = false;

        MkText(win, "Title", Pad, 16, W - 2 * Pad - 168, 32, 26, White, TextAnchor.MiddleLeft, FontStyle.Bold).text = "BA BOT";
        _sub = MkText(win, "Sub", Pad, 50, W - 2 * Pad - 168, 18, 13, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
        _sub.text = Loc.T("AUTOMATION CONTROL");
        Btn(win, "Lang", W - Pad - 156, 18, 38, 28, RowBg, White, Loc.Current == Lang.Da ? "DA" : "EN", 14, ToggleLanguage, "Switch language: English / Dansk");
        Btn(win, "SizeDown", W - Pad - 114, 18, 32, 28, RowBg, White, "-", 20, () => ChangeScale(-0.1f), "Make the panel smaller");
        Btn(win, "SizeUp", W - Pad - 78, 18, 32, 28, RowBg, White, "+", 20, () => ChangeScale(0.1f), "Make the panel bigger");
        Btn(win, "Close", W - Pad - 32, 18, 32, 28, Red, White, "X", 16, () => { try { onClose(); } catch { } }, "Close the panel (F8). Drag the title bar to move it; use - / + to resize.");

        // Status card
        Panel(win, "StatusBg", Pad, 78, ContentW, 92, Inset, 10);
        _status = MkText(win, "Status", Pad + 16, 90, ContentW - 32, 72, 15, White, TextAnchor.UpperLeft, FontStyle.Normal);
        _status.lineSpacing = 1.25f;

        // Tab bar
        BuildTabBar(win);

        // Tab content containers
        _tabPanels = new GameObject[4];
        for (int i = 0; i < 4; i++)
            _tabPanels[i] = MakeRect(win, "Tab" + i, ContentX, ContentY, ContentW, ContentH).gameObject;
        BuildHomeTab(_tabPanels[TabHome].transform);
        BuildAutoTab(_tabPanels[TabAuto].transform);
        BuildStaffTab(_tabPanels[TabStaff].transform);
        BuildLogTab(_tabPanels[TabLog].transform);

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

        SelectTab(_activeTab);
    }

    // ---- tabs ----

    private void BuildTabBar(Transform win)
    {
        _tabBtns = new Image[4];
        _tabLabels = new Text[4];
        string[] names = { Loc.T("HOME"), Loc.T("AUTO"), Loc.T("STAFF"), Loc.T("LOG") };
        string[] tips =
        {
            "Overview: status, master switch, live mode and quick actions.",
            "Automation: turn features on/off and tune their settings.",
            "Staff: pick any employee and train their skills to 100%.",
            "Activity log: what the bot has been doing.",
        };
        float gap = 6f, tw = (ContentW - 3 * gap) / 4f;
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            float x = Pad + i * (tw + gap);
            var bg = Panel(win, "TabBtn" + i, x, 182, tw, 34, RowBg, 8);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg; ApplyBtnColors(btn, RowBg);
            btn.onClick.AddListener(() => SelectTab(idx));
            var lbl = MkText(bg.transform, "l", 0, 0, tw, 34, 14, Dim, TextAnchor.MiddleCenter, FontStyle.Bold);
            lbl.text = names[i]; Stretch(lbl.rectTransform);
            AddTip(bg.gameObject, tips[i]);
            _tabBtns[i] = bg; _tabLabels[i] = lbl;
        }
    }

    private void SelectTab(int tab)
    {
        _activeTab = tab;
        for (int i = 0; i < 4; i++)
        {
            if (_tabPanels != null && _tabPanels[i] != null) _tabPanels[i].SetActive(i == tab);
            if (_tabBtns != null && _tabBtns[i] != null) _tabBtns[i].color = i == tab ? Blue : RowBg;
            if (_tabLabels != null && _tabLabels[i] != null) _tabLabels[i].color = i == tab ? White : Dim;
        }
    }

    private void BuildHomeTab(Transform c)
    {
        var cfg = _cfg;
        float y = 0;
        MakeToggle(c, 0, y, ContentW, "MASTER", () => cfg.MasterEnabled, v => cfg.MasterEnabled = v,
            "Master switch. Must be ON for any automation to run. Off = the mod does nothing.");
        y += 54;
        MakeToggle(c, 0, y, ContentW, "LIVE MODE", () => cfg.LiveWrites, v => cfg.LiveWrites = v,
            "OFF (default) = automation only PREVIEWS what it would do (safe). ON = it actually pays taxes, restocks and gives bonuses.");
        y += 64;

        Btn(c, "RunNow", 0, y, ContentW, 48, Blue, White, Loc.T("RUN NOW"), 18, () => { try { _runNow(); } catch { } },
            "Run one automation pass now. Turn on MASTER + the features you want first.");
        y += 62;

        MkText(c, "QaHdr", 2, y, ContentW, 16, 12, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("QUICK ACTIONS");
        y += 22;
        float tw = (ContentW - 36) / 4f;
        Btn(c, "Cash", 0 * (tw + 12), y, tw, 44, Green, White, "+$100K", 14, () => GameActions.AddMoney(100000f), "Instantly add $100,000 cash.");
        Btn(c, "Energy", 1 * (tw + 12), y, tw, 44, Blue, White, Loc.T("ENERGY"), 14, GameActions.RefillEnergy, "Instantly refill your energy AND food to full.");
        Btn(c, "Skip", 2 * (tw + 12), y, tw, 44, RowBg, White, Loc.T("SKIP DAY"), 14, GameActions.SkipToMorning, "Fast-forward to the next morning (08:00). Crossing midnight runs the daily automation.");
        Btn(c, "Taxi", 3 * (tw + 12), y, tw, 44, Cyan, Slate, Loc.T("TAXI"), 14, GameActions.CallTaxi, "Open the taxi map to fast-travel.");
    }

    private void BuildAutoTab(Transform c)
    {
        var cfg = _cfg;
        var defs = new (string disp, Func<bool> g, Action<bool> s, string tip)[]
        {
            ("FINANCE",   () => cfg.FinanceEnabled,   v => cfg.FinanceEnabled = v,   "Auto-pays your taxes the moment they come due."),
            ("EMPLOYEES", () => cfg.EmployeesEnabled, v => cfg.EmployeesEnabled = v, "Morale bonus to unhappy staff when allowed, and finishes completed training."),
            ("LOGISTICS", () => cfg.LogisticsEnabled, v => cfg.LogisticsEnabled = v, "Sets up a repeating weekly import for any product running low."),
            ("RESTOCK",   () => cfg.RestockEnabled,   v => cfg.RestockEnabled = v,   "Buys products back up to your target when shelves run low."),
            ("PRICING",   () => cfg.PricingEnabled,   v => cfg.PricingEnabled = v,   "Auto-sets each product to the game's optimal neighborhood price."),
            ("WELLBEING", () => cfg.WellbeingEnabled, v => cfg.WellbeingEnabled = v, "Refills energy and tops up happiness so you never stop to rest."),
            ("TURBO",     () => GameActions.TurboOn,  v => GameActions.SetTurbo(v, cfg.TurboPercent), "AFK accelerator: speeds game time so days pass fast and daily automation runs while away."),
            ("FEE",       () => cfg.ServiceFeeEnabled,v => cfg.ServiceFeeEnabled = v,"Optional challenge: charges cash each time the bot does work. Off = free."),
        };
        float colW = (ContentW - 12) / 2f;
        for (int i = 0; i < defs.Length; i++)
        {
            int col = i % 2, row = i / 2;
            MakeToggle(c, col * (colW + 12), row * 50, colW, defs[i].disp, defs[i].g, defs[i].s, defs[i].tip);
        }
        float y = ((defs.Length + 1) / 2) * 50 + 8;

        AddStepper(c, ContentW, ref y, () => $"{Loc.T("RESERVE FLOOR")}   ${cfg.CashReserveFloor:N0}",
            () => cfg.CashReserveFloor = Math.Max(0m, cfg.CashReserveFloor - 500m), () => cfg.CashReserveFloor += 500m,
            "Automation never spends below this cash cushion.");
        AddStepper(c, ContentW, ref y, () => $"{Loc.T("RESTOCK TARGET")}   {cfg.RestockTarget}",
            () => cfg.RestockTarget = Math.Max(1, cfg.RestockTarget - 5), () => cfg.RestockTarget += 5,
            "Auto-restock fills each product up to this many units.");
        AddStepper(c, ContentW, ref y, () => $"{Loc.T("LOW-STOCK ALERT")}   {cfg.LowStockAlertPercent}%",
            () => cfg.LowStockAlertPercent = Math.Max(25, cfg.LowStockAlertPercent - 5),
            () => cfg.LowStockAlertPercent = Math.Min(90, cfg.LowStockAlertPercent + 5),
            "Warn that stock is running low when a product drops below this % of full. Higher = warned earlier. The game's default is 25%.");
        AddStepper(c, ContentW, ref y, () => $"{Loc.T("FEE / RUN")}   ${cfg.ServiceFeePerRun:N0}",
            () => cfg.ServiceFeePerRun = Math.Max(0m, cfg.ServiceFeePerRun - 50m), () => cfg.ServiceFeePerRun += 50m,
            "Cash charged per automation run when FEE is on.");
        AddStepper(c, ContentW, ref y, () => $"{Loc.T("TURBO SPEED")}   {cfg.TurboPercent}%",
            () => { cfg.TurboPercent = Math.Max(100, cfg.TurboPercent - 50); if (GameActions.TurboOn) GameActions.SetTurbo(true, cfg.TurboPercent); },
            () => { cfg.TurboPercent = Math.Min(1000, cfg.TurboPercent + 50); if (GameActions.TurboOn) GameActions.SetTurbo(true, cfg.TurboPercent); },
            "How fast the TURBO accelerator runs, as a percent of normal. 300 = 3x.");
    }

    private void BuildStaffTab(Transform c)
    {
        MkText(c, "StaffHint", 2, 0, ContentW, 18, 13, Dim, TextAnchor.MiddleLeft, FontStyle.Normal).text
            = Loc.T("Tap MAX to train a person's skills to 100%.");
        float top = 24f, listH = ContentH - top;
        var box = Panel(c, "SkillBg", 0, top, ContentW, listH, Inset, 10);
        _skillsEmpty = MkText(box.transform, "SkillEmpty", 14, 10, ContentW - 28, 18, 13, Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
        _skillsEmpty.text = Loc.T("Hire staff to train their skills.");

        var viewport = MakeRect(box.transform, "Viewport", 6, 6, ContentW - 12, listH - 12);
        viewport.gameObject.AddComponent<RectMask2D>();
        _skillContent = MakeRect(viewport, "Content", 0, 0, ContentW - 12, 0);

        var sr = box.gameObject.AddComponent<ScrollRect>();
        sr.viewport = viewport; sr.content = _skillContent;
        sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 26f;
    }

    private void BuildLogTab(Transform c)
    {
        MkText(c, "LogHdr", 2, 0, ContentW, 16, 12, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold).text = Loc.T("ACTIVITY");
        float top = 22f;
        Panel(c, "LogBg", 0, top, ContentW, ContentH - top, Inset, 10);
        _log = MkText(c, "Log", 14, top + 10, ContentW - 28, ContentH - top - 20, 13, Dim, TextAnchor.UpperLeft, FontStyle.Normal);
        _log.lineSpacing = 1.25f;
    }

    public void Refresh(AutomationConfig cfg, GameSnapshot s)
    {
        if (_root == null) return;
        try
        {
            _status.text = s.HasSave
                ? $"<size=26><b>${s.Money:N0}</b></size>\n" +
                  $"{Loc.T("DAY")} {s.Day}   {s.Hour:00}:{(int)s.Minute:00}      {Loc.T("NET")} ${s.NetWorth:N0}\n" +
                  $"{Loc.T("Shops")} {s.PlayerBusinesses}    {Loc.T("Staff")} {s.Employees}    {Loc.T("Tax due")} ${s.TaxDue:N0}"
                : Loc.T("NO SAVE LOADED");

            if (_sub != null)
            {
                if (!s.HasSave) { _sub.text = Loc.T("WAITING FOR SAVE"); _sub.color = Dim; }
                else if (!cfg.MasterEnabled) { _sub.text = Loc.T("PAUSED - TURN ON MASTER"); _sub.color = Dim; }
                else
                {
                    string mode = cfg.LiveWrites ? Loc.T("LIVE") : Loc.T("PREVIEW");
                    _sub.text = $"{Loc.T("RUNNING")} - {mode}";
                    _sub.color = Green;
                }
            }

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
                int n = Math.Min(lines.Count, 16);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < n; i++) sb.Append(lines[i]).Append('\n');
                _log.text = n > 0 ? sb.ToString() : Loc.T("No activity yet");
            }

            if (_activeTab == TabStaff) RefreshSkills();
        }
        catch { }
    }

    private void RefreshSkills()
    {
        if (_skillContent == null) return;
        float now = Time.realtimeSinceStartup;
        if (now - _skillsReadAt > 1f) { _skillsCache = SkillProbe.Read(); _skillsReadAt = now; }
        int count = _skillsCache != null ? _skillsCache.Count : 0;
        if (_skillsEmpty != null) _skillsEmpty.gameObject.SetActive(count == 0);
        EnsureSkillRows(count);
        _skillContent.sizeDelta = new Vector2(ContentW - 12, Mathf.Max(count * SkillRowH, 1f));
        for (int i = 0; i < _skillRows.Count; i++)
        {
            var r = _skillRows[i];
            if (i < count)
            {
                var p = _skillsCache[i];
                r.Person = p;
                r.Name.text = p.Name;
                bool full = p.Percent >= 99.5f;
                r.Pct.text = $"{p.Percent:0}%";
                r.Pill.color = full ? Green : Blue;
                r.PillText.text = full ? Loc.T("100%") : Loc.T("MAX");
                r.Row.SetActive(true);
            }
            else { r.Person = default; r.Row.SetActive(false); }
        }
    }

    private void EnsureSkillRows(int n)
    {
        float rw = ContentW - 12;
        while (_skillRows.Count < n)
        {
            int idx = _skillRows.Count;
            float ry = idx * SkillRowH;
            var bg = Panel(_skillContent, "sk" + idx, 2, ry + 2, rw - 4, SkillRowH - 4, RowBg, 8);
            var nameT = MkText(bg.transform, "nm", 12, 0, rw - 158, SkillRowH - 4, 14, White, TextAnchor.MiddleLeft, FontStyle.Bold);
            var pctT = MkText(bg.transform, "pc", rw - 148, 0, 52, SkillRowH - 4, 13, Cyan, TextAnchor.MiddleRight, FontStyle.Bold);
            var pill = Panel(bg.transform, "pl", rw - 86, 5, 74, SkillRowH - 14, Blue, 10);
            var pillBtn = pill.gameObject.AddComponent<Button>();
            pillBtn.targetGraphic = pill; ApplyBtnColors(pillBtn, Blue);
            var pillTxt = MkText(pill.transform, "pt", 0, 0, 74, SkillRowH - 14, 12, White, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(pillTxt.rectTransform);
            var row = new SkillRow { Row = bg.gameObject, Name = nameT, Pct = pctT, Pill = pill, PillText = pillTxt };
            pillBtn.onClick.AddListener(() => { try { if (row.Person.Emp != null && SkillProbe.MaxOut(row.Person.Emp)) _skillsReadAt = -99f; } catch { } });
            AddTip(bg.gameObject, "Train this person's skills to 100%.");
            _skillRows.Add(row);
        }
    }

    // ---- builders ----

    private void MakeToggle(Transform parent, float x, float y, float w, string disp, Func<bool> get, Action<bool> set, string tip)
    {
        var row = Panel(parent, "tg_" + disp, x, y, w, 46, RowBg, 9);
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

    private void AddStepper(Transform parent, float width, ref float y, Func<string> format, Action minus, Action plus, string tip)
    {
        var lbl = MkText(parent, "steplbl", 2, y + 7, width - 108, 30, 15, White, TextAnchor.MiddleLeft, FontStyle.Bold);
        Btn(parent, "Minus", width - 94, y, 44, 34, RowBg, White, "-", 24, () => { try { minus(); } catch { } }, tip);
        Btn(parent, "Plus", width - 46, y, 44, 34, RowBg, White, "+", 24, () => { try { plus(); } catch { } }, tip);
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
        if (_cfg != null) _cfg.Language = Loc.Current == Lang.Da ? "da" : "en";
        Rebuild();
    }

    private void Rebuild()
    {
        if (_cfg == null) return;
        bool wasVisible = _root != null && _root.activeSelf;
        int tab = _activeTab;
        var cfg = _cfg; var run = _runNow; var close = _onClose;
        Destroy();
        _toggles.Clear();
        _steppers.Clear();
        _skillRows.Clear();
        _skillsEmpty = null;
        _skillContent = null;
        _skillsReadAt = -99f;
        _activeTab = tab;
        Build(cfg, run, close);
        SetVisible(wasVisible);
    }

    // ---- primitives ----

    private RectTransform MakeRect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
        return rt;
    }

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
