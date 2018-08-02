using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using FastExpressionCompiler;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Bench
{
    public static class IonSerializerExpression
    {
        private static readonly ManagedBinaryWriter Writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray);

        private static readonly MethodInfo EnumGetnameMethod = typeof(Enum).GetMethod(nameof(Enum.GetName));

        private static readonly MethodInfo WriteNullTypeMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteNull), new[] {typeof(IonType)});
        private static readonly MethodInfo WriteStringMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteString));
        private static readonly MethodInfo WriteBoolMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteBool));
        private static readonly MethodInfo WriteIntMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteInt), new[] {typeof(long)});
        private static readonly MethodInfo WriteBigIntegerMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteInt), new[] {typeof(BigInteger)});
        private static readonly MethodInfo WriteFloatMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteFloat));
        private static readonly MethodInfo WriteTimestampMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteTimestamp));
        private static readonly MethodInfo WriteDecimalMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteDecimal));
        private static readonly MethodInfo WriteBlobMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteBlob));
        private static readonly MethodInfo WriteSymbolMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.WriteSymbol));

        private static readonly MethodInfo StepInMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.StepIn));
        private static readonly MethodInfo StepOutMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.StepOut));
        private static readonly MethodInfo SetFieldNameMethod = typeof(ManagedBinaryWriter).GetMethod(nameof(IIonWriter.SetFieldName));

        private static readonly ConstructorInfo TimeStampFromDateTime = typeof(Timestamp).GetConstructor(new[] {typeof(DateTime)});
        private static readonly ConstructorInfo TimeStampFromDateTimeOffset = typeof(Timestamp).GetConstructor(new[] {typeof(DateTimeOffset)});

        private static readonly Dictionary<Type, Delegate> Cache = new Dictionary<Type, Delegate>();


        private static Action<T, ManagedBinaryWriter> GetAction<T>()
        {
            var type = typeof(T);
            if (Cache.TryGetValue(type, out var action))
                return (Action<T, ManagedBinaryWriter>) action;

            var objParam = Expression.Parameter(type, "obj");
            var writerParam = Expression.Parameter(typeof(ManagedBinaryWriter), "writer");
            var writeActionExpression = GetWriteActionForType(type, objParam, writerParam);
            if (writeActionExpression == null)
                throw new IonException("Cannot create concrete type");

            var lambdaExp = Expression.Lambda<Action<T, ManagedBinaryWriter>>(writeActionExpression, objParam, writerParam);

            Action<T, ManagedBinaryWriter> act = lambdaExp.CompileFast();
            Cache.Add(type, act);

            return act;
        }

        public static byte[] Serialize<T>(T obj)
        {
            var action = GetAction<T>();
            //now write
            byte[] bytes = null;
            action(obj, Writer);
            Writer.Flush(ref bytes);
            return bytes;
        }

        private static Expression GetWriteActionForType(Type type, Expression target, ParameterExpression writerExpression)
        {
            Expression result;
            //scalar
            if ((result = TryGetWriteScalarExpression(type, target, writerExpression)) != null)
            {
                return result;
            }

            //byte array
            if ((result = TryGetWriteByteArrayExpression(type, target, writerExpression)) != null)
            {
                return result;
            }

            //collection
            if ((result = TryGetWriteListExpression(type, target, writerExpression)) != null)
            {
                return result;
            }

            //struct
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Expression structExpression = Expression.Call(writerExpression, StepInMethod, Expression.Constant(IonType.Struct));
            foreach (var propertyInfo in properties)
            {
                if (!propertyInfo.CanRead)
                    continue;

                var propertyValueExp = Expression.Property(target, propertyInfo);
                var writeValueExpression = GetWriteActionForType(propertyInfo.PropertyType, propertyValueExp, writerExpression);
                var setFieldNameExp = Expression.Call(writerExpression, SetFieldNameMethod, Expression.Constant(propertyInfo.Name));
                structExpression = Expression.Block(
                    structExpression, setFieldNameExp, writeValueExpression
                );
            }

            //then stepout
            structExpression = Expression.Block(structExpression, Expression.Call(writerExpression, StepOutMethod));

            //null check
            var notNull = Expression.NotEqual(target, Expression.Constant(null, type));
            result = Expression.Condition(notNull, structExpression, Expression.Call(writerExpression, WriteNullTypeMethod, Expression.Constant(IonType.Struct)));
            return result;
        }

        private static Expression TryGetWriteListExpression(Type type, Expression target, ParameterExpression writerExpression)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type))
                return null;


            Type genericType;
            if (type.IsArray)
            {
                genericType = type.GetElementType();
            }
            else
            {
                if (!type.IsGenericType)
                    return null;

                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length > 1)
                {
                    //why would this happen?
                    throw new IonException("More than one generic argument for collection");
                }

                genericType = genericArgs[0];
            }

            Expression listExpression = Expression.Call(writerExpression, StepInMethod, Expression.Constant(IonType.List));
            //iteration
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(genericType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(genericType);
            var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            var getEnumeratorCall = Expression.Call(target, enumerableType.GetMethod("GetEnumerator"));

            var loopVar = Expression.Parameter(genericType, "loopVar");

            var breakLabel = Expression.Label("LoopBreak");
            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)));

            var loopExp = Expression.Block(new[] {enumeratorVar},
                Expression.Assign(enumeratorVar, getEnumeratorCall),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Equal(moveNextCall, Expression.Constant(true)),
                        Expression.Block(new[] {loopVar},
                            Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
                            GetWriteActionForType(genericType, loopVar, writerExpression)
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel)
            );
            listExpression = Expression.Block(listExpression, loopExp, Expression.Call(writerExpression, StepOutMethod));
            return listExpression;
        }

        private static Expression TryGetWriteByteArrayExpression(Type type, Expression target, ParameterExpression writerExpression)
        {
            Expression result = null;
            if (type == typeof(byte[]))
            {
                result = Expression.Call(writerExpression, WriteBlobMethod, Expression.Convert(target, typeof(ReadOnlySpan<byte>)));
            }

            if (type == typeof(ReadOnlyMemory<byte>))
            {
                result = Expression.Call(writerExpression, WriteBlobMethod, target);
            }

            if (type == typeof(Memory<byte>))
            {
                result = Expression.Call(writerExpression, WriteBlobMethod, target);
            }

            //TODO handle byte collection
//            if (typeof(IEnumerable<byte>).IsAssignableFrom(type))
//            {
//                var arrayTarget = 
//            }

            if (result == null)
                return null;

            //null check
            var notNull = Expression.NotEqual(target, Expression.Constant(null, type));
            result = Expression.Condition(notNull, result,
                Expression.Call(writerExpression, WriteNullTypeMethod, Expression.Constant(IonType.Blob)));
            return result;
        }

        private static Expression TryGetWriteScalarExpression(Type type, Expression target, ParameterExpression writerExpression)
        {
            if (type.IsEnum)
            {
                return Expression.Call(writerExpression, WriteSymbolMethod,
                    Expression.Call(EnumGetnameMethod, Expression.Constant(type), Expression.Convert(target, typeof(object))));
            }


            if (type == typeof(string))
                return Expression.Call(writerExpression, WriteStringMethod, target);

            if (type == typeof(int))
            {
                return Expression.Call(writerExpression, WriteIntMethod, Expression.Convert(target, typeof(long)));
            }

            if (type == typeof(long))
            {
                return Expression.Call(writerExpression, WriteIntMethod, target);
            }

            if (type == typeof(BigInteger))
            {
                return Expression.Call(writerExpression, WriteBigIntegerMethod, target);
            }

            if (type == typeof(bool))
            {
                return Expression.Call(writerExpression, WriteBoolMethod, target);
            }

            if (type == typeof(float))
            {
                return Expression.Call(writerExpression, WriteFloatMethod, target);
            }

            if (type == typeof(double))
            {
                return Expression.Call(writerExpression, WriteFloatMethod, target);
            }

            if (type == typeof(DateTime))
            {
                return Expression.Call(writerExpression, WriteTimestampMethod, Expression.New(TimeStampFromDateTime, target));
            }

            if (type == typeof(DateTimeOffset))
            {
                return Expression.Call(writerExpression, WriteTimestampMethod, Expression.New(TimeStampFromDateTimeOffset, target));
            }

            if (type == typeof(decimal))
            {
                return Expression.Call(writerExpression, WriteDecimalMethod, target);
            }

            return null;
        }
    }
}
