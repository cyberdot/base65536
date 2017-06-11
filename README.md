﻿# base65536

An implementation of [base65536][1] in C#.

## Usage

```csharp
using CyberDot.Encoding.Base65536;

;

var encoded = Base65536.Encode("hello world"); // Output: 驨ꍬ啯𒁷ꍲᕤ

var decoded = Base65536.Decode(encoded); // Output: hello world 
```

## Credits
Javascript original implementation: [base65536](https://github.com/ferno/base65536).

## License

The MIT License (MIT)

[1]: https://github.com/qntm/base65536
