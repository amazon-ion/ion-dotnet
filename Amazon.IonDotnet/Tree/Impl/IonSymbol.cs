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

namespace Amazon.IonDotnet.Tree.Impl
{
    using Amazon.IonDotnet.Internals;

    internal sealed class IonSymbol : IonText, IIonSymbol
    {
        private readonly ImportLocation importLocation;
        private int sid;

        public IonSymbol(string text, int sid = SymbolToken.UnknownSid, ImportLocation importLocation = default)
            : this(new SymbolToken(text, sid, importLocation))
        {
        }

        public IonSymbol(SymbolToken symbolToken)
            : base(symbolToken.Text, symbolToken == default)
        {
            this.sid = symbolToken.Sid;
            this.importLocation = symbolToken.ImportLocation;
        }

        private IonSymbol(bool isNull)
            : base(null, isNull)
        {
        }

        public override string StringValue
        {
            get => base.StringValue;
        }

        public override SymbolToken SymbolValue
        {
            get => new SymbolToken(this.StringVal, this.sid, this.importLocation);
        }

        /// <summary>
        /// Returns a new null.symbol value.
        /// </summary>
        /// <returns>A null IonSymbol.</returns>
        public static IonSymbol NewNull() => new IonSymbol(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            if (!(other is IonSymbol oSymbol))
            {
                return false;
            }

            if (this.NullFlagOn())
            {
                return oSymbol.IsNull;
            }

            return !oSymbol.IsNull && oSymbol.StringVal == this.StringValue;
        }

        public override IonType Type() => IonType.Symbol;

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (this.NullFlagOn())
            {
                writer.WriteNull(IonType.Symbol);
                return;
            }

            writer.WriteSymbolToken(this.SymbolValue);
        }
    }
}
