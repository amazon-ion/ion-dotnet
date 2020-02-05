﻿using System.IO;
using Amazon.IonDotnet.Internals.Text;

namespace Amazon.IonDotnet.Bench
{
    public static class IonExpressionText
    {
        public static string Serialize<T>(T obj)
        {
            var sw = new StringWriter();
            var writer = new IonTextWriter(sw, null);
            var action = IonSerializerExpression.GetAction<T>();
            action(obj, writer);
            writer.Finish();
            return sw.ToString();
        }
    }
}
