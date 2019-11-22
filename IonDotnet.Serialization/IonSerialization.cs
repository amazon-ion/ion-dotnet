using System;
using System.IO;
using IonDotnet.Internals.Text;

namespace IonDotnet.Serialization
{
    public static class IonSerialization
    {
        public static readonly IonTextSerializer Text = new IonTextSerializer();

        public static readonly IonBinarySerializer Binary = new IonBinarySerializer();

        /// <summary>
        /// Generic de-serializer method when the payload form is unknown
        /// </summary>
        /// <param name="payload"></param>
        /// <typeparam name="T">Output type</typeparam>
        /// <returns>De-serialized object</returns>
        public static T Deserialize<T>(byte[] payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            if (payload.Length == 0)
                return default;

            //payload is either utf-8 or binary, figure out
            var isBinary = payload.Length >= 4
                           && payload[0] == 0xE0
                           && payload[1] == 0x01
                           && payload[2] == 0x00
                           && payload[3] == 0xEA;
            if (isBinary)
            {
                return Binary.Deserialize<T>(payload);
            }

            using (var stream = new MemoryStream(payload))
            {
                var reader = new UserTextReader(stream);
                reader.MoveNext();
                return (T) IonSerializationPrivate.Deserialize(reader, typeof(T));
            }
        }
    }
}
