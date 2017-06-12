using System;
using CyberDot.Encoding.Base65536.Tests.TheoryData;
using FluentAssertions;
using Xunit;

namespace CyberDot.Encoding.Base65536.Tests
{
    public class Base65536Tests
    {
        [Theory]
        [ClassData(typeof(BadValuesData))]
        public void Should_raise_exception_on_bad_input(string fileName, string input)
        {
            Assert.Throws<ArgumentException>(() => Base65536.Decode(input));
        }

        [Theory]
        [ClassData(typeof(DoubleBytesEncodeData))]
        public void Should_encode_double_bytes_data(string filename, byte[] input, string expected)
        {
            var encoded = Base65536.Encode(input);

            encoded.ShouldBeEquivalentTo(expected);
        }

        [Theory]
        [ClassData(typeof(DoubleBytesDecodeData))]
        public void Should_decode_double_bytes_data(string filename, string input, byte[] expected)
        {
            var decoded = Base65536.Decode(input);

            decoded.ShouldBeEquivalentTo(expected);
        }

        [Theory]
        [ClassData(typeof(IgnoreGarbageDecodeData))]
        public void Should_raise_exception_with_default_ignore_garbage_flag_value(string filename, string input, byte[] expected)
        {
            Assert.Throws<ArgumentException>(() => Base65536.Decode(input));
        }

        [Theory]
        [ClassData(typeof(IgnoreGarbageDecodeData))]
        public void Should_raise_exception_with_ignore_garbage_flag_value_set_to_false(string filename, string input, byte[] expected)
        {
            Assert.Throws<ArgumentException>(() => Base65536.Decode(input));
        }

        [Theory]
        [ClassData(typeof(IgnoreGarbageDecodeData))]
        public void Should_decode_with_ignore_garbage_flag_value_set_to_true(string filename, string input, byte[] expected)
        {
            var decoded = Base65536.Decode(input, true);

            decoded.ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public void Should_encode_pairs_data()
        {
            var encodeData = new PairsEncodeData();
            foreach (var item in encodeData)
            {
                var input = (byte[]) item[1];
                var expected = (string) item[2];
                var encoded = Base65536.Encode(input);

                encoded.ShouldBeEquivalentTo(expected);
            }
        }

        [Fact]
        public void Should_decode_pairs_data()
        {
            var decodeData = new PairsDecodeData();
            foreach (var item in decodeData)
            {
                var input = (string) item[1];
                var expected = (byte[]) item[2];
                var decoded = Base65536.Decode(input);

                decoded.ShouldBeEquivalentTo(expected);
            }
        }

        [Theory]
        [ClassData(typeof(SingleBytesEncodeData))]
        public void Should_encode_single_bytes_data(string filename, byte[] input, string expected)
        {
            var encoded = Base65536.Encode(input);

            encoded.ShouldBeEquivalentTo(expected);
        }

        [Theory]
        [ClassData(typeof(SingleBytesDecodeData))]
        public void Should_decode_single_bytes_data(string filename, string input, byte[] expected)
        {
            var decoded = Base65536.Decode(input);

            decoded.ShouldBeEquivalentTo(expected);
        }
    }
}