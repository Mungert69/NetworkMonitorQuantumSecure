using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace NetworkMonitorQuantumSecure.Tests;

public class ResourceKeyTests
{
    private static readonly XNamespace XNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";

    private static readonly string SolutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string[] ResourceDictionaries =
    {
        Path.Combine(SolutionRoot, "App.xaml"),
        Path.Combine(SolutionRoot, "Resources", "Styles", "Colors.xaml"),
        Path.Combine(SolutionRoot, "Resources", "Styles", "Styles.xaml"),
    };

    [Fact]
    public void DataViewPage_ReferencesExistingResources()
    {
        var keys = LoadDefinedResourceKeys();
        var referencedKeys = ExtractReferencedResourceKeys(
            Path.Combine(SolutionRoot, "DataViewPage.xaml"),
            out var localKeys);

        AssertMissingKeys(referencedKeys, keys, localKeys, "DataViewPage.xaml");
    }

    [Fact]
    public void StatusDetailsPopup_ReferencesExistingResources()
    {
        var keys = LoadDefinedResourceKeys();
        var referencedKeys = ExtractReferencedResourceKeys(
            Path.Combine(SolutionRoot, "Views", "ShowDetailsPopup.xaml"),
            out var localKeys);

        AssertMissingKeys(referencedKeys, keys, localKeys, "Views/ShowDetailsPopup.xaml");
    }

    private static HashSet<string> LoadDefinedResourceKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in ResourceDictionaries)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var document = XDocument.Load(path);
            foreach (var element in document.Descendants())
            {
                var key = element.Attribute(XNamespace + "Key")?.Value;
                if (!string.IsNullOrWhiteSpace(key))
                {
                    keys.Add(key);
                }
            }
        }

        return keys;
    }

    private static HashSet<string> ExtractReferencedResourceKeys(string xamlPath, out HashSet<string> localKeys)
    {
        localKeys = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);

        var xaml = File.ReadAllText(xamlPath);
        var document = XDocument.Load(xamlPath);

        foreach (var element in document.Descendants())
        {
            var key = element.Attribute(XNamespace + "Key")?.Value;
            if (!string.IsNullOrWhiteSpace(key))
            {
                localKeys.Add(key);
            }
        }

        // Matches StaticResource or DynamicResource usages inside markup extensions
        var regex = new Regex(@"\b(?:StaticResource|DynamicResource)\s+(?<key>[\w\.]+)", RegexOptions.Compiled);

        foreach (Match match in regex.Matches(xaml))
        {
            var key = match.Groups["key"].Value;
            if (!string.IsNullOrWhiteSpace(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private static void AssertMissingKeys(IEnumerable<string> referencedKeys, HashSet<string> definedKeys, HashSet<string> localKeys, string fileName)
    {
        var missing = referencedKeys
            .Where(key => !definedKeys.Contains(key) && !localKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"The following resource keys referenced in {fileName} are not defined: {string.Join(", ", missing)}");
    }
}
