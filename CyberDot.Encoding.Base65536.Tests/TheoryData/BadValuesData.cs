using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CyberDot.Encoding.Base65536.Tests.TheoryData
{
    public class BadValuesData : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = DataReader.BadInputFiles
            .Select(kv => new object[]
            {
                kv.Key,
                System.Text.Encoding.UTF8.GetString(kv.Value)
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