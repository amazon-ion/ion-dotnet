using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace IonDotnet.Internals
{
    /// <summary>
    /// Base logic for (supposedly)  all Ion writers.
    /// </summary>
    internal abstract class PrivateIonWriterBase : IPrivateWriter
    {
        public bool IsStreamCopyOptimized => false;

        /// <inheritdoc />
        /// <summary>
        /// Default implementation of writing reader value.
        /// Can be overriden to optimize.
        /// </summary>
        public void WriteValue(IIonReader reader) => WriteValueRecursively(reader.CurrentType, reader);

        public void WriteValues(IIonReader reader)
        {
            //TODO possible optimization?
            if (reader.CurrentType == IonType.None)
            {
                reader.MoveNext();
            }

            while (reader.CurrentType != IonType.None)
            {
                WriteValue(reader);
                reader.MoveNext();
            }
        }

        private void WriteValueRecursively(IonType type, IIonReader reader)
        {
            TryWriteFieldName(reader);
            TryWriteAnnotations(reader);

            if (reader.CurrentIsNull)
            {
                WriteNull(type);
                return;
            }

            switch (type)
            {
                case IonType.Bool:
                    WriteBool(reader.BoolValue());
                    break;
                case IonType.Int:
                    switch (reader.GetIntegerSize())
                    {
                        case IntegerSize.Int:
                            WriteInt(reader.IntValue());
                            break;
                        case IntegerSize.Long:
                            WriteInt(reader.LongValue());
                            break;
                        case IntegerSize.BigInteger:
                            WriteInt(reader.BigIntegerValue());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case IonType.Float:
                    WriteFloat(reader.DoubleValue());
                    break;
                case IonType.Decimal:
                    WriteDecimal(reader.DecimalValue());
                    break;
                case IonType.Timestamp:
                    WriteTimestamp(reader.TimestampValue());
                    break;
                case IonType.Symbol:
                    WriteSymbol(reader.SymbolValue().Text);
                    break;
                case IonType.String:
                    WriteString(reader.StringValue());
                    break;
                case IonType.Clob:
                case IonType.Blob:
                    WriteClob(reader.NewByteArray());
                    break;
                case IonType.List:
                case IonType.Sexp:
                case IonType.Struct:
                    WriteContainerRecursively(type, reader);
                    break;
            }
        }

        private void WriteContainerRecursively(IonType type, IIonReader reader)
        {
            Debug.Assert(type.IsContainer());

            StepIn(type);
            reader.StepIn();
            while ((type = reader.MoveNext()) != IonType.Null)
            {
                WriteValueRecursively(type, reader);
            }

            reader.StepOut();
            StepOut();
        }

        private void TryWriteFieldName(IIonReader reader)
        {
            if (!IsInStruct || IsFieldNameSet()) return;

            var tok = reader.GetFieldNameSymbol();
            if (tok == default)
                throw new InvalidOperationException("Field name is not set");

            SetFieldNameSymbol(tok);
        }

        private void TryWriteAnnotations(IIonReader reader)
        {
//            SymbolToken[] a = reader.GetTypeAnnotationSymbols();
//            // At present, we must always call this, even when the list is empty,
//            // because local symtab diversion leaves the $ion_symbol_table
//            // dangling on the system writer! TODO fix that, it's broken.
//            SetTypeAnnotationSymbols(a);
        }

        public abstract void WriteNull();
        public abstract void WriteNull(IonType type);
        public abstract void WriteBool(bool value);
        public abstract void WriteInt(long value);
        public abstract void WriteInt(BigInteger value);
        public abstract void WriteFloat(double value);
        public abstract void WriteDecimal(decimal value);
        public abstract void WriteTimestamp(Timestamp value);
        public abstract void WriteSymbol(string symbol);
        public abstract void WriteSymbolToken(SymbolToken symbolToken);
        public abstract void WriteString(string value);
        public abstract void WriteBlob(ReadOnlySpan<byte> value);
        public abstract void WriteClob(ReadOnlySpan<byte> value);
        public abstract void SetTypeAnnotation(string annotation);
        public abstract void AddTypeAnnotation(string annotation);
        public abstract void Dispose();
        public abstract ISymbolTable SymbolTable { get; }
        public abstract void Flush();
        public abstract void Finish();
        public abstract Task FinishAsync();
        public abstract void SetFieldName(string name);
        public abstract void SetFieldNameSymbol(SymbolToken symbol);
        public abstract void StepIn(IonType type);
        public abstract void StepOut();
        public abstract bool IsInStruct { get; }
        public abstract void SetTypeAnnotationSymbols(IEnumerable<SymbolToken> annotations);
        public abstract bool IsFieldNameSet();
        public abstract int GetDepth();
        public abstract void WriteIonVersionMarker();
        public abstract Task FlushAsync();
    }
}
