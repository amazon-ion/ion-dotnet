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

using Amazon.IonDotnet.Internals;

namespace Amazon.IonDotnet.Tree.Impl
{
    internal sealed class IonSymbol : IonText, IIonSymbol
    {
        private int _sid;
        private ImportLocation _importLocation;

        public IonSymbol(string text, int sid = SymbolToken.UnknownSid, ImportLocation importLocation = default) : this(new SymbolToken(text, sid, importLocation))
        {
        }

        public IonSymbol(SymbolToken symbolToken) : base(symbolToken.Text, symbolToken == default)
        {
            _sid = symbolToken.Sid;
            _importLocation = symbolToken.ImportLocation;
        }

        private IonSymbol(bool isNull) : base(null, isNull)
        {
        }

        /// <summary>
        /// Returns a new null.symbol value.
        /// </summary>
        public static IonSymbol NewNull() => new IonSymbol(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            if (!(other is IonSymbol oSymbol))
                return false;

            if (NullFlagOn())
                return oSymbol.IsNull;

            return !oSymbol.IsNull && oSymbol.StringVal == StringValue;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Symbol);
                return;
            }

            writer.WriteSymbolToken(SymbolValue);
        }

        public override IonType Type() => IonType.Symbol;

        public override string StringValue
        {
            get => base.StringValue;
        }

        public override SymbolToken SymbolValue
        {
            get => new SymbolToken(StringVal, _sid, _importLocation);
        }
    }
}
