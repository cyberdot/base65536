using System.Collections;

namespace CyberDot.Encoding.Base2048.Tests.TheoryData;

/// <summary>
/// Malformed Base2048 strings from test-data/bad: each case is (filename, input string)
/// and is expected to fail decoding.
///
/// "every-byte.txt" is deliberately excluded: it truncates the final (padding) character
/// off an otherwise-valid encoding, and by coincidence the bits left dangling happen to
/// read as valid "all 1s" padding, so decoding it does not - and per the reference
/// algorithm's design, cannot - throw. That case is covered separately in
/// Base2048Tests.Should_silently_truncate_when_dropped_bits_look_like_padding.
/// </summary>
public sealed class BadValuesData : IEnumerable<object[]>
{
    private static readonly List<object[]> Data = DataReader.ReadTextFiles("bad")
        .Where(kv => kv.Key != "every-byte.txt")
        .Select(kv => new object[] { kv.Key, kv.Value })
        .ToList();

    public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
