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
            sortCommand.AddOption(inputFileOption);
            sortCommand.SetHandler(async context =>
            {
                var inputPath = context.ParseResult.GetValueForOption(inputFileOption);
                var outPath = context.ParseResult.GetValueForOption(outFileOption);
                var token = context.GetCancellationToken();
                returnCode = await SortFile(inputPath!, outPath!, token);
            });

            rootCommand.AddCommand(createCommand);
            rootCommand.AddCommand(sortCommand);
            await rootCommand.InvokeAsync(args);
            return returnCode;
        }

        private static async Task<int> CreateTestFile(string output, int size, CancellationToken ct)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            try
            {
                await Task.Delay(5000, ct);
            }
            catch (OperationCanceledException)
            {
                await Console.Error.WriteLineAsync("The operation was aborted");
                return 1;
            }

            Console.WriteLine($"Test file '{output}' of size {size} MB generated.");
            return 0;
        }

        private static async Task<int> SortFile(string input, string output, CancellationToken ct)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));
            try
            {
                await Task.Delay(5000, ct);
            }
            catch (OperationCanceledException)
            {
                await Console.Error.WriteLineAsync("The operation was aborted");
                return 1;
            }
            Console.WriteLine($"File '{input}' sorted and saved to '{output}'.");
            return 0;
        }
    }
}