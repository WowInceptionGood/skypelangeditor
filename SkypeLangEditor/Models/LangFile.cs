using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace SkypeLangEditor.Models
{
    public class LangFile
    {
        public ObservableCollection<LangEntry> Entries { get; } = new ObservableCollection<LangEntry>();

        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Specified language file not found.", filePath);
            }

            Entries.Clear();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                int equalIndex = line.IndexOf('=');
                if (equalIndex >= 0)
                {
                    string key = line.Substring(0, equalIndex);
                    string value = line.Substring(equalIndex + 1);
                    Entries.Add(new LangEntry(key, value));
                }
                else
                {
                    Entries.Add(new LangEntry(line, string.Empty));
                }
            }
        }

        public void SaveToFile(string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                foreach (var entry in Entries)
                {
                    writer.WriteLine($"{entry.Key}={entry.Value}");
                }
            }
        }

        public void LoadFromString(string content)
        {
            Entries.Clear();
            if (string.IsNullOrEmpty(content))
                return;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                int equalIndex = line.IndexOf('=');
                if (equalIndex >= 0)
                {
                    string key = line.Substring(0, equalIndex);
                    string value = line.Substring(equalIndex + 1);
                    Entries.Add(new LangEntry(key, value));
                }
                else
                {
                    Entries.Add(new LangEntry(line, string.Empty));
                }
            }
        }

        public string SaveToString()
        {
            var sb = new StringBuilder();
            foreach (var entry in Entries)
            {
                sb.AppendLine($"{entry.Key}={entry.Value}");
            }
            return sb.ToString();
        }
    }
}
