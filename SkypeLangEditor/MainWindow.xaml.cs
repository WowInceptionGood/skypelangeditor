using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using SkypeLangEditor.Models;
using SkypeLangEditor.Services;

namespace SkypeLangEditor
{
    public partial class MainWindow : Window
    {
        private LangFile _currentLangFile = new LangFile();
        private string _currentFilePath = null;
        private bool _isModified = false;

        private readonly SearchReplaceEngine _searchReplaceEngine = new SearchReplaceEngine();
        private readonly TemplateManager _templateManager = new TemplateManager();
        private ICollectionView _filteredView;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDataGrid();
            UpdateTitle();
            UpdateStats();
        }

        private void InitializeDataGrid()
        {
            _filteredView = CollectionViewSource.GetDefaultView(_currentLangFile.Entries);
            _filteredView.Filter = FilterLangEntry;
            EntriesDataGrid.ItemsSource = _filteredView;

            _currentLangFile.Entries.CollectionChanged += (s, e) =>
            {
                SetModified(true);
                UpdateStats();
            };
        }

        private bool FilterLangEntry(object obj)
        {
            if (obj is not LangEntry entry)
                return false;

            string query = SearchTextBox?.Text;
            if (string.IsNullOrEmpty(query))
                return true;

            bool matchCase = MatchCaseCheckBox?.IsChecked == true;
            StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int scopeIndex = ScopeComboBox?.SelectedIndex ?? 0;
            // 0: Both, 1: Key Only, 2: Value Only

            bool keyMatches = scopeIndex != 2 && (entry.Key?.IndexOf(query, comparison) >= 0);
            bool valueMatches = scopeIndex != 1 && (entry.Value?.IndexOf(query, comparison) >= 0);

            return keyMatches || valueMatches;
        }

