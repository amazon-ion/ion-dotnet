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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Tests.Common;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.IonDotnet.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedParameter.Global

namespace Amazon.IonDotnet.Tests.Integration
{
    /// <summary>
    /// Provide semantic testing based on ion-test.
    /// </summary>
    [TestClass]
    public class Vector
    {
        private static readonly HashSet<string> Excludes = new HashSet<string>
        {
            "subfieldVarInt.ion"
        };

        private static readonly DirectoryInfo IonTestDir = DirStructure.IonTestDir();
        private static readonly DirectoryInfo GoodDir = IonTestDir.GetDirectories("good").First();
        private static readonly DirectoryInfo GoodTimestampDir = IonTestDir.GetDirectories("good/timestamp").First();
        private static readonly DirectoryInfo GoodTimestampEquivDir = IonTestDir.GetDirectories("good/timestamp/equivTimeline").First();
        private static readonly DirectoryInfo GoodEquivDir = IonTestDir.GetDirectories("good/equivs").First();
        private static readonly DirectoryInfo GoodEquivUtf8Dir = IonTestDir.GetDirectories("good/equivs/utf8").First();
        private static readonly DirectoryInfo GoodNonEquivDir = IonTestDir.GetDirectories("good/non-equivs").First();

        private static IEnumerable<FileInfo> GetIonFiles(DirectoryInfo dirInfo)
            => dirInfo.GetFiles()
                .Where(f => !Excludes.Contains(f.Name)
                            //this is for debugging the interested file
                            //&& f.Name == "item1.10n"
                            && (f.Name.EndsWith(".ion") || f.Name.EndsWith(".10n")));

        public static IEnumerable<object[]> GoodFiles()
        {
            return GetIonFiles(GoodDir)
                .Select(f => new[] {f});
        }

        public static IEnumerable<object[]> GoodTimestampFiles()
        {
            return GetIonFiles(GoodTimestampDir)
                .Select(f => new[] {f});
        }

        public static IEnumerable<object[]> GoodTimestampEquivFiles()
        {
            return GetIonFiles(GoodTimestampEquivDir)
                .Select(f => new[] {f});
        }

        public static IEnumerable<object[]> GoodEquivFiles()
        {
            return GetIonFiles(GoodEquivDir)
                .Select(f => new[] {f});
        }

        public static IEnumerable<object[]> GoodEquivUtf8Files()
        {
            return GetIonFiles(GoodEquivUtf8Dir)
                .Select(f => new[] {f});
        }

        public static IEnumerable<object[]> GoodNonEquivFiles()
        {
            return GetIonFiles(GoodNonEquivDir)
                .Select(f => new[] {f});
        }

        public static string TestCaseName(MethodInfo methodInfo, object[] data)
        {
            var fileFullName = ((FileInfo)data[0]).FullName;
            var testDirIdx = fileFullName.IndexOf(IonTestDir.FullName, StringComparison.OrdinalIgnoreCase);
            return fileFullName.Substring(testDirIdx + IonTestDir.FullName.Length);
        }

        /// <summary>
        /// Just make sure this doesn't cause exception
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GoodFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodTimestampFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodTimestampEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodEquivUtf8Files), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodNonEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        public void LoadGood_Successful(FileInfo fi)
        {
            LoadFile(fi);
        }

