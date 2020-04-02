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

using System;
using System.IO;
using System.Text;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Internals.Text;
using Amazon.IonDotnet.Tests.Common;
using Amazon.IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class TextWriterTest
    {
        [TestMethod]
        public void NoSingleQuotesSymbol()
        {
            StringWriter sw = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(sw);

            textWriter.SetFieldName("hello");
            textWriter.AddTypeAnnotation("ion");
            textWriter.AddTypeAnnotation("hash");
            textWriter.WriteSymbol("world");

            Assert.AreEqual("null [5] null {hello:ion::hash::world}", sw.ToString());
        }

        [TestMethod]
        public void NoQuoteSymbol2()
        {
            var text =
                SystemSymbols.IonSymbolTable + "::" +
                "{" +
                "   symbols:[\"s1\", \"s2\"]" +
                "}\n" +
                "$10\n" +
                "$11\n";
            var ionValueFactory = new ValueFactory();
            var datagram = IonLoader.Default.Load(text);
            datagram.Add(ionValueFactory.NewSymbol("abc"));
            datagram.Add(ionValueFactory.NewSymbol(new SymbolToken(null, 13))); // s3.
            // Text.
            var textOutput = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(textOutput);
            datagram.WriteTo(textWriter);
            textWriter.Finish();
            var expected = "s1 s2 abc $13";
            var actual = textOutput.ToString();
            Assert.AreEqual(expected, actual);
        }
    }
}