        private void SetModified(bool modified)
        {
            _isModified = modified;
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath) ? "Untitled.lang" : System.IO.Path.GetFileName(_currentFilePath);
            string modIndicator = _isModified ? "*" : "";
            Title = $"Skype .lang Translation Editor - {fileName}{modIndicator}";
        }

        private void UpdateStats()
        {
            int total = _currentLangFile.Entries.Count;
            int filtered = _filteredView?.Cast<object>().Count() ?? total;
            StatsText.Text = $"Total Entries: {total} | Displayed: {filtered}";
        }

        #region File Operations

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmSaveIfModified())
                return;

            _currentLangFile.Entries.Clear();
            _currentFilePath = null;
            SetModified(false);
            StatusText.Text = "New file created.";
            UpdateStats();
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmSaveIfModified())
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Skype Language Files (*.lang)|*.lang|All Files (*.*)|*.*",
                Title = "Open Skype Translation File"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _currentLangFile.LoadFromFile(dialog.FileName);
                    _currentFilePath = dialog.FileName;
                    SetModified(false);
                    StatusText.Text = $"Opened {dialog.FileName}";
                    UpdateStats();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file:\n{ex.Message}", "Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentFile(saveAs: false);
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentFile(saveAs: true);
        }

        private bool SaveCurrentFile(bool saveAs)
        {
            if (saveAs || string.IsNullOrEmpty(_currentFilePath))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Skype Language Files (*.lang)|*.lang|All Files (*.*)|*.*",
                    Title = "Save Skype Translation File",
                    DefaultExt = "lang"
                };

                if (dialog.ShowDialog() == true)
                {
                    _currentFilePath = dialog.FileName;
                }
                else
                {
                    return false;
                }
            }

            try
            {
                _currentLangFile.SaveToFile(_currentFilePath);
                SetModified(false);
                StatusText.Text = $"Saved to {_currentFilePath}";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ConfirmSaveIfModified()
        {
            if (!_isModified)
                return true;

            var result = MessageBox.Show("You have unsaved changes. Do you want to save them before continuing?",
                                        "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                return SaveCurrentFile(saveAs: false);
            }
            return result == MessageBoxResult.No;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmSaveIfModified())
            {
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region Edit Operations

        private void MenuAddEntry_Click(object sender, RoutedEventArgs e)
        {
            var newEntry = new LangEntry("sNEW_KEY", "New Value");
            _currentLangFile.Entries.Add(newEntry);
            EntriesDataGrid.SelectedItem = newEntry;
            EntriesDataGrid.ScrollIntoView(newEntry);
            StatusText.Text = "Added new entry.";
        }

        private void MenuDeleteEntry_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = EntriesDataGrid.SelectedItems.Cast<LangEntry>().ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more entries to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var item in selectedItems)
            {
                _currentLangFile.Entries.Remove(item);
            }

            StatusText.Text = $"Deleted {selectedItems.Count} entry(ies).";
        }

        private void MenuSortEntries_Click(object sender, RoutedEventArgs e)
        {
            var sortedList = _currentLangFile.Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
            _currentLangFile.Entries.Clear();
            foreach (var item in sortedList)
            {
                _currentLangFile.Entries.Add(item);
            }
            StatusText.Text = "Sorted entries alphabetically by key.";
        }

        #endregion

        #region Search and Replace

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filteredView?.Refresh();
            UpdateStats();
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            ReplaceTextBox.Text = string.Empty;
            _filteredView?.Refresh();
            UpdateStats();
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Please enter search text.", "Search & Replace", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SearchScope scope = SearchScope.Both;
            switch (ScopeComboBox.SelectedIndex)
            {
                case 1: scope = SearchScope.KeyOnly; break;
                case 2: scope = SearchScope.ValueOnly; break;
            }

            var options = new SearchReplaceOptions
            {
                SearchText = searchText,
                ReplaceText = ReplaceTextBox.Text,
                UseRegex = RegexCheckBox.IsChecked == true,
                MatchCase = MatchCaseCheckBox.IsChecked == true,
                Scope = scope
            };

            try
            {
                var result = _searchReplaceEngine.ReplaceAll(_currentLangFile.Entries, options);
                _filteredView.Refresh();
                SetModified(true);
                UpdateStats();
                StatusText.Text = $"Replaced {result.MatchesFound} instance(s) across {result.EntriesModified} entry(ies).";
                MessageBox.Show($"Search & Replace Complete:\n\nTotal matches replaced: {result.MatchesFound}\nEntries modified: {result.EntriesModified}",
                                "Search & Replace Result", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during search & replace:\n{ex.Message}", "Regex / Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Template Operations

        private void MenuApplyTemplateToFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Skype Language Files (*.lang)|*.lang|All Files (*.*)|*.*",
                Title = "Select Template File to Apply"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var template = new LangFile();
                    template.LoadFromFile(dialog.FileName);

                    int added = _templateManager.ApplyTemplateToDocument(_currentLangFile, template);
                    _filteredView.Refresh();
                    UpdateStats();

                    if (added > 0)
                    {
                        SetModified(true);
                        StatusText.Text = $"Appended {added} missing key(s) from template.";
                        MessageBox.Show($"Successfully appended {added} missing key/value pair(s) from template file.", "Template Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusText.Text = "Template applied: No new keys missing.";
                        MessageBox.Show("No new keys were added. The active document already contains all keys from the template.", "Template Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error applying template:\n{ex.Message}", "Template Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuApplyTemplateToDirectory_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Skype Language Files (*.lang)|*.lang|All Files (*.*)|*.*",
                Title = "Step 1: Select Template File"
            };

            if (fileDialog.ShowDialog() != true)
                return;

            string templatePath = fileDialog.FileName;

            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "Step 2: Select Target Directory Containing .lang Files";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(folderDialog.SelectedPath))
                {
                    string targetDir = folderDialog.SelectedPath;
                    try
                    {
                        var result = _templateManager.ApplyTemplateToDirectory(targetDir, templatePath, recursive: true);
                        StatusText.Text = $"Directory batch completed: {result.TotalKeysAdded} keys added across {result.AffectedFilePaths.Count} files.";
                        MessageBox.Show($"Template Directory Processing Complete:\n\nFiles Scanned: {result.FilesProcessed}\nFiles Modified: {result.AffectedFilePaths.Count}\nTotal Key Pairs Appended: {result.TotalKeysAdded}",
                                        "Directory Processing Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error applying template to directory:\n{ex.Message}", "Directory Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion

        private void EntriesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            SetModified(true);
        }
    }
}
