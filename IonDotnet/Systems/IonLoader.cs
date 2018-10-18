using System;
using System.IO;
using System.Text;
using IonDotnet.Internals;
using IonDotnet.Internals.Text;
using IonDotnet.Tree;

namespace IonDotnet.Systems
{
    public class IonLoader
    {
        /// <summary>
        /// The default Ion loader without any catalog.
        /// </summary>
        public static readonly IonLoader Default = new IonLoader();

        public static IonLoader FromCatalog(ICatalog catalog)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load a string of Ion text.
        /// </summary>
        /// <param name="ionText">Ion text string.</param>
        /// <returns>An <see cref="IonDatagram"/> tree view.</returns>
        public IonDatagram Load(string ionText)
        {
            var reader = new UserTextReader(ionText);
            return WriteDatagram(reader);
        }

        /// <summary>
        /// Load Ion data from a byte buffer. Detecting whether it's binary or Unicode text Ion.
        /// </summary>
        /// <param name="data">Byte buffer.</param>
        /// <returns>An <see cref="IonDatagram"/> tree view.</returns>
        public IonDatagram Load(Span<byte> data)
        {
            throw new NotImplementedException();
        }

        public IonDatagram Load(Stream stream)
        {
            var reader = IonReaderBuilder.Build(stream);
            return WriteDatagram(reader);
        }

        /// <summary>
        /// Load Ion data from a stream. Detecting whether it's binary or Unicode text Ion.
        /// </summary>
        /// <param name="stream">Byte stream</param>
        /// <param name="encoding">Type of text encoding used.</param>
        /// <returns>An <see cref="IonDatagram"/> tree view.</returns>
        /// <remarks>This method does not own the stream and the caller is resposible for disposing it.</remarks>
        public IonDatagram Load(Stream stream, Encoding encoding)
        {
            var reader = IonReaderBuilder.Build(stream, encoding);
            return WriteDatagram(reader);
        }

        public IonDatagram Load(FileInfo ionFile)
        {
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
        /// <param name="encoding">The type of text encoding used.</param>
        /// <returns>An <see cref="IonDatagram"/> tree view.</returns>
        public IonDatagram Load(FileInfo ionFile, Encoding encoding)
        {
            using (var stream = ionFile.OpenRead())
            {
                var datagram = Load(stream, encoding);
                return datagram;
            }
        }

        private static IonDatagram WriteDatagram(IIonReader reader)
        {
            var datagram = new IonDatagram();
            var writer = new IonTreeWriter(datagram);
            writer.WriteValues(reader);
            return datagram;
        }
    }
}
