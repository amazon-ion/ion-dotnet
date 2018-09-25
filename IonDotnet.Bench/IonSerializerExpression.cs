using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using FastExpressionCompiler;

namespace IonDotnet.Bench
{
    public static class IonSerializerExpression
    {
        private static readonly MethodInfo EnumGetnameMethod = typeof(Enum).GetMethod(nameof(Enum.GetName));

        private static readonly MethodInfo WriteNullTypeMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteNull), new[] {typeof(IonType)});
        private static readonly MethodInfo WriteStringMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteString));
        private static readonly MethodInfo WriteBoolMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteBool));
        private static readonly MethodInfo WriteIntMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteInt), new[] {typeof(long)});
        private static readonly MethodInfo WriteBigIntegerMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteInt), new[] {typeof(BigInteger)});
        private static readonly MethodInfo WriteFloatMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteFloat));
        private static readonly MethodInfo WriteTimestampMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteTimestamp));
        private static readonly MethodInfo WriteDecimalMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteDecimal));
        private static readonly MethodInfo WriteBlobMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteBlob));
        private static readonly MethodInfo WriteSymbolMethod = typeof(IValueWriter).GetMethod(nameof(IValueWriter.WriteSymbol));

        private static readonly MethodInfo StepInMethod = typeof(IIonWriter).GetMethod(nameof(IIonWriter.StepIn));
        private static readonly MethodInfo StepOutMethod = typeof(IIonWriter).GetMethod(nameof(IIonWriter.StepOut));
        private static readonly MethodInfo SetFieldNameMethod = typeof(IIonWriter).GetMethod(nameof(IIonWriter.SetFieldName));

        private static readonly ConstructorInfo TimeStampFromDateTime = typeof(Timestamp).GetConstructor(new[] {typeof(DateTime)});
        private static readonly ConstructorInfo TimeStampFromDateTimeOffset = typeof(Timestamp).GetConstructor(new[] {typeof(DateTimeOffset)});

        private static readonly Dictionary<Type, Delegate> Cache = new Dictionary<Type, Delegate>();

        public static Action<T, IIonWriter> GetAction<T>()
        {
            var type = typeof(T);
            if (Cache.TryGetValue(type, out var action))
                return (Action<T, IIonWriter>) action;

            lock (Cache)
            {
                // this lock should only block Cache.Add()
                var objParam = Expression.Parameter(type, "obj");
                var writerParam = Expression.Parameter(typeof(IIonWriter), "writer");
                var writeActionExpression = GetWriteActionForType(type, objParam, writerParam);
                if (writeActionExpression == null)
                    throw new IonException("Cannot create concrete type");

                var lambdaExp = Expression.Lambda<Action<T, IIonWriter>>(writeActionExpression, objParam, writerParam);

                Action<T, IIonWriter> act = lambdaExp.CompileFast();
                Cache.Add(type, act);

                return act;
            }
        }

        private static Expression GetWriteActionForType(Type type, Expression target, ParameterExpression writerExpression)
        {
            Expression result;
            //scalar
            if ((result = TryGetWriteScalarExpression(type, target, writerExpression)) != null)
                return result;

            //byte array
            if ((result = TryGetWriteByteArrayExpression(type, target, writerExpression)) != null)
                return result;

            //collection
            if ((result = TryGetWriteEnumerableExpression(type, target, writerExpression)) != null)
                return result;

            //struct, stepin
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

        private static Expression TryGetWriteEnumerableExpression(Type type, Expression target, ParameterExpression writerExpression)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type))
                return null;

            if (type.IsArray)
                return TryGetWriteArrayExpression(type, target, writerExpression);


            if (!type.IsGenericType)
                return null;

            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 1)
                //why would this happen?
                throw new IonException("More than one generic argument for collection");

            var genericType = genericArgs[0];

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
            //null check
            var notNull = Expression.NotEqual(target, Expression.Constant(null, type));
            var result = Expression.Condition(notNull, listExpression,
                Expression.Call(writerExpression, WriteNullTypeMethod, Expression.Constant(IonType.List)));
            return result;
        }

        private static Expression TryGetWriteArrayExpression(Type type, Expression target, ParameterExpression writerExpression)
        {
            var arrayType = type.GetElementType();
            Expression enumArrayExpression = Expression.Call(writerExpression, StepInMethod, Expression.Constant(IonType.List));
            var lengthExp = Expression.Property(target, "Length");
            var breakLabel = Expression.Label("LoopBreak");
            var loopVar = Expression.Parameter(arrayType, "loopVar");
            var idxVarExp = Expression.Variable(typeof(int), "i");

            var loopExp = Expression.Block(new[] {idxVarExp},
                Expression.Assign(idxVarExp, Expression.Constant(0)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(idxVarExp, lengthExp),
                        Expression.Block(new[] {loopVar},
                            Expression.Assign(loopVar, Expression.ArrayIndex(target, idxVarExp)),
                            Expression.Assign(idxVarExp, Expression.Add(idxVarExp, Expression.Constant(1))),
                            GetWriteActionForType(arrayType, loopVar, writerExpression)
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel)
            );
            enumArrayExpression = Expression.Block(enumArrayExpression, loopExp, Expression.Call(writerExpression, StepOutMethod));
            //null check
            var notNull = Expression.NotEqual(target, Expression.Constant(null, type));
            var result = Expression.Condition(notNull, enumArrayExpression,
                Expression.Call(writerExpression, WriteNullTypeMethod, Expression.Constant(IonType.List)));
            return result;
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
                return Expression.Call(writerExpression, WriteSymbolMethod, Expression.Call(
                    EnumGetnameMethod,
                    Expression.Constant(type),
                    Expression.Convert(target, typeof(object))
                ));


            if (type == typeof(string))
                return Expression.Call(writerExpression, WriteStringMethod, target);

            if (type == typeof(int))
                return Expression.Call(writerExpression, WriteIntMethod, Expression.Convert(target, typeof(long)));

            if (type == typeof(long))
                return Expression.Call(writerExpression, WriteIntMethod, target);

            if (type == typeof(BigInteger))
                return Expression.Call(writerExpression, WriteBigIntegerMethod, target);

            if (type == typeof(bool))
                return Expression.Call(writerExpression, WriteBoolMethod, target);

            if (type == typeof(float))
                return Expression.Call(writerExpression, WriteFloatMethod, target);

            if (type == typeof(double))
                return Expression.Call(writerExpression, WriteFloatMethod, target);

            if (type == typeof(DateTime))
                return Expression.Call(writerExpression, WriteTimestampMethod, Expression.New(TimeStampFromDateTime, target));

            if (type == typeof(DateTimeOffset))
                return Expression.Call(writerExpression, WriteTimestampMethod, Expression.New(TimeStampFromDateTimeOffset, target));

            if (type == typeof(decimal))
                return Expression.Call(writerExpression, WriteDecimalMethod, target);

            return null;
        }
    }
}
