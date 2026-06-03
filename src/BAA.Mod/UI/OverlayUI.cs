using BAA.Core.Config;
using UnityEngine;

namespace BAA.Mod.UI;

/// <summary>
/// The in-game control panel: a styled, dark-theme IMGUI overlay (toggled with F8). Uses the explicit
/// GUI.* API with manual layout (reliable under IL2CPP) rather than GUILayout's params-array overloads.
/// Reads live state for the header and binds toggles directly to the shared AutomationConfig.
/// </summary>
internal sealed class OverlayUI
{
    private const float W = 384f, H = 520f, X = 24f, Y = 24f, Pad = 16f;

    private bool _built;
    private GUIStyle _panel, _card, _title, _sub, _value, _label, _section, _toggle, _field, _pill, _logline;
    private string _reserveBuf;

    public void Draw(AutomationConfig cfg, GameSnapshot s)
    {
        if (!_built)
            Build();
        _reserveBuf ??= ((long)cfg.CashReserveFloor).ToString();

        GUI.Box(new Rect(X, Y, W, H), "", _panel);

        float ix = X + Pad, iw = W - 2f * Pad, cy = Y + Pad;

        // --- Header ---
        GUI.Label(new Rect(ix, cy, iw - 44, 26), "BA BOT", _title);
        GUI.Label(new Rect(X + W - Pad - 36, cy + 3, 36, 18), "F8", _pill);
        cy += 28;
        GUI.Label(new Rect(ix, cy, iw, 16), "Automation control panel", _sub);
        cy += 26;

        // --- Live status card ---
        GUI.Box(new Rect(ix, cy, iw, 56), "", _card);
        if (s.HasSave)
        {
            GUI.Label(new Rect(ix + 12, cy + 7, iw - 24, 24), $"${s.Money:N0}", _value);
            GUI.Label(new Rect(ix + 12, cy + 33, iw - 24, 16),
                $"Day {s.Day}   {s.Hour:00}:{(int)s.Minute:00}    •    Net ${s.NetWorth:N0}", _sub);
        }
        else
        {
            GUI.Label(new Rect(ix + 12, cy + 16, iw - 24, 22), "No save loaded", _value);
        }
        cy += 56 + 12;

        // --- Features ---
        GUI.Label(new Rect(ix, cy, iw, 14), "FEATURES", _section);
        cy += 20;
        cfg.MasterEnabled = GUI.Toggle(new Rect(ix, cy, iw, 22), cfg.MasterEnabled, "  Automation (master)", _toggle);
        cy += 28;
        cfg.RestockEnabled = Feature(ix, ref cy, iw, "Auto-restock", cfg.RestockEnabled);
        cfg.LogisticsEnabled = Feature(ix, ref cy, iw, "Logistics", cfg.LogisticsEnabled);
        cfg.EmployeesEnabled = Feature(ix, ref cy, iw, "Employees", cfg.EmployeesEnabled);
        cfg.FinanceEnabled = Feature(ix, ref cy, iw, "Finance auto-pay", cfg.FinanceEnabled);
        cfg.TimeSkipEnabled = Feature(ix, ref cy, iw, "Time-skip (AFK)", cfg.TimeSkipEnabled);
        cy += 6;

        // --- Reserve floor ---
        GUI.Label(new Rect(ix, cy + 3, 150, 20), "Cash reserve floor $", _label);
        _reserveBuf = GUI.TextField(new Rect(ix + 156, cy, 96, 22), _reserveBuf, _field);
        if (decimal.TryParse(_reserveBuf, out var rf))
            cfg.CashReserveFloor = rf;
        cy += 32;

        // --- Activity log ---
        GUI.Label(new Rect(ix, cy, iw, 14), "ACTIVITY", _section);
        cy += 20;
        float logH = Y + H - cy - Pad;
        GUI.Box(new Rect(ix, cy, iw, logH), "", _card);
        var lines = Diagnostics.Activity.Recent();
        float ly = cy + 6;
        for (int i = 0; i < lines.Count && ly < cy + logH - 14; i++)
        {
            GUI.Label(new Rect(ix + 8, ly, iw - 16, 14), lines[i], _logline);
            ly += 15;
        }
    }

    private bool Feature(float ix, ref float cy, float iw, string label, bool value)
    {
        var result = GUI.Toggle(new Rect(ix + 14, cy, iw - 14, 22), value, "  " + label, _toggle);
        cy += 24;
        return result;
    }

    private void Build()
    {
        Color text = new Color(0.88f, 0.90f, 0.94f);
        Color dim = new Color(0.58f, 0.63f, 0.72f);
        Color accent = new Color(0.36f, 0.80f, 1f);
        Color green = new Color(0.42f, 0.88f, 0.56f);

        _panel = new GUIStyle { normal = { background = Solid(new Color(0.07f, 0.08f, 0.11f, 0.97f)) } };
        _panel.border = new RectOffset(0, 0, 0, 0);
        _card = new GUIStyle { normal = { background = Solid(new Color(1f, 1f, 1f, 0.05f)) } };

        _title = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold };
        _title.normal.textColor = accent;

        _sub = new GUIStyle { fontSize = 12 };
        _sub.normal.textColor = dim;

        _value = new GUIStyle { fontSize = 19, fontStyle = FontStyle.Bold };
        _value.normal.textColor = text;

        _label = new GUIStyle { fontSize = 13 };
        _label.normal.textColor = text;

        _section = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold };
        _section.normal.textColor = accent;

        _logline = new GUIStyle { fontSize = 11, wordWrap = false };
        _logline.normal.textColor = dim;

        _pill = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
        _pill.normal.textColor = dim;
        _pill.normal.background = Solid(new Color(1f, 1f, 1f, 0.08f));

        // Il2CppInterop doesn't expose GUIStyle's copy ctor, so build fresh and borrow the skin's
        // checkbox graphics by reference (textures are immutable; we don't mutate the shared skin).
        var skinToggle = GUI.skin.toggle;
        _toggle = new GUIStyle { fontSize = 13, border = skinToggle.border, padding = skinToggle.padding };
        _toggle.normal.background = skinToggle.normal.background;
        _toggle.onNormal.background = skinToggle.onNormal.background;
        _toggle.hover.background = skinToggle.hover.background;
        _toggle.onHover.background = skinToggle.onHover.background;
        _toggle.normal.textColor = text;
        _toggle.hover.textColor = text;
        _toggle.onNormal.textColor = green;
        _toggle.onHover.textColor = green;

        _field = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleLeft, border = GUI.skin.textField.border, padding = new RectOffset(6, 6, 3, 3) };
        _field.normal.background = Solid(new Color(0f, 0f, 0f, 0.35f));
        _field.normal.textColor = text;

        _built = true;
    }

    private static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }
}
