using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CyberDot.Encoding.Base65536.Tests.TheoryData
{
    public static class DataReader
    {
        private static ReadOnlyDictionary<string, byte[]> doubleBytesBinFiles;
        private static ReadOnlyDictionary<string, byte[]> doubleBytesTxtFiles;
        private static ReadOnlyDictionary<string, byte[]> badInputFiles;
        private static ReadOnlyDictionary<string, byte[]> ignoreGarbageBinFiles;
        private static ReadOnlyDictionary<string, byte[]> ignoreGarbageTxtFiles;
        private static ReadOnlyDictionary<string, byte[]> pairsTxtFiles;
        private static ReadOnlyDictionary<string, byte[]> pairsBinFiles;
        private static ReadOnlyDictionary<string, byte[]> singleBytesBinFiles;
        private static ReadOnlyDictionary<string, byte[]> singleBytesTxtFiles;

        public static Tuple<string, byte[]> Read(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            
            return new Tuple<string, byte[]>(fileInfo.Name, File.ReadAllBytes(filePath));
        }
        public static ReadOnlyDictionary<string, byte[]> ReadAllIn(string folderPath)
        {
            var files = Directory.GetFiles(folderPath);
            return new ReadOnlyDictionary<string, byte[]>(
                files.Select(Read).ToDictionary(k => k.Item1, v => v.Item2));
        }

        public static ReadOnlyDictionary<string, byte[]> DoubleBytesBinFiles
        {
            get
            {
                if (doubleBytesBinFiles == null)
                {
                    doubleBytesBinFiles = ReadAllIn(Path.Combine("DoubledBytes", "binary"));
                }
                return doubleBytesBinFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> DoubleBytesTxtFiles
        {
            get
            {
                if (doubleBytesTxtFiles == null)
                {
                    doubleBytesTxtFiles = ReadAllIn(Path.Combine("DoubledBytes", "text"));
                }
                return doubleBytesTxtFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> IgnoreGarbageBinFiles
        {
            get
            {
                if (ignoreGarbageBinFiles == null)
                {
                    ignoreGarbageBinFiles = ReadAllIn(Path.Combine("IgnoreGarbage", "binary"));
                }
                return ignoreGarbageBinFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> IgnoreGarbageTxtFiles
        {
            get
            {
                if (ignoreGarbageTxtFiles == null)
                {
                    ignoreGarbageTxtFiles = ReadAllIn(Path.Combine("IgnoreGarbage", "text"));
                }
                return ignoreGarbageTxtFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> PairsTxtFiles
        {
            get
            {
                if (pairsTxtFiles == null)
                {
                    pairsTxtFiles = ReadAllIn(Path.Combine("Pairs", "text"));
                }
                return pairsTxtFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> PairsBinFiles
        {
            get
            {
                if (pairsBinFiles == null)
                {
                    pairsBinFiles = ReadAllIn(Path.Combine("Pairs", "binary"));
                }
                return pairsBinFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> SingleBytesBinFiles
        {
            get
            {
                if (singleBytesBinFiles == null)
                {
                    singleBytesBinFiles = ReadAllIn(Path.Combine("SingleBytes", "binary"));
                }
                return singleBytesBinFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> SingleBytesTxtFiles
        {
            get
            {
                if (singleBytesTxtFiles == null)
                {
                    singleBytesTxtFiles = ReadAllIn(Path.Combine("SingleBytes", "text"));
                }
                return singleBytesTxtFiles;
            }
        }
        public static ReadOnlyDictionary<string, byte[]> BadInputFiles
        {
            get
            {
                if (badInputFiles == null)
                {
                    badInputFiles = ReadAllIn("Bad");
                }
                return badInputFiles;
            }
        }
    }
}