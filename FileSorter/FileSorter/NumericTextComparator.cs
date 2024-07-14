namespace FileSorter;
internal class NumericTextComparator : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null)
        {
            return 0;
        }
        if (x == null)
        {
            return -1;
        }

        if (y == null)
        {
            return 1;
        }
        (int xNumberPart, string xTextPart) = ExtractNumberAndText(x);
        (int yNumberPart, string yTextPart) = ExtractNumberAndText(y);

        var result = string.Compare(xTextPart, yTextPart, StringComparison.OrdinalIgnoreCase);
        if (result != 0)
        {
            return result;
        }

        return xNumberPart.CompareTo(yNumberPart);
    }

    private (int numberPart, string textPart) ExtractNumberAndText(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty");
        }

        int dotIndex = input.IndexOf('.');
        if (dotIndex == -1)
        {
            throw new ArgumentException("Input does not contain a separator character");
        }

        string numberPart = input.Substring(0, dotIndex);
        string textPart = input.Substring(dotIndex + 1);

        if (!int.TryParse(numberPart, out var number))
        {
            throw new ArgumentException("Left part must be a valid number");
        }

        return (number, textPart);
    }
}