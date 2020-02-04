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

using System.Collections.Generic;
using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

namespace IonDotnet.Builders
{
    public static class IonBinaryWriterBuilder
    {
        /// <summary>
        /// Build a binary writer that write to a stream.
        /// </summary>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="imports">Imported symbol tables used to encode symbols.</param>
        /// <param name="forceFloat64">Always write float values in 64 bits. When false, float values will be
        /// written in 32 bits when it is possible to do so without losing fidelity.</param>
        /// <returns>A new Ion writer.</returns>
        public static IIonWriter Build(
            Stream outputStream,
            IEnumerable<ISymbolTable> imports = null,
            bool forceFloat64 = false)
        {
            outputStream.CheckStreamCanWrite();
            return new ManagedBinaryWriter(
                outputStream,
                imports ?? Symbols.EmptySymbolTablesArray,
                forceFloat64);
        }
    }
}
