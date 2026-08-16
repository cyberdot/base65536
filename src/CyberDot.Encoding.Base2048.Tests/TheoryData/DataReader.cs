using System.Collections.ObjectModel;

namespace CyberDot.Encoding.Base2048.Tests.TheoryData;

internal static class DataReader
{
    private static readonly string TestDataRoot = Path.Combine(AppContext.BaseDirectory, "test-data");

    public static ReadOnlyDictionary<string, byte[]> ReadBinFiles(string relativeFolder)
    {
        var folder = Path.Combine(TestDataRoot, relativeFolder);
        var files = new Dictionary<string, byte[]>();

        foreach (var file in Directory.GetFiles(folder, "*.bin"))
        {
            files[Path.GetFileName(file)] = File.ReadAllBytes(file);
        }

        return new ReadOnlyDictionary<string, byte[]>(files);
    }

    public static ReadOnlyDictionary<string, string> ReadTextFiles(string relativeFolder)
    {
        var folder = Path.Combine(TestDataRoot, relativeFolder);
        var files = new Dictionary<string, string>();

        foreach (var file in Directory.GetFiles(folder, "*.txt"))
        {
            // A couple of the "bad" fixtures pick up a trailing newline from the editor
            // that produced them; strip it so it isn't mistaken for encoded content.
            var text = File.ReadAllText(file, System.Text.Encoding.UTF8).TrimEnd('\r', '\n');
            files[Path.GetFileName(file)] = text;
        }

        return new ReadOnlyDictionary<string, string>(files);
    }
}
