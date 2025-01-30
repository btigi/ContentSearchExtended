using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Drawing;

var builder = new ConfigurationBuilder()
                .AddJsonFile($"cse.json", true, true);

var config = builder.Build();
var fileForegroundColour = Color.FromName(config["FileForegroundColour"] ?? "Yellow");
var fileBackgroundColour = Color.FromName(config["FileBackgroundColour"] ?? "Black");

var fileForegroundConsoleColour = FromColour(fileForegroundColour);
var fileBackgroundConsoleColour = FromColour(fileBackgroundColour);

var lineNumberGoregroundColor = Color.FromName(config["LineNumberForegroundColor"] ?? "Cyan");
var lineNumberGoregroundConsoleColor = FromColour(lineNumberGoregroundColor);

var lineForegroundColour = Color.FromName(config["LineForegroundColour"] ?? "White");
var lineForegroundConsoleColour = FromColour(lineForegroundColour);

var lineBackgroundColour = Color.FromName(config["LineBackgroundColour"] ?? "Black");
var lineBackgroundConsoleColour = FromColour(lineBackgroundColour);

var defaultForegroundColour = Color.FromName(config["DefaultForegroundColour"] ?? "White");
var defaultForegroundConsoleColour = FromColour(defaultForegroundColour);

var defaultBackgroundColour = Color.FromName(config["DefaultBackgroundColour"] ?? "Black");
var defaultBackgroundConsoleColour = FromColour(defaultBackgroundColour);

var errorBackgroundColour = Color.FromName(config["ErrorBackgroundColour"] ?? "Black");
var errorBackgroundConsoleColour = FromColour(errorBackgroundColour);

var errorForegroundColour = Color.FromName(config["ErrorForegroundColour"] ?? "Red");
var errorForegroundConsoleColour = FromColour(errorForegroundColour);

var existingBackgroundColour = Console.BackgroundColor;
var existingForegroundColour = Console.ForegroundColor;

var searchDirectory = Directory.GetCurrentDirectory();
var extension = "*";
var searchText = "";

if (args.Length > 0 && Path.Exists(args[0]))
{
    searchDirectory = args[0];
}

Console.WriteLine($"Current directory {searchDirectory}");
Console.WriteLine($"Current extension {extension}");

IEnumerable<(string file, int lineNumber, string content)> results = [];
IEnumerable<string> files = [];
List<string> filters = [];

