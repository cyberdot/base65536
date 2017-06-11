using System;
using System.Collections.Generic;
using static CyberDot.Encoding.Base65536.Constants;

namespace CyberDot.Encoding.Base65536
{
    public static class StringExtensions
    {
        private static int ConvertToCodePoint(string str, int i)
        {
            if (char.IsHighSurrogate(str[i]) || char.IsLowSurrogate(str[i]))
            {
                return str[i];
            }

            return char.ConvertToUtf32(str, i);
        }
        public static IEnumerable<int> ToCodePoints(this string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            var i = 0;
            while (i < str.Length)
            {
                var fst = ConvertToCodePoint(str, i);
                i++;

                if (High <= fst && fst < High + Offset)
                {
                    var snd = ConvertToCodePoint(str, i);
                    i++;

                    if (Low <= snd && snd < Low + Offset)
                    {
                        yield return (fst - High) * Offset + (snd - Low) + BmpThreshold;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid UTF 16");
                    }
                }
                else
                {
                    yield return fst;
                }
            }
        }
    }
}