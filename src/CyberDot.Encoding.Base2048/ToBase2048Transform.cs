using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static CyberDot.Encoding.Base2048.Constants;

namespace CyberDot.Encoding.Base2048
{
    /// <summary>
    /// Streams raw bytes into their Base2048 representation, serialised using whichever
    /// <see cref="Encoding"/> is supplied to the constructor (UTF-8 by default). Intended
    /// for use with <see cref="CryptoStream"/>.
    /// </summary>
    public class ToBase2048Transform : ICryptoTransform
    {
        private readonly System.Text.Encoding encoding;

        // Bit accumulator state, carried across TransformBlock calls: unlike a byte-pair
        // scheme, 8 (bits per byte) never divides evenly into 11 (bits per char), so
        // almost every input byte leaves a partial character pending.
        private int z;
        private int numZBits;

        public ToBase2048Transform(System.Text.Encoding encoding = null)
        {
            this.encoding = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            OutputBlockSize = this.encoding.GetMaxByteCount(1);
        }

        public bool CanReuseTransform => true;
        public bool CanTransformMultipleBlocks => true;
        public int InputBlockSize => 1;
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
            z = 0;
            numZBits = 0;
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

            var output = new List<byte>(inputCount);
            var end = inputOffset + inputCount;

            for (var i = inputOffset; i < end; i++)
            {
                var b = inputBuffer[i];

                // Take most significant bit first.
                for (var j = BitsPerByte - 1; j >= 0; j--)
                {
                    var bit = (b >> j) & 1;

                    z = (z << 1) + bit;
                    numZBits++;

                    if (numZBits == BitsPerChar)
                    {
                        WriteChar(Base2048.EncodeMap[numZBits][z], output);
                        z = 0;
                        numZBits = 0;
                    }
                }
            }

            if (isFinal && numZBits != 0)
            {
                // Final bits require special treatment: pad `z` out with 1s until its
                // width matches a known repertoire (11 bits, then 3 bits), then encode
                // as normal against that repertoire.
                while (!Base2048.EncodeMap.ContainsKey(numZBits))
                {
                    z = (z << 1) + 1;
                    numZBits++;
                }

                WriteChar(Base2048.EncodeMap[numZBits][z], output);
                z = 0;
                numZBits = 0;
            }

            return output.ToArray();
        }

        private void WriteChar(char ch, List<byte> output)
        {
            output.AddRange(encoding.GetBytes(new[] { ch }));
        }
    }
}
