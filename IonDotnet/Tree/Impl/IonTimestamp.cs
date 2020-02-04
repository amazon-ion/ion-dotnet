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

using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    internal sealed class IonTimestamp : IonValue, IIonTimestamp
    {
        private Timestamp _timestamp;

        public IonTimestamp(Timestamp val) : base(false)
        {
            _timestamp = val;
        }

        private IonTimestamp(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.timestamp value.
        /// </summary>
        public static IonTimestamp NewNull() => new IonTimestamp(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;
            
            if (!(other is IonTimestamp oTimestamp))
                return false;
            if (NullFlagOn())
                return other.IsNull;

            return !other.IsNull && _timestamp == oTimestamp._timestamp;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Timestamp);
                return;
            }

            writer.WriteTimestamp(_timestamp);
        }

        public override Timestamp TimestampValue
        {
            get
            {
                ThrowIfNull();
                return _timestamp;
            }
        }

        public override IonType Type() => IonType.Timestamp;
    }
}