var input = string.Empty;
var handled = false;
var action = string.Empty;
do
{
    Console.Write(">");
    input = Console.ReadLine();
    if (input == null)
    {
        ShowError("No input text", errorBackgroundConsoleColour, errorForegroundConsoleColour, defaultBackgroundConsoleColour, defaultForegroundConsoleColour);
        handled = true;
        continue;
    }

    if (input.StartsWith("e ", StringComparison.CurrentCultureIgnoreCase) || input.StartsWith("ext ", StringComparison.CurrentCultureIgnoreCase))
    {
        var parts = input.Split(' ');
        if (parts.Length > 1)
        {
            extension = parts[1];
            results = [];
            files = [];
            Console.WriteLine($"Set ext to {parts[1]}. Results cleared.");
            continue;
        }
    }

    if (input.StartsWith("d ", StringComparison.CurrentCultureIgnoreCase) || input.StartsWith("dir ", StringComparison.CurrentCultureIgnoreCase))
    {
        var parts = input.Split(' ');
        if (Path.Exists(parts[1]))
        {
            searchDirectory = parts[1];
            results = [];
            files = [];
            Console.WriteLine($"Set search directory to {parts[1]}. Results cleared.");
        }
        else
        {
            ShowError("Directory does not exist", errorBackgroundConsoleColour, errorForegroundConsoleColour, defaultBackgroundConsoleColour, defaultForegroundConsoleColour);
        }
        continue;
    }

    if (input.StartsWith("r ", StringComparison.CurrentCultureIgnoreCase) || input.StartsWith("reset ", StringComparison.CurrentCultureIgnoreCase))
    {
        Console.WriteLine($"Results cleared.");
        results = [];
        files = [];
        continue;
    }

    if (input.StartsWith("s ", StringComparison.CurrentCultureIgnoreCase) || input.StartsWith("search ", StringComparison.CurrentCultureIgnoreCase))
    {
        var parts = input.Split(' ');
        if (parts.Length > 1)
        {
            searchText = string.Join(" ", parts.Skip(1));
            results = [];
            files = GetFiles(searchDirectory, extension);
            results = SearchContentListInFiles(files, searchText);
            action = "Searching";
            handled = true;
        }
    }

    if (input.StartsWith("f ", StringComparison.CurrentCultureIgnoreCase) || input.StartsWith("filter ", StringComparison.CurrentCultureIgnoreCase))
    {
        var parts = input.Split(' ');
        if (parts.Length > 1)
        {
            searchText = string.Join(" ", parts.Skip(1));
            filters.Add(searchText);
            results = SearchContentListInFiles(files, searchText);
            files = results.Select(s => s.file).Distinct();
            action = "Filtering";
            handled = true;
        }
    }

    if (input.StartsWith("sf", StringComparison.CurrentCultureIgnoreCase) || input.StartsWith("show", StringComparison.CurrentCultureIgnoreCase))
    {
        foreach (var filter in filters)
        {
            Console.WriteLine(filter);
        }
        continue;
    }

    if (handled)
    {
        Console.WriteLine($"{action}: {searchDirectory}");
        foreach (var fileHit in results.GroupBy(gb => gb.file))
        {
            Console.ForegroundColor = fileForegroundConsoleColour;
            Console.BackgroundColor = fileBackgroundConsoleColour;
            // Swap \ for / to Windows Terminal makes a clickable link
            Console.WriteLine($"file:///{fileHit.Key.Replace('\\', '/')}");
            foreach (var (_, lineNumber, content) in fileHit)
            {
                Console.BackgroundColor = lineBackgroundConsoleColour;
                Console.ForegroundColor = lineNumberGoregroundConsoleColor;
                Console.Write($"  [{lineNumber}]  ");
                Console.ForegroundColor = lineForegroundConsoleColour;
                Console.Write($"{content.Trim()}");
                Console.WriteLine();
            }
        }

        Console.WriteLine($"");
        Console.ForegroundColor = defaultForegroundConsoleColour;
        Console.BackgroundColor = defaultBackgroundConsoleColour;
    }
    else if (!string.IsNullOrEmpty(input))
    {
        ShowError("Unknown command - no action taken", errorBackgroundConsoleColour, errorForegroundConsoleColour, defaultBackgroundConsoleColour, defaultForegroundConsoleColour);
    }

} while (input != "q");

Console.BackgroundColor = existingBackgroundColour;
Console.ForegroundColor = existingForegroundColour;

static void ShowError(string message, ConsoleColor errorBackgroundConsoleColour, ConsoleColor errorForegroundConsoleColour, ConsoleColor defaultBackgroundConsoleColour, ConsoleColor defaultForegroundConsoleColour)
{
    Console.ForegroundColor = errorForegroundConsoleColour;
    Console.BackgroundColor = errorBackgroundConsoleColour;
    Console.WriteLine(message);
    Console.ForegroundColor = defaultForegroundConsoleColour;
    Console.BackgroundColor = defaultBackgroundConsoleColour;
}

static IEnumerable<string> GetFiles(string searchFolder, string extension)
{
    var files = Directory.EnumerateFiles(searchFolder, $"*.{extension}", SearchOption.AllDirectories);
    return files;
}

static IEnumerable<(string file, int lineNumber, string content)> SearchContentListInFiles(IEnumerable<string> fileToSearchIn, string searchText)
{
    var result = new BlockingCollection<(string file, int line, string content)>();


    Parallel.ForEach(fileToSearchIn, (file) =>
    {
        var fileContent = File.ReadLines(file);

        var fileContentResult = fileContent.Select((line, i) => new { line, i })
              .Where(x => x.line.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
              .Select(s => new { s.i, s.line });

        foreach (var r in fileContentResult)
        {
            result.Add((file, r.i, r.line));
        }
    });

    return result;
}

// https://stackoverflow.com/a/29192463/9659
static ConsoleColor FromColour(Color c)
{
    int index = (c.R > 128 | c.G > 128 | c.B > 128) ? 8 : 0; // Bright bit
    index |= (c.R > 64) ? 4 : 0; // Red bit
    index |= (c.G > 64) ? 2 : 0; // Green bit
    index |= (c.B > 64) ? 1 : 0; // Blue bit
    return (System.ConsoleColor)index;
}