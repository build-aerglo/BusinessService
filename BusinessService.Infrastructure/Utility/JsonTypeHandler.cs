using System.Data;
using System.Text;
using Dapper;
using Newtonsoft.Json;

namespace BusinessService.Infrastructure.Utility;

public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        if (value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        // Newtonsoft serialization
        parameter.Value = JsonConvert.SerializeObject(value);
    }

    public override T Parse(object value)
    {
        if (value == null || value is DBNull)
            return default!;

        // jsonb comes in as byte[]
        if (value is byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            return Deserialize(json);
        }

        // json comes in as string
        if (value is string str)
        {
            return Deserialize(str);
        }

        // Already typed (rare)
        if (value is T t)
            return t;

        throw new InvalidCastException(
            $"Cannot convert database value of type {value.GetType()} to {typeof(T)}");
    }

    private static T Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return default!;

        return JsonConvert.DeserializeObject<T>(json)!;
    }
}