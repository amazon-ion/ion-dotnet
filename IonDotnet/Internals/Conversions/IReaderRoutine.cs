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

namespace IonDotnet.Internals.Conversions
{
    /// <summary>
    /// List of C# types that the ion value translates to
    /// </summary>
    [Flags]
    internal enum ScalarType
    {
        Nothing = 0,
        Null = 1 << 0,
        Bool = 1 << 1,
        Int = 1 << 2,
        Long = 1 << 3,
        BigInteger = 1 << 4,
        Decimal = 1 << 5,
        Double = 1 << 6,
        String = 1 << 7,
        Timestamp = 1 << 8
    }
}
