using System.Collections.Concurrent;

namespace FileSorter
{
    internal class UnsortedSegmentConsumer
    {
        private readonly BlockingCollection<UnsortedSegment> _segments;
        private readonly BlockingCollection<string> _sortedFileNames;
        private readonly string _workFolder;
        private readonly string _fileNamePrefix;
        private readonly int _consumerId;
        private readonly FileNumberProvider _fileNumberProvider;
        private readonly IComparer<string> _comparer;

        public UnsortedSegmentConsumer(
            BlockingCollection<UnsortedSegment> segments, 
            BlockingCollection<string> sortedFileNames, 
            string workFolder, 
            string fileNamePrefix, 
            int consumerId, 
            FileNumberProvider fileNumberProvider, 
            IComparer<string> comparer)
        {
            _segments = segments;
            _sortedFileNames = sortedFileNames;
            _workFolder = workFolder;
            _fileNamePrefix = fileNamePrefix;
            _consumerId = consumerId;
            _fileNumberProvider = fileNumberProvider;
            _comparer = comparer;
        }

        public void Consume()
        {
            foreach (var segment in _segments.GetConsumingEnumerable())
            {
                var chunkFileName = Path.Combine(_workFolder, $"{_fileNamePrefix}_{_fileNumberProvider.GetNextNumber()}");
                using MemoryStream ms = new MemoryStream(segment.Data.Length + segment.Tail.Count);
                {
                    ms.Write(segment.Data);
                    if (segment.Tail.Count > 0)
                    {
                        ms.Write(segment.Tail.ToArray(), 0, segment.Tail.Count);
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    var lines = new string[segment.LineCount];

                    using var streamReader = new StreamReader(ms);
                    int index = 0;
                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();
                        if (line != null)
                        {
                            lines[index++] = line;
                        }
                    }

                    Array.Sort(lines, 0, index, _comparer);
                    File.WriteAllLines(chunkFileName, lines);
                    _sortedFileNames.Add(chunkFileName);
#if DEBUG
                    Console.WriteLine($"Consumer {_consumerId} consumed segment with line count: {segment.LineCount}. Saved to file {chunkFileName}");
#endif
                }
            }
        }
    }
}