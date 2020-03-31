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

namespace Amazon.IonDotnet.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;

    /// <summary>
    /// Base logic for Ion writers.
    /// </summary>
    internal abstract class PrivateIonWriterBase : IPrivateWriter
    {
        public bool IsStreamCopyOptimized => false;

        public abstract ISymbolTable SymbolTable { get; }

        public abstract bool IsInStruct { get; }

        /// <inheritdoc />
        /// <summary>
        /// Default implementation of writing reader value.
        /// Can be overriden to optimize.
        /// </summary>
        public void WriteValue(IIonReader reader) => this.WriteValueRecursively(reader.CurrentType, reader);

        public void WriteValues(IIonReader reader)
        {
            if (reader.CurrentType == IonType.None)
            {
                reader.MoveNext();
            }

            while (reader.CurrentType != IonType.None)
            {
                this.WriteValue(reader);
                reader.MoveNext();
            }
        }

        public abstract void WriteNull();

        public abstract void WriteNull(IonType type);

        public abstract void WriteBool(bool value);

        public abstract void WriteInt(long value);

        public abstract void WriteInt(BigInteger value);

        public abstract void WriteFloat(double value);

        public abstract void WriteDecimal(decimal value);

        public abstract void WriteDecimal(BigDecimal value);

        public abstract void WriteTimestamp(Timestamp value);

        public abstract void WriteSymbol(string symbol);

        public abstract void WriteSymbolToken(SymbolToken symbolToken);

        public abstract void WriteString(string value);

        public abstract void WriteBlob(ReadOnlySpan<byte> value);

        public abstract void WriteClob(ReadOnlySpan<byte> value);

        public abstract void AddTypeAnnotation(string annotation);

        public abstract void AddTypeAnnotationSymbol(SymbolToken symbolToken);

        public abstract void ClearTypeAnnotations();

        public abstract void Dispose();

        public abstract void Flush();

        public abstract void Finish();

        public abstract void SetFieldName(string name);

        public abstract void SetFieldNameSymbol(SymbolToken symbol);

        public abstract void StepIn(IonType type);

        public abstract void StepOut();

        public abstract void SetTypeAnnotations(IEnumerable<string> annotations);

        public abstract bool IsFieldNameSet();

        public abstract int GetDepth();

        public abstract void WriteIonVersionMarker();

        private void WriteValueRecursively(IonType type, IIonReader reader)
        {
            this.TryWriteFieldName(reader);
            this.TryWriteAnnotationSymbols(reader);

            if (reader.CurrentIsNull)
            {
                this.WriteNull(type);
                return;
            }

            switch (type)
            {
                case IonType.Bool:
                    this.WriteBool(reader.BoolValue());
                    break;
                case IonType.Int:
                    switch (reader.GetIntegerSize())
                    {
                        case IntegerSize.Int:
                            this.WriteInt(reader.IntValue());
                            break;
                        case IntegerSize.Long:
                            this.WriteInt(reader.LongValue());
                            break;
                        case IntegerSize.BigInteger:
                            this.WriteInt(reader.BigIntegerValue());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case IonType.Float:
                    this.WriteFloat(reader.DoubleValue());
                    break;
                case IonType.Decimal:
                    this.WriteDecimal(reader.DecimalValue());
                    break;
                case IonType.Timestamp:
                    this.WriteTimestamp(reader.TimestampValue());
                    break;
                case IonType.Symbol:
                    this.WriteSymbolToken(reader.SymbolValue());
                    break;
                case IonType.String:
                    this.WriteString(reader.StringValue());
                    break;
                case IonType.Clob:
                    this.WriteClob(reader.NewByteArray());
                    break;
                case IonType.Blob:
                    this.WriteBlob(reader.NewByteArray());
                    break;
                case IonType.List:
                case IonType.Sexp:
                case IonType.Struct:
                    this.WriteContainerRecursively(type, reader);
                    break;
            }
        }

        private void WriteContainerRecursively(IonType type, IIonReader reader)
        {
            Debug.Assert(type.IsContainer(), "type IsContainer is false");

            this.StepIn(type);
            reader.StepIn();
            while ((type = reader.MoveNext()) != IonType.None)
            {
                this.WriteValueRecursively(type, reader);
            }

            reader.StepOut();
            this.StepOut();
        }

        private void TryWriteFieldName(IIonReader reader)
        {
            if (!this.IsInStruct || this.IsFieldNameSet())
            {
                return;
            }

            var tok = reader.GetFieldNameSymbol();
            if (tok == default)
            {
                throw new InvalidOperationException("Field name is not set");
            }

            this.SetFieldNameSymbol(tok);
        }

        private void TryWriteAnnotationSymbols(IIonReader reader)
        {
            var annots = reader.GetTypeAnnotationSymbols();
            foreach (var a in annots)
            {
                this.AddTypeAnnotationSymbol(a);
            }
        }
    }
}
