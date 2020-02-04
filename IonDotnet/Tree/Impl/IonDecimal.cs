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
using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    internal sealed class IonDecimal : IonValue, IIonDecimal
    {
        private BigDecimal _val;

        public IonDecimal(double doubleValue) : this(Convert.ToDecimal(doubleValue))
        {
        }

        public IonDecimal(decimal value) : this(new BigDecimal(value))
        {
        }

        public IonDecimal(BigDecimal bigDecimal) : base(false)
        {
            _val = bigDecimal;
        }

        private IonDecimal(bool isNull) : base(isNull)
        {
        }

        public static IonDecimal NewNull() => new IonDecimal(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            var otherDec = (IonDecimal)other;

            if (NullFlagOn())
                return otherDec.IsNull;
            if (other.IsNull)
                return false;

            if (BigDecimalValue.IsNegativeZero ^ otherDec.BigDecimalValue.IsNegativeZero)
                return false;

            if (otherDec.BigDecimalValue.Scale > 0 || BigDecimalValue.Scale > 0)
            {
                //precision matters, make sure that this has the same precision
                return BigDecimalValue.Scale == otherDec.BigDecimalValue.Scale
                       && BigDecimalValue.IntVal == otherDec.BigDecimalValue.IntVal;
            }

            //this only compares values
            return BigDecimalValue == otherDec.BigDecimalValue;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Decimal);
                return;
            }

            writer.WriteDecimal(BigDecimalValue);
        }

        public override decimal DecimalValue
        {
            get
            {
                ThrowIfNull();
                return _val.ToDecimal();
            }
        }

        public override BigDecimal BigDecimalValue
        {
            get
            {
                ThrowIfNull();
                return _val;
            }
        }

        public override IonType Type() => IonType.Decimal;
    }
}
