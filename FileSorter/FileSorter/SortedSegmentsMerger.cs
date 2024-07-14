namespace FileSorter;

internal class SortedSegmentsMerger
{
    private readonly IComparer<string> _comparer;

    public SortedSegmentsMerger(IComparer<string> comparer)
    {
        _comparer = comparer;
    }

    public async Task MergeAsync(IReadOnlyList<string> sortedFiles, string target, CancellationToken cancellationToken)
    {
        var filesToMerge = new List<string>(sortedFiles);

        while (true)
        {
            if (filesToMerge.Count == 1)
            {
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                File.Move(filesToMerge[0], target);
                return;
            }

            var pairs = filesToMerge.Chunk(2).ToList();
            var nextFilesToMerge = new List<string>();

            await Parallel.ForEachAsync(pairs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken },
                async (pair, ct) =>
                {
                    if (pair.Length == 2)
                    {
                        var outFilename = $"{pair[0]}.tmp";
                        await MergeTwoFilesAsync(pair[0], pair[1], File.OpenWrite(outFilename), ct);
                        File.Move(pair[0], $"{pair[0]}.remove");
                        File.Move(pair[1], $"{pair[1]}.remove");
                        File.Move(outFilename, pair[0]);
                        File.Delete($"{pair[0]}.remove");
                        File.Delete($"{pair[1]}.remove");
                        lock (nextFilesToMerge)
                        {
                            nextFilesToMerge.Add(pair[0]);
                        }
                    }
                    else
                    {
                        lock (nextFilesToMerge)
                        {
                            nextFilesToMerge.Add(pair[0]);
                        }
                    }
                });

            filesToMerge.Clear();
            filesToMerge.AddRange(nextFilesToMerge);
        }
    }
    private async Task MergeTwoFilesAsync(string first, string second, Stream outputStream, CancellationToken ct)
    {
        await using var writer = new StreamWriter(outputStream, bufferSize: 65536);
        using var reader1 = new StreamReader(File.OpenRead(first), bufferSize: 65536);
        using var reader2 = new StreamReader(File.OpenRead(second), bufferSize: 65536);
        var line1 = await reader1.ReadLineAsync(ct);
        var line2 = await reader2.ReadLineAsync(ct);

        while (line1 != null || line2 != null)
        {
            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            if (line1 != null && line2 != null)
            {
                if (_comparer.Compare(line1, line2) <= 0)
                {
                    await writer.WriteLineAsync(line1);
                    line1 = await reader1.ReadLineAsync(ct);
                }
                else
                {
                    await writer.WriteLineAsync(line2);
                    line2 = await reader2.ReadLineAsync(ct);
                }
            }
            else if (line1 != null)
            {
                await writer.WriteLineAsync(line1);
                line1 = await reader1.ReadLineAsync(ct);
            }
            else if (line2 != null)
            {
                await writer.WriteLineAsync(line2);
                line2 = await reader2.ReadLineAsync(ct);
            }
        }
    }
}
