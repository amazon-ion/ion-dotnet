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
    /// An Ion S-exp value.
    /// </summary>
    internal sealed class IonSexp : IonSequence, IIonSexp
    {
        public IonSexp()
            : this(false)
        {
        }

        private IonSexp(bool isNull)
            : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.list value.
        /// </summary>
        /// <returns>A null IonSexp.</returns>
        public static IonSexp NewNull() => new IonSexp(true);

        public override IonType Type() => IonType.Sexp;
    }
}
