using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using IonDotnet.Systems;
using IonDotnet.Tree;

namespace IonDotnet.Internals
{
    internal class IonTreeWriter : IonSystemWriter
    {
        private IonContainer _currentContainer;

        public IonTreeWriter(IonContainer root) : base(IonWriterBuilderBase.InitialIvmHandlingOption.Suppress)
        {
            _currentContainer = root;
        }

        public override void WriteNull()
        {
            throw new NotImplementedException();
        }

        public override void WriteNull(IonType type)
        {
            throw new NotImplementedException();
        }

        public override void WriteBool(bool value)
        {
            throw new NotImplementedException();
        }

        public override void WriteInt(long value)
        {
            throw new NotImplementedException();
        }

        public override void WriteInt(BigInteger value)
        {
            throw new NotImplementedException();
        }

        public override void WriteFloat(double value)
        {
            throw new NotImplementedException();
        }

        public override void WriteDecimal(decimal value)
        {
            throw new NotImplementedException();
        }

        public override void WriteTimestamp(Timestamp value)
        {
            throw new NotImplementedException();
        }

        public override void WriteString(string value)
        {
            throw new NotImplementedException();
        }

        public override void WriteBlob(ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }

        public override void WriteClob(ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            //nothing to do here
        }

        public override Task FlushAsync()
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void Finish()
        {
            throw new NotImplementedException();
        }

        public override Task FinishAsync()
        {
            throw new NotImplementedException();
        }

        public override void StepIn(IonType type)
        {
            throw new NotImplementedException();
        }

        public override void StepOut()
        {
            throw new NotImplementedException();
        }

        public override bool IsInStruct => _currentContainer.Type == IonType.Struct;

        public override int GetDepth()
        {
            throw new NotImplementedException();
        }

        protected override void WriteSymbolString(SymbolToken value)
        {
            var symbol = new IonSymbol(value);
            AppendValue(symbol);
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
                if (field.Text is null)
                    throw new InvalidOperationException("Field name is missing");

                var structContainer = (_currentContainer as IonStruct);
                Debug.Assert(structContainer != null);
                structContainer[field.Text] = value;
            }
            else
            {
                _currentContainer.Add(value);
            }
        }
    }
}
