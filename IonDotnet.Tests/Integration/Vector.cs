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
//                            && f.Name == "utf16.ion"
                            && (f.Name.EndsWith(".ion") || f.Name.EndsWith(".10n")));

        public static IEnumerable<object[]> GoodFiles()
        {
            return GetIonFiles(GoodDir)
//                .Skip(119)
//                .Take(1)
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
            foreach (var topLevelValue in datagram)
            {
                Assert.IsTrue(topLevelValue is IonSequence);
                var sequence = (IonSequence) topLevelValue;
                foreach (var seqChild in sequence)
                {
                    foreach (var seqChild2 in sequence)
                    {
                        Assert.IsTrue(seqChild.IsEquivalentTo(seqChild2));
                    }
                }
            }
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
    }
}
