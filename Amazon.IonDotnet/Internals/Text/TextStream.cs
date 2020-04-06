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

namespace Amazon.IonDotnet.Internals.Text
{
    using System;

    /// <summary>
    /// A unified stream that can represent both text and Utf-8 bytes.
    /// </summary>
    /// <remarks>This stream is not an <see cref="IDisposable"/> and does not any underlying stream(s).</remarks>
    internal abstract class TextStream
    {
        /// <summary>
        /// Read an 'unit', which, depending on the kind of stream, might be a 2-byte <see cref="char"/>
        /// or a <see cref="byte"/>.
        /// </summary>
        /// <returns>The next unit read.</returns>
        public abstract int Read();

        /// <summary>
        /// Try to unread the character unit.
        /// </summary>
        /// <param name="c">Has to be most-recently read unit.</param>
        public abstract void Unread(int c);
    }
}
