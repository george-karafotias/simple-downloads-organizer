
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SimpleDownloadsOrganizer.Models;
using SimpleDownloadsOrganizer.Services;
using SimpleDownloadsOrganizer.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace SimpleDownloadsOrganizer
{
    public sealed partial class MainWindow : Window
    {
        private const string FolderToken = "SelectedFolder";

        private readonly FileOrganizerService _organizer = new();

        private StorageFolder? _selectedFolder;

        private readonly FileMoveService _fileMoveService = new();

        private IReadOnlyList<OrganizationPlanItem>? _currentPlan;

        private readonly UndoService _undoService = new();
        private IReadOnlyList<FileMoveRecord>? _lastMoveRecords;
        private StorageFolder? _undoFolder;

        public MainWindow()
        {
            InitializeComponent();

            StrategyComboBox.SelectedIndex = 0;

            _ = RestoreFolderAsync();
        }

        private async void ChooseFolderButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative
                    .GetWindowHandle(this);

                WinRT.Interop.InitializeWithWindow
                    .Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();

                // The user cancelled the picker.
                if (folder is null)
                    return;

                _selectedFolder = folder;

                StorageApplicationPermissions.FutureAccessList
                    .AddOrReplace(FolderToken, folder);

                FolderPathText.Text = folder.Path;

                _currentPlan = null;
                OrganizeButton.IsEnabled = false;
                PreviewPanel.Visibility = Visibility.Collapsed;

                await RefreshFileCountAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    $"Could not select the folder: {ex.Message}";
            }
        }

        private async Task RestoreFolderAsync()
        {
            try
            {
                var accessList =
                    StorageApplicationPermissions.FutureAccessList;

                if (!accessList.ContainsItem(FolderToken))
                {
                    StatusText.Text =
                        "Choose a folder to get started.";

                    return;
                }

                _selectedFolder =
                    await accessList.GetFolderAsync(FolderToken);

                FolderPathText.Text = _selectedFolder.Path;

                await RefreshFileCountAsync();
            }
            catch (Exception)
            {
                _selectedFolder = null;

                FolderPathText.Text =
                    "Choose a folder to get started";

                FileCountText.Text = "No folder selected";
                PreviewChangesButton.IsEnabled = false;

                StatusText.Text =
                    "The saved folder is no longer accessible. Choose it again.";
            }
        }

        private async Task RefreshFileCountAsync()
        {
            if (_selectedFolder is null)
                return;

            try
            {
                var files = await _selectedFolder.GetFilesAsync();

                FileCountText.Text =
                    $"{files.Count} files found";

                PreviewChangesButton.IsEnabled = true;
                OrganizeButton.IsEnabled = true;

                StatusText.Text =
                    "Folder scanned successfully.";
            }
            catch (Exception ex)
            {
                FileCountText.Text = "Unable to read files.";
                PreviewChangesButton.IsEnabled = false;

                StatusText.Text =
                    $"Could not scan the folder: {ex.Message}";
            }
        }

        private void AdvancedModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (AdvancedOptionsPanel is null)
                return;

            AdvancedOptionsPanel.Visibility =
                AdvancedModeCheckBox.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            InvalidatePlan();
        }

        private void StrategyComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            InvalidatePlan();
        }

        private void InvalidatePlan()
        {
            _currentPlan = null;

            if (OrganizeButton is not null)
                OrganizeButton.IsEnabled = _selectedFolder is not null;

            if (PreviewPanel is not null)
                PreviewPanel.Visibility = Visibility.Collapsed;
        }

        private IOrganizationStrategy GetSelectedStrategy()
        {
            if (AdvancedModeCheckBox.IsChecked != true)
                return new MonthAndDayStrategy();

            return StrategyComboBox.SelectedIndex switch
            {
                1 => new FileExtensionStrategy(),
                2 => new FileCategoryStrategy(),
                _ => new MonthAndDayStrategy()
            };
        }

        private async void PreviewChangesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedFolder is null)
                return;

            PreviewChangesButton.IsEnabled = false;
            ChooseFolderButton.IsEnabled = false;

            StatusText.Text = "Preparing preview...";

            try
            {
                var strategy = GetSelectedStrategy();

                var plan = await _organizer.CreatePlanAsync(
                    _selectedFolder,
                    strategy);

                _currentPlan = plan;
                PreviewList.ItemsSource = plan;
                OrganizeButton.IsEnabled = plan.Count > 0;

                PreviewDescriptionText.Text =
                    $"Method: {strategy.Name}. " +
                    $"{plan.Count} files in the plan.";

                PreviewPanel.Visibility = Visibility.Visible;

                StatusText.Text =
                    $"Preview ready: {plan.Count} files. No files have been moved.";
            }
            catch (Exception ex)
            {
                PreviewPanel.Visibility = Visibility.Collapsed;

                StatusText.Text =
                    $"Could not prepare preview: {ex.Message}";
            }
            finally
            {
                ChooseFolderButton.IsEnabled = true;

                PreviewChangesButton.IsEnabled =
                    _selectedFolder is not null;
            }
        }

        private async void OrganizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFolder is null)
                return;

            // Preserve the approved plan for this operation.
            var planToExecute = _currentPlan;

            OrganizeButton.IsEnabled = false;
            PreviewChangesButton.IsEnabled = false;
            ChooseFolderButton.IsEnabled = false;
            AdvancedModeCheckBox.IsEnabled = false;
            StrategyComboBox.IsEnabled = false;

            StatusText.Text = "Organizing files...";

            try
            {
                if (planToExecute is null)
                {
                    var strategy = GetSelectedStrategy();

                    planToExecute = await _organizer.CreatePlanAsync(
                        _selectedFolder,
                        strategy);
                }

                var result = await _fileMoveService.ExecutePlanAsync(
                    _selectedFolder,
                    planToExecute);

                // Keep only the successful moves from the latest operation.
                _lastMoveRecords = result.MovedFiles.Count > 0
                    ? result.MovedFiles.ToList()
                    : null;

                _undoFolder = _lastMoveRecords is not null
                    ? _selectedFolder
                    : null;

                UndoButton.IsEnabled = _lastMoveRecords is not null;

                // The original preview is now stale.
                _currentPlan = null;
                PreviewPanel.Visibility = Visibility.Collapsed;

                await RefreshFileCountAsync();

                StatusText.Text =
                    $"Organization finished. " +
                    $"Moved: {result.MovedCount}. " +
                    $"Skipped: {result.SkippedCount}. " +
                    $"Failed: {result.FailedCount}.";

                if (result.Errors.Count > 0)
                {
                    StatusText.Text +=
                        $" First error: {result.Errors[0]}";
                }
            }
            catch (Exception ex)
            {
                _currentPlan = null;
                PreviewPanel.Visibility = Visibility.Collapsed;

                StatusText.Text =
                    $"Organization could not be completed: {ex.Message}";
            }
            finally
            {
                ChooseFolderButton.IsEnabled = true;
                AdvancedModeCheckBox.IsEnabled = true;
                StrategyComboBox.IsEnabled = true;

                PreviewChangesButton.IsEnabled =
                    _selectedFolder is not null;

                OrganizeButton.IsEnabled = _selectedFolder is not null;
            }
        }

        private async void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_undoFolder is null ||
                _lastMoveRecords is null ||
                _lastMoveRecords.Count == 0)
            {
                return;
            }

            var recordsToUndo = _lastMoveRecords;
            var folderToUndo = _undoFolder;

            UndoButton.IsEnabled = false;
            OrganizeButton.IsEnabled = false;
            PreviewChangesButton.IsEnabled = false;
            ChooseFolderButton.IsEnabled = false;
            AdvancedModeCheckBox.IsEnabled = false;
            StrategyComboBox.IsEnabled = false;

            StatusText.Text = "Undoing the last operation...";

            try
            {
                var result = await _undoService.UndoAsync(
                    folderToUndo,
                    recordsToUndo);

                // Remove successfully restored files from the undo history.
                // Any skipped or failed files remain available for another attempt.
                _lastMoveRecords = recordsToUndo
                    .Where(record => !result.RestoredFiles.Contains(record))
                    .ToList();

                if (_lastMoveRecords.Count == 0)
                {
                    _lastMoveRecords = null;
                    _undoFolder = null;
                }

                UndoButton.IsEnabled = _lastMoveRecords is not null;

                StatusText.Text =
                    $"Undo finished. " +
                    $"Restored: {result.RestoredCount}. " +
                    $"Skipped: {result.SkippedCount}. " +
                    $"Failed: {result.FailedCount}.";

                if (result.SkippedFiles.Count > 0)
                {
                    StatusText.Text +=
                        $" First skipped file: {result.SkippedFiles[0]}";
                }
                else if (result.Errors.Count > 0)
                {
                    StatusText.Text +=
                        $" First error: {result.Errors[0]}";
                }

                PreviewPanel.Visibility = Visibility.Collapsed;
                _currentPlan = null;

                // Refresh the count for the currently selected folder.
                if (_selectedFolder is not null)
                    await RefreshFileCountAsync();

                // RefreshFileCountAsync updates the status text, so restore
                // the undo summary afterwards.
                StatusText.Text =
                    $"Undo finished. " +
                    $"Restored: {result.RestoredCount}. " +
                    $"Skipped: {result.SkippedCount}. " +
                    $"Failed: {result.FailedCount}.";

                if (result.SkippedFiles.Count > 0)
                {
                    StatusText.Text +=
                        $" First skipped file: {result.SkippedFiles[0]}";
                }
                else if (result.Errors.Count > 0)
                {
                    StatusText.Text +=
                        $" First error: {result.Errors[0]}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    $"Undo could not be completed: {ex.Message}";
            }
            finally
            {
                ChooseFolderButton.IsEnabled = true;
                AdvancedModeCheckBox.IsEnabled = true;
                StrategyComboBox.IsEnabled = true;

                PreviewChangesButton.IsEnabled =
                    _selectedFolder is not null;

                OrganizeButton.IsEnabled = _selectedFolder is not null;

                UndoButton.IsEnabled =
                    _lastMoveRecords is not null &&
                    _lastMoveRecords.Count > 0;
            }
        }
    }
}
