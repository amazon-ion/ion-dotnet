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

namespace IonDotnet.Tree
{
    public interface IIonValue : IIonNull, IIonBool, IIonInt, IIonFloat,
        IIonDecimal, IIonTimestamp, IIonSymbol, IIonString, IIonClob,
        IIonBlob, IIonList, IIonSexp, IIonStruct, IIonDatagram
    {
        SymbolToken FieldNameSymbol { get; set; }
        bool IsNull { get; }
        bool IsReadOnly { get; }
        void AddTypeAnnotation(string annotation);
        void AddTypeAnnotation(SymbolToken annotation);
        void ClearAnnotations();
        IReadOnlyCollection<SymbolToken> GetTypeAnnotations();
        bool HasAnnotation(string text);
        bool IsEquivalentTo(IIonValue value);
        void MakeNull();
        void MakeReadOnly();
        string ToPrettyString();
        IonType Type();
        void WriteTo(IIonWriter writer);
    }
}
