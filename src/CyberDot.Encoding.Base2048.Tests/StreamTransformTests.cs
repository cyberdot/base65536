using AwesomeAssertions;
using CyberDot.Encoding.Base2048.Tests.TheoryData;
using System.Security.Cryptography;
using TextEncoding = System.Text.Encoding;

namespace CyberDot.Encoding.Base2048.Tests;

public class StreamTransformTests
{
    private static readonly int[] ChunkSizes = { 1, 2, 3, 4, 5, 7, 11, 13, 64 };

    private static readonly TextEncoding[] SafeEncodings = { TextEncoding.UTF8, TextEncoding.Unicode, TextEncoding.UTF32 };

    private static byte[] EncodeViaCryptoStream(byte[] data, TextEncoding? encoding = null)
    {
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, new ToBase2048Transform(encoding), CryptoStreamMode.Write, leaveOpen: true))
        {
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    private static byte[] DecodeViaCryptoStream(byte[] bytes, TextEncoding? encoding = null)
    {
        using var ms = new MemoryStream(bytes);
        using var cs = new CryptoStream(ms, new FromBase2048Transform(encoding), CryptoStreamMode.Read);
        using var outMs = new MemoryStream();
        cs.CopyTo(outMs);
        return outMs.ToArray();
    }

    private static byte[] DriveTransform(ICryptoTransform transform, byte[] input, int chunkSize)
    {
        using var outMs = new MemoryStream();
        var i = 0;

        while (input.Length - i > chunkSize)
        {
            var buffer = new byte[chunkSize * 8 + 32];
            var written = transform.TransformBlock(input, i, chunkSize, buffer, 0);
            outMs.Write(buffer, 0, written);
            i += chunkSize;
        }

        var final = transform.TransformFinalBlock(input, i, input.Length - i);
        outMs.Write(final, 0, final.Length);

        return outMs.ToArray();
    }

    private static byte[] AsBytes(string value, TextEncoding? encoding = null) => (encoding ?? TextEncoding.UTF8).GetBytes(value);

    // --- Round trip against the existing fixtures (default: UTF-8) -------------------------------------------

    [Theory]
    [ClassData(typeof(PairsData))]
    public void Should_stream_encode_pairs_data(string filename, byte[] input, string expected)
    {
        EncodeViaCryptoStream(input).Should().BeEquivalentTo(AsBytes(expected), options => options, filename);
    }

    [Theory]
    [ClassData(typeof(PairsData))]
    public void Should_stream_decode_pairs_data(string filename, byte[] expected, string input)
    {
        DecodeViaCryptoStream(AsBytes(input)).Should().BeEquivalentTo(expected, options => options, filename);
    }

    [Theory]
    [ClassData(typeof(SingleBytesData))]
    public void Should_stream_encode_single_byte_data(string filename, byte[] input, string expected)
    {
        EncodeViaCryptoStream(input).Should().BeEquivalentTo(AsBytes(expected), options => options, filename);
    }

    [Theory]
    [ClassData(typeof(SingleBytesData))]
    public void Should_stream_decode_single_byte_data(string filename, byte[] expected, string input)
    {
        DecodeViaCryptoStream(AsBytes(input)).Should().BeEquivalentTo(expected, options => options, filename);
    }

    [Theory]
    [ClassData(typeof(BadValuesData))]
    public void Should_raise_exception_on_bad_input_when_streaming(string filename, string input)
    {
        var act = () => DecodeViaCryptoStream(AsBytes(input));

        act.Should().Throw<ArgumentException>(filename);
    }

    [Fact]
    public void Should_silently_truncate_when_dropped_bits_look_like_padding_when_streaming()
    {
        // Same known edge case covered in Base2048Tests: dropping the final padding
        // character off "every-byte" leaves dangling bits that coincidentally read as
        // valid padding, so this cannot throw. Verified here too so the streaming path
        // stays behaviourally consistent with Base2048.Decode.
        var badInput = DataReader.ReadTextFiles("bad")["every-byte.txt"];
        var original = DataReader.ReadBinFiles("pairs")["every-byte.bin"];

        var decoded = DecodeViaCryptoStream(AsBytes(badInput));

        decoded.Should().BeEquivalentTo(original.Take(original.Length - 1), options => options);
    }

    // --- Round trip across all three encodings ------------------------------------------------------------

    [Fact]
    public void Should_round_trip_pairs_data_across_all_encodings()
    {
        foreach (var encoding in SafeEncodings)
        {
            foreach (var item in new PairsData())
            {
                var input = (byte[])item[1];
                var expected = (string)item[2];

                EncodeViaCryptoStream(input, encoding)
                    .Should().BeEquivalentTo(AsBytes(expected, encoding), options => options, $"encoding {encoding.EncodingName}");

                DecodeViaCryptoStream(AsBytes(expected, encoding), encoding)
                    .Should().BeEquivalentTo(input, options => options, $"encoding {encoding.EncodingName}");
            }
        }
    }

    // --- Chunked feeding: exercises the persistent bit-accumulator state across calls ------------------------

    [Theory]
    [ClassData(typeof(PairsData))]
    public void Should_encode_correctly_regardless_of_chunk_size(string filename, byte[] input, string expected)
    {
        foreach (var encoding in SafeEncodings)
        {
            var expectedBytes = AsBytes(expected, encoding);

            foreach (var chunkSize in ChunkSizes)
            {
                DriveTransform(new ToBase2048Transform(encoding), input, chunkSize)
                    .Should().BeEquivalentTo(expectedBytes, options => options, $"chunk size {chunkSize}, encoding {encoding.EncodingName}, file {filename}");
            }
        }
    }

    [Theory]
    [ClassData(typeof(PairsData))]
    public void Should_decode_correctly_regardless_of_chunk_size(string filename, byte[] expected, string input)
    {
        foreach (var encoding in SafeEncodings)
        {
            var inputBytes = AsBytes(input, encoding);

            foreach (var chunkSize in ChunkSizes)
            {
                DriveTransform(new FromBase2048Transform(encoding), inputBytes, chunkSize)
                    .Should().BeEquivalentTo(expected, options => options, $"chunk size {chunkSize}, encoding {encoding.EncodingName}, file {filename}");
            }
        }
    }

    // --- Round trip fuzzing over random data of varying lengths -----------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(17)]
    [InlineData(256)]
    [InlineData(1000)]
    public void Should_round_trip_random_data_with_odd_chunk_sizes(int length)
    {
        var data = new byte[length];
        new Random(length + 1).NextBytes(data);

        foreach (var encoding in SafeEncodings)
        {
            foreach (var chunkSize in ChunkSizes)
            {
                var encoded = DriveTransform(new ToBase2048Transform(encoding), data, chunkSize);
                var decoded = DriveTransform(new FromBase2048Transform(encoding), encoded, chunkSize);

                decoded.Should().BeEquivalentTo(data, options => options, $"chunk size {chunkSize}, encoding {encoding.EncodingName}, length {length}");
            }
        }
    }

    // --- Transform metadata --------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SafeEncodingsMemberData))]
    public void ToBase2048Transform_should_expose_expected_metadata(TextEncoding encoding)
    {
        var transform = new ToBase2048Transform(encoding);

        transform.InputBlockSize.Should().Be(1);
        transform.OutputBlockSize.Should().BePositive();
        transform.CanTransformMultipleBlocks.Should().BeTrue();
        transform.CanReuseTransform.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SafeEncodingsMemberData))]
    public void FromBase2048Transform_should_expose_expected_metadata(TextEncoding encoding)
    {
        var transform = new FromBase2048Transform(encoding);

        transform.InputBlockSize.Should().Be(1);
        transform.OutputBlockSize.Should().Be(1);
        transform.CanTransformMultipleBlocks.Should().BeTrue();
        transform.CanReuseTransform.Should().BeTrue();
    }

    [Fact]
    public void Transforms_should_default_to_utf8_when_no_encoding_supplied()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };

        EncodeViaCryptoStream(data).Should().BeEquivalentTo(AsBytes(Base2048.Encode(data), TextEncoding.UTF8), options => options);
    }

    public static TheoryData<TextEncoding> SafeEncodingsMemberData()
    {
        var data = new TheoryData<TextEncoding>();
        foreach (var encoding in SafeEncodings) data.Add(encoding);
        return data;
    }

    // --- Error cases specific to streaming decode -----------------------------------------------------------

    [Fact]
    public void Should_throw_on_unrecognised_character()
    {
        var act = () => DecodeViaCryptoStream(AsBytes("#"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_throw_on_truncated_multi_byte_utf8_sequence_at_end_of_stream()
    {
        // caseDemo.txt's final character ('Ƽ', U+01BC) is confirmed non-ASCII (2 UTF-8
        // bytes), so chopping the last byte off its UTF-8 form leaves a genuinely
        // incomplete sequence rather than just removing a whole char - which is what
        // would happen if the final char were one of Base2048's ASCII code points
        // ('0'-'9', used by both the main and padding repertoires).
        var encoded = AsBytes(DataReader.ReadTextFiles("pairs")["caseDemo.txt"], TextEncoding.UTF8);
        var truncated = new byte[encoded.Length - 1];
        Array.Copy(encoded, truncated, truncated.Length);

        // Thrown as DecoderFallbackException, a subtype of ArgumentException.
        var act = () => DecodeViaCryptoStream(truncated);
        act.Should().ThrowExactly<System.Text.DecoderFallbackException>();
    }

    [Fact]
    public void Should_reuse_transform_instances_across_multiple_streams()
    {
        var toTransform = new ToBase2048Transform();
        var fromTransform = new FromBase2048Transform();

        var first = new byte[] { 1, 2, 3 };
        var second = new byte[] { 4, 5, 6, 7, 8 };

        foreach (var data in new[] { first, second })
        {
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, toTransform, CryptoStreamMode.Write, leaveOpen: true))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }

            using var decodedMs = new MemoryStream(ms.ToArray());
            using var decodeCs = new CryptoStream(decodedMs, fromTransform, CryptoStreamMode.Read);
            using var outMs = new MemoryStream();
            decodeCs.CopyTo(outMs);

            outMs.ToArray().Should().BeEquivalentTo(data, options => options);
        }
    }
}
