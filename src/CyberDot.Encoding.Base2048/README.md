# CyberDot.Encoding.Base2048

An implementation of [base2048][1] encoding in C#.

## Usage

```csharp
using CyberDot.Encoding.Base2048;

var bytes = System.Text.Encoding.UTF8.GetBytes("hello world");
var encoded = Base2048.Encode(bytes); // Output: ڵϠɲණæஊಢࢷ

var decoded = Base2048.Decode(encoded); // Output: hello world
```

### Streaming

`ToBase2048Transform`/`FromBase2048Transform` implement `ICryptoTransform`, so they plug
into `CryptoStream` for streaming encode/decode without buffering the whole payload in
memory. They default to UTF-8, or accept any `Encoding` via the constructor.

```csharp
using System.Security.Cryptography;

using var output = new MemoryStream();
using (var cryptoStream = new CryptoStream(output, new ToBase2048Transform(), CryptoStreamMode.Write))
{
    sourceStream.CopyTo(cryptoStream);
}

using var decoded = new MemoryStream();
using (var cryptoStream = new CryptoStream(encodedStream, new FromBase2048Transform(), CryptoStreamMode.Read))
{
    cryptoStream.CopyTo(decoded);
}
```

## Credits
Javascript original implementation: [base2048](https://github.com/qntm/base2048).

## License

The MIT License (MIT)

[1]: https://github.com/qntm/base2048
