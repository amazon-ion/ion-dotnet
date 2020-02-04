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
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

namespace IonDotnet.Serialization
{
    public class IonBinarySerializer
    {
        public byte[] Serialize<T>(T obj, IScalarWriter scalarWriter = null)
        {
            using (var stream = new MemoryStream())
            {
                using (var binWriter = new ManagedBinaryWriter(stream, Symbols.EmptySymbolTablesArray))
                {
                    IonSerializationPrivate.WriteObject(binWriter, obj, scalarWriter);
                    binWriter.Flush();
                    binWriter.Finish();
                }

                return stream.GetWrittenBuffer();
            }
        }

        public void Serialize<T>(T obj, Stream stream, IScalarWriter scalarWriter = null)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));


            using (var binWriter = new ManagedBinaryWriter(stream, Symbols.EmptySymbolTablesArray))
            {
                IonSerializationPrivate.WriteObject(binWriter, obj, scalarWriter);
                binWriter.Flush();
            }
        }

        /// <summary>
        /// Deserialize a binary format to object type T
        /// </summary>
        /// <param name="binary">Binary input</param>
        /// <typeparam name="T">Type of object to deserialize to</typeparam>
        /// <returns>Deserialized object</returns>
        public T Deserialize<T>(byte[] binary)
        {
            using (var stream = new MemoryStream(binary))
            {
                var reader = new UserBinaryReader(stream);
                reader.MoveNext();
                return (T) IonSerializationPrivate.Deserialize(reader, typeof(T));
            }
        }
    }
}
