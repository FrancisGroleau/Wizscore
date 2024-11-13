using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

[Generator]
public class ResxIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Access .resx files from AdditionalTextsProvider (IncrementalValuesProvider<AdditionalText>)
        var resxFiles = context.AdditionalTextsProvider;

        // Collect and process each .resx file incrementally
        var resxFileData = resxFiles
            .Where(file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .Select((file, cancellationToken) =>
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Path);
                var languageCode = ExtractLanguageCode(fileNameWithoutExtension);
                var viewName = ExtractViewName(fileNameWithoutExtension);
                return new ResxFileData
                {
                    FileName = fileNameWithoutExtension,
                    LanguageCode = languageCode,
                    ViewName = viewName,
                    Text = file.GetText(cancellationToken) // Get the content of the resx file
                };
            })
            .Where(fileData => fileData.Text != null)
            .Collect(); // Collect all .resx files for incremental processing

        // Group resx files by viewName incrementally after all files are processed
        var groupedResxFiles = resxFileData.Select((files, cancellationToken) =>
        {
            return files
                .GroupBy(fileData => fileData.ViewName)
                .Select(group => new { ViewName = group.Key, Files = group.ToList() })
                .ToList();
        });

        // Register the source output for the grouped resx files
        context.RegisterSourceOutput(groupedResxFiles, (spc, groups) =>
        {
            // Generate source for each group (viewName) in the grouped .resx files
            foreach (var group in groups)
            {
                var className = ToValidClassName(group.ViewName); // Clean view name to valid class name
                var generatedSource = GenerateStaticClassSource(group.Files);

                // Ensure unique file name to avoid conflicts during rebuild
                var uniqueFileName = $"{className}Resx.g.cs"; // Just use class name for source file
                spc.AddSource(uniqueFileName, SourceText.From(generatedSource, Encoding.UTF8));
            }
        });
    }

    // Helper to extract the language code from the filename (e.g., "Views.Home.Index.En.resx" -> "En")
    private string ExtractLanguageCode(string fileNameWithoutExtension)
    {
        var parts = fileNameWithoutExtension.Split('.');
        return parts.Last(); // Language code is the last part of the filename
    }

    // Helper to extract the view name (e.g., "Views.Home.Index" from "Views.Home.Index.En.resx")
    private string ExtractViewName(string fileNameWithoutExtension)
    {
        var parts = fileNameWithoutExtension.Split('.');
        return string.Join(".", parts.Take(parts.Length - 1));  // All parts except the last (language code)
    }

    // Helper method to generate the static class source from grouped resx files
    private string GenerateStaticClassSource(List<ResxFileData> viewGroup)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace Generated");
        sb.AppendLine("{");

        // Generate the class name based on the view name
        var className = ToValidClassName(viewGroup.First().ViewName);
        sb.AppendLine($"    public static class {className}Resx");
        sb.AppendLine("    {");

        // Use a HashSet to ensure resource names are unique
        var allEntries = new Dictionary<string, string>(); // Using a dictionary to store name-value pairs

        // Combine entries from all .resx files for this view
        foreach (var fileData in viewGroup)
        {
            var entries = ParseResxContent(fileData.Text.ToString());

            // Add each entry to the dictionary (overwriting if the key exists)
            foreach (var entry in entries)
            {
                if (!allEntries.Keys.Any(a => a == entry.Name))
                {
                    allEntries[entry.Name] = entry.Value; // If the key exists, the value is overwritten (last one wins)
                }
            }
        }

        // Generate static properties for each resource entry
        foreach (var entry in allEntries)
        {
            sb.AppendLine($"        public static string {entry.Key} => \"{entry.Key}\";");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // Method to parse the content of a .resx file and extract name-value pairs
    private List<(string Name, string Value)> ParseResxContent(string resxContent)
    {
        var entries = new List<(string, string)>();

        using (var reader = new System.IO.StringReader(resxContent))
        {
            var xmlDocument = XDocument.Load(reader);
            foreach (var dataElement in xmlDocument.Descendants("data"))
            {
                var name = dataElement.Attribute("name")?.Value;
                var value = dataElement.Element("value")?.Value;

                if (!string.IsNullOrEmpty(name) && value != null)
                {
                    entries.Add((name, value));
                }
            }
        }

        return entries;
    }

    // Utility method to convert the view name into a valid C# class name
    private string ToValidClassName(string viewName)
    {
        return viewName.Replace(".", "").Replace("_", "").Replace("-", "");
    }

    // A strongly typed model to hold the data from a single .resx file
    private class ResxFileData
    {
        public string FileName { get; set; }  // The filename without extension
        public string LanguageCode { get; set; }  // The language code (e.g., "En", "Fr")
        public string ViewName { get; set; }  // The view name (e.g., "Views.Home.Index")
        public SourceText Text { get; set; }  // The content of the .resx file
    }
}
