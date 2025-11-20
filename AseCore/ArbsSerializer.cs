using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace AseCore
{
    public static class ArbsSerializer
    {
        private static readonly BinaryFormatter Formatter = new BinaryFormatter();

        public static void Serialize(string path, object o)
        {
            try
            {
                using (var fs = File.OpenWrite(path))
                {
                    Formatter.Serialize(fs, o);
                }
            }
            catch (Exception e)
            {
                throw new SerializationException($"Could not serialize object of type {o.GetType().FullName} to file {path}", e);
            }
        }

        public static object Deserialize(string path, string type)
        {
            object deserialized;
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    deserialized = Formatter.Deserialize(fs);
                }
            }
            catch (Exception e)
            {
                throw new SerializationException($"Could not deserialize file {path}", e);
            }

            var deserializedType = ArbsAsmLoader.GetArbsType(type);
            if (deserializedType.IsInstanceOfType(deserialized)) return deserialized;

            throw new InvalidDataException($"Deserialized object is of type {deserializedType.FullName}, but of {deserialized.GetType().FullName}");
        }
    }
}