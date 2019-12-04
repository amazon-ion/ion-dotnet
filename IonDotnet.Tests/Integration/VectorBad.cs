using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IonDotnet.Systems;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    [TestClass]
    public class VectorBad
    {
        private static readonly HashSet<string> Excludes = new HashSet<string>
        {
            "clob_3.ion",
            "clob_4.ion",
            "clob_5.ion",
            "clob_6.ion",
            "clob_7.ion",
            "clob_8.ion",
            "clob_9.ion",
            "clobWithLongLiteralBlockCommentAtEnd.ion",
            "clobWithLongLiteralCommentsInMiddle.ion",
            "clobWithNonAsciiCharacter.ion",
            "clobWithShortLiteralBlockCommentAtEnd.ion",
            "clobWithShortLiteralInlineCommentAtEnd.ion",
            "listWithClosingBrace.ion",
            "listWithClosingParen.ion",
            "longStringSplitEscape_1.ion",
            "longStringSplitEscape_2.ion",
            "longStringSplitEscape_3.ion",
            "offsetHours_1.ion",
            "offsetHours_2.ion",
            "offsetMinutes_1.ion",
            "offsetMinutes_2.ion",
            "offsetMinutes_3.ion",
            "sexpWithClosingBrace.ion",
            "sexpWithClosingBracket.ion",
            "shortUtf8Sequence_1.ion",
            "shortUtf8Sequence_2.ion",
            "shortUtf8Sequence_3.ion",
            "string_3.ion",
            "string_4.ion",
            "structWithClosingBracket.ion",
            "structWithClosingParen.ion",
            "symbol_10.ion",
            "symbol_11.ion",
            "clobWithLongLiteralInlineCommentAtEnd.ion"
        };

        private static readonly DirectoryInfo IonTestDir = DirStructure.IonTestDir();
        private static readonly DirectoryInfo BadDir = IonTestDir.GetDirectories("bad").First();
        private static readonly DirectoryInfo BadUtf8Dir = IonTestDir.GetDirectories("bad/utf8").First();
        private static readonly DirectoryInfo BadTimestampDir = IonTestDir.GetDirectories("bad/timestamp").First();
        private static readonly DirectoryInfo BadOutOfRangeDir = IonTestDir.GetDirectories("bad/timestamp/outOfRange").First();

        private static IEnumerable<FileInfo> GetIonFiles(DirectoryInfo dirInfo)
           => dirInfo.GetFiles()
           .Where(f => !Excludes.Contains(f.Name)
                       && f.Name.EndsWith(".ion") || f.Name.EndsWith(".10n"));

        public static IEnumerable<object[]> BadFiles()
        {
            return GetIonFiles(BadDir)
                .Select(f => new[] { f });
        }

        public static IEnumerable<object[]> BadUtf8Files()
        {
            return GetIonFiles(BadUtf8Dir)
                .Select(f => new[] { f });
        }

        public static IEnumerable<object[]> BadTimestamp()
        {
            return GetIonFiles(BadTimestampDir)
                .Select(f => new[] { f });
        }

        public static IEnumerable<object[]> BadOutOfRangeTimestamp()
        {
            return GetIonFiles(BadOutOfRangeDir)
                .Select(f => new[] { f });
        }

        public static string TestCaseName(MethodInfo methodInfo, object[] data)
        {
            var fileFullName = ((FileInfo)data[0]).FullName;
            var testDirIdx = fileFullName.IndexOf(IonTestDir.FullName, StringComparison.OrdinalIgnoreCase);
            return fileFullName.Substring(testDirIdx + IonTestDir.FullName.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        [DynamicData(nameof(BadFiles), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(BadUtf8Files), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(BadTimestamp), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        [DynamicData(nameof(BadOutOfRangeTimestamp), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestCaseName))]
        public void LoadBad(FileInfo fi)
        {
            IonLoader.WithReaderOptions(new ReaderOptions { Format = ReaderFormat.Text }).Load(fi);
        }
    }
}
