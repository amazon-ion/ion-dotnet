using System;
using System.IO;

namespace IonDotnet.Utils
{
    public static class StreamExtension
    {
        /// <summary>
        /// Attempts to get the data written to a <see cref="MemoryStream"/> as efficiently as possible.
        /// </summary>
        /// <param name="memStream">Memory stream</param>
        /// <returns>Byte array of the written data</returns>
        public static byte[] GetWrittenBuffer(this MemoryStream memStream)
        {
#if NETSTANDARD1_3
            return memStream.ToArray();
#else
            try
            {
                var buffer = memStream.GetBuffer();
                if (buffer.Length == memStream.Length)
                {
                    //this means the buffer is correctly set to the written data by setting MemoryBuffer.Capacity
                    return buffer;
                }

                return memStream.ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                //No access to the underlying buffer
                return memStream.ToArray();
            }
#endif
        }
        
        public static void CheckStreamCanWrite(this Stream outputStream)
        {
            if (!outputStream.CanWrite)
                throw new ArgumentException("Output stream must be writable", nameof(outputStream));
        }
    }
}
