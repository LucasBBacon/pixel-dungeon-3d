using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PixelDungeon.Core.Utils;

public class Bundle
{
    private const string ClassName = "__className";

    private static readonly Dictionary<string, string> Aliases = new();

    private readonly JsonObject _data;

    public Bundle() : this(new JsonObject())
    {
    }

    private Bundle(JsonObject data)
    {
        _data = data;
    }

    public override string ToString()
    {
        return _data == null ? "null" : _data.ToJsonString();
    }

    public bool IsNull => _data == null;

    public List<string> Fields()
    {
        return _data.Select(pair => pair.Key).ToList();
    }

    public bool Contains(string key)
    {
        return Node(key) != null;
    }

    public bool GetBoolean(string key)
    {
        return Value(key, false);
    }

    public int GetInt(string key)
    {
        return Value(key, 0);
    }

    public float GetFloat(string key)
    {
        // java returns NaN for a missing float, 0 is safer
        return Value(key, 0f);
    }

    public string GetString(string key)
    {
        return Value(key, "");
    }

    public Bundle GetBundle(string key)
    {
        return new Bundle(Node(key) as JsonObject);
    }

    public int[] GetIntArray(string key)
    {
        return Value<int[]>(key, null);
    }

    public bool[] GetBooleanArray(string key)
    {
        return Value<bool[]>(key, null);
    }

    public string[] GetStringArray(string key)
    {
        return Value<string[]>(key, null);
    }

    private IBundlable Get()
    {
        // The Java catches every exception here and returns null
        try
        {
            var className = GetString(ClassName);
            if (Aliases.TryGetValue(className, out var aliased))
            {
                className = aliased;
            }

            var type = ResolveType(className);
            if (type == null)
            {
                return null;
            }

            var instance = (IBundlable)Activator.CreateInstance(type);
            instance?.RestoreFromBundle(this);
            return instance;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public IBundlable Get(string key)
    {
        return GetBundle(key).Get();
    }

    public TEnum GetEnum<TEnum>(string key) where TEnum : struct, Enum
    {
        if (Enum.TryParse(GetString(key), out TEnum result) && Enum.IsDefined(typeof(TEnum), result))
        {
            return result;
        }

        return Enum.GetValues<TEnum>()[0];
    }

    public List<IBundlable> GetCollection(string key)
    {
        var list = new List<IBundlable>();
        if (Node(key) is JsonArray array)
        {
            list.AddRange(
                array
                    .Select(element => new Bundle(element as JsonObject)
                        .Get())
            );
        }

        return list;
    }

    public void Put(string key, bool value)
    {
        _data[key] = JsonValue.Create(value);
    }

    public void Put(string key, int value)
    {
        _data[key] = JsonValue.Create(value);
    }

    public void Put(string key, float value)
    {
        _data[key] = JsonValue.Create(value);
    }

    public void Put(string key, string value)
    {
        _data[key] = value == null ? null : JsonValue.Create(value);
    }

    public void Put(string key, Bundle bundle)
    {
        // a JsonNode can have only one parent, nested obj is cloned
        _data[key] = bundle._data?.DeepClone();
    }

    public void Put(string key, int[] array)
    {
        _data[key] = JsonSerializer.SerializeToNode(array);
    }

    public void Put(string key, bool[] array)
    {
        _data[key] = JsonSerializer.SerializeToNode(array);
    }

    public void Put(string key, string[] array)
    {
        _data[key] = JsonSerializer.SerializeToNode(array);
    }

    public void Put(string key, IBundlable value)
    {
        if (value == null)
        {
            return;
        }

        var bundle = new Bundle();
        bundle.Put(ClassName, value.GetType().FullName);
        value.StoreInBundle(bundle);
        _data[key] = bundle._data;
    }

    public void Put(string key, Enum value)
    {
        if (value != null)
        {
            _data[key] = JsonValue.Create(value.ToString());
        }
    }

    public void Put(string key, IEnumerable<IBundlable> collection)
    {
        var array = new JsonArray();
        foreach (var value in collection)
        {
            var bundle = new Bundle();
            bundle.Put(ClassName, value.GetType().FullName);
            value.StoreInBundle(bundle);
            array.Add(bundle._data);
        }

        _data[key] = array;
    }

    public static void AddAlias(Type type, string alias)
    {
        Aliases[alias] = type.FullName;
    }

    private static Type ResolveType(string name)
    {
        var type = typeof(Bundle).Assembly.GetType(name);
        if (type != null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(name);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    public static Bundle Read(Stream stream)
    {
        try
        {
            return JsonNode.Parse(stream) is JsonObject json ? new Bundle(json) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static Bundle Read(byte[] bytes)
    {
        try
        {
            return JsonNode.Parse(bytes) is JsonObject json ? new Bundle(json) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool Write(Bundle bundle, Stream stream)
    {
        try
        {
            using var writer = new Utf8JsonWriter(stream);
            bundle._data.WriteTo(writer);
            writer.Flush();
            return true;
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException or NotSupportedException
                                      or ArgumentException)
        {
            return false;
        }
    }

    private JsonNode Node(string key)
    {
        return _data != null && _data.TryGetPropertyValue(key, out var node) ? node : null;
    }

    private T Value<T>(string key, T fallback)
    {
        var node = Node(key);
        if (node == null)
        {
            return fallback;
        }

        try
        {
            return node.Deserialize<T>();
        }
        catch (JsonException)
        {
            return fallback;
        }
        catch (InvalidOperationException)
        {
            return fallback;
        }
    }
}