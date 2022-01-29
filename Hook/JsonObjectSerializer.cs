using Microsoft.Toolkit.Helpers;
using Newtonsoft.Json;

namespace Hook
{
    internal class JsonObjectSerializer : IObjectSerializer
    {
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings();

        string IObjectSerializer.Serialize<T>(T value) => JsonConvert.SerializeObject(value, typeof(T), Formatting.Indented, settings);

        public T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value, settings);
    }
}
