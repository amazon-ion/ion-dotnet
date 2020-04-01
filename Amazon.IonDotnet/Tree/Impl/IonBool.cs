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

    /// <summary>
    /// Ion object holding a boolean value.
    /// </summary>
    internal sealed class IonBool : IonValue, IIonBool
    {
        public IonBool(bool value)
            : base(false)
        {
            this.BoolTrueFlagOn(value);
        }

        public override bool BoolValue
        {
            get
            {
                this.ThrowIfNull();
                return this.BoolTrueFlagOn();
            }
        }

        /// <summary>
        /// Returns a new null.bool value.
        /// </summary>
        /// <returns>A new null IonBool.</returns>
        public static IonBool NewNull()
        {
            var v = new IonBool(false);
            v.MakeNull();
            return v;
        }

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            var otherBool = (IonBool)other;

            if (this.NullFlagOn())
            {
                return otherBool.IsNull;
            }

            return !otherBool.IsNull && otherBool.BoolValue == this.BoolValue;
        }

        public override IonType Type() => IonType.Bool;

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (this.NullFlagOn())
            {
                writer.WriteNull(IonType.Bool);
            }
            else
            {
                writer.WriteBool(this.BoolTrueFlagOn());
            }
        }
    }
}
