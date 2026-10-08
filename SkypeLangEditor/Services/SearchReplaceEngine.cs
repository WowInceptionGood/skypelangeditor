using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SkypeLangEditor.Models;

namespace SkypeLangEditor.Services
{
    public enum SearchScope
    {
        Both,
        KeyOnly,
        ValueOnly
    }

    public class SearchReplaceOptions
    {
        public string SearchText { get; set; } = string.Empty;
        public string ReplaceText { get; set; } = string.Empty;
        public bool UseRegex { get; set; }
        public bool MatchCase { get; set; }
        public SearchScope Scope { get; set; } = SearchScope.Both;
    }

    public class SearchReplaceResult
    {
        public int MatchesFound { get; set; }
        public int EntriesModified { get; set; }
    }

    public class SearchReplaceEngine
    {
        public SearchReplaceResult ReplaceAll(IEnumerable<LangEntry> entries, SearchReplaceOptions options)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var result = new SearchReplaceResult();
            if (string.IsNullOrEmpty(options.SearchText))
                return result;

            Regex regex = null;
            if (options.UseRegex)
            {
                var regexOptions = options.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                regex = new Regex(options.SearchText, regexOptions);
            }

            StringComparison comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var entry in entries)
            {
                bool entryModified = false;

                // Process Key
                if (options.Scope == SearchScope.Both || options.Scope == SearchScope.KeyOnly)
                {
                    if (PerformReplace(entry.Key, options.SearchText, options.ReplaceText, options.UseRegex, regex, comparison, out string newKey, out int keyMatches))
                    {
                        entry.Key = newKey;
                        result.MatchesFound += keyMatches;
                        entryModified = true;
                    }
                }

                // Process Value
                if (options.Scope == SearchScope.Both || options.Scope == SearchScope.ValueOnly)
                {
                    if (PerformReplace(entry.Value, options.SearchText, options.ReplaceText, options.UseRegex, regex, comparison, out string newValue, out int valMatches))
                    {
                        entry.Value = newValue;
                        result.MatchesFound += valMatches;
                        entryModified = true;
                    }
                }

                if (entryModified)
                {
                    result.EntriesModified++;
                }
            }

            return result;
        }

        private bool PerformReplace(
            string input,
            string searchText,
            string replaceText,
            bool useRegex,
            Regex regex,
            StringComparison comparison,
            out string output,
            out int matchCount)
        {
            output = input;
            matchCount = 0;

            if (string.IsNullOrEmpty(input))
                return false;

            if (useRegex)
            {
                var matches = regex.Matches(input);
                if (matches.Count > 0)
                {
                    matchCount = matches.Count;
                    output = regex.Replace(input, replaceText ?? string.Empty);
                    return true;
                }
            }
            else
            {
                int index = 0;
                int count = 0;
                while ((index = input.IndexOf(searchText, index, comparison)) != -1)
                {
                    count++;
                    index += searchText.Length;
                }

                if (count > 0)
                {
                    matchCount = count;
                    output = ReplaceString(input, searchText, replaceText ?? string.Empty, comparison);
                    return true;
                }
            }

            return false;
        }

        private string ReplaceString(string source, string oldString, string newString, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldString))
                return source;

            int index = 0;
            var sb = new System.Text.StringBuilder();

            while (true)
            {
                int nextIndex = source.IndexOf(oldString, index, comparison);
                if (nextIndex == -1)
                {
                    sb.Append(source.Substring(index));
                    break;
                }

                sb.Append(source.Substring(index, nextIndex - index));
                sb.Append(newString);
                index = nextIndex + oldString.Length;
            }

            return sb.ToString();
        }
    }
}
