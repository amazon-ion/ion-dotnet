/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using IonDotnet.Builders;
using IonDotnet.Internals.Text;

namespace IonDotnet.Serialization
{
    public class IonTextSerializer
    {
        public string Serialize<T>(T obj, IScalarWriter scalarWriter = null)
            => Serialize(obj, IonTextOptions.Default, scalarWriter);

        public string Serialize<T>(T obj, IonTextOptions options, IScalarWriter scalarWriter = null)
        {
            using (var sw = new StringWriter())
            {
                var writer = new IonTextWriter(sw, options);
                IonSerializationPrivate.WriteObject(writer, obj, scalarWriter);
                return sw.ToString();
            }
        }

        public Task SerializeAsync<T>(T obj, Stream stream, IonTextOptions options)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));

            using (var streamWriter = new StreamWriter(stream))
            {
                var writer = new IonTextWriter(streamWriter, options);
                IonSerializationPrivate.WriteObject(writer, obj, null);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deserialize a text format to object type T
        /// </summary>
        /// <param name="text">Text input</param>
        /// <typeparam name="T">Type of object to deserialize to</typeparam>
        /// <returns>Deserialized object</returns>
        public T Deserialize<T>(string text)
        {
            var reader = new UserTextReader(text);
            reader.MoveNext();
            return (T) IonSerializationPrivate.Deserialize(reader, typeof(T));
        }
    }
}
