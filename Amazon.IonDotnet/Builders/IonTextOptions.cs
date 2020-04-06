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

namespace Amazon.IonDotnet.Builders
{
    using System;

    public class IonTextOptions
    {
        public static readonly IonTextOptions Default = new IonTextOptions();

        /// <summary>
        /// Gets or Sets a value indicating whether to use indented format.
        /// </summary>
        public bool PrettyPrint { get; set; }

        /// <summary>
        /// Gets or sets new-line sequence, default to system-specific sequence.
        /// </summary>
        public string LineSeparator { get; set; } = Environment.NewLine;

        /// <summary>
        /// Gets or sets a value indicating whether to write symbols as strings.
        /// </summary>
        public bool SymbolAsString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip annotations.
        /// </summary>
        public bool SkipAnnotations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all null values are written as 'null'.
        /// </summary>
        public bool UntypedNull { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Timestamps are written as milliseconds since Epoch.
        /// </summary>
        public bool TimestampAsMillis { get; set; }

        /// <summary>
        /// Gets or sets the maximum string length before it is wrapped.
        /// </summary>
        public int LongStringThreshold { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets a value indicating whether to write the Ion version marker.
        /// </summary>
        public bool WriteVersionMarker { get; set; }
    }
}
