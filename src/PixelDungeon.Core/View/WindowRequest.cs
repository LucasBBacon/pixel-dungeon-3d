namespace PixelDungeon.Core.View;

public sealed class WindowRequest
{
    public readonly string Title;
    public readonly string Body;
    public readonly string[] Options;
    public readonly Action<int> OnSelect;

    public WindowRequest(string title,
        string body,
        string[] options,
        Action<int> onSelect)
    {
        Title = title;
        Body = body;
        Options = options;
        OnSelect = onSelect;
    }

    public static WindowRequest Message(string body)
    {
        return new WindowRequest(null, body, ["OK"], null);
    }

    public bool IsChoice => Options.Length > 1;

    public void Select(int index)
    {
        OnSelect?.Invoke(index);
    }
}