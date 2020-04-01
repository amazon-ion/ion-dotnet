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

    internal sealed class IonTimestamp : IonValue, IIonTimestamp
    {
        private readonly Timestamp timestamp;

        public IonTimestamp(Timestamp val)
            : base(false)
        {
            this.timestamp = val;
        }

        private IonTimestamp(bool isNull)
            : base(isNull)
        {
        }

        public override Timestamp TimestampValue
        {
            get
            {
                this.ThrowIfNull();
                return this.timestamp;
            }
        }

        /// <summary>
        /// Returns a new null.timestamp value.
        /// </summary>
        /// <returns>A null IonTimestamp.</returns>
        public static IonTimestamp NewNull() => new IonTimestamp(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            if (!(other is IonTimestamp oTimestamp))
            {
                return false;
            }

            if (this.NullFlagOn())
            {
                return other.IsNull;
            }

            return !other.IsNull && this.timestamp == oTimestamp.timestamp;
        }

        public override IonType Type() => IonType.Timestamp;

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (this.NullFlagOn())
            {
                writer.WriteNull(IonType.Timestamp);
                return;
            }

            writer.WriteTimestamp(this.timestamp);
        }
    }
}
