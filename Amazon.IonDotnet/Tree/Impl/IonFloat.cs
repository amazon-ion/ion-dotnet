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
    using System;
    using System.Collections.Generic;
    using Amazon.IonDotnet.Internals;
    using Amazon.IonDotnet.Utils;

    /// <inheritdoc />
    /// <summary>
    /// Ion value representing a floating point number.
    /// </summary>
    internal sealed class IonFloat : IonValue, IIonFloat
    {
        private readonly double d;

        public IonFloat(double value)
            : base(false)
        {
            this.d = value;
        }

        private IonFloat(bool isNull)
            : base(isNull)
        {
        }

        /// <summary>
        /// Gets the value of this float as <see cref="double"/>.
        /// </summary>
        public override double DoubleValue
        {
            get
            {
                this.ThrowIfNull();
                return this.d;
            }
        }

        /// <summary>
        /// Returns a new null.float value.
        /// </summary>
        /// <returns>A null IonFloat.</returns>
        public static IonFloat NewNull() => new IonFloat(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            var oFloat = (IonFloat)other;

            if (this.NullFlagOn())
            {
                return oFloat.IsNull;
            }

            if (oFloat.IsNull)
            {
                return false;
            }

            if (PrivateHelper.IsNegativeZero(this.d) ^ PrivateHelper.IsNegativeZero(oFloat.d))
            {
                return false;
            }

            return EqualityComparer<double>.Default.Equals(oFloat.DoubleValue, this.DoubleValue);
        }

        public override IonType Type() => IonType.Float;

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (this.NullFlagOn())
            {
                writer.WriteNull(IonType.Float);
                return;
            }

            writer.WriteFloat(this.d);
        }
    }
}
