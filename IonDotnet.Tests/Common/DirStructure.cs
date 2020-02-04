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

namespace IonDotnet.Tests.Common
{
    internal static class DirStructure
    {
        private static DirectoryInfo GetRootDir()
        {
            var dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (!string.Equals(dirInfo.Name, "ion-dotnet", StringComparison.OrdinalIgnoreCase))
            {
                dirInfo = Directory.GetParent(dirInfo.FullName);
            }

            return dirInfo;
        }

        private static DirectoryInfo TestDatDir()
        {
            var root = GetRootDir();
            return new DirectoryInfo(Path.Combine(
                root.FullName, "IonDotnet.Tests", "TestDat"));
        }

        // ion-tests/iontestdata/
        public static DirectoryInfo IonTestDir()
        {
            var root = GetRootDir();
            return new DirectoryInfo(Path.Combine(
                root.FullName, "ion-tests", "iontestdata"));
        }

        public static FileInfo OwnFile(string relativePath)
        {
            var testDatDir = TestDatDir();
            return new FileInfo(Path.Combine(testDatDir.FullName, relativePath));
        }

        public static FileInfo IonTestFile(string relativePath)
        {
            var testDatDir = IonTestDir();
            return new FileInfo(Path.Combine(testDatDir.FullName, relativePath));
        }

        public static byte[] OwnTestFileAsBytes(string relativePath)
        {
            var ownFile = OwnFile(relativePath);
            return File.ReadAllBytes(ownFile.FullName);
        }

        /// <remarks>Dispose this stream after using</remarks>
        public static Stream OwnTestFileAsStream(string relativePath)
        {
            var ownFile = OwnFile(relativePath);
            return ownFile.OpenRead();
        }

        public static byte[] IonTestFileAsBytes(string relativePath)
        {
            var file = IonTestFile(relativePath);
            return File.ReadAllBytes(file.FullName);
        }

        /// <remarks>Dispose this stream after using</remarks>
        public static Stream IonTestFileAsStream(string relativePath)
        {
            var file = IonTestFile(relativePath);
            return file.OpenRead();
        }
    }
}
