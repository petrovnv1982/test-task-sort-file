namespace FileSorter
{
    internal class RandomTextFileGenerator
    {
        private readonly Random _random = new Random();

        public async Task Generate(Stream outStream, long maxSize, CancellationToken ct)
        {
            await using var outputWriter = new StreamWriter(outStream);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var number = _random.Next(1,10000);
                var text = GenerateRandomText(150);
                string line = $"{number}.{text}";
                await outputWriter.WriteLineAsync(line);
                if (outStream.Position > maxSize)
                {
                    break;
                }
            }
        }
        
        private string GenerateRandomText(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var textArray = new char[length];
            for (int i = 0; i < length; i++)
            {
                textArray[i] = chars[_random.Next(chars.Length)];
            }
            return new string(textArray);
        }
    }
}