        [TestMethod]
        [DynamicData(nameof(GoodFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodTimestampFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodTimestampEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodEquivUtf8Files), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodNonEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        public void LoadGood_RoundTrip(FileInfo fi)
        {
            var datagram = LoadFile(fi, out var readerTable);
            RoundTrip_AssertText(datagram, readerTable);
            RoundTrip_AssertBinary(datagram, readerTable);
        }

        private static void RoundTrip_AssertText(IIonValue datagram, ISymbolTable readerTable)
        {
            var sw = new StringWriter();
            var writer = IonTextWriterBuilder.Build(sw, new IonTextOptions {PrettyPrint = true}, readerTable.GetImportedTables());
            datagram.WriteTo(writer);
            writer.Finish();
            var text = sw.ToString();
            Console.WriteLine(text);
            var catalog = Symbols.GetReaderCatalog(readerTable);
            var datagram2 = IonLoader.WithReaderOptions(new ReaderOptions {Catalog = catalog}).Load(text);
            AssertDatagramEquivalent(datagram, datagram2);
        }

        private static void RoundTrip_AssertBinary(IIonValue datagram, ISymbolTable readerTable)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = IonBinaryWriterBuilder.Build(ms, readerTable.GetImportedTables()))
                {
                    datagram.WriteTo(writer);
                    writer.Finish();
                    var bin = ms.ToArray();
                    var catalog = Symbols.GetReaderCatalog(readerTable);
                    var datagram2 = IonLoader.WithReaderOptions(new ReaderOptions {Catalog = catalog, Format = ReaderFormat.Binary}).Load(bin);
                    AssertDatagramEquivalent(datagram, datagram2);
                }
            }
        }

        /// <summary>
        /// Execute the good/equivs semantics. 
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GoodEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        public void Good_Equivalence(FileInfo fi)
        {
            var datagram = LoadFile(fi);
            var i = 0;
            foreach (var topLevelValue in datagram)
            {
                i++;
                Assert.IsTrue(topLevelValue is IonSequence);
                var sequence = (IonSequence) topLevelValue;
                if (sequence.HasAnnotation("embedded_documents"))
                {
                    EmbeddedDocumentEquiv(sequence, true, i);
                    continue;
                }

                foreach (var seqChild in sequence)
                {
                    foreach (var seqChild2 in sequence)
                    {
                        if (seqChild == seqChild2)
                        {
                            continue;
                        }

                        var equiv = seqChild.IsEquivalentTo(seqChild2);
                        if (!equiv)
                        {
                            Console.WriteLine(i);
                            equiv = seqChild.IsEquivalentTo(seqChild2);
                            Console.WriteLine(seqChild.ToPrettyString());
                            Console.WriteLine(seqChild2.ToPrettyString());
                        }

                        Assert.IsTrue(equiv);
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(GoodNonEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        public void Good_Non_Equivalence(FileInfo fi)
        {
            var datagram = LoadFile(fi);
            var i = 0;
            foreach (var topLevelValue in datagram)
            {
                i++;
                if (fi.Name == "timestamps.ion" && i >= 9)
                {
                    continue;
                }

                Assert.IsTrue(topLevelValue is IonSequence);
                var sequence = (IonSequence) topLevelValue;

                if (sequence.HasAnnotation("embedded_documents"))
                {
                    EmbeddedDocumentEquiv(sequence, false, i);
                    continue;
                }

                foreach (var seqChild in sequence)
                {
                    foreach (var seqChild2 in sequence)
                    {
                        if (seqChild == seqChild2)
                        {
                            continue;
                        }

                        var equiv = seqChild.IsEquivalentTo(seqChild2);
                        if (equiv)
                        {
                            Console.WriteLine(seqChild.Type() + seqChild.ToPrettyString());
                            Console.WriteLine(seqChild2.Type() + seqChild2.ToPrettyString());
                            Console.WriteLine(i);
                            equiv = seqChild.IsEquivalentTo(seqChild2);
                        }

                        Assert.IsFalse(equiv);
                    }
                }
            }
        }

        private static void EmbeddedDocumentEquiv(IonSequence sequence, bool expected, int i)
        {
            foreach (var doc1 in sequence)
            {
                Console.WriteLine(i);
                Assert.IsTrue(doc1 is IonString);
                var dg1 = IonLoader.Default.Load(((IonString) doc1).StringValue);
                foreach (var doc2 in sequence)
                {
                    if (doc1 == doc2)
                    {
                        continue;
                    }

                    var dg2 = IonLoader.Default.Load(((IonString) doc2).StringValue);
                    var eq = AssertDatagramEquivalent(dg1, dg2);
                    if (expected != eq)
                    {
                        Console.WriteLine(((IonString) doc1).StringValue);
                        Console.WriteLine(((IonString) doc2).StringValue);
                    }

                    Assert.AreEqual(expected, eq);
                }
            }
        }

        private static bool AssertDatagramEquivalent(IIonValue d1, IIonValue d2)
        {
            IonValue[] values1 = GetIonValues(d1);
            IonValue[] values2 = GetIonValues(d2);

            var eq = values1.SequenceEqual(values2, IonValueComparer);
            return eq;
        }

        private static IonValue[] GetIonValues(IIonValue value)
        {
            if (value is null)
                return new IonValue[0];

            IonValue[] ionValues = new IonValue[value.Count];
            int counter = 0;
            foreach (var ionValue in value)
            {
                ionValues[counter++] = (IonValue)ionValue;
            }

            return ionValues;
        }

        private static IIonValue LoadFile(FileInfo fi, out ISymbolTable readerTable)
        {
            if (fi.Name == "utf16.ion")
            {
                return IonLoader
                    .WithReaderOptions(new ReaderOptions {Encoding = new UnicodeEncoding(true, true)})
                    .Load(fi, out readerTable);
            }

            if (fi.Name == "utf32.ion")
            {
                return IonLoader
                    .WithReaderOptions(new ReaderOptions {Encoding = new UTF32Encoding(true, true)})
                    .Load(fi, out readerTable);
            }

            var tree = IonLoader.Default.Load(fi, out readerTable);
            return tree;
        }

        private static IIonValue LoadFile(FileInfo fi) => LoadFile(fi, out _);

        private class ValueComparer : IEqualityComparer<IonValue>
        {
            public bool Equals(IonValue x, IonValue y) => x.IsEquivalentTo(y);

            public int GetHashCode(IonValue obj) => obj.GetHashCode();
        }

        private static readonly ValueComparer IonValueComparer = new ValueComparer();
    }
}
