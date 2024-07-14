using System.Collections.Concurrent;
using System.CommandLine;

namespace FileSorter
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var returnCode = 0;

            var outFileOption = new Option<string>(
                name: "--output",
                description: "The path to the output file")
            { IsRequired = true };
            outFileOption.AddAlias("-o");

            var rootCommand = new RootCommand("An application for sorting large text files");
            rootCommand.AddGlobalOption(outFileOption);

            var createCommand = new Command("create", "Create a test text file");
            var fileSizeOption = new Option<int>(
                name: "--size",
                description: "The size of the test file in MB")
            { IsRequired = true };
            fileSizeOption.AddAlias("-s");
            createCommand.AddOption(fileSizeOption);
            createCommand.SetHandler(async context =>
            {
                var size = context.ParseResult.GetValueForOption(fileSizeOption);
                var path = context.ParseResult.GetValueForOption(outFileOption);
                var token = context.GetCancellationToken();
                returnCode = await CreateTestFile(path!, size, token);
            });

            var sortCommand = new Command("sort", "Sort a text file");
            var inputFileOption = new Option<string>(
                name: "--input",
                description: "The path to the input file")
            { IsRequired = true };
            inputFileOption.AddAlias("-i");
            
            var workFolderOption = new Option<string>(
                    name: "--workFolder",
                    description: "The path to the work directory")
                { IsRequired = true };
            workFolderOption.AddAlias("-wf");

            sortCommand.AddOption(inputFileOption);
            sortCommand.AddOption(workFolderOption);
            sortCommand.SetHandler(async context =>
            {
                var inputPath = context.ParseResult.GetValueForOption(inputFileOption);
                var outPath = context.ParseResult.GetValueForOption(outFileOption);
                var workFolder = context.ParseResult.GetValueForOption(workFolderOption);
                var token = context.GetCancellationToken();
                returnCode = await SortFile(inputPath!, outPath!, workFolder!, token);
            });

            rootCommand.AddCommand(createCommand);
            rootCommand.AddCommand(sortCommand);
            await rootCommand.InvokeAsync(args);
            return returnCode;
        }

        private static async Task<int> CreateTestFile(string output, int size, CancellationToken ct)
        {
            if (size is < 1 or > 100 * 1024 * 2024)
            {
                await Console.Error.WriteLineAsync($"{nameof(size)} is out of range");
                return -1;
            }

            try
            {
                RandomTextFileGenerator textFileGenerator = new RandomTextFileGenerator();
                await textFileGenerator.Generate(File.OpenWrite(output), size * 1024 * 1024, ct);
            }
            catch (OperationCanceledException)
            {
                await Console.Error.WriteLineAsync("The operation was aborted");
                await Clear(output);
                return 1;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.Message);
                await Clear(output);
                return -1;
            }

            Console.WriteLine($"Test file '{output}' of size {size} MB generated.");
            return 0;
        }

        private static async Task<int> SortFile(string input, string output, string workFolder, CancellationToken ct)
        {
            bool needToDeleteWorkFolder = false;
            try
            {
                needToDeleteWorkFolder = EnsureEmptyFolder(workFolder);
                var sortedSegments = await SplitFileToSortedSegmentsAsync(workFolder, File.OpenRead(input), ct);
                Console.WriteLine(sortedSegments.Count);
            }
            catch (OperationCanceledException)
            {
                await Console.Error.WriteLineAsync("The operation was aborted");
                await Clear(output);
                return 1;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.Message);
                await Clear(output);
                return -1;
            }
            finally
            {
                if (needToDeleteWorkFolder)
                {
                    await Clear(workFolder);
                }
            }

            Console.WriteLine($"File '{input}' sorted and saved to '{output}'.");
            return 0;
        }

        private static async Task<List<string>> SplitFileToSortedSegmentsAsync(string workFolder, Stream sourceStream, CancellationToken ct)
        {
            int threadCount = Environment.ProcessorCount;
            var unsortedSegmentsQueue = new BlockingCollection<UnsortedSegment>(boundedCapacity: threadCount);
            var sortedFileNames = new BlockingCollection<string>();
            var consumerTasks = new Task[threadCount];
            var fileNumProvider = new FileNumberProvider();
            for (int i = 0; i < threadCount; i++)
            {
                var consumerId = i;
                UnsortedSegmentConsumer consumer = new UnsortedSegmentConsumer(unsortedSegmentsQueue, sortedFileNames, workFolder,
                    "input", consumerId, fileNumProvider, new NumericTextComparator());
                consumerTasks[i] = Task.Run(() => consumer.Consume(), ct);
            }

            var producer = new UnsortedSegmentProducer(unsortedSegmentsQueue, 2 * 1024 * 1024, sourceStream);
            await producer.ProduceAsync(ct);
            await Task.WhenAll(consumerTasks);
            return sortedFileNames.ToList();
        }

        private static bool EnsureEmptyFolder(string workFolder)
        {
            if (Directory.Exists(workFolder))
            {
                if (Directory.GetFiles(workFolder).Length > 0 || Directory.GetDirectories(workFolder).Length > 0)
                {
                    throw new InvalidOperationException("The folder must be empty.");
                }
            }
            else
            {
                Directory.CreateDirectory(workFolder);
                return true;
            }

            return false;
        }

        private static async Task Clear(string pathToDelete)
        {
            try
            {
                if (File.Exists(pathToDelete))
                {
                    File.Delete(pathToDelete);
                }
                else if (Directory.Exists(pathToDelete))
                {
                    Directory.Delete(pathToDelete, true);
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Unable to delete: {pathToDelete}. Error: {ex.Message}");
            }
        }
    }
}