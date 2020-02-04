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
    /// <inheritdoc />
    /// <summary>
    /// An Ion string value.
    /// </summary>
    internal sealed class IonString : IonText, IIonString
    {
        public IonString(string value) : base(value, value is null)
        {
        }

        /// <summary>
        /// Returns a new null.string value.
        /// </summary>
        public static IonString NewNull() => new IonString(null);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            if (!(other is IonString otherString))
                return false;
            return StringVal == otherString.StringVal;
        }

        internal override void WriteBodyTo(IPrivateWriter writer) => writer.WriteString(StringVal);

        public override IonType Type() => IonType.String;
    }
}
