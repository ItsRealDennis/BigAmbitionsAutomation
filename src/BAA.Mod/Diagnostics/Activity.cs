namespace BAA.Mod.Diagnostics;

/// <summary>A small newest-first ring buffer of human-readable activity lines shown in the overlay.</summary>
internal static class Activity
{
    private const int Max = 50;
    private static readonly System.Collections.Generic.List<string> _items = new();

    public static void Add(string message)
    {
        lock (_items)
        {
            _items.Insert(0, message);
            if (_items.Count > Max)
                _items.RemoveAt(_items.Count - 1);
        }
    }

    public static System.Collections.Generic.List<string> Recent()
    {
        lock (_items)
        {
            return new System.Collections.Generic.List<string>(_items);
        }
    }
}
