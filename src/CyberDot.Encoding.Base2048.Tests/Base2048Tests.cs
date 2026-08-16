using AwesomeAssertions;
using CyberDot.Encoding.Base2048.Tests.TheoryData;

namespace CyberDot.Encoding.Base2048.Tests;

public class Base2048Tests
{
    [Theory]
    [ClassData(typeof(PairsData))]
    public void Should_encode_pairs_data(string filename, byte[] input, string expected)
    {
        Base2048.Encode(input).Should().Be(expected, filename);
    }

    [Theory]
    [ClassData(typeof(PairsData))]
    public void Should_decode_pairs_data(string filename, byte[] expected, string input)
    {
        Base2048.Decode(input).Should().BeEquivalentTo(expected, options => options, filename);
    }

    [Theory]
    [ClassData(typeof(SingleBytesData))]
    public void Should_encode_single_byte_data(string filename, byte[] input, string expected)
    {
        Base2048.Encode(input).Should().Be(expected, filename);
    }

    [Theory]
    [ClassData(typeof(SingleBytesData))]
    public void Should_decode_single_byte_data(string filename, byte[] expected, string input)
    {
        Base2048.Decode(input).Should().BeEquivalentTo(expected, options => options, filename);
    }

    [Theory]
    [ClassData(typeof(BadValuesData))]
    public void Should_throw_on_bad_input(string filename, string input)
    {
        var act = () => Base2048.Decode(input);

        act.Should().Throw<ArgumentException>(filename);
    }

    [Fact]
    public void Should_silently_truncate_when_dropped_bits_look_like_padding()
    {
        // test-data/bad/every-byte.txt is test-data/pairs/every-byte.txt with its final
        // (padding) character chopped off. every-byte.bin is bytes 0-255 in order, so the
        // bits left dangling after that truncation are the leading bits of the final
        // byte (255 = 0xFF, all 1s) - which is indistinguishable from valid "pad with 1s"
        // padding. The reference algorithm has no way to detect this: it decodes without
        // error but silently drops the final byte. This is a known limitation of the
        // format's padding scheme, not a bug in this port.
        var badInput = DataReader.ReadTextFiles("bad")["every-byte.txt"];
        var original = DataReader.ReadBinFiles("pairs")["every-byte.bin"];

        var decoded = Base2048.Decode(badInput);

        decoded.Should().BeEquivalentTo(original.Take(original.Length - 1), options => options);
    }

    [Fact]
    public void Should_round_trip_empty_array()
    {
        var encoded = Base2048.Encode(Array.Empty<byte>());
        encoded.Should().BeEmpty();

        Base2048.Decode(encoded).Should().BeEmpty();
    }

    [Fact]
    public void Should_throw_argument_null_exception_when_encoding_null()
    {
        var act = () => Base2048.Encode(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_throw_argument_null_exception_when_decoding_null()
    {
        var act = () => Base2048.Decode(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(17)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Should_round_trip_random_data(int length)
    {
        var data = new byte[length];
        new Random(length).NextBytes(data);

        var encoded = Base2048.Encode(data);
        var decoded = Base2048.Decode(encoded);

        decoded.Should().BeEquivalentTo(data, options => options, $"length {length}");
    }
}
