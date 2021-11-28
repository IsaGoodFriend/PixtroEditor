using DSDecmp.Formats.Nitro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DSDecmp
{
    /// <summary>
    /// Utility class for compression using LZ-like compression schemes.
    /// </summary>
    public static class LZUtil
	{
		private static LZ10 lzCompression = new LZ10();

		public static byte[] Compress(byte[] array)
		{
			using (var memStream = new MemoryStream(array))
			{
				using (var compressStream = new MemoryStream())
				{
					LZ10.LookAhead = false;
					lzCompression.Compress(memStream, memStream.Length, compressStream);
					var noLook = compressStream.ToArray();

					memStream.Position = 0;
					compressStream.SetLength(0);

					LZ10.LookAhead = true;
					lzCompression.Compress(memStream, memStream.Length, compressStream);

					var lookAhead = compressStream.ToArray();

					if (noLook.Length == lookAhead.Length)
					{
						return noLook;
					}
					else if (noLook.Length > lookAhead.Length)
					{
						return lookAhead;
					}
					else
					{
						return noLook;
					}
				}
			}
		}
		public static byte[] Decompress(byte[] array)
		{
			using (var memStream = new MemoryStream(array))
			{
				return Decompress(memStream);
			}
		}
		public static byte[] Decompress(MemoryStream memStream)
		{
			return Decompress(memStream).ToArray();
		}
		public static byte[] Decompress(Stream stream)
		{
			return DecompressToStream(stream).ToArray();
		}

		/// <summary>
		/// Decompresses LZ77-compressed data from the given input stream.
		/// </summary>
		/// <param name="input">The input stream to read from.</param>
		/// <returns>The decompressed data.</returns>
		public static MemoryStream DecompressToStream(Stream input)
		{
			int max = 0;
			BinaryReader reader = new BinaryReader(input);

			int start = (int)input.Position;

			// Check LZ77 type.
			if (reader.ReadByte() != 0x10)
				throw new ArgumentException("Input stream does not contain LZ77-compressed data.", "input");

			// Read the size.
			int size = reader.ReadUInt16() | (reader.ReadByte() << 16);


			var output = new MemoryStream(size);

			// Begin decompression.
			while (output.Length < size)
			{
				// Load flags for the next 8 blocks.
				int flagByte = reader.ReadByte();

				// Process the next 8 blocks.
				for (int i = 0; i < 8; i++)
				{
					// Check if the block is compressed.
					if ((flagByte & (0x80 >> i)) == 0)
					{
						// Uncompressed block; copy single byte.
						output.WriteByte(reader.ReadByte());

						max = Math.Max((int)input.Position, max);
					}
					else
					{
						// Compressed block; read block.
						ushort block = reader.ReadUInt16();
						max = Math.Max((int)input.Position, max);

						// Get byte count.
						int count = ((block >> 4) & 0xF) + 3;
						// Get displacement.
						int disp = ((block & 0xF) << 8) | ((block >> 8) & 0xFF);

						// Save current position and copying position.
						long outPos = output.Position;
						long copyPos = output.Position - disp - 1;

						// Copy all bytes.
						for (int j = 0; j < count; j++)
						{
							// Read byte to be copied.
							output.Position = copyPos++;
							byte b = (byte)output.ReadByte();

							// Write byte to be copied.
							output.Position = outPos++;
							output.WriteByte(b);
						}
					}

					// If all data has been decompressed, stop.
					if (output.Length >= size)
					{
						break;
					}
				}
			}

			output.Position = 0;
			return output;
		}

		/// <summary>
		/// Determine the maximum size of a LZ-compressed block starting at newPtr, using the already compressed data
		/// starting at oldPtr. Takes O(inLength * oldLength) = O(n^2) time.
		/// </summary>
		/// <param name="newPtr">The start of the data that needs to be compressed.</param>
		/// <param name="newLength">The number of bytes that still need to be compressed.
		/// (or: the maximum number of bytes that _may_ be compressed into one block)</param>
		/// <param name="oldPtr">The start of the raw file.</param>
		/// <param name="oldLength">The number of bytes already compressed.</param>
		/// <param name="disp">The offset of the start of the longest block to refer to.</param>
		/// <param name="minDisp">The minimum allowed value for 'disp'.</param>
		/// <returns>The length of the longest sequence of bytes that can be copied from the already decompressed data.</returns>
		public static unsafe int GetOccurrenceLength(byte* newPtr, int newLength, byte* oldPtr, int oldLength, out int disp, int minDisp = 1)
        {
            disp = 0;
            if (newLength == 0)
                return 0;
            int maxLength = 0;
            // try every possible 'disp' value (disp = oldLength - i)
            for (int i = 0; i < oldLength - minDisp; i++)
            {
                // work from the start of the old data to the end, to mimic the original implementation's behaviour
                // (and going from start to end or from end to start does not influence the compression ratio anyway)
                byte* currentOldStart = oldPtr + i;
                int currentLength = 0;
                // determine the length we can copy if we go back (oldLength - i) bytes
                // always check the next 'newLength' bytes, and not just the available 'old' bytes,
                // as the copied data can also originate from what we're currently trying to compress.
                for (int j = 0; j < newLength; j++)
                {
                    // stop when the bytes are no longer the same
                    if (*(currentOldStart + j) != *(newPtr + j))
                        break;
                    currentLength++;
                }

                // update the optimal value
                if (currentLength > maxLength)
                {
                    maxLength = currentLength;
                    disp = oldLength - i;

                    // if we cannot do better anyway, stop trying.
                    if (maxLength == newLength)
                        break;
                }
            }
            return maxLength;
        }
    }
}
