using System;
using System.Collections.Generic;
using System.Text;
using static CyberDot.Encoding.Base2048.Constants;

namespace CyberDot.Encoding.Base2048
{
    public static class Base2048
    {
        // Compressed representation of inclusive ranges of characters used in this
        // encoding, ported directly from the reference implementation. Every two
        // characters denote the first and last code point of an inclusive range.
        private static readonly string[] PairStrings =
        {
            "89AZazÆÆÐÐØØÞßææððøøþþĐđĦħııĸĸŁłŊŋŒœŦŧƀƟƢƮƱǃǝǝǤǥǶǷȜȝȠȥȴʯͰͳͶͷͻͽͿͿΑΡΣΩαωϏϏϗϯϳϳϷϸϺϿЂЂЄІЈЋЏИКикяђђєіјћџѵѸҁҊӀӃӏӔӕӘәӠӡӨөӶӷӺԯԱՖաֆאתװײؠءاؿفي٠٩ٮٯٱٴٹڿہہۃےەەۮۼۿۿܐܐܒܯݍޥޱޱ߀ߪࠀࠕࡀࡘࡠࡪࢠࢴࢶࢽऄनपरलळवहऽऽॐॐॠॡ०९ॲঀঅঌএঐওনপরললশহঽঽৎৎৠৡ০ৱ৴৹ৼৼਅਊਏਐਓਨਪਰਲਲਵਵਸਹੜੜ੦੯ੲੴઅઍએઑઓનપરલળવહઽઽૐૐૠૡ૦૯ૹૹଅଌଏଐଓନପରଲଳଵହଽଽୟୡ୦୯ୱ୷ஃஃஅஊஎஐஒஓககஙசஜஜஞடணதநபமஹௐௐ௦௲అఌఎఐఒనపహఽఽౘౚౠౡ౦౯౸౾ಀಀಅಌಎಐಒನಪಳವಹಽಽೞೞೠೡ೦೯ೱೲഅഌഎഐഒഺഽഽൎൎൔൖ൘ൡ൦൸ൺൿඅඖකනඳරලලවෆ෦෯กะาาเๅ๐๙ກຂຄຄງຈຊຊຍຍດທນຟມຣລລວວສຫອະາາຽຽເໄ໐໙ໞໟༀༀ༠༳ཀགངཇཉཌཎདནབམཛཝཨཪཬྈྌကဥဧဪဿ၉ၐၕ",
            "07",
        };

        // numZBits -> repertoire, indexed by the numeric value z (0-based).
        internal static readonly Dictionary<int, char[]> EncodeMap;

        // character -> (numZBits: Key, z: Value)
        internal static readonly Dictionary<char, KeyValuePair<int, int>> DecodeMap;

        static Base2048()
        {
            EncodeMap = new Dictionary<int, char[]>();
            DecodeMap = new Dictionary<char, KeyValuePair<int, int>>();

            for (var r = 0; r < PairStrings.Length; r++)
            {
                var repertoire = new List<char>();

                var pairString = PairStrings[r];
                for (var i = 0; i < pairString.Length; i += 2)
                {
                    var first = pairString[i];
                    var last = pairString[i + 1];

                    for (var codePoint = first; codePoint <= last; codePoint++)
                    {
                        repertoire.Add((char)codePoint);
                    }
                }

                var numZBits = BitsPerChar - BitsPerByte * r; // 0 -> 11, 1 -> 3
                var repertoireArray = repertoire.ToArray();
                EncodeMap[numZBits] = repertoireArray;

                for (var z = 0; z < repertoireArray.Length; z++)
                {
                    DecodeMap[repertoireArray[z]] = new KeyValuePair<int, int>(numZBits, z);
                }
            }
        }

        public static string Encode(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var result = new StringBuilder();
            var z = 0;
            var numZBits = 0;

            foreach (var b in data)
            {
                // Take most significant bit first.
                for (var j = BitsPerByte - 1; j >= 0; j--)
                {
                    var bit = (b >> j) & 1;

                    z = (z << 1) + bit;
                    numZBits++;

                    if (numZBits == BitsPerChar)
                    {
                        result.Append(EncodeMap[numZBits][z]);
                        z = 0;
                        numZBits = 0;
                    }
                }
            }

            if (numZBits != 0)
            {
                // Final bits require special treatment: pad `z` out with 1s until its
                // width matches a known repertoire (11 bits, then 3 bits), then encode
                // as normal against that repertoire.
                while (!EncodeMap.ContainsKey(numZBits))
                {
                    z = (z << 1) + 1;
                    numZBits++;
                }

                result.Append(EncodeMap[numZBits][z]);
            }

            return result.ToString();
        }

        public static byte[] Decode(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var length = data.Length;

            // This length is a guess. There's a chance we allocate one more byte here
            // than we actually need, so we slice it off at the end if so.
            var bytes = new byte[length * BitsPerChar / BitsPerByte];
            var numBytes = 0;
            var currentByte = 0;
            var numByteBits = 0;

            for (var i = 0; i < length; i++)
            {
                var chr = data[i];

                if (!DecodeMap.TryGetValue(chr, out var entry))
                {
                    throw new ArgumentException("Unrecognised Base2048 character: " + chr);
                }

                var numZBits = entry.Key;
                var z = entry.Value;

                if (numZBits != BitsPerChar && i != length - 1)
                {
                    throw new ArgumentException("Secondary character found before end of input at position " + i);
                }

                // Take most significant bit first.
                for (var j = numZBits - 1; j >= 0; j--)
                {
                    var bit = (z >> j) & 1;

                    currentByte = (currentByte << 1) + bit;
                    numByteBits++;

                    if (numByteBits == BitsPerByte)
                    {
                        bytes[numBytes] = (byte)currentByte;
                        numBytes++;
                        currentByte = 0;
                        numByteBits = 0;
                    }
                }
            }

            // Final padding bits! Requires special consideration.
            // We always pad with 1s, so what's left over must be all 1s too.
            if (currentByte != (1 << numByteBits) - 1)
            {
                throw new ArgumentException("Padding mismatch");
            }

            if (numBytes == bytes.Length)
            {
                return bytes;
            }

            var result = new byte[numBytes];
            Array.Copy(bytes, result, numBytes);
            return result;
        }
    }
}
