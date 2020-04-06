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

namespace Amazon.IonDotnet
{
    using System;

    /// <inheritdoc cref="IEquatable{T}" />
    /// <summary>
    /// A SymbolToken providing both the symbol text and the assigned symbol ID.
    /// Symbol tokens may be interned into a <see cref="T:Amazon.IonDotnet.ISymbolTable" />.
    /// </summary>
    /// <remarks>
    /// A text=null or sid=-1 value might indicate that such field is unknown in the contextual symbol table.
    /// </remarks>
    public readonly struct SymbolToken : IEquatable<SymbolToken>
    {
        /// <summary>
        /// The default Sid, which is unknown.
        /// </summary>
        public const int UnknownSid = -1;

        /// <summary>
        /// The default value, corresponds to not_found/unknown.
        /// </summary>
        public static readonly SymbolToken None = default;

        public static readonly SymbolToken[] EmptyArray = new SymbolToken[0];

        /// <summary>
        /// The text of this symbol.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The import location of this symbol token.
        /// </summary>
        public readonly ImportLocation ImportLocation;

        private readonly int sid;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolToken"/> struct.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="sid">Sid.</param>
        /// <param name="importLocation">ImportLocation.</param>
        public SymbolToken(string text, int sid, ImportLocation importLocation = default)
        {
            // Note: due to the fact that C# structs are initialized 'blank' (all fields 0), and we want the default
            // Sid to be Unknown(-1), the actual field value is shifted by +1 compared to the publicly
            // returned value
            this.Text = text;
            this.sid = sid + 1;
            this.ImportLocation = importLocation;
        }

        /// <summary>
        /// Gets the ID of this symbol token.
        /// </summary>
        public int Sid => this.sid - 1;

        public static bool operator ==(SymbolToken x, SymbolToken y) => x.Text == y.Text && x.Sid == y.Sid && x.ImportLocation == y.ImportLocation;

        public static bool operator !=(SymbolToken x, SymbolToken y) => !(x == y);

        // Override everything to avoid boxing allocation
        public override string ToString() => $"SymbolToken::{{text:{this.Text}, id:{this.Sid}, importLocation:{this.ImportLocation.ToString()}}}";

        public override bool Equals(object that) => that is SymbolToken token && this.Equals(token);

        public override int GetHashCode() => this.Text?.GetHashCode() ?? this.Sid;

        public bool Equals(SymbolToken other) => this == other;

        public bool IsEquivalentTo(SymbolToken other)
        {
            if (!(this.Text is null))
            {
                return this.Text == other.Text;
            }

            if (other.Text != null)
            {
                return false;
            }

            return other.ImportLocation == this.ImportLocation;
        }
    }
}
