﻿/*
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
using System.Text;
using Amazon.IonDotnet.Internals.Binary;
using Amazon.IonDotnet.Internals.Text;
using Amazon.IonDotnet.Internals.Tree;
using Amazon.IonDotnet.Tree;

namespace Amazon.IonDotnet.Builders
{
    public enum ReaderFormat
    {
        Detect,
        Binary,
        Text
    }

    public struct ReaderOptions
    {
        public ReaderFormat Format;
        public Encoding Encoding;
        public ICatalog Catalog;
    }

    /// <summary>
    /// Builder that can generate <see cref="IIonReader"/> instances for different types of input. 
    /// </summary>
    /// <remarks>
    /// Note that Ion readers work with pre-created input streams, so there is no method that accept a byte[]. Callers are
    /// responsible for creating the stream and disposing them. 
    /// </remarks>
    public static class IonReaderBuilder
    {
        /// <summary>
        /// Build a text reader for the string with a catalog.
        /// </summary>
        /// <param name="text">Ion text</param>
        /// <param name="options">Reader options.</param>
        /// <returns>Ion text reader</returns>
        public static IIonReader Build(string text, ReaderOptions options = default)
        {
            return new UserTextReader(text, options.Catalog);
        }

        public static IIonReader Build(IIonValue value, ReaderOptions options = default)
        {
            return new UserTreeReader(value, options.Catalog);
        }

        public static IIonReader Build(byte[] data, ReaderOptions options = default)
        {
            return Build(new MemoryStream(data), options);
        }

        public static IIonReader Build(Stream stream, ReaderOptions options = default)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            if (options.Encoding is null)
            {
                options.Encoding = Encoding.UTF8;
            }

            switch (options.Format)
            {
                default:
                    throw new ArgumentOutOfRangeException();
                case ReaderFormat.Detect:
                    return DetectFormatAndBuild(stream, options);
                case ReaderFormat.Binary:
                    return new UserBinaryReader(stream, options.Catalog);
                case ReaderFormat.Text:
                    return new UserTextReader(stream, options.Catalog);
            }
        }

        private static IIonReader DetectFormatAndBuild(Stream stream, in ReaderOptions options)
        {
            /* Notes about implementation
               The stream can contain text or binary ion. The ion reader should figure it out. Since we don't want this call to block 
               in case the stream is a network stream or file stream, it can (and should )
               return a wrapper which peeks the stream and check for Bvm, and delegate the rest to the approriate reader.
               Special case is when the stream is a memory stream which can be read directly, in which case we can do the Bvm checking right away.
               Also the Bvm might not be neccessary for the binary reader (except maybe for checking Ion version) so we might end up passing the 
               already-read stream to the binary reader. 
            */

            //this is the dumbed down implementation
            Span<byte> initialBytes = stackalloc byte[BinaryConstants.BinaryVersionMarkerLength];
            var bytesRead = stream.Read(initialBytes);
            var didSeek = stream.CanSeek;
            if (didSeek)
            {
                try
                {
                    stream.Seek(-bytesRead, SeekOrigin.Current);
                }
                catch (IOException)
                {
                    didSeek = false;
                }
            }

            if (IsBinaryData(initialBytes.Slice(0, bytesRead)))
            {
                //skipping the version marker should be fine for binary reader
                return new UserBinaryReader(stream, options.Catalog);
            }

            return didSeek
                ? new UserTextReader(stream, options.Encoding, options.Catalog)
                : new UserTextReader(stream, options.Encoding, initialBytes.Slice(0, bytesRead), options.Catalog);
        }

        private static bool IsBinaryData(Span<byte> initialByte)
        {
            //progressively check the binary version marker
            return initialByte.Length >= 4
                   && initialByte[0] == 0xE0
                   && initialByte[1] == 0x01
                   && initialByte[2] == 0x00
                   && initialByte[3] == 0xEA;
        }
    }
}
