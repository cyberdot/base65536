using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static CyberDot.Encoding.Base65536.Constants;

namespace CyberDot.Encoding.Base65536
{
    /// <summary>
    /// Streams a Base65536 sequence back into the original raw bytes.
    ///
    /// Base65536 only uses "safe" Unicode code points - no unassigned code points, no
    /// whitespace, no control characters - so the input text may validly have been
    /// serialised as UTF-8, UTF-16 or UTF-32. Pass the matching <see cref="Encoding"/> to
    /// the constructor (UTF-8 by default). Intended for use with <see cref="CryptoStream"/>.
    /// </summary>
    public class FromBase65536Transform : ICryptoTransform
    {
        private readonly bool ignoreGarbage;
        private readonly Decoder decoder;
        private char? pendingHighSurrogate;
        private bool done;

        public FromBase65536Transform(System.Text.Encoding encoding = null, bool ignoreGarbage = false)
        {
            this.ignoreGarbage = ignoreGarbage;
            decoder = (encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)).GetDecoder();
        }

        public bool CanReuseTransform => true;
        public bool CanTransformMultipleBlocks => true;
        public int InputBlockSize => 1;
        public int OutputBlockSize => 1;

        public void Dispose()
        {
        }

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
            pendingHighSurrogate = null;
            done = false;
        }

        // The Decoder buffers any incomplete multibyte sequence internally across calls
        // (and, with a throwing Encoding, rejects malformed/truncated input), so we never
        // need to track leftover bytes ourselves here - only the resulting chars.
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
                HandleUnit(chars[i], output);
            }

            if (isFinal && pendingHighSurrogate.HasValue)
            {
                throw new ArgumentException("Invalid UTF 16");
            }

            return output.ToArray();
        }

        private void HandleUnit(char unit, List<byte> output)
        {
            if (pendingHighSurrogate.HasValue)
            {
                var high = pendingHighSurrogate.Value;
                pendingHighSurrogate = null;

                if (Low <= unit && unit < Low + Offset)
                {
                    var codePoint = (high - High) * Offset + (unit - Low) + BmpThreshold;
                    HandleCodePoint(codePoint, output);
                }
                else
                {
                    throw new ArgumentException("Invalid UTF 16");
                }
            }
            else if (High <= unit && unit < High + Offset)
            {
                pendingHighSurrogate = unit;
            }
            else
            {
                HandleCodePoint(unit, output);
            }
        }

        private void HandleCodePoint(int codePoint, List<byte> output)
        {
            var p1 = codePoint & (PossibleBytes - 1);
            var blockStart = codePoint - p1;

            if (blockStart == PaddingBlockStart)
            {
                if (done)
                {
                    throw new ArgumentException("Base65536 sequence continued after final byte");
                }

                output.Add((byte)p1);
                done = true;
            }
            else if (Base65536.DecodeMap.TryGetValue(blockStart, out var p2))
            {
                if (done)
                {
                    throw new ArgumentException("Base65536 sequence continued after final byte");
                }

                output.Add((byte)p1);
                output.Add((byte)p2);
            }
            else if (!ignoreGarbage)
            {
                throw new ArgumentException("Not a valid base65536 code point " + codePoint);
            }
        }
    }
}
