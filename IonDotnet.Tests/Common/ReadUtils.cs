using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using IonDotnet.Internals.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Common
{
    public static class ReadUtils
    {
        public static bool NextIsEmptyStruct(this IIonReader reader)
        {
            if (reader.MoveNext() != IonType.Struct) return false;
            var empty = true;
            reader.StepIn();
            if (reader.MoveNext() != null)
            {
                empty = false;
            }

            reader.StepOut();
            return empty;
        }

        public static void AssertFlatStruct(IIonReader reader, IEnumerable<(string key, object value)> kvps)
        {
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            reader.StepIn();

            foreach (var kvp in kvps)
            {
                reader.MoveNext();
                Assert.AreEqual(kvp.key, reader.CurrentFieldName);
                Assert.IsTrue(IsIonTypeCompatible(reader.CurrentType, kvp.value.GetType()));
                Assert.AreEqual(kvp.value, reader.MapIonValueToDotnet());
            }

            reader.StepOut();
        }

        private static bool IsIonTypeCompatible(IonType ionType, Type dotnetType)
        {
            if (!ionType.IsScalar()) return false;

            switch (ionType)
            {
                default:
                    return false;
                case IonType w when w.Id == IonType.Bool.Id:
                    return dotnetType == typeof(bool);
                case IonType w when w.Id == IonType.Int.Id:
                    return dotnetType == typeof(int) || dotnetType == typeof(long) || dotnetType == typeof(BigInteger);
                case IonType w when w.Id == IonType.Float.Id:
                    return dotnetType == typeof(float) || dotnetType == typeof(double);
                case IonType w when w.Id == IonType.Decimal.Id:
                    return dotnetType == typeof(decimal);
                case IonType w when w.Id == IonType.String.Id:
                    return dotnetType == typeof(string);
                case IonType w when w.Id == IonType.Timestamp.Id:
                    return dotnetType == typeof(DateTime) || dotnetType == typeof(DateTimeOffset) || dotnetType == typeof(Timestamp);
            }
        }

        private static object MapIonValueToDotnet(this IIonReader reader)
        {
            switch (reader.CurrentType)
            {
                default:
                    return null;
                case IonType w when w.Id == IonType.Bool.Id:
                    return reader.BoolValue();
                case IonType w when w.Id == IonType.Int.Id:
                    switch (reader.GetIntegerSize())
                    {
                        default: return null;
                        case IntegerSize.BigInteger: return reader.BigIntegerValue();
                        case IntegerSize.Int: return reader.IntValue();
                        case IntegerSize.Long: return reader.LongValue();
                    }
                case IonType w when w.Id == IonType.Float.Id:
                    var doubleVal = reader.DoubleValue();
                    if (Math.Abs(doubleVal - (float) doubleVal) < float.Epsilon) return (float) doubleVal;

                    return doubleVal;
                case IonType w when w.Id == IonType.Decimal.Id:
                    return reader.DecimalValue().ToDecimal();
                case IonType w when w.Id == IonType.String.Id:
                    return reader.StringValue();
                case IonType w when w.Id == IonType.Timestamp.Id:
                    return reader.TimestampValue();
            }
        }

        public static class Binary
        {
            public static bool ReadSingleBool(byte[] data)
            {
                var reader = new UserBinaryReader(new MemoryStream(data));
                Assert.AreEqual(IonType.Bool, reader.MoveNext());
                return reader.BoolValue();
            }

            public static bool DatagramEmpty(byte[] data)
            {
                var reader = new UserBinaryReader(new MemoryStream(data));
                return reader.MoveNext() == null;
            }

            public static BigDecimal ReadSingleDecimal(byte[] data)
            {
                var reader = new UserBinaryReader(new MemoryStream(data));
                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                return reader.DecimalValue();
            }
        }
    }
}
