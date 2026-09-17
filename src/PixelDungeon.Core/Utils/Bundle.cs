using System.Text.Json;
using System.Text.Json.Nodes;

namespace PixelDungeon.Core.Utils;

public class Bundle
{
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