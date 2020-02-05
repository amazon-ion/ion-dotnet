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
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Integration
{
    [TestClass]
    public class VectorBad
    {
        private static readonly HashSet<string> Excludes = new HashSet<string>
        {
            "shortUtf8Sequence_1.ion",
            "shortUtf8Sequence_2.ion",
            "shortUtf8Sequence_3.ion"
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
