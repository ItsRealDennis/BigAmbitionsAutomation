using System.Collections.Generic;

namespace BaBot.Diagnostics;

/// <summary>A small newest-first ring buffer of human-readable activity lines shown in the overlay.</summary>
internal static class Activity
{
    private const int Max = 50;
    private static readonly List<string> _items = new();

    public static void Add(string message)
    {
        // Also surface to the Unity log so actions/previews are visible (Player.log) without a panel.
        try { UnityEngine.Debug.Log("[BA BOT] " + message); } catch { }
        lock (_items)
        {
            _items.Insert(0, message);
            if (_items.Count > Max)
                _items.RemoveAt(_items.Count - 1);
        }
    }

    public static List<string> Recent()
    {
        lock (_items)
        {
            return new List<string>(_items);
        }
    }
}
