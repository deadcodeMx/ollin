using System.Text.Json;
using Ollin.Core.Localization;
using Xunit;

namespace Ollin.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void PluralesEIdiomaRtl()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ollin-loc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "localization"));
        try
        {
            File.WriteAllText(Path.Combine(root, "localization", "es.json"), JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["item"] = "objeto",
                ["item.one"] = "objeto",
                ["item.other"] = "objetos",
            }));
            File.WriteAllText(Path.Combine(root, "localization", "ar.json"), JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["item"] = "عنصر",
            }));

            var database = new LocalizationDatabase(root);
            database.SetLanguage("es");
            Assert.Equal("objetos", database.GetPlural("item", 3));
            database.SetLanguage("ar");
            Assert.True(database.IsRightToLeft);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
