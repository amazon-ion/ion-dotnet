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
    using Amazon.IonDotnet.Tree;
    using Amazon.IonDotnet.Tree.Impl;

    internal class IonTreeWriter : IonSystemWriter
    {
        private readonly Stack<IIonContainer> containers = new Stack<IIonContainer>();
        private IIonContainer currentContainer;

        public IonTreeWriter(IonContainer root)
        {
            Debug.Assert(root != null, "root is null");
            this.currentContainer = root;
            this.containers.Push(root);
        }

        public override bool IsInStruct => ((IIonValue)this.currentContainer).Type() == IonType.Struct;

        public override void WriteNull()
        {
            var v = new IonNull();
            this.AppendValue(v);
        }

        public override void WriteNull(IonType type)
        {
            IonValue v;

            switch (type)
            {
                case IonType.Null:
                    v = new IonNull();
                    break;
                case IonType.Bool:
                    v = IonBool.NewNull();
                    break;
                case IonType.Int:
                    v = IonInt.NewNull();
                    break;
                case IonType.Float:
                    v = IonFloat.NewNull();
                    break;
                case IonType.Decimal:
                    v = IonDecimal.NewNull();
                    break;
                case IonType.Timestamp:
                    v = IonTimestamp.NewNull();
                    break;
                case IonType.Symbol:
                    v = IonSymbol.NewNull();
                    break;
                case IonType.String:
                    v = new IonString(null);
                    break;
                case IonType.Clob:
                    v = IonClob.NewNull();
                    break;
                case IonType.Blob:
                    v = IonBlob.NewNull();
                    break;
                case IonType.List:
                    v = IonList.NewNull();
                    break;
                case IonType.Sexp:
                    v = IonSexp.NewNull();
                    break;
                case IonType.Struct:
                    v = IonStruct.NewNull();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            this.AppendValue(v);
        }

        public override void WriteBool(bool value)
        {
            var v = new IonBool(value);
            this.AppendValue(v);
        }

        public override void WriteInt(long value)
        {
            var v = new IonInt(value);
            this.AppendValue(v);
        }

        public override void WriteInt(BigInteger value)
        {
            var v = new IonInt(value);
            this.AppendValue(v);
        }

        public override void WriteFloat(double value)
        {
            var v = new IonFloat(value);
            this.AppendValue(v);
        }

        public override void WriteDecimal(decimal value)
        {
            var v = new IonDecimal(value);
            this.AppendValue(v);
        }

        public override void WriteDecimal(BigDecimal value)
        {
            var v = new IonDecimal(value);
            this.AppendValue(v);
        }

        public override void WriteTimestamp(Timestamp value)
        {
            var v = new IonTimestamp(value);
            this.AppendValue(v);
        }

        public override void WriteString(string value)
        {
            var v = new IonString(value);
            this.AppendValue(v);
        }

        public override void WriteBlob(ReadOnlySpan<byte> value)
        {
            this.AppendValue(new IonBlob(value));
        }

        public override void WriteClob(ReadOnlySpan<byte> value)
        {
            this.AppendValue(new IonClob(value));
        }

        public override void Dispose()
        {
            // nothing to do here
        }

        public override void Flush()
        {
            // nothing to do here
        }

        public override void Finish()
        {
            // nothing to do here
        }

        public override void StepIn(IonType type)
        {
            IonContainer c;
            switch (type)
            {
                case IonType.List:
                    c = new IonList();
                    break;
                case IonType.Sexp:
                    c = new IonSexp();
                    break;
                case IonType.Struct:
                    c = new IonStruct();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            this.containers.Push(c);
            this.AppendValue(c);
            this.currentContainer = c;
        }

        public override void StepOut()
        {
            if (this.containers.Count > 0)
            {
                this.containers.Pop();
                this.currentContainer = this.containers.Peek();
            }
            else
            {
                throw new InvalidOperationException("Cannot step out of top level value");
            }
        }

        public override int GetDepth()
        {
            return this.containers.Count;
        }

        protected override void WriteSymbolAsIs(SymbolToken symbolToken)
        {
            this.AppendValue(new IonSymbol(symbolToken));
        }

        protected override void WriteIonVersionMarker(ISymbolTable systemSymtab)
        {
            // do nothing
        }

        /// <summary>
        /// Append an Ion value to this datagram.
        /// </summary>
        private void AppendValue(IonValue value)
        {
            if (this.annotations.Count > 0)
            {
                value.ClearAnnotations();
                foreach (var annotation in this.annotations)
                {
                    value.AddTypeAnnotation(annotation);
                }

                this.annotations.Clear();
            }

            if (this.IsInStruct)
            {
                var field = this.AssumeFieldNameSymbol();
                this.ClearFieldName();
                if (field == default)
                {
                    throw new InvalidOperationException("Field name is missing");
                }

                var structContainer = this.currentContainer as IonStruct;
                Debug.Assert(structContainer != null, "structContainer is null");
                structContainer.Add(field, value);
            }
            else
            {
                this.currentContainer.Add(value);
            }
        }
    }
}
