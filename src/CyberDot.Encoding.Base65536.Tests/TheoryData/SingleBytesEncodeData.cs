using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CyberDot.Encoding.Base65536.Tests.TheoryData
{
    public class SingleBytesEncodeData : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = DataReader.SingleBytesBinFiles.Select(kv => new object[]
            {
                kv.Key,
                kv.Value,
                System.Text.Encoding.UTF8.GetString(DataReader.SingleBytesTxtFiles[kv.Key.Replace("bin", "txt")])
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