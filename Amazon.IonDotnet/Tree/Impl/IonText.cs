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
    /// <inheritdoc />
    /// <summary>
    /// Represent Ion textual values.
    /// </summary>
    internal abstract class IonText : IonValue, IIonText
    {
        protected string stringVal;

        protected IonText(string text, bool isNull)
            : base(isNull)
        {
            this.stringVal = text;
        }

        /// <summary>
        /// Gets the textual value as string.
        /// </summary>
        public override string StringValue
        {
            get => this.stringVal;
        }

        public override void MakeNull()
        {
            base.MakeNull();
            this.stringVal = null;
        }
    }
}
