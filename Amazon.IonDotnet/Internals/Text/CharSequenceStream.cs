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
    using System.Diagnostics;

    internal class CharSequenceStream : TextStream
    {
        private readonly ReadOnlyMemory<char> chars;
        private int idx;

        public CharSequenceStream(string text)
            : this(text.AsMemory())
        {
        }

        public CharSequenceStream(ReadOnlyMemory<char> chars)
        {
            this.chars = chars;
        }

        public override int Read() => this.idx == this.chars.Length ? -1 : this.chars.Span[this.idx++];

        public override void Unread(int c)
        {
            // EOF
            if (c == -1)
            {
                return;
            }

            // since we have access to the memory layout we can just reduce the index;
            Debug.Assert(this.idx > 0, "idx is less than 1");
            Debug.Assert(this.chars.Span[this.idx - 1] == c, "Span of idx -1 does not match c");
            this.idx--;
        }
    }
}
