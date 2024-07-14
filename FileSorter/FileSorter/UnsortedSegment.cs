namespace FileSorter;
internal class UnsortedSegment
{
    public UnsortedSegment(byte[] data, List<byte> tail, int lineCount)
    {
        Data = data;
        Tail = tail;
        LineCount = lineCount;
    }
    public byte[] Data { get; private set; }
    public List<byte> Tail { get; private set; }
    public int LineCount { get; private set; }
}