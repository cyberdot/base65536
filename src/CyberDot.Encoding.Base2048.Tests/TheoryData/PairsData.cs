using System.Collections;

namespace CyberDot.Encoding.Base2048.Tests.TheoryData;

/// <summary>
/// Round-trip fixtures from test-data/pairs: each case is (filename, raw bytes, expected
/// Base2048 string).
/// </summary>
public sealed class PairsData : IEnumerable<object[]>
{
    private static readonly List<object[]> Data = Build("pairs");

    internal static List<object[]> Build(string relativeFolder)
    {
        var binFiles = DataReader.ReadBinFiles(relativeFolder);
        var textFiles = DataReader.ReadTextFiles(relativeFolder);

        var result = new List<object[]>();
        foreach (var (name, bytes) in binFiles)
        {
            var textName = Path.ChangeExtension(name, ".txt");
            result.Add(new object[] { name, bytes, textFiles[textName] });
        }

        return result;
    }

    public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
