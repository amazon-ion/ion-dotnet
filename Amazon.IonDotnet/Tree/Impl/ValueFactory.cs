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
using System.Numerics;

namespace Amazon.IonDotnet.Tree.Impl
{
    public class ValueFactory : IValueFactory
    {
        public ValueFactory()
        {
        }

        public IIonValue NewNullBlob()
        {
            return IonBlob.NewNull();
        }

        public IIonValue NewBlob(ReadOnlySpan<byte> bytes)
        {
            return new IonBlob(bytes);
        }

        public IIonValue NewNullBool()
        {
            return IonBool.NewNull();
        }

        public IIonValue NewBool(bool value)
        {
            return new IonBool(value);
        }

        public IIonValue NewNullClob()
        {
            return IonClob.NewNull();
        }

        public IIonValue NewClob(ReadOnlySpan<byte> bytes)
        {
            return new IonClob(bytes);
        }

        public IIonValue NewNullDecimal()
        {
            return IonDecimal.NewNull();
        }

        public IIonValue NewDecimal(double doubleValue)
        {
            return new IonDecimal(doubleValue);
        }

        public IIonValue NewDecimal(decimal value)
        {
            return new IonDecimal(value);
        }

        public IIonValue NewDecimal(BigDecimal bigDecimal)
        {
            return new IonDecimal(bigDecimal);
        }

        public IIonValue NewNullFloat()
        {
            return IonFloat.NewNull();
        }

        public IIonValue NewFloat(double value)
        {
            return new IonFloat(value);
        }

        public IIonValue NewNullInt()
        {
            return IonInt.NewNull();
        }

        public IIonValue NewInt(long value)
        {
            return new IonInt(value);
        }

        public IIonValue NewInt(BigInteger value)
        {
            return new IonInt(value);
        }

        public IIonValue NewNullList()
        {
            return IonList.NewNull();
        }

        public IIonValue NewEmptyList()
        {
            return new IonList();
        }

        public IIonValue NewNull()
        {
            return new IonNull();
        }

        public IIonValue NewNullSexp()
        {
            return IonSexp.NewNull();
        }

        public IIonValue NewEmptySexp()
        {
            return new IonSexp();
        }

        public IIonValue NewNullString()
        {
            return IonString.NewNull();
        }

        public IIonValue NewString(string value)
        {
            return new IonString(value);
        }

        public IIonValue NewNullStruct()
        {
            return IonStruct.NewNull();
        }

        public IIonValue NewEmptyStruct()
        {
            return new IonStruct();
        }

        public IIonValue NewNullSymbol()
        {
            return IonSymbol.NewNull();
        }

        public IIonValue NewSymbol(SymbolToken symbolToken)
        {
            return new IonSymbol(symbolToken);
        }

        public IIonValue NewSymbol(string text)
        {
            return new IonSymbol(text);
        }

        public IIonValue NewNullTimestamp()
        {
            return IonTimestamp.NewNull();
        }

        public IIonValue NewTimestamp(Timestamp val)
        {
            return new IonTimestamp(val);
        }
    }
}
