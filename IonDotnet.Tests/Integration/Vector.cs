using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IonDotnet.Systems;
using IonDotnet.Tests.Common;
using IonDotnet.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedParameter.Global

namespace IonDotnet.Tests.Integration
{
    /// <summary>
    /// Provide semantic testing based on ion-test.
    /// </summary>
    [TestClass]
    public class Vector
    {
        private static readonly HashSet<string> Excludes = new HashSet<string>
        {
            "subfieldVarInt.ion",
            "whitespace.ion"
        };

        private static readonly DirectoryInfo IonTestDir = DirStructure.IonTestDir();
        private static readonly DirectoryInfo GoodDir = IonTestDir.GetDirectories("good").First();
        private static readonly DirectoryInfo GoodTimestampDir = IonTestDir.GetDirectories("good/timestamp").First();
        private static readonly DirectoryInfo GoodEquivDir = IonTestDir.GetDirectories("good/equivs").First();
        private static readonly DirectoryInfo GoodNonEquivDir = IonTestDir.GetDirectories("good/non-equivs").First();

        private static IEnumerable<FileInfo> GetIonFiles(DirectoryInfo dirInfo)
            => dirInfo.GetFiles()
                .Where(f => !Excludes.Contains(f.Name)
                            //this is for debugging the interested file
                            //&& f.Name == "timestamps.ion"
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

        public static IEnumerable<object[]> GoodEquivFiles()
        {
            return GetIonFiles(GoodEquivDir)
                .Select(f => new[] {f});
        }

        public static IEnumerable<object[]> GoodNonEquivFiles()
        {
            return GetIonFiles(GoodNonEquivDir)
                .Select(f => new[] {f});
        }

        public static string TestCaseName(MethodInfo methodInfo, object[] data)
        {
            var fileFullName = ((FileInfo) data[0]).FullName;
            var testDirIdx = fileFullName.IndexOf(IonTestDir.FullName, StringComparison.OrdinalIgnoreCase);
            return fileFullName.Substring(testDirIdx + IonTestDir.FullName.Length);
        }

        /// <summary>
        /// Just make sure this doesn't cause exception
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GoodFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodTimestampFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(GoodNonEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        public void LoadGood_Successful(FileInfo fi)
        {
            LoadFile(fi);
        }

        /// <summary>
        /// Execute the good/equivs semantics. 
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GoodEquivFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        public void Good_Equivalence(FileInfo fi)
        {
            var datagram = LoadFile(fi);
            int i = 0;
            foreach (var topLevelValue in datagram)
            {
                i++;
                Assert.IsTrue(topLevelValue is IonSequence);
                var sequence = (IonSequence) topLevelValue;
                if (sequence.HasAnnotation("embedded_documents"))
                {
                    EmbeddedDocumentEquiv(sequence, true);
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
                            equiv = seqChild.IsEquivalentTo(seqChild2);
                            Console.WriteLine(seqChild.ToPrettyString());
                            Console.WriteLine(seqChild2.ToPrettyString());
                            Console.WriteLine(i);
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
            int i = 0;
            foreach (var topLevelValue in datagram)
            {
                i++;
                Assert.IsTrue(topLevelValue is IonSequence);
                var sequence = (IonSequence) topLevelValue;

                if (sequence.HasAnnotation("embedded_documents"))
                {
                    EmbeddedDocumentEquiv(sequence, false);
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
                            Console.WriteLine(seqChild.Type + seqChild.ToPrettyString());
                            Console.WriteLine(seqChild2.Type + seqChild2.ToPrettyString());
                            Console.WriteLine(i);
                            equiv = seqChild.IsEquivalentTo(seqChild2);
                        }

                        Assert.IsFalse(equiv);
                    }
                }
            }
        }

        private static void EmbeddedDocumentEquiv(IonSequence sequence, bool expected)
        {
            foreach (var doc1 in sequence)
            {
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
                    Assert.AreEqual(expected, eq);
                }
            }
        }

        private static bool AssertDatagramEquivalent(IonDatagram d1, IonDatagram d2)
        {
            var eq = d1.SequenceEqual(d2, IonValueComparer);
            return eq;
        }

        private static IonDatagram LoadFile(FileInfo fi)
        {
            if (fi.Name == "utf16.ion")
            {
                return IonLoader.Default.Load(fi, new UnicodeEncoding(true, true));
            }

            if (fi.Name == "utf32.ion")
            {
                return IonLoader.Default.Load(fi, new UTF32Encoding(true, true));
            }

            return IonLoader.Default.Load(fi);
        }

        private class ValueComparer : IEqualityComparer<IonValue>
        {
            public bool Equals(IonValue x, IonValue y)
            {
                return x.IsEquivalentTo(y);
            }

            public int GetHashCode(IonValue obj)
            {
                return obj.GetHashCode();
            }
        }

        private static readonly ValueComparer IonValueComparer = new ValueComparer();
    }
}
