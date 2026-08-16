using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static CyberDot.Encoding.Base2048.Constants;

namespace CyberDot.Encoding.Base2048
{
    /// <summary>
    /// Streams a Base2048 sequence back into the original raw bytes. Pass the
    /// <see cref="Encoding"/> the input was serialised with (UTF-8 by default). Intended
    /// for use with <see cref="CryptoStream"/>.
    /// </summary>
    public class FromBase2048Transform : ICryptoTransform
    {
        private readonly Decoder decoder;

        // Byte accumulator state, carried across TransformBlock calls.
        private int currentByte;
        private int numByteBits;

        // A repertoire-1 (3-bit, "special") character is only valid as the very last
        // character of the whole stream. Once we've consumed one, seeing any further
        // character at all is an error.
        private bool sawSpecialChar;

        public FromBase2048Transform(System.Text.Encoding encoding = null)
        {
            decoder = (encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)).GetDecoder();
        }

        public bool CanReuseTransform => true;
        public bool CanTransformMultipleBlocks => true;
        public int InputBlockSize => 1;
        public int OutputBlockSize => 1;

        public void Dispose() { }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            var output = ProcessBytes(inputBuffer, inputOffset, inputCount, isFinal: false);
            Array.Copy(output, 0, outputBuffer, outputOffset, output.Length);
            return output.Length;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var result = ProcessBytes(inputBuffer, inputOffset, inputCount, isFinal: true);
            Reset();
            return result;
        }

        private void Reset()
        {
            decoder.Reset();
            currentByte = 0;
            numByteBits = 0;
            sawSpecialChar = false;
        }

        // The Decoder buffers any incomplete multi-byte sequence internally across calls
        // (and, with a throwing Encoding, rejects malformed/truncated input), so we only
        // need to track our own bit-accumulator state here, not leftover input bytes.
        private byte[] ProcessBytes(byte[] inputBuffer, int inputOffset, int inputCount, bool isFinal)
        {
            if (inputBuffer == null)
            {
                throw new ArgumentNullException(nameof(inputBuffer));
            }

            var maxChars = decoder.GetCharCount(inputBuffer, inputOffset, inputCount, isFinal);
            var chars = maxChars == 0 ? Array.Empty<char>() : new char[maxChars];
            var charCount = decoder.GetChars(inputBuffer, inputOffset, inputCount, chars, 0, isFinal);

            var output = new List<byte>(charCount);

            for (var i = 0; i < charCount; i++)
            {
                HandleChar(chars[i], output);
            }

            if (isFinal && currentByte != (1 << numByteBits) - 1)
            {
                throw new ArgumentException("Padding mismatch");
            }

            return output.ToArray();
        }

        private void HandleChar(char ch, List<byte> output)
        {
            if (!Base2048.DecodeMap.TryGetValue(ch, out var entry))
            {
                throw new ArgumentException("Unrecognised Base2048 character: " + ch);
            }

            if (sawSpecialChar)
            {
                throw new ArgumentException("Secondary character found before end of input");
            }

            var numZBits = entry.Key;
            var z = entry.Value;

            if (numZBits != BitsPerChar)
            {
                sawSpecialChar = true;
            }

            // Take most significant bit first.
            for (var j = numZBits - 1; j >= 0; j--)
            {
                var bit = (z >> j) & 1;

                currentByte = (currentByte << 1) + bit;
                numByteBits++;

                if (numByteBits == BitsPerByte)
                {
                    output.Add((byte)currentByte);
                    currentByte = 0;
                    numByteBits = 0;
                }
            }
        }
    }
}
