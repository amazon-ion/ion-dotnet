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

namespace Amazon.IonDotnet.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using static System.Diagnostics.Debug;

    /// <inheritdoc cref="ICatalog" />
    /// <summary>
    /// A basic implementation for a mutable catalog.
    /// </summary>
    public class SimpleCatalog : IMutableCatalog, IEnumerable<ISymbolTable>
    {
        private static readonly MaxFirstComparer Comparer = new MaxFirstComparer();

        private readonly Dictionary<string, SortedList<int, ISymbolTable>> tablesByName
            = new Dictionary<string, SortedList<int, ISymbolTable>>();

        public ISymbolTable GetTable(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            SortedList<int, ISymbolTable> versions;
            lock (this.tablesByName)
            {
                if (!this.tablesByName.TryGetValue(name, out versions))
                {
                    return null;
                }
            }

            lock (versions)
            {
                return versions.First().Value;
            }
        }

        public ISymbolTable GetTable(string name, int version)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (version < 1)
            {
                throw new ArgumentException("Must be >=1", nameof(version));
            }

            SortedList<int, ISymbolTable> versions;
            lock (this.tablesByName)
            {
                if (!this.tablesByName.TryGetValue(name, out versions))
                {
                    return null;
                }
            }

            lock (versions)
            {
                if (versions.TryGetValue(version, out var table))
                {
                    return table;
                }

                Assert(versions.Count > 0, "Count is not > than 0");
                var bestMatch = GetBestMatchOfVersion(version, versions.Keys);
                table = versions[bestMatch];
                Assert(table != null, "table is null");
                return table;
            }
        }

        public void PutTable(ISymbolTable sharedTable)
        {
            if (sharedTable.IsLocal || sharedTable.IsSystem || sharedTable.IsSubstitute)
            {
                throw new ArgumentException("table must be shared, non-subtitute", nameof(sharedTable));
            }

            // make a local copy
            var name = sharedTable.Name;
            var version = sharedTable.Version;
            Assert(version > 0, "version is not > 0");

            lock (this.tablesByName)
            {
                if (!this.tablesByName.TryGetValue(name, out var versions))
                {
                    versions = new SortedList<int, ISymbolTable>(Comparer);
                    this.tablesByName.Add(name, versions);
                }

                lock (versions)
                {
                    versions[version] = sharedTable;
                }
            }
        }

        public IEnumerator<ISymbolTable> GetEnumerator()
        {
            List<ISymbolTable> tables;
            lock (this.tablesByName)
            {
                tables = new List<ISymbolTable>(this.tablesByName.Count);
                foreach (var kvp in this.tablesByName)
                {
                    var versions = kvp.Value;
                    lock (versions)
                    {
                        tables.AddRange(versions.Values);
                    }
                }
            }

            return tables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private static int GetBestMatchOfVersion(int requestedVersion, IEnumerable<int> versionsKeys)
        {
            var best = requestedVersion;
            var ibest = -1;
            foreach (var available in versionsKeys)
            {
                Assert(available != requestedVersion, "available is not the requestedVersion");
                var v = available;

                if (requestedVersion < best)
                {
                    if (requestedVersion >= v || v >= best)
                    {
                        continue;
                    }

                    best = v;
                    ibest = available;
                }
                else if (best < requestedVersion)
                {
                    if (best >= v)
                    {
                        continue;
                    }

                    best = v;
                    ibest = available;
                }
                else
                {
                    best = v;
                    ibest = available;
                }
            }

            return ibest;
        }

        private class MaxFirstComparer : IComparer<int>
        {
            public int Compare(int x, int y) => y.CompareTo(x);
        }
    }
}
