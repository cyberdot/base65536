using System.Collections;

namespace CyberDot.Encoding.Base2048.Tests.TheoryData;

/// <summary>
/// Round-trip fixtures from test-data/pairs/single-bytes: one case per possible byte
/// value (0-255), each as (filename, raw bytes, expected Base2048 string).
/// </summary>
public sealed class SingleBytesData : IEnumerable<object[]>
{
    private static readonly List<object[]> Data = PairsData.Build("pairs/single-bytes");

    public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
