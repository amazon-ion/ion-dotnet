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
using IonDotnet.Builders;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    public abstract class IntegrationTestBase
    {
        public enum InputStyle
        {
            MemoryStream,
            FileStream,
            Text,
            NoSeekStream
        }

        private Stream _stream;

        [TestCleanup]
        public void Cleanup()
        {
            _stream?.Dispose();
        }

        /// <summary>
        /// Apply the same writing logic to binary and text writers and assert the accuracy 
        /// </summary>
        protected void AssertReaderWriter(Action<IIonReader> assertReader, Action<IIonWriter> writerFunc)
        {
            //bin
            using (var s = new MemoryStream())
            {
                var binWriter = IonBinaryWriterBuilder.Build(s);
                writerFunc(binWriter);
                s.Seek(0, SeekOrigin.Begin);
                var binReader = IonReaderBuilder.Build(s);
                assertReader(binReader);
            }

            //text
            var sw = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(sw);
            writerFunc(textWriter);
            var str = sw.ToString();
            var textReader = IonReaderBuilder.Build(str);
            assertReader(textReader);
        }

        protected IIonReader ReaderFromFile(FileInfo file, InputStyle style)
        {
            _stream?.Dispose();
            switch (style)
            {
                case InputStyle.MemoryStream:
                    var bytes = File.ReadAllBytes(file.FullName);
                    _stream = new MemoryStream(bytes);
                    return IonReaderBuilder.Build(_stream);
                case InputStyle.FileStream:
                    _stream = file.OpenRead();
                    return IonReaderBuilder.Build(_stream);
                case InputStyle.Text:
                    var str = File.ReadAllText(file.FullName, Encoding.UTF8);
                    return IonReaderBuilder.Build(str);
                case InputStyle.NoSeekStream:
                    var b = File.ReadAllBytes(file.FullName);
                    _stream = new NoSeekMemStream(b);
                    return IonReaderBuilder.Build(_stream);
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, null);
            }
        }
    }
}
