using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using IonDotnet.Systems;
using IonDotnet.Tree.Impl;

namespace IonDotnet.Internals
{
    internal class IonTreeWriter : IonSystemWriter
    {
        private IonContainer _currentContainer;

        public IonTreeWriter(IonContainer root)
        {
            Debug.Assert(root != null);
            _currentContainer = root;
        }

        public override void WriteNull()
        {
            var v = new IonNull();
            AppendValue(v);
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

            AppendValue(v);
        }

        public override void WriteBool(bool value)
        {
            var v = new IonBool(value);
            AppendValue(v);
        }

        public override void WriteInt(long value)
        {
            var v = new IonInt(value);
            AppendValue(v);
        }

        public override void WriteInt(BigInteger value)
        {
            var v = new IonInt(value);
            AppendValue(v);
        }

        public override void WriteFloat(double value)
        {
            var v = new IonFloat(value);
            AppendValue(v);
        }

        public override void WriteDecimal(decimal value)
        {
            var v = new IonDecimal(value);
            AppendValue(v);
        }

        public override void WriteDecimal(BigDecimal value)
        {
            var v = new IonDecimal(value);
            AppendValue(v);
        }

        public override void WriteTimestamp(Timestamp value)
        {
            var v = new IonTimestamp(value);
            AppendValue(v);
        }

        public override void WriteString(string value)
        {
            var v = new IonString(value);
            AppendValue(v);
        }

        public override void WriteBlob(ReadOnlySpan<byte> value)
        {
            AppendValue(new IonBlob(value));
        }

        public override void WriteClob(ReadOnlySpan<byte> value)
        {
            AppendValue(new IonClob(value));
        }

        public override void Dispose()
        {
            //nothing to do here
        }


        public override void Flush()
        {
            //nothing to do here
        }

        public override void Finish()
        {
            //nothing to do here
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

            AppendValue(c);
            _currentContainer = c;
        }

        public override void StepOut()
        {
            if (_currentContainer.Container is null)
                throw new InvalidOperationException("Cannot step out of top level value");
            _currentContainer = _currentContainer.Container;
        }

        public override bool IsInStruct => _currentContainer.Type == IonType.Struct;

        public override int GetDepth()
        {
            var count = 1;
            var container = _currentContainer;
            while (container.Container != null)
            {
                container = container.Container;
                count++;
            }

            if (container is IonDatagram)
            {
                //top-level datagram doesn't count
                count--;
            }

            return count;
        }

        protected override void WriteSymbolAsIs(SymbolToken symbolToken)
        {
            AppendValue(new IonSymbol(symbolToken));
        }

        protected override void WriteIonVersionMarker(ISymbolTable systemSymtab)
        {
            //do nothing
        }

        /// <summary>
        /// Append an Ion value to this datagram.
        /// </summary>
        private void AppendValue(IonValue value)
        {
            if (_annotations.Count > 0)
            {
                value.ClearAnnotations();
                foreach (var annotation in _annotations)
                {
                    value.AddTypeAnnotation(annotation);
                }

                _annotations.Clear();
            }

            if (IsInStruct)
            {
                var field = AssumeFieldNameSymbol();
                ClearFieldName();
                if (field == default)
                    throw new InvalidOperationException("Field name is missing");

                var structContainer = _currentContainer as IonStruct;
                Debug.Assert(structContainer != null);
                structContainer.Add(field, value);
            }
            else
            {
                _currentContainer.Add(value);
            }

            Debug.Assert(value.Container == _currentContainer);
        }
    }
}
