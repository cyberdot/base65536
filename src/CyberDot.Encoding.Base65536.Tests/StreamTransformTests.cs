using System;
using System.IO;
using System.Security.Cryptography;
using AwesomeAssertions;
using CyberDot.Encoding.Base65536.Tests.TheoryData;
using Xunit;
using TextEncoding = System.Text.Encoding;

namespace CyberDot.Encoding.Base65536.Tests
{
    public class StreamTransformTests
    {
        private static readonly int[] ChunkSizes = [1, 2, 3, 4, 5, 7, 13, 64];

        // Base65536 only uses "safe" Unicode code points (no unassigned code points, no
        // whitespace, no control characters), so the resulting text is equally valid as
        // UTF-8, UTF-16 or UTF-32 - these are the three encodings the transforms support.
        private static readonly TextEncoding[] SafeEncodings = [TextEncoding.UTF8, TextEncoding.Unicode, TextEncoding.UTF32];

        private static byte[] EncodeViaCryptoStream(byte[] data, TextEncoding encoding = null)
        {
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, new ToBase65536Transform(encoding), CryptoStreamMode.Write, leaveOpen: true))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }

            return ms.ToArray();
        }

        private static byte[] DecodeViaCryptoStream(byte[] bytes, TextEncoding encoding = null, bool ignoreGarbage = false)
        {
            using var ms = new MemoryStream(bytes);
            using var cs = new CryptoStream(ms, new FromBase65536Transform(encoding, ignoreGarbage), CryptoStreamMode.Read);
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

        private static byte[] AsBytes(string value, TextEncoding encoding = null) => (encoding ?? TextEncoding.UTF8).GetBytes(value);

        // --- Round trip against the existing string-based fixtures (default: UTF-8) --------------------------

        [Theory]
        [ClassData(typeof(SingleBytesEncodeData))]
        public void Should_stream_encode_single_bytes_data(string _, byte[] input, string expected)
        {
            EncodeViaCryptoStream(input).Should().BeEquivalentTo(AsBytes(expected));
        }

        [Theory]
        [ClassData(typeof(DoubleBytesEncodeData))]
        public void Should_stream_encode_double_bytes_data(string _, byte[] input, string expected)
        {
            EncodeViaCryptoStream(input).Should().BeEquivalentTo(AsBytes(expected));
        }

        [Theory]
        [ClassData(typeof(SingleBytesDecodeData))]
        public void Should_stream_decode_single_bytes_data(string _, string input, byte[] expected)
        {
            DecodeViaCryptoStream(AsBytes(input)).Should().BeEquivalentTo(expected);
        }

        [Theory]
        [ClassData(typeof(DoubleBytesDecodeData))]
        public void Should_stream_decode_double_bytes_data(string _, string input, byte[] expected)
        {
            DecodeViaCryptoStream(AsBytes(input)).Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void Should_stream_encode_pairs_data()
        {
            foreach (var item in new PairsEncodeData())
            {
                var input = (byte[])item[1];
                var expected = (string)item[2];

                EncodeViaCryptoStream(input).Should().BeEquivalentTo(AsBytes(expected));
            }
        }

        [Fact]
        public void Should_stream_decode_pairs_data()
        {
            foreach (var item in new PairsDecodeData())
            {
                var input = (string)item[1];
                var expected = (byte[])item[2];

                DecodeViaCryptoStream(AsBytes(input)).Should().BeEquivalentTo(expected);
            }
        }

        [Theory]
        [ClassData(typeof(BadValuesData))]
        public void Should_raise_exception_on_bad_input_when_streaming(string _, string input)
        {
            var act = () => DecodeViaCryptoStream(AsBytes(input));
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [ClassData(typeof(IgnoreGarbageDecodeData))]
        public void Should_raise_exception_with_default_ignore_garbage_flag_value_when_streaming(string _, string input, byte[] expected)
        {
            var act = () => DecodeViaCryptoStream(AsBytes(input));
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [ClassData(typeof(IgnoreGarbageDecodeData))]
        public void Should_stream_decode_with_ignore_garbage_flag_value_set_to_true(string _, string input, byte[] expected)
        {
            DecodeViaCryptoStream(AsBytes(input), ignoreGarbage: true).Should().BeEquivalentTo(expected);
        }

        // --- Round trip across all three "safe" encodings -------------------------------------------------------

        [Fact]
        public void Should_round_trip_pairs_data_across_all_safe_encodings()
        {
            foreach (var encoding in SafeEncodings)
            {
                foreach (var item in new PairsEncodeData())
                {
                    var input = (byte[])item[1];
                    var expected = (string)item[2];

                    EncodeViaCryptoStream(input, encoding)
                        .Should().BeEquivalentTo(AsBytes(expected, encoding), $"encoding {encoding.EncodingName}");
                }

                foreach (var item in new PairsDecodeData())
                {
                    var input = (string)item[1];
                    var expected = (byte[])item[2];

                    DecodeViaCryptoStream(AsBytes(input, encoding), encoding)
                        .Should().BeEquivalentTo(expected, $"encoding {encoding.EncodingName}");
                }
            }
        }

        // --- Chunked feeding: exercises leftover-byte / split-multi-byte-sequence buffering across calls ---------

        [Theory]
        [ClassData(typeof(PairsEncodeData))]
        public void Should_encode_correctly_regardless_of_chunk_size(string filename, byte[] input, string expected)
        {
            foreach (var encoding in SafeEncodings)
            {
                var expectedBytes = AsBytes(expected, encoding);

                foreach (var chunkSize in ChunkSizes)
                {
                    DriveTransform(new ToBase65536Transform(encoding), input, chunkSize)
                        .Should().BeEquivalentTo(expectedBytes, $"chunk size {chunkSize}, encoding {encoding.EncodingName}, file {filename}");
                }
            }
        }

        [Theory]
        [ClassData(typeof(PairsDecodeData))]
        public void Should_decode_correctly_regardless_of_chunk_size(string filename, string input, byte[] expected)
        {
            foreach (var encoding in SafeEncodings)
            {
                var inputBytes = AsBytes(input, encoding);

                foreach (var chunkSize in ChunkSizes)
                {
                    DriveTransform(new FromBase65536Transform(encoding), inputBytes, chunkSize)
                        .Should().BeEquivalentTo(expected, $"chunk size {chunkSize}, encoding {encoding.EncodingName}, file {filename}");
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
        [InlineData(4097)]
        public void Should_round_trip_random_data_through_crypto_stream(int length)
        {
            var data = new byte[length];
            new Random(length).NextBytes(data);

            foreach (var encoding in SafeEncodings)
            {
                var encoded = EncodeViaCryptoStream(data, encoding);
                encoded.Should().BeEquivalentTo(AsBytes(Base65536.Encode(data), encoding), $"encoding {encoding.EncodingName}");

                var decoded = DecodeViaCryptoStream(encoded, encoding);
                decoded.Should().BeEquivalentTo(data, $"encoding {encoding.EncodingName}");
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(17)]
        [InlineData(256)]
        public void Should_round_trip_random_data_with_odd_chunk_sizes(int length)
        {
            var data = new byte[length];
            new Random(length + 1).NextBytes(data);

            foreach (var encoding in SafeEncodings)
            {
                foreach (var chunkSize in ChunkSizes)
                {
                    var encoded = DriveTransform(new ToBase65536Transform(encoding), data, chunkSize);
                    var decoded = DriveTransform(new FromBase65536Transform(encoding), encoded, chunkSize);

                    decoded.Should().BeEquivalentTo(data, $"chunk size {chunkSize}, encoding {encoding.EncodingName}, length {length}");
                }
            }
        }

        // --- Transform metadata --------------------------------------------------------------------------------

        [Theory]
        [MemberData(nameof(SafeEncodingsMemberData))]
        public void Should_expose_expected_metadata_on_encode(TextEncoding encoding)
        {
            var transform = new ToBase65536Transform(encoding);

            transform.InputBlockSize.Should().Be(2);
            transform.OutputBlockSize.Should().BeGreaterThanOrEqualTo(4);
            transform.CanTransformMultipleBlocks.Should().BeTrue();
            transform.CanReuseTransform.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(SafeEncodingsMemberData))]
        public void Should_expose_expected_metadata_on_decode(TextEncoding encoding)
        {
            var transform = new FromBase65536Transform(encoding);

            transform.InputBlockSize.Should().Be(1);
            transform.OutputBlockSize.Should().Be(1);
            transform.CanTransformMultipleBlocks.Should().BeTrue();
            transform.CanReuseTransform.Should().BeTrue();
        }

        [Fact]
        public void Should_default_to_utf8_when_no_encoding_supplied()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };

            EncodeViaCryptoStream(data).Should().BeEquivalentTo(AsBytes(Base65536.Encode(data), TextEncoding.UTF8));
        }

        public static TheoryData<TextEncoding> SafeEncodingsMemberData()
        {
            var data = new TheoryData<TextEncoding>();
            foreach (var encoding in SafeEncodings) data.Add(encoding);
            return data;
        }

        // --- Error cases specific to streaming decode -----------------------------------------------------------

        [Fact]
        public void Should_throw_when_sequence_continues_after_final_byte_while_streaming()
        {
            var encoded = Base65536.Encode([5]) + Base65536.Encode([7]);

            var act = () => DecodeViaCryptoStream(AsBytes(encoded));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_throw_on_dangling_high_surrogate_at_end_of_stream_with_utf16_encoding()
        {
            var bytes = AsBytes("\uD800", TextEncoding.Unicode);

            var act = () => DecodeViaCryptoStream(bytes, TextEncoding.Unicode);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_throw_when_high_surrogate_not_followed_by_low_surrogate_with_utf16_encoding()
        {
            var bytes = AsBytes("\uD800A", TextEncoding.Unicode);

            var act = () => DecodeViaCryptoStream(bytes, TextEncoding.Unicode);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_throw_on_truncated_multi_byte_utf8_sequence_at_end_of_stream()
        {
            // Every Base65536 code point is 3 or 4 bytes in UTF-8; chop the last byte off
            // so the final block ends mid-sequence.
            var encoded = AsBytes(Base65536.Encode([1, 2]), TextEncoding.UTF8);
            var truncated = new byte[encoded.Length - 1];
            Array.Copy(encoded, truncated, truncated.Length);

            // Thrown as DecoderFallbackException, a subtype of ArgumentException.
            var act = () => DecodeViaCryptoStream(truncated);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_throw_on_stray_utf8_continuation_byte()
        {
            var act = () => DecodeViaCryptoStream([0x80]);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_reuse_transform_instances_across_multiple_streams()
        {
            var toTransform = new ToBase65536Transform();
            var fromTransform = new FromBase65536Transform();

            var first = new byte[] { 1, 2, 3 };
            var second = new byte[] { 4, 5, 6, 7 };

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

                outMs.ToArray().Should().BeEquivalentTo(data);
            }
        }
    }
}
