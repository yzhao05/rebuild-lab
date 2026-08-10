using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Xml.Linq;

if (args.Length == 0)
{
    Console.Error.WriteLine(
        "Pass source directories, optionally followed by --references-from <Unity .csproj>.");
    return 2;
}

List<string> sourceArguments = new();
List<string> referenceProjects = new();
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--references-from")
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine("--references-from requires a .csproj path.");
            return 2;
        }

        referenceProjects.Add(Path.GetFullPath(args[++i]));
    }
    else
    {
        sourceArguments.Add(args[i]);
    }
}

if (sourceArguments.Count == 0)
{
    Console.Error.WriteLine("At least one source directory is required.");
    return 2;
}

List<string> sourceFiles = new();
foreach (string argument in sourceArguments)
{
    string path = Path.GetFullPath(argument);
    if (Directory.Exists(path))
    {
        sourceFiles.AddRange(
            Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                .Where(IsPackageSourceFile));
    }
    else if (File.Exists(path) && string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
    {
        sourceFiles.Add(path);
    }
    else
    {
        Console.Error.WriteLine($"Source path does not exist: {path}");
        return 2;
    }
}

int errorCount = 0;
List<SyntaxTree> syntaxTrees = new();
foreach (string sourceFile in sourceFiles.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path))
{
    string source = await File.ReadAllTextAsync(sourceFile);
    CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(
        "ENABLE_INPUT_SYSTEM",
        "ENABLE_LEGACY_INPUT_MANAGER");
    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: sourceFile);
    syntaxTrees.Add(syntaxTree);
    foreach (Diagnostic diagnostic in syntaxTree.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error))
    {
        Console.Error.WriteLine(diagnostic);
        errorCount++;
    }
}

if (errorCount > 0)
{
    Console.Error.WriteLine($"C# syntax check failed with {errorCount} error(s).");
    return 1;
}

Console.WriteLine($"C# syntax check passed for {sourceFiles.Count} file(s).");

if (referenceProjects.Count > 0)
{
    HashSet<string> referencePaths = new(StringComparer.OrdinalIgnoreCase);
    foreach (string projectPath in referenceProjects)
    {
        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Reference project does not exist: {projectPath}");
            return 2;
        }

        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        XDocument project = XDocument.Load(projectPath);
        foreach (XElement hintPathElement in project.Descendants().Where(element => element.Name.LocalName == "HintPath"))
        {
            string value = hintPathElement.Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string resolved = Path.IsPathRooted(value)
                ? value
                : Path.GetFullPath(Path.Combine(projectDirectory, value));
            if (File.Exists(resolved))
            {
                referencePaths.Add(resolved);
            }
        }
    }

    List<MetadataReference> references = referencePaths
        .Select(path => MetadataReference.CreateFromFile(path))
        .Cast<MetadataReference>()
        .ToList();
    CSharpCompilation compilation = CSharpCompilation.Create(
        "EMVR.Common.Validation",
        syntaxTrees,
        references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    List<Diagnostic> semanticErrors = compilation.GetDiagnostics()
        .Where(item => item.Severity == DiagnosticSeverity.Error)
        .ToList();
    if (semanticErrors.Count > 0)
    {
        foreach (Diagnostic diagnostic in semanticErrors)
        {
            Console.Error.WriteLine(diagnostic);
        }

        Console.Error.WriteLine($"Unity API compilation check failed with {semanticErrors.Count} error(s).");
        return 1;
    }

    Console.WriteLine(
        $"Unity API compilation check passed with {references.Count} metadata reference(s).");
}

return 0;

static bool IsPackageSourceFile(string path)
{
    string[] segments = Path.GetFullPath(path).Split(
        new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
        StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < segments.Length - 1; i++)
    {
        if (segments[i].EndsWith("~", StringComparison.Ordinal)
            || string.Equals(segments[i], "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segments[i], "obj", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    return true;
}
