using System.Collections.Generic;
using System.IO;
using IonDotnet.Internals.Text;

namespace IonDotnet.Systems
{
    public static class IonTextWriterBuilder
    {
        /// <summary>
        /// Build an Ion text writer
        /// </summary>
        /// <param name="textWriter">Writer that can write to the output</param>
        /// <param name="imports">Symbol tables that the write can use to encode symbols.</param>
        /// <returns>Ion text writer</returns>
        public static IIonWriter Build(TextWriter textWriter, IEnumerable<ISymbolTable> imports = null)
        {
            return new IonTextWriter(textWriter, imports);
        }

        /// <summary>
        /// Build an Ion text writer
        /// </summary>
        /// <param name="textWriter">Writer that can write to the output</param>
        /// <param name="options">Text writer options</param>
        /// <param name="imports">Symbol tables that the write can use to encode symbols.</param>
        /// <returns>Ion text writer</returns>
        public static IIonWriter Build(TextWriter textWriter, IonTextOptions options, IEnumerable<ISymbolTable> imports = null)
        {
            return new IonTextWriter(textWriter, options, imports);
        }
    }
}
