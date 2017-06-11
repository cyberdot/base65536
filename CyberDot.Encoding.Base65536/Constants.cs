namespace CyberDot.Encoding.Base65536
{
    internal static class Constants
    {
        public const int PaddingBlockStart = 5376;
        public const int PossibleBytes = 1 << 8;
        public const int BmpThreshold = 1 << 16;
        public const int High = 0xD800;
        public const int Low = 0xDC00;
        public const int Offset = 1 << 10;
    }
}