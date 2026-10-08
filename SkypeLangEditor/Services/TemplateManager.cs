using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkypeLangEditor.Services
{
    public class TemplateResult
    {
        public int FilesProcessed { get; set; }
        public int TotalKeysAdded { get; set; }
        public List<string> AffectedFilePaths { get; } = new List<string>();
    }

    public class TemplateManager
    {
        /// <summary>
        /// Applies template entries to a single LangFile.
        /// Appends template keys that do not exist in target document.
        /// Returns number of added keys.
        /// </summary>
        public int ApplyTemplateToDocument(Models.LangFile targetDocument, Models.LangFile templateDocument)
        {
            if (targetDocument == null) throw new ArgumentNullException(nameof(targetDocument));
            if (templateDocument == null) throw new ArgumentNullException(nameof(templateDocument));

            var existingKeys = new HashSet<string>(
                targetDocument.Entries.Select(e => e.Key),
                StringComparer.Ordinal
            );

            int addedCount = 0;
            foreach (var templateEntry in templateDocument.Entries)
            {
                if (!existingKeys.Contains(templateEntry.Key))
                {
                    targetDocument.Entries.Add(templateEntry.Clone());
                    existingKeys.Add(templateEntry.Key);
                    addedCount++;
                }
            }

            return addedCount;
        }

        /// <summary>
        /// Applies a template file to all .lang files found in targetDirectory (and subdirectories option).
        /// </summary>
        public TemplateResult ApplyTemplateToDirectory(string targetDirectory, string templateFilePath, bool recursive = true)
        {
            if (string.IsNullOrEmpty(targetDirectory) || !Directory.Exists(targetDirectory))
            {
                throw new DirectoryNotFoundException($"Target directory '{targetDirectory}' does not exist.");
            }

            var template = new Models.LangFile();
            template.LoadFromFile(templateFilePath);

            var result = new TemplateResult();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var targetFiles = Directory.GetFiles(targetDirectory, "*.lang", searchOption);

            foreach (var file in targetFiles)
            {
                // Skip the template file itself if it happens to be in the target directory
                if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(templateFilePath), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var langFile = new Models.LangFile();
                langFile.LoadFromFile(file);

                int added = ApplyTemplateToDocument(langFile, template);
                if (added > 0)
                {
                    langFile.SaveToFile(file);
                    result.TotalKeysAdded += added;
                    result.AffectedFilePaths.Add(file);
                }

                result.FilesProcessed++;
            }

            return result;
        }
    }
}
