using System.IO;
using System.Linq;
using SkypeLangEditor.Models;
using SkypeLangEditor.Services;
using Xunit;

namespace SkypeLangEditor.Tests
{
    public class LangFileTests
    {
        [Fact]
        public void LoadFromString_ParsesKeyValuePairsCorrectly()
        {
            string content = "sKEY_ONE=Value One\r\nsKEY_TWO=Value=With=Equals\r\nsKEY_THREE=";
            var file = new LangFile();
            file.LoadFromString(content);

            Assert.Equal(3, file.Entries.Count);
            Assert.Equal("sKEY_ONE", file.Entries[0].Key);
            Assert.Equal("Value One", file.Entries[0].Value);

            Assert.Equal("sKEY_TWO", file.Entries[1].Key);
            Assert.Equal("Value=With=Equals", file.Entries[1].Value);

            Assert.Equal("sKEY_THREE", file.Entries[2].Key);
            Assert.Equal(string.Empty, file.Entries[2].Value);
        }

        [Fact]
        public void SaveToString_OutputsFormattedKeyValuePairs()
        {
            var file = new LangFile();
            file.Entries.Add(new LangEntry("sKEY_A", "ValA"));
            file.Entries.Add(new LangEntry("sKEY_B", "ValB"));

            string result = file.SaveToString();
            Assert.Contains("sKEY_A=ValA", result);
            Assert.Contains("sKEY_B=ValB", result);
        }
    }

    public class TemplateManagerTests
    {
        [Fact]
        public void ApplyTemplateToDocument_AppendsMissingKeysOnly()
        {
            var target = new LangFile();
            target.LoadFromString("sKEY_EXISTING=Original Value\r\nsKEY_COMMON=Target Value");

            var template = new LangFile();
            template.LoadFromString("sKEY_COMMON=Template Value\r\nsKEY_NEW1=New Val 1\r\nsKEY_NEW2=New Val 2");

            var manager = new TemplateManager();
            int addedCount = manager.ApplyTemplateToDocument(target, template);

            Assert.Equal(2, addedCount);
            Assert.Equal(4, target.Entries.Count);

            // Verify sKEY_COMMON kept its original target value
            Assert.Equal("Target Value", target.Entries.First(e => e.Key == "sKEY_COMMON").Value);

            // Verify new keys appended
            Assert.Equal("New Val 1", target.Entries.First(e => e.Key == "sKEY_NEW1").Value);
            Assert.Equal("New Val 2", target.Entries.First(e => e.Key == "sKEY_NEW2").Value);
        }

        [Fact]
        public void ApplyTemplateToDirectory_UpdatesAllLangFilesInDirectory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                string file1 = Path.Combine(tempDir, "en.lang");
                string file2 = Path.Combine(tempDir, "fr.lang");
                string templateFile = Path.Combine(tempDir, "template.lang");

                File.WriteAllText(file1, "sKEY_A=Hello\r\n");
                File.WriteAllText(file2, "sKEY_A=Bonjour\r\nsKEY_B=Monde\r\n");
                File.WriteAllText(templateFile, "sKEY_A=Default\r\nsKEY_B=World\r\nsKEY_C=Extra\r\n");

                var manager = new TemplateManager();
                var result = manager.ApplyTemplateToDirectory(tempDir, templateFile, recursive: false);

                Assert.Equal(2, result.FilesProcessed);
                // en.lang missing B and C (2 added)
                // fr.lang missing C (1 added)
                Assert.Equal(3, result.TotalKeysAdded);

                var enLang = new LangFile();
                enLang.LoadFromFile(file1);
                Assert.Equal(3, enLang.Entries.Count);
                Assert.Equal("Hello", enLang.Entries.First(e => e.Key == "sKEY_A").Value);
                Assert.Equal("World", enLang.Entries.First(e => e.Key == "sKEY_B").Value);
                Assert.Equal("Extra", enLang.Entries.First(e => e.Key == "sKEY_C").Value);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }

    public class SearchReplaceEngineTests
    {
        [Fact]
        public void ReplaceAll_LiteralSearch_ReplacesMatches()
        {
            var entries = new[]
            {
                new LangEntry("sKEY_TITLE", "Skype Application"),
                new LangEntry("sKEY_WELCOME", "Welcome to Skype!")
            };

            var engine = new SearchReplaceEngine();
            var options = new SearchReplaceOptions
            {
                SearchText = "Skype",
                ReplaceText = "App",
                UseRegex = false,
                MatchCase = true,
                Scope = SearchScope.ValueOnly
            };

            var result = engine.ReplaceAll(entries, options);

            Assert.Equal(2, result.MatchesFound);
            Assert.Equal(2, result.EntriesModified);
            Assert.Equal("App Application", entries[0].Value);
            Assert.Equal("Welcome to App!", entries[1].Value);
        }

        [Fact]
        public void ReplaceAll_RegexSearch_ReplacesMatches()
        {
            var entries = new[]
            {
                new LangEntry("sKEY_100", "Total count: 100 items"),
                new LangEntry("sKEY_200", "Total count: 200 items")
            };

            var engine = new SearchReplaceEngine();
            var options = new SearchReplaceOptions
            {
                SearchText = @"\d+",
                ReplaceText = "###",
                UseRegex = true,
                MatchCase = false,
                Scope = SearchScope.ValueOnly
            };

            var result = engine.ReplaceAll(entries, options);

            Assert.Equal(2, result.MatchesFound);
            Assert.Equal("Total count: ### items", entries[0].Value);
            Assert.Equal("Total count: ### items", entries[1].Value);
        }

        [Fact]
        public void ReplaceAll_KeyAndValueScope_ReplacesBoth()
        {
            var entries = new[]
            {
                new LangEntry("sSKYPE_KEY", "Skype Value")
            };

            var engine = new SearchReplaceEngine();
            var options = new SearchReplaceOptions
            {
                SearchText = "Skype",
                ReplaceText = "Tool",
                UseRegex = false,
                MatchCase = false,
                Scope = SearchScope.Both
            };

            var result = engine.ReplaceAll(entries, options);

            Assert.Equal(2, result.MatchesFound);
            Assert.Equal("sTool_KEY", entries[0].Key);
            Assert.Equal("Tool Value", entries[0].Value);
        }
    }
}
