using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items;

public class ItemStatusHandler<T> where T : Item
{
    private readonly Type[] _items;

    private readonly Dictionary<Type, int> _images = new();
    private readonly Dictionary<Type, string> _labels = new();
    private readonly HashSet<Type> _known = [];

    public ItemStatusHandler(Type[] items, string[] allLabels, int[] allImages)
    {
        _items = items;

        var labelsLeft = new List<string>(allLabels);
        var imagesLeft = new List<int>(allImages);

        foreach (var item in items)
        {
            var index = Random.Int(labelsLeft.Count);

            _labels[item] = labelsLeft[index];
            labelsLeft.RemoveAt(index);

            _images[item] = imagesLeft[index];
            imagesLeft.RemoveAt(index);
        }
    }

    public ItemStatusHandler(Type[] items, string[] labels, int[] images, Bundle bundle)
    {
        _items = items;

        Restore(bundle, labels, images);
    }

    private const string PfxImage = "_image";

    private const string PfxLabel = "_label";

    private const string PfxKnown = "_known";

    public void Save(Bundle bundle)
    {
        foreach (var type in _items)
        {
            var itemName = type.FullName;
            bundle.Put(itemName + PfxImage, _images[type]);
            bundle.Put(itemName + PfxLabel, _labels[type]);
            bundle.Put(itemName + PfxKnown, _known.Contains(type));
        }
    }

    private void Restore(Bundle bundle, string[] allLabels, int[] allImages)
    {
        var labelsLeft = new List<string>(allLabels);
        var imagesLeft = new List<int>(allImages);

        foreach (var item in _items)
        {
            var itemName = item.FullName;

            if (bundle.Contains(itemName + PfxLabel))
            {
                var label = bundle.GetString(itemName + PfxLabel);
                _labels[item] = label;
                labelsLeft.Remove(label);

                var image = bundle.GetInt(itemName + PfxImage);
                _images[item] = image;
                imagesLeft.Remove(image);

                if (bundle.GetBoolean(itemName + PfxKnown))
                {
                    _known.Add(item);
                }
            }
            else
            {
                var index = Random.Int(labelsLeft.Count);

                _labels[item] = labelsLeft[index];
                labelsLeft.RemoveAt(index);

                _images[item] = imagesLeft[index];
                imagesLeft.RemoveAt(index);
            }
        }
    }

    public int Image(T item) => _images[item.GetType()];

    public string Label(T item) => _labels[item.GetType()];

    public bool IsKnown(T item) => _known.Contains(item.GetType());

    public void Know(T item)
    {
        _known.Add(item.GetType());

        if (_known.Count != _items.Length - 1) return;

        foreach (var type in _items)
        {
            if (_known.Add(type))
            {
                break;
            }
        }
    }

    public List<Type> Known() => [.. _items.Where(_known.Contains)];

    public List<Type> Unknown() => [.. _items.Where(item => !_known.Contains(item))];
}