namespace Cerbot.Extensions
{
    public static class ByteExtensions
    {
        /// <summary>
        /// Copies all the elements of the current one-dimensional Array to the specified one-dimensional Array.
        /// </summary>
        /// <param name="bytes">Byte array source.</param>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from the current Array.</param>
        /// <param name="startIndex">A 32-bit integer that represents the index in array at which copying begins.</param>
        /// <param name="length">The number of bytes to copy into the array.</param>
        /// <returns></returns>
        public static void CopyTo(this byte[] bytes, byte[] array, int startIndex, int length)
        {
            for (var n = 0; n < length; n++)
            {
                array[n] = bytes[startIndex + n];
            }
        }
    }
}
