# CyberDot.Encoding

Binary-to-text encoding implementations in C#.

| Package | NuGet | Description |
| --- | --- | --- |
| [CyberDot.Encoding.Base65536](src/CyberDot.Encoding.Base65536) | `CyberDot.Encoding.Base65536` | [Base65536][base65536-js] - packs 2 bytes per character using "safe" Unicode code points, so the encoded text is equally valid as UTF-8, UTF-16 or UTF-32. |
| [CyberDot.Encoding.Base2048](src/CyberDot.Encoding.Base2048) | `CyberDot.Encoding.Base2048` | [Base2048][base2048-js] - packs 11 bits per character, optimised for services with per-character length limits (e.g. Twitter). |

Both packages expose the same shape of API: a static `Encode`/`Decode` pair for one-shot
use, plus `ICryptoTransform` implementations (`To*Transform`/`From*Transform`) that plug
into `CryptoStream` for streaming encode/decode without buffering the whole payload in
memory.

## Usage

```csharp
using CyberDot.Encoding.Base65536;

var bytes = System.Text.Encoding.UTF8.GetBytes("hello world");
var encoded = Base65536.Encode(bytes); // Output: 驨ꍬ啯𒁷ꍲᕤ
var decoded = Base65536.Decode(encoded); // Output: hello world
```

```csharp
using CyberDot.Encoding.Base2048;

var bytes = System.Text.Encoding.UTF8.GetBytes("hello world");
var encoded = Base2048.Encode(bytes); // Output: ڵϠɲණæஊಢࢷ
var decoded = Base2048.Decode(encoded); // Output: hello world
```

See each package's own README for streaming usage and further details:
- [src/CyberDot.Encoding.Base65536/README.md](src/CyberDot.Encoding.Base65536/README.md)
- [src/CyberDot.Encoding.Base2048/README.md](src/CyberDot.Encoding.Base2048/README.md)

## Solution layout

```
CyberDot.Encoding.sln
src/
  CyberDot.Encoding.Base65536/        library
  CyberDot.Encoding.Base65536.Tests/  tests
  CyberDot.Encoding.Base2048/         library
  CyberDot.Encoding.Base2048.Tests/   tests
```

## Credits
- Base65536 JavaScript original: [qntm/base65536][base65536-js]
- Base2048 JavaScript original: [qntm/base2048][base2048-js]

## License

The MIT License (MIT)

[base65536-js]: https://github.com/qntm/base65536
[base2048-js]: https://github.com/qntm/base2048
