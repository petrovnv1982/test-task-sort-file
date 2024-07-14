namespace FileSorter;
internal class FileNumberProvider
{
    private int _currentIndex;
    private readonly object _locker = new();

    public int GetNextNumber()
    {
        lock (_locker)
        {
            var result = _currentIndex;
            _currentIndex++;
            return result;
        }
    }
}
