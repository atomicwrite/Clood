using System.Text.RegularExpressions;

public class GitIgnoreCompiler
{
    public class CompileResult
    {
        private Regex[] positives;
        private Regex[] negatives;

        public CompileResult(Regex[] positives, Regex[] negatives)
        {
            this.positives = positives;
            this.negatives = negatives;
        }

        public bool Accepts(string input)
        {
            if (input.StartsWith("/")) input = input.Substring(1);
            return negatives[0].IsMatch(input) || !positives[0].IsMatch(input);
        }

        public bool Denies(string input)
        {
            if (input.StartsWith("/")) input = input.Substring(1);
            return !(negatives[0].IsMatch(input) || !positives[0].IsMatch(input));
        }

        public bool Maybe(string input)
        {
            if (input.StartsWith("/")) input = input.Substring(1);
            return positives[1].IsMatch(input) && !negatives[1].IsMatch(input);
        }
    }

    public static CompileResult Compile(string content)
    {
        var parsed = Parse(content);
        return new CompileResult(parsed.Item1, parsed.Item2);
    }

    private static Tuple<Regex[], Regex[]> Parse(string content)
    {
        var lines = content.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
            .ToList();

        var lists = lines.Aggregate(
            new Tuple<List<string>, List<string>>(new List<string>(), new List<string>()),
            (acc, line) =>
            {
                var isNegative = line.StartsWith("!");
                if (isNegative) line = line.Substring(1);
                if (line.StartsWith("/")) line = line.Substring(1);

                if (isNegative)
                    acc.Item2.Add(line);
                else
                    acc.Item1.Add(line);

                return acc;
            });

        Func<List<string>, Regex[]> prepareRegexes = list =>
        {
            var prepared = list.OrderBy(x => x)
                .Select(pattern => new[]
                {
                    PrepareRegexPattern(pattern),
                    PreparePartialRegex(pattern)
                })
                .Aggregate(new[] { new List<string>(), new List<string>() },
                    (acc, item) =>
                    {
                        acc[0].Add(item[0]);
                        acc[1].Add(item[1]);
                        return acc;
                    });

            return new[]
            {
                prepared[0].Count > 0 ? new Regex($"^(({string.Join(")|(", prepared[0])}))", RegexOptions.Compiled) : new Regex("$^", RegexOptions.Compiled),
                prepared[1].Count > 0 ? new Regex($"^(({string.Join(")|(", prepared[1])}))", RegexOptions.Compiled) : new Regex("$^", RegexOptions.Compiled)
            };
        };

        return Tuple.Create(prepareRegexes(lists.Item1), prepareRegexes(lists.Item2));
    }

    private static string PrepareRegexPattern(string pattern)
    {
        return Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".");
    }

    private static string PreparePartialRegex(string pattern)
    {
        return string.Join("", pattern.Split('/')
            .Select((item, index) => index > 0
                ? $"(/{PrepareRegexPattern(item)})?"
                : $"({PrepareRegexPattern(item)})"));
    }
}