using System;
using BAA.Core.Config;
using BaBot.Diagnostics;
using UnityEngine;

namespace BaBot.UI;

/// <summary>
/// The in-game control panel (F8): a rounded, slate-blue, game-matching IMGUI overlay. In the Mono
/// build IMGUI is not stripped, so this is the same proven premium panel we shipped before, drawn from
/// GUI.Box + GUI.Label + manual click detection. All user text goes through <see cref="Loc.T"/>.
/// </summary>
internal sealed class OverlayUI
{
    private const float W = 380f, H = 880f, X = 24f, Y = 24f, Pad = 16f;
    private const decimal ReserveStep = 500m;
    private const decimal FeeStep = 50m;

    /// <summary>Screen rect of the panel in GUI space (origin top-left) — used to block click-through.</summary>
    internal static Rect PanelRect => new Rect(X, Y, W, H);

    /// <summary>Invoked when the user presses the RUN NOW button (wired by the behaviour to the engine).</summary>
    public Action OnRunNow;

    private static readonly Color Slate = new(0.21f, 0.25f, 0.31f, 0.97f);
    private static readonly Color Inset = new(0.12f, 0.14f, 0.18f, 0.85f);
    private static readonly Color Blue = new(0.18f, 0.50f, 0.82f);
    private static readonly Color Green = new(0.33f, 0.66f, 0.34f);
    private static readonly Color Cyan = new(0.36f, 0.80f, 1f);
    private static readonly Color SwitchOff = new(0.30f, 0.35f, 0.42f);
    private static readonly Color White = new(0.96f, 0.97f, 0.99f);
    private static readonly Color Dim = new(0.68f, 0.74f, 0.82f);

    private bool _built;
    private GUIStyle _panel, _inset, _title, _sub, _subWarn, _value, _label, _section, _logline, _pill, _pillOn, _footer;
    private GUIStyle _btnBlue, _btnGreen, _btnDark, _swOn, _swOff, _tipBg, _tipStyle;
    private string _tip;
    private Vector2 _tipAt;

    public void Draw(AutomationConfig cfg, GameSnapshot s)
    {
        if (!_built)
            Build();

        _tip = null;
        GUI.Box(new Rect(X, Y, W, H), "", _panel);
        float ix = X + Pad, iw = W - 2f * Pad, cy = Y + Pad;

        // --- Header ---
        GUI.Label(new Rect(ix, cy, iw - 44, 28), "BA BOT", _title);
        GUI.Box(new Rect(X + W - Pad - 38, cy + 4, 38, 20), "F8", _pill);
        cy += 30;
        GUI.Label(new Rect(ix, cy, iw - 76, 14), Loc.T("AUTOMATION CONTROL"), _sub);
        if (LangPill(new Rect(X + W - Pad - 70, cy - 2, 33, 18), "EN", Loc.Current == Lang.En)) SetLang(cfg, Lang.En);
        if (LangPill(new Rect(X + W - Pad - 35, cy - 2, 33, 18), "DA", Loc.Current == Lang.Da)) SetLang(cfg, Lang.Da);
        cy += 24;

        // --- Live status ---
        GUI.Box(new Rect(ix, cy, iw, 92), "", _inset);
        if (s.HasSave)
        {
            GUI.Label(new Rect(ix + 14, cy + 8, iw - 28, 24), $"${s.Money:N0}", _value);
            GUI.Label(new Rect(ix + 14, cy + 33, iw - 28, 16),
                $"{Loc.T("DAY")} {s.Day}   {s.Hour:00}:{(int)s.Minute:00}   •   {Loc.T("NET")} ${s.NetWorth:N0}", _sub);
            GUI.Label(new Rect(ix + 14, cy + 52, iw - 28, 16),
                $"{Loc.T("Shops")} {s.PlayerBusinesses}   •   {Loc.T("Energy")} {s.Energy:0}   •   {Loc.T("Happy")} {s.Happiness:0}", _sub);
            var taxStyle = s.TaxDue > 0f ? _subWarn : _sub;
            GUI.Label(new Rect(ix + 14, cy + 70, iw - 28, 16),
                $"{Loc.T("Tax due")} ${s.TaxDue:N0}   •   {Loc.T("Loans")} {s.Loans}   •   {Loc.T("Staff")} {s.Employees}", taxStyle);
        }
        else
        {
            GUI.Label(new Rect(ix + 14, cy + 34, iw - 28, 22), Loc.T("NO SAVE LOADED"), _value);
        }
        cy += 92 + 14;

        // --- Quick actions ---
        GUI.Label(new Rect(ix, cy, iw, 13), Loc.T("QUICK ACTIONS"), _section);
        cy += 18;
        float bw = (iw - 8) / 2f;
        if (Button(new Rect(ix, cy, bw, 30), "+$1,000", _btnBlue, "Instantly add $1,000 cash. Handy for testing the mod."))
            GameActions.AddMoney(1000f);
        if (Button(new Rect(ix + bw + 8, cy, bw, 30), Loc.T("ENERGY 100%"), _btnGreen, "Instantly refill your energy to full."))
            GameActions.RefillEnergy();
        cy += 30 + 8;
        if (Button(new Rect(ix, cy, iw, 26), Loc.T("RUN NOW"), _btnDark, "Runs one automation pass now. Turn on AUTOMATION (MASTER) and the features you want first. Previews unless Live mode is on; respects your reserve floor."))
            OnRunNow?.Invoke();
        cy += 26 + 14;

        // --- Features ---
        GUI.Label(new Rect(ix, cy, iw, 13), Loc.T("FEATURES"), _section);
        cy += 18;
        cfg.MasterEnabled = SwitchRow(ix, ref cy, iw, "AUTOMATION (MASTER)", cfg.MasterEnabled, false, "Master switch. Must be ON for anything below to run. Off = the mod does nothing.");
        cfg.FinanceEnabled = SwitchRow(ix, ref cy, iw, "FINANCE AUTO-PAY", cfg.FinanceEnabled, true, "Auto-pays your taxes the moment they come due — the one finance chore the game won't do for you. Rent, wages and loans already settle nightly. Respects your reserve floor.");
        cfg.EmployeesEnabled = SwitchRow(ix, ref cy, iw, "EMPLOYEES", cfg.EmployeesEnabled, true, "Keeps staff productive: pays a morale bonus to unhappy employees when the game allows one, and finishes completed training.");
        cfg.LogisticsEnabled = SwitchRow(ix, ref cy, iw, "LOGISTICS", cfg.LogisticsEnabled, true, "Sets up a repeating weekly import for any product running low so stock keeps flowing without manual reordering.");
        cfg.RestockEnabled = SwitchRow(ix, ref cy, iw, "AUTO-RESTOCK", cfg.RestockEnabled, true, "Buys products back up to target when shelves run low (live in Live mode; previews otherwise).");
        cfg.TimeSkipEnabled = SwitchRow(ix, ref cy, iw, "TIME-SKIP (AFK)", cfg.TimeSkipEnabled, true, "Fast-forwards in-game time while your businesses keep earning. Turn off for normal speed.");
        cfg.WellbeingEnabled = SwitchRow(ix, ref cy, iw, "AUTO-WELLBEING", cfg.WellbeingEnabled, true, "Automatically refills your energy (and tops up happiness) so you never stop to sleep or eat.");
        cfg.ServiceFeeEnabled = SwitchRow(ix, ref cy, iw, "SERVICE FEE", cfg.ServiceFeeEnabled, true, "Optional challenge: charges in-game cash each time automation does work for you, so leaning on the bot has a cost. OFF = free (default). Respects your reserve floor.");
        cfg.LiveWrites = SwitchRow(ix, ref cy, iw, "LIVE MODE", cfg.LiveWrites, false, "OFF (default) = automation only PREVIEWS what it would do (safe). ON = it actually pays taxes, restocks and gives bonuses. Turn on only while watching the game.");
        cy += 8;

        // --- Reserve floor ---
        cy = Stepper(ix, cy, iw, $"{Loc.T("RESERVE FLOOR")}  ${cfg.CashReserveFloor:N0}",
            "Automation never spends below this cash cushion. Use the minus / plus buttons to adjust.",
            () => cfg.CashReserveFloor = System.Math.Max(0m, cfg.CashReserveFloor - ReserveStep),
            () => cfg.CashReserveFloor += ReserveStep);

        // --- Restock target ---
        cy = Stepper(ix, cy, iw, $"{Loc.T("RESTOCK TARGET")}  {cfg.RestockTarget}",
            "Auto-restock fills each product up to this many units.",
            () => cfg.RestockTarget = System.Math.Max(1, cfg.RestockTarget - 5),
            () => cfg.RestockTarget += 5);

        // --- Service fee per run ---
        cy = Stepper(ix, cy, iw, $"{Loc.T("FEE / RUN")}  ${cfg.ServiceFeePerRun:N0}",
            "Cash charged per automation run when SERVICE FEE is on. Use the minus / plus buttons to adjust.",
            () => cfg.ServiceFeePerRun = System.Math.Max(0m, cfg.ServiceFeePerRun - FeeStep),
            () => cfg.ServiceFeePerRun += FeeStep);

        // --- Activity log ---
        GUI.Label(new Rect(ix, cy, iw, 13), Loc.T("ACTIVITY"), _section);
        cy += 18;
        float logH = Y + H - cy - Pad - 20;
        if (logH < 30) logH = 30;
        GUI.Box(new Rect(ix, cy, iw, logH), "", _inset);
        var lines = Activity.Recent();
        float ly = cy + 8;
        for (int i = 0; i < lines.Count && ly < cy + logH - 14; i++)
        {
            GUI.Label(new Rect(ix + 10, ly, iw - 20, 14), lines[i], _logline);
            ly += 15;
        }

        // Footer
        GUI.Label(new Rect(ix, Y + H - Pad - 14, iw, 14), $"BA BOT  v0.6.0     •     {Loc.T("F8 to toggle")}", _footer);

        DrawTooltip();
    }

    private static void SetLang(AutomationConfig cfg, Lang lang)
    {
        Loc.Current = lang;
        cfg.Language = lang == Lang.Da ? "da" : "en";
    }

    private float Stepper(float ix, float cy, float iw, string label, string tip, Action minus, Action plus)
    {
        GUI.Label(new Rect(ix, cy + 5, iw - 76, 20), label, _label);
        TipIf(new Rect(ix, cy, iw - 76, 26), tip);
        if (Button(new Rect(ix + iw - 68, cy, 30, 26), "−", _btnDark)) minus();
        if (Button(new Rect(ix + iw - 32, cy, 30, 26), "+", _btnDark)) plus();
        return cy + 36;
    }

    // --- Custom controls (Box + Label + manual hit-test) ---

    private bool LangPill(Rect r, string label, bool active)
    {
        GUI.Box(r, label, active ? _pillOn : _pill);
        return Clicked(r);
    }

    private bool SwitchRow(float x, ref float cy, float w, string label, bool value, bool indent, string tip)
    {
        float lx = x + (indent ? 16 : 0);
        var row = new Rect(x, cy, w, 26);
        GUI.Label(new Rect(lx, cy + 5, w - 60, 18), Loc.T(label), indent ? _label : _section);

        var pill = new Rect(x + w - 50, cy + 2, 48, 22);
        GUI.Box(pill, value ? Loc.T("ON") : Loc.T("OFF"), value ? _swOn : _swOff);

        TipIf(row, tip);
        cy += 28;
        return Clicked(row) ? !value : value;
    }

    private bool Button(Rect r, string label, GUIStyle style, string tip = null)
    {
        GUI.Box(r, label, style);
        TipIf(r, tip);
        return Clicked(r);
    }

    private void TipIf(Rect r, string tip)
    {
        if (string.IsNullOrEmpty(tip)) return;
        var e = Event.current;
        if (e != null && r.Contains(e.mousePosition)) { _tip = Loc.T(tip); _tipAt = e.mousePosition; }
    }

    private void DrawTooltip()
    {
        if (string.IsNullOrEmpty(_tip)) return;
        int lines = Mathf.Max(1, Mathf.CeilToInt(_tip.Length / 34f));
        float tw = 230f, th = lines * 16f + 14f;
        float tx = _tipAt.x + 16f, ty = _tipAt.y + 18f;
        if (tx + tw + 12f > Screen.width) tx = _tipAt.x - tw - 16f;
        if (ty + th + 10f > Screen.height) ty = Screen.height - th - 12f;
        if (tx < 4f) tx = 4f;
        if (ty < 4f) ty = 4f;
        GUI.Box(new Rect(tx, ty, tw, th), "", _tipBg);
        GUI.Label(new Rect(tx + 10f, ty + 7f, tw - 20f, th - 12f), _tip, _tipStyle);
    }

    private static bool Clicked(Rect r)
    {
        var e = Event.current;
        if (e != null && e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
        {
            e.Use();
            return true;
        }
        return false;
    }

    private void Build()
    {
        _panel = Rounded(Slate, 12);
        _inset = Rounded(Inset, 8);
        _pill = Rounded(new Color(1f, 1f, 1f, 0.10f), 6);
        _pill.fontSize = 11; _pill.fontStyle = FontStyle.Bold; _pill.alignment = TextAnchor.MiddleCenter; _pill.normal.textColor = Dim;
        _pillOn = Rounded(Cyan, 6);
        _pillOn.fontSize = 11; _pillOn.fontStyle = FontStyle.Bold; _pillOn.alignment = TextAnchor.MiddleCenter; _pillOn.normal.textColor = new Color(0.04f, 0.09f, 0.14f);

        _title = Text(22, White, FontStyle.Bold);
        _sub = Text(11, Dim, FontStyle.Bold);
        _subWarn = Text(11, new Color(0.98f, 0.74f, 0.36f), FontStyle.Bold);
        _value = Text(20, White, FontStyle.Bold);
        _label = Text(13, White, FontStyle.Bold);
        _section = Text(11, Dim, FontStyle.Bold);
        _logline = Text(11, Dim);
        _footer = Text(10, new Color(0.46f, 0.52f, 0.62f));
        _footer.alignment = TextAnchor.MiddleCenter;
        _tipBg = Rounded(new Color(0.03f, 0.04f, 0.07f, 0.98f), 8);
        _tipStyle = Text(12, new Color(0.90f, 0.93f, 0.97f));
        _tipStyle.wordWrap = true;

        _btnBlue = ButtonStyle(Blue);
        _btnGreen = ButtonStyle(Green);
        _btnDark = ButtonStyle(SwitchOff);

        _swOn = Rounded(Green, 8);
        _swOn.fontSize = 11; _swOn.fontStyle = FontStyle.Bold; _swOn.alignment = TextAnchor.MiddleCenter; _swOn.normal.textColor = White;
        _swOff = Rounded(SwitchOff, 8);
        _swOff.fontSize = 11; _swOff.fontStyle = FontStyle.Bold; _swOff.alignment = TextAnchor.MiddleCenter; _swOff.normal.textColor = Dim;

        _built = true;
    }

    private static GUIStyle ButtonStyle(Color fill)
    {
        var s = Rounded(fill, 8);
        s.fontSize = 13; s.fontStyle = FontStyle.Bold; s.alignment = TextAnchor.MiddleCenter;
        s.normal.textColor = new Color(1f, 1f, 1f, 0.98f);
        return s;
    }

    private static GUIStyle Text(int size, Color color, FontStyle style = FontStyle.Normal)
    {
        var s = new GUIStyle { fontSize = size, fontStyle = style };
        s.normal.textColor = color;
        return s;
    }

    private static GUIStyle Rounded(Color fill, int radius)
    {
        var s = new GUIStyle();
        s.normal.background = RoundedTex(radius, fill);
        s.border = new RectOffset(radius, radius, radius, radius);
        return s;
    }

    /// <summary>A rounded-rect texture for 9-slice backgrounds (corners stay crisp when stretched).</summary>
    private static Texture2D RoundedTex(int radius, Color fill)
    {
        int size = radius * 2 + 2;
        var clear = new Color(0f, 0f, 0f, 0f);
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cx = x < radius ? radius : (x > size - 1 - radius ? size - 1 - radius : x);
                int cy = y < radius ? radius : (y > size - 1 - radius ? size - 1 - radius : y);
                float dx = x - cx, dy = y - cy;
                bool inside = dx * dx + dy * dy <= (radius + 0.5f) * (radius + 0.5f);
                t.SetPixel(x, y, inside ? fill : clear);
            }
        }
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        t.hideFlags = HideFlags.HideAndDontSave;
        t.Apply();
        return t;
    }
}
