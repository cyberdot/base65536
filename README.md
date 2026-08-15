# base65536

An implementation of [base65536][1] encoding in C#.

Base65536 only uses "safe" Unicode code points (no unassigned code points, no
whitespace, no control characters), which means the encoded text is equally valid as UTF-8, UTF-16 or UTF-32.

## Usage

```csharp
using CyberDot.Encoding.Base65536;

var bytes = System.Text.Encoding.UTF8.GetBytes("hello world");
var encoded = Base65536.Encode(bytes); // Output: 驨ꍬ啯𒁷ꍲᕤ

var decoded = Base65536.Decode(encoded); // Output: hello world 
```

### Streaming

`ToBase65536Transform`/`FromBase65536Transform` implement
`ICryptoTransform`, so they plug into `CryptoStream` for streaming encode/decode without
buffering the whole payload in memory. They default to UTF-8, or accept any of the three UTF encodings
via an `Encoding` parameter.

```csharp
using System.Security.Cryptography;

using var output = new MemoryStream();
using (var cryptoStream = new CryptoStream(output, new ToBase65536Transform(), CryptoStreamMode.Write))
{
    sourceStream.CopyTo(cryptoStream);
}

using var decoded = new MemoryStream();
using (var cryptoStream = new CryptoStream(encodedStream, new FromBase65536Transform(), CryptoStreamMode.Read))
{
    cryptoStream.CopyTo(decoded);
}
```

## Credits
Javascript original implementation: [base65536](https://github.com/ferno/base65536).

## License

The MIT License (MIT)

[1]: https://github.com/qntm/base65536
