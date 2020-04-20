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
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class TextWriterTest
    {
        [TestMethod]
        public void NoQuotedSymbolAndFieldName()
        {
            var sw = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(sw);

            textWriter.StepIn(IonType.Struct);
            textWriter.SetFieldName("hello");
            textWriter.AddTypeAnnotation("ion");
            textWriter.WriteSymbol("world");
            textWriter.StepOut();

            Assert.AreEqual("{hello:ion::world}", sw.ToString());
        }

        [TestMethod]
        public void QuotedSymbolAndFieldName()
        {
            var sw = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(sw);

            textWriter.StepIn(IonType.Struct);
            textWriter.SetFieldName("true");
            textWriter.AddTypeAnnotation("ion");
            textWriter.WriteSymbol("null");
            textWriter.StepOut();

            Assert.AreEqual("{'true':ion::'null'}", sw.ToString());
        }


        [TestMethod]
        [DataRow("+inf", "nan", "s1 '+inf' 'nan'")]
        [DataRow("s2", "abc", "s1 s2 abc")]
        public void WriteSymbolWithSymbolTable(String tableSym, String newSym, String expectedText)
        {
            var symbol = "symbols:[\"s1\", \"" + tableSym + "\"]";
            var text =
                SystemSymbols.IonSymbolTable + "::" +
                "{" +
                symbol +
                "}\n" +
                "$10\n" +
                "$11\n";

            var ionValueFactory = new ValueFactory();
            var datagram = IonLoader.Default.Load(text);
            datagram.Add(ionValueFactory.NewSymbol(newSym));

            var sw = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(sw);
            datagram.WriteTo(textWriter);
            textWriter.Finish();
            var expected = expectedText;
            var actual = sw.ToString();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(UnknownSymbolException))]
        public void WriteSymbolWithSymbolTableOutOfRange()
        {
            var text =
                SystemSymbols.IonSymbolTable + "::" +
                "{" +
                "symbols:[\"s1\"]" +
                "}\n" +
                "$10\n";

            var ionValueFactory = new ValueFactory();
            var datagram = IonLoader.Default.Load(text);
            datagram.Add(ionValueFactory.NewSymbol("with_unknown_sid"));
            datagram.Add(ionValueFactory.NewSymbol(new SymbolToken(null, 12)));

            var sw = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(sw);
            // Should trhow exception as sid 12 exceeds the max ID of the symbol table currently in scope 
            datagram.WriteTo(textWriter);
            textWriter.Finish();
        }
    }
}
