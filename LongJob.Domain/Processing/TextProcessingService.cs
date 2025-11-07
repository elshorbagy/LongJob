using System.Text;

namespace LongJob.Domain.Processing;

public static class TextProcessingService
{
    public static string BuildOutput(string input)
    {
        var grouped = input
            .GroupBy(c => c)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key}{g.Count()}");

        var left = string.Concat(grouped);
        var right = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        return $"{left}/{right}";
    }
}
