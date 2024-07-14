using System.Collections.Concurrent;

namespace FileSorter
{
    internal class UnsortedSegmentProducer
    {
        private readonly BlockingCollection<UnsortedSegment> _segments;
        private readonly int _fileSize;
        private readonly Stream _sourceStream;
        private readonly char _lineSeparator = Environment.NewLine[^1];  //for some OS line separator contains 2 chars, we take last

        public UnsortedSegmentProducer(BlockingCollection<UnsortedSegment> segments, int fileSize, Stream sourceStream)
        {
            _segments = segments;
            _fileSize = fileSize;
            _sourceStream = sourceStream;
        }

        public async Task ProduceAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[_fileSize];
            await using (_sourceStream)
            {
                while (_sourceStream.Position < _sourceStream.Length)
                {
                    var tail = new List<byte>();
                    cancellationToken.ThrowIfCancellationRequested();
                    var lineCount = 0;
                    var index = 0;
                    while (index < _fileSize)
                    {
                        var value = _sourceStream.ReadByte();
                        if (value == -1)
                        {
                            break;
                        }
                        var byteValue = (byte)value;
                        buffer[index] = byteValue;
                        index++;
                        if (byteValue == _lineSeparator)
                        {
                            lineCount++;
                        }
                    }

                    //We need to append the tail of the line to the same file.
                    if (buffer[index - 1] != _lineSeparator)
                    {
                        while (true)
                        {
                            var value = _sourceStream.ReadByte();
                            if (value == -1)
                            {
                                break;
                            }
                            var byteValue = (byte)value;
                            tail.Add(byteValue);
                            if (byteValue == _lineSeparator)
                            {
                                break;
                            }
                        }
                        lineCount++;
                    }
                    var segmentData = new byte[index];
                    Array.Copy(buffer, segmentData, segmentData.Length);
                    var unsortedSegment = new UnsortedSegment(segmentData, tail, lineCount);
                    _segments.Add(unsortedSegment, cancellationToken);
                }
                _segments.CompleteAdding();
            }
        }
    }
}
