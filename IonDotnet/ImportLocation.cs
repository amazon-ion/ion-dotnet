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

namespace IonDotnet
{
    public readonly struct ImportLocation
    {

        /// <summary>
        /// The default ImportName, which is unknown
        /// </summary>
        public const string UnknownImportName = default;

        /// <summary>
        /// The default value, corresponds to not_found/unknown
        /// </summary>
        public static readonly ImportLocation None = default;

        /// <summary>
        /// The import name of this import location.
        /// </summary>
        public readonly string ImportName;

        /// <summary>
        /// The ID of this import location.
        /// </summary>
        public readonly int Sid;

        /// <summary>
        /// Create a new ImportLocation struct.
        /// </summary>
        /// <param name="importName">ImportName</param>
        /// <param name="sid">Sid</param>
        public ImportLocation(string importName, int sid)
        {
            ImportName = importName;
            Sid = sid;
        }

        public override string ToString() => $"ImportLocation::{{importName:{ImportName}, id:{Sid}}}";

        public static bool operator ==(ImportLocation x, ImportLocation y) => x.ImportName == y.ImportName && x.Sid == y.Sid;

        public static bool operator !=(ImportLocation x, ImportLocation y) => !(x == y);

        public override bool Equals(object that) => that is ImportLocation token && Equals(token);

        public bool Equals(ImportLocation other) => this == other;

        public override int GetHashCode() => ImportName?.GetHashCode() ?? Sid;
    }
}
