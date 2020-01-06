using System.IO;
using IonDotnet.Internals;
using IonDotnet.Tree;
using IonDotnet.Tree.Impl;

namespace IonDotnet.Builders
{
    public class IonLoader
    {
        /// <summary>
        /// The default Ion loader without any catalog.
        /// </summary>
        public static readonly IonLoader Default = new IonLoader(default);

        private readonly ReaderOptions _readerOptions;

        public static IonLoader WithReaderOptions(in ReaderOptions readerOptions)
        {
            return new IonLoader(readerOptions);
        }

        private IonLoader(ReaderOptions options)
        {
            _readerOptions = options;
        }

        /// <summary>
        /// Load a string of Ion text.
        /// </summary>
        /// <param name="ionText">Ion text string.</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(string ionText)
        {
            var reader = IonReaderBuilder.Build(ionText, _readerOptions);
            return WriteDatagram(reader);
        }

        /// <summary>
        /// Load a string of Ion text.
        /// </summary>
        /// <param name="ionText">Ion text string.</param>
        /// <param name="readerTable">Reader's local symbol table.</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(string ionText, out ISymbolTable readerTable)
        {
            var reader = IonReaderBuilder.Build(ionText, _readerOptions);
            var dg = WriteDatagram(reader);
            readerTable = reader.GetSymbolTable();
            return dg;
        }

        //        /// <summary>
        //        /// Load Ion data from a byte buffer. Detecting whether it's binary or Unicode text Ion.
        //        /// </summary>
        //        /// <param name="data">Byte buffer.</param>
        //        /// <returns>An <see cref="IonDatagram"/> tree view.</returns>
        //        public IonDatagram Load(Span<byte> data)
        //        {
        //            throw new NotImplementedException();
        //        }

        /// <summary>
        /// Load Ion data from stream.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(Stream stream)
        {
            var reader = IonReaderBuilder.Build(stream, _readerOptions);
            var dg = WriteDatagram(reader);
            return dg;
        }

        /// <summary>
        /// Load Ion data from stream.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="readerTable">Reader's local symbol table.</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(Stream stream, out ISymbolTable readerTable)
        {
            var reader = IonReaderBuilder.Build(stream, _readerOptions);
            var dg = WriteDatagram(reader);
            readerTable = reader.GetSymbolTable();
            return dg;
        }

        /// <summary>
        /// Load Ion data from a file. Detecting whether it's binary or Unicode text Ion.
        /// </summary>
        /// <param name="ionFile">Ion file.</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(FileInfo ionFile)
        {
            if (!ionFile.Exists)
            {
                throw new FileNotFoundException("File must exist", ionFile.FullName);
            }

            using (var stream = ionFile.OpenRead())
            {
                var datagram = Load(stream);
                return datagram;
            }
        }

        /// <summary>
        /// Load Ion data from a file. Detecting whether it's binary or Unicode text Ion.
        /// </summary>
        /// <param name="ionFile">Ion file.</param>
        /// <param name="readerTable">The local table used by the reader.</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(FileInfo ionFile, out ISymbolTable readerTable)
        {
            using (var stream = ionFile.OpenRead())
            {
                var datagram = Load(stream, out readerTable);
                return datagram;
            }
        }

        /// <summary>
        /// Load Ion data from a byte array.
        /// </summary>
        /// <param name="data">Ion data.</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var datagram = Load(stream);
                return datagram;
            }
        }

        /// <summary>
        /// Load Ion data from a byte array.
        /// </summary>
        /// <param name="data">Ion data.</param>
        /// <param name="readerTable">The local table used by the reader.</param>
        /// <returns>An <see cref="IIonValue"/> tree view, which is an instance of IIonDatagram</returns>
        public IIonValue Load(byte[] data, out ISymbolTable readerTable)
        {
            using (var stream = new MemoryStream(data))
            {
                var datagram = Load(stream, out readerTable);
                return datagram;
            }
        }

        private static IIonValue WriteDatagram(IIonReader reader)
        {
            var datagram = new IonDatagram();
            var writer = new IonTreeWriter(datagram);
            writer.WriteValues(reader);
            return datagram;
        }
    }
}
