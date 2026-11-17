using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

public static class EventSerialization
{
    public static readonly JsonSerializerSettings Settings = CreateSettings();

    private static JsonSerializerSettings CreateSettings()
    {
        return new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Objects,
            SerializationBinder = new ShortNameBinder(),
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = { new StringEnumConverter() }
        };
    }

    private class ShortNameBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            var t = Type.GetType(typeName);
            if (t != null) return t;

#pragma warning disable UAC0005
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
#pragma warning restore UAC0005
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }

            return null;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }
    }
}