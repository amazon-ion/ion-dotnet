using System;
using System.Numerics;
using IonDotnet.Conversions;

namespace IonDotnet.Internals.Text
{
    public class RawTextReader : IIonReader
    {
        #region States

        private const int StateBeforeAnnotationDatagram = 0;
        private const int StateBeforeAnnotationContained = 1;
        private const int StateBeforeAnnotationSexp = 2;
        private const int StateBeforeFieldName = 3;
        private const int StateBeforeValueContent = 4;
        private const int StateBeforeValueContentSexp = 5;
        private const int StateInLongString = 6;
        private const int StateInClobDoubleQuotedContent = 7;
        private const int StateInClobTripleQuotedContent = 8;
        private const int StateInBlobContent = 9;
        private const int StateAfterValueContents = 10;
        private const int StateEof = 11;
        private const int StateMax = 11;

        #endregion

        #region Actions

        private const int ActionNotDefined = 0;
        private const int ActionLoadFieldName = 1;
        private const int ActionLoadAnnotation = 2;
        private const int ActionStartStruct = 3;
        private const int ActionStartList = 4;
        private const int ActionStartSexp = 5;
        private const int ActionStartLob = 6;
        private const int ActionLoadScalar = 8;
        private const int ActionPlusInf = 9;
        private const int ActionMinusInf = 10;
        private const int ActionEatComma = 11; // if this is unnecessary (because load_scalar handle it) we don't need "after_value"
        private const int ActionFinishContainer = 12;
        private const int ActionFinishLob = 13;
        private const int ActionFinishDatagram = 14;
        private const int ActionEof = 15;

        private static readonly int[,] TransitionActions = MakeTransitionActionArray();

        private static int[,] MakeTransitionActionArray()
        {
            var actions = new int[StateMax + 1, TextConstants.TokenMax + 1];

            actions[StateBeforeAnnotationDatagram, TextConstants.TokenEof] = ActionFinishDatagram;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenUnknownNumeric] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenInt] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenBinary] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenHex] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenDecimal] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenFloat] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenFloatInf] = ActionPlusInf;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenFloatMinusInf] = ActionMinusInf;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenTimestamp] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenStringDoubleQuote] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenStringTripleQuote] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenSymbolIdentifier] = ActionLoadAnnotation;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenSymbolQuoted] = ActionLoadAnnotation;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenParen] = ActionStartSexp;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenBrace] = ActionStartStruct;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenSquare] = ActionStartList;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenDoubleBrace] = ActionStartLob;

            // both before_annotation and after_annotation are essentially the same as
            // BOF (after_annotation can't accept EOF as valid however)
            for (int i = 0, tc = TextConstants.TokenMax + 1; i < tc; i++)
            {
                actions[StateBeforeAnnotationContained, i] = actions[StateBeforeAnnotationDatagram, i];
                actions[StateBeforeAnnotationSexp, i] = actions[StateBeforeAnnotationDatagram, i];
                actions[StateBeforeValueContent, i] = actions[StateBeforeAnnotationDatagram, i];
                actions[StateBeforeValueContentSexp, i] = actions[StateBeforeAnnotationDatagram, i];
            }

            // now patch up the differences between these 4 states handling of tokens vs before_annotation_datagram
            actions[StateBeforeAnnotationContained, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeAnnotationContained, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateBeforeAnnotationContained, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateBeforeAnnotationContained, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            actions[StateBeforeAnnotationSexp, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenSymbolOperator] = ActionLoadScalar;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenDot] = ActionLoadScalar;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            actions[StateBeforeValueContent, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeValueContent, TextConstants.TokenSymbolIdentifier] = ActionLoadScalar;
            actions[StateBeforeValueContent, TextConstants.TokenSymbolQuoted] = ActionLoadScalar;

            actions[StateBeforeValueContentSexp, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeValueContentSexp, TextConstants.TokenSymbolIdentifier] = ActionLoadScalar;
            actions[StateBeforeValueContentSexp, TextConstants.TokenSymbolQuoted] = ActionLoadScalar;
            actions[StateBeforeValueContentSexp, TextConstants.TokenSymbolOperator] = ActionLoadScalar;

            actions[StateBeforeFieldName, TextConstants.TokenEof] = 0;
            actions[StateBeforeFieldName, TextConstants.TokenSymbolIdentifier] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenSymbolQuoted] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenStringDoubleQuote] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenStringTripleQuote] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateBeforeFieldName, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateBeforeFieldName, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            // after a value we'll either see a separator (like ',') or a containers closing token. If we're not in a container
            // (i.e. we're at the top level) then this isn't the state we should be in.  We'll be in StateBeforeAnnotationDatagram
            actions[StateAfterValueContents, TextConstants.TokenComma] = ActionEatComma;
            actions[StateAfterValueContents, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateAfterValueContents, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateAfterValueContents, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            // the three "in_<lob>" value states have to be handled
            // specially, they can only scan forward to the end of
            // the content on next, or read content for the user otherwise
            actions[StateInClobDoubleQuotedContent, TextConstants.TokenCloseBrace] = ActionFinishLob;
            actions[StateInClobTripleQuotedContent, TextConstants.TokenCloseBrace] = ActionFinishLob;
            actions[StateInBlobContent, TextConstants.TokenCloseBrace] = ActionFinishLob;

            // the eof action exists because finishing an unread value can place the scanner just before
            // the input stream eof and set the current state to eof - in which case we just need to return eof
            for (int i = 0, tc = TextConstants.TokenMax + 1; i < tc; i++)
            {
                actions[StateEof, i] = ActionEof;
            }

            return actions;
        }

        #endregion

        public IonType MoveNext()
        {
            throw new NotImplementedException();
        }

        public void StepIn()
        {
            throw new NotImplementedException();
        }

        public void StepOut()
        {
            throw new NotImplementedException();
        }

        public int CurrentDepth { get; }

        public ISymbolTable GetSymbolTable()
        {
            throw new NotImplementedException();
        }

        public IonType CurrentType { get; }

        public IntegerSize GetIntegerSize()
        {
            throw new NotImplementedException();
        }

        public string CurrentFieldName { get; }

        public SymbolToken GetFieldNameSymbol()
        {
            throw new NotImplementedException();
        }

        public bool CurrentIsNull { get; }
        public bool IsInStruct { get; }

        public bool BoolValue()
        {
            throw new NotImplementedException();
        }

        public int IntValue()
        {
            throw new NotImplementedException();
        }

        public long LongValue()
        {
            throw new NotImplementedException();
        }

        public BigInteger BigIntegerValue()
        {
            throw new NotImplementedException();
        }

        public double DoubleValue()
        {
            throw new NotImplementedException();
        }

        public decimal DecimalValue()
        {
            throw new NotImplementedException();
        }

        public Timestamp TimestampValue()
        {
            throw new NotImplementedException();
        }

        public string StringValue()
        {
            throw new NotImplementedException();
        }

        public SymbolToken SymbolValue()
        {
            throw new NotImplementedException();
        }

        public int GetLobByteSize()
        {
            throw new NotImplementedException();
        }

        public byte[] NewByteArray()
        {
            throw new NotImplementedException();
        }

        public int GetBytes(ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertTo(Type targetType, IScalarConverter scalarConverter, out object result)
        {
            throw new NotImplementedException();
        }
    }
}
