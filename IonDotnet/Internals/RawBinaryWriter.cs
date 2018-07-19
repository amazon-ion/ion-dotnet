using System;
using System.Numerics;

namespace IonDotnet.Internals
{
    internal class RawBinaryWriter : IIonWriter
    {

        public ISymbolTable SymbolTable { get; }
        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Finish()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void SetFieldName(string name)
        {
            throw new NotImplementedException();
        }

        public void SetFieldNameSymbol(SymbolToken name)
        {
            throw new NotImplementedException();
        }

        public void StepIn(IonType type)
        {
            throw new NotImplementedException();
        }

        public void StepOut()
        {
            throw new NotImplementedException();
        }

        public bool IsInStruct { get; }
        public void WriteValue(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteValues(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteNull()
        {
            throw new NotImplementedException();
        }

        public void WriteNull(IonType type)
        {
            throw new NotImplementedException();
        }

        public void WriteBool(bool value)
        {
            throw new NotImplementedException();
        }

        public void WriteInt(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteInt(BigInteger value)
        {
            throw new NotImplementedException();
        }

        public void WriteFloat(double value)
        {
            throw new NotImplementedException();
        }

        public void WriteDecimal(decimal value)
        {
            throw new NotImplementedException();
        }

        public void WriteTimestamp(DateTime value)
        {
            throw new NotImplementedException();
        }

        public void WriteSymbol(SymbolToken symbolToken)
        {
            throw new NotImplementedException();
        }

        public void WriteString(string value)
        {
            throw new NotImplementedException();
        }

        public void WriteBlob(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void WriteBlob(ArraySegment<byte> value)
        {
            throw new NotImplementedException();
        }

        public void WriteClob(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void WriteClob(ArraySegment<byte> value)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotations(params string[] annotations)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotationSymbols(ArraySegment<SymbolToken> annotations)
        {
            throw new NotImplementedException();
        }

        public void AddTypeAnnotation(string annotation)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
