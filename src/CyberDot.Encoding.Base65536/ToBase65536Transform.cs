using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static CyberDot.Encoding.Base65536.Constants;

namespace CyberDot.Encoding.Base65536
{
    /// <summary>
    /// Streams raw bytes into their Base65536 representation.
    ///
    /// Base65536 only uses "safe" Unicode code points - no unassigned code points, no
    /// whitespace, no control characters - so the resulting text is equally valid as
    /// UTF-8, UTF-16 or UTF-32. This transform serialises that text using whichever of
    /// those <see cref="Encoding"/>s is supplied to the constructor (UTF-8 by default).
    /// Intended for use with <see cref="CryptoStream"/>.
    /// </summary>
    public class ToBase65536Transform : ICryptoTransform
    {
        private readonly System.Text.Encoding encoding;
        private byte? pendingByte;

        public ToBase65536Transform(System.Text.Encoding encoding = null)
        {
            this.encoding = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            OutputBlockSize = this.encoding.GetMaxByteCount(2);
        }

        public bool CanReuseTransform => true;
        public bool CanTransformMultipleBlocks => true;
        public int InputBlockSize => 2;
        public int OutputBlockSize { get; }

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
            pendingByte = null;
            return result;
        }

        private byte[] ProcessBytes(byte[] inputBuffer, int inputOffset, int inputCount, bool isFinal)
        {
            if (inputBuffer == null)
            {
                throw new ArgumentNullException(nameof(inputBuffer));
            }

            if (inputOffset < 0 || inputCount < 0 || inputOffset + inputCount > inputBuffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(inputCount));
            }

            var output = new List<byte>(inputCount * 2);
            var i = inputOffset;
            var end = inputOffset + inputCount;

            while (true)
            {
                byte p1;
                if (pendingByte.HasValue)
                {
                    p1 = pendingByte.Value;
                    pendingByte = null;
                }
                else if (i < end)
                {
                    p1 = inputBuffer[i];
                    i++;
                }
                else
                {
                    break;
                }

                int blockStart;
                if (i < end)
                {
                    blockStart = Base65536.EncodeMap[inputBuffer[i]];
                    i++;
                }
                else if (isFinal)
                {
                    blockStart = PaddingBlockStart;
                }
                else
                {
                    pendingByte = p1;
                    break;
                }

                WriteCodePoint(blockStart + p1, output);
            }

            return output.ToArray();
        }

        // A resolved code point is always a single valid Unicode scalar value (never a
        // lone surrogate), so it maps to exactly one well-formed char (BMP) or char pair
        // (supplementary plane), which any Encoding can serialise directly.
        private void WriteCodePoint(int codePoint, List<byte> output)
        {
            char[] chars;

            if (codePoint < BmpThreshold)
            {
                chars = new[] { (char)codePoint };
            }
            else
            {
                chars = new[]
                {
                    (char)(High + (codePoint - BmpThreshold) / Offset),
                    (char)(Low + (codePoint - BmpThreshold) % Offset)
                };
            }

            output.AddRange(encoding.GetBytes(chars));
        }
    }
}
