namespace PixelDungeon.Core.View;

public interface ICellListener
{
    void OnSelect(int? cell); // null means selection canceled
    string Prompt();
}