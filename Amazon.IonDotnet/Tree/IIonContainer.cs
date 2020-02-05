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

using System.Collections.Generic;

namespace Amazon.IonDotnet.Tree
{
    public interface IIonContainer
    {
        int Count { get; }
        void Add(IIonValue item);
        void Clear();
        bool Contains(IIonValue item);
        void CopyTo(IIonValue[] array, int arrayIndex);
        IEnumerator<IIonValue> GetEnumerator();
        bool Remove(IIonValue item);
    }
}
