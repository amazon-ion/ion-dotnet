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
using System.Collections.Generic;
using Amazon.IonDotnet.Internals;
using Amazon.IonDotnet.Utils;

namespace Amazon.IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// Ion value representing a floating point number.
    /// </summary>
    internal sealed class IonFloat : IonValue, IIonFloat
    {
        private double _d;

        public IonFloat(double value) : base(false)
        {
            _d = value;
        }

        private IonFloat(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.float value.
        /// </summary>
        public static IonFloat NewNull() => new IonFloat(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            var oFloat = (IonFloat)other;

            if (NullFlagOn())
                return oFloat.IsNull;
            if (oFloat.IsNull)
                return false;

            if (PrivateHelper.IsNegativeZero(_d) ^ PrivateHelper.IsNegativeZero(oFloat._d))
                return false;

            return EqualityComparer<double>.Default.Equals(oFloat.DoubleValue, DoubleValue);
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Float);
                return;
            }

            writer.WriteFloat(_d);
        }

        /// <summary>
        /// Get or set the value of this float as <see cref="System.Double"/>.
        /// </summary>
        public override double DoubleValue
        {
            get
            {
                ThrowIfNull();
                return _d;
            }
        }

        public override IonType Type() => IonType.Float;
    }
}
