using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CyberDot.Encoding.Base65536.Tests.TheoryData
{
    public class PairsDecodeData : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = DataReader.PairsTxtFiles.Select(kv => new object[]
            {
                kv.Key,
                System.Text.Encoding.UTF8.GetString(kv.Value),
                DataReader.PairsBinFiles[kv.Key.Replace("txt", "bin")]
            })
            .ToList();


        public IEnumerator<object[]> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}