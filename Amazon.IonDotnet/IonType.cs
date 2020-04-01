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

namespace Amazon.IonDotnet
{
    public enum IonType : short
    {
        None = -1,
        Null = 0,
        Bool = 1,
        Int = 2, // note that INT is actually 0x2 **and** 0x3 in the Ion binary encoding
        Float = 4,
        Decimal = 5,
        Timestamp = 6,
        Symbol = 7,
        String = 8,
        Clob = 9,
        Blob = 10,
        List = 11,
        Sexp = 12,
        Struct = 13,
        Datagram = 14,
    }

#pragma warning disable SA1649 // File name should match first type name
    public static class IonTypeExtensions
#pragma warning restore SA1649 // File name should match first type name
    {
        /// <summary>
        /// Determines whether a type represents an Ion container.
        /// </summary>
        /// <param name="t">IonType enum.</param>
        /// <returns>true when t is enum after List.</returns>
        public static bool IsContainer(this IonType t) => t >= IonType.List;

        /// <summary>
        /// Determines whether a type represents an Ion text scalar.
        /// </summary>
        /// <param name="t">IonType enum.</param>
        /// <returns>true when t is String or Symbol.</returns>
        public static bool IsText(this IonType t) => t == IonType.String || t == IonType.Symbol;

        /// <summary>
        /// Determines whether a type represents an Ion LOB.
        /// </summary>
        /// <param name="t">IonType enum.</param>
        /// <returns>true when t is Blob or Clob.</returns>
        public static bool IsLob(this IonType t) => t == IonType.Blob || t == IonType.Clob;

        /// <summary>
        /// Determines whether a type represents a scalar value type.
        /// </summary>
        /// <param name="t">IonType enum.</param>
        /// <returns>true when the this is a scalar type.</returns>
        public static bool IsScalar(this IonType t) => t > IonType.None && t < IonType.List;

        /// <summary>
        /// Determines whether a type represents a numeric type.
        /// </summary>
        /// <param name="t">IonType enum.</param>
        /// <returns>true when this type is numeric.</returns>
        public static bool IsNumeric(this IonType t) => t == IonType.Int || t == IonType.Float || t == IonType.Decimal;
    }
}
