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

namespace Amazon.IonDotnet
{
    /// <summary>
    /// Constants for symbols defined by the Ion specification.
    /// </summary>
    public static class SystemSymbols
    {
        /// <summary>
        /// The text of system symbol {@value}, as defined by Ion 1.0.
        /// </summary>
        public const string Ion = "$ion";

        /// <summary>
        /// The ID of system symbol {@value #ION}, as defined by Ion 1.0.
        /// </summary>
        public const int IonSid = 1;

        /// <summary>
        /// The text of system symbol {@value}, as defined by Ion 1.0.
        /// This value is the Version Identifier for Ion 1.0.
        /// </summary>
        public const string Ion10 = "$ion_1_0";

        public const int Ion10Sid = 2;

        public const string IonSymbolTable = "$ion_symbol_table";

        public const int IonSymbolTableSid = 3;

        public const string Name = "name";

        public const int NameSid = 4;

        public const string Version = "version";

        public const int VersionSid = 5;

        public const string Imports = "imports";

        public const int ImportsSid = 6;

        public const string Symbols = "symbols";

        public const int SymbolsSid = 7;

        public const string MaxId = "max_id";

        public const int MaxIdSid = 8;

        public const string IonSharedSymbolTable = "$ion_shared_symbol_table";

        public const int IonSharedSymbolTableSid = 9;

        public const int Ion10MaxId = 9;

        /// <summary>
        /// Resolve the symbol table field sid.
        /// </summary>
        /// <param name="fieldName">Symbol table field name.</param>
        /// <returns>Symbol ID.</returns>
        public static int ResolveSidForSymbolTableField(string fieldName)
        {
            const int shortestName = 4; // 'name'
            if (fieldName is null || fieldName.Length < shortestName)
            {
                return SymbolToken.UnknownSid;
            }

            switch (fieldName[0])
            {
                case 'v':
                    if (Version.Equals(fieldName))
                    {
                        return VersionSid;
                    }

                    break;
                case 'n':
                    if (Name.Equals(fieldName))
                    {
                        return NameSid;
                    }

                    break;
                case 's':
                    if (Symbols.Equals(fieldName))
                    {
                        return SymbolsSid;
                    }

                    break;
                case 'i':
                    if (Imports.Equals(fieldName))
                    {
                        return ImportsSid;
                    }

                    break;
                case 'm':
                    if (MaxId.Equals(fieldName))
                    {
                        return MaxIdSid;
                    }

                    break;
            }

            return SymbolToken.UnknownSid;
        }
    }
}
