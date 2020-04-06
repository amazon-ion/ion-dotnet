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

namespace Amazon.IonDotnet.Builders
{
    using System.Collections.Generic;
    using System.IO;
    using Amazon.IonDotnet.Internals.Text;

    public static class IonTextWriterBuilder
    {
        /// <summary>
        /// Build an Ion text writer.
        /// </summary>
        /// <param name="textWriter">Writer that can write to the output.</param>
        /// <param name="imports">Symbol tables that the write can use to encode symbols.</param>
        /// <returns>Ion text writer.</returns>
        public static IIonWriter Build(TextWriter textWriter, IEnumerable<ISymbolTable> imports = null)
        {
            return new IonTextWriter(textWriter, imports);
        }

        /// <summary>
        /// Build an Ion text writer.
        /// </summary>
        /// <param name="textWriter">Writer that can write to the output.</param>
        /// <param name="options">Text writer options.</param>
        /// <param name="imports">Symbol tables that the write can use to encode symbols.</param>
        /// <returns>Ion text writer.</returns>
        public static IIonWriter Build(TextWriter textWriter, IonTextOptions options, IEnumerable<ISymbolTable> imports = null)
        {
            return new IonTextWriter(textWriter, options, imports);
        }
    }
}
