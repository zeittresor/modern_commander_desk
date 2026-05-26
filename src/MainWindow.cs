using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Styling;
using ModernCommanderDesk.Models;
using ModernCommanderDesk.Services;
using ModernCommanderDesk.PreviewHandlers;

namespace ModernCommanderDesk;

public sealed class MainWindow : Window
{
    private FilePane _leftPane = null!;
    private FilePane _rightPane = null!;
    private FilePane _activePane = null!;
    private TextBlock _statusText = null!;
    private TextBlock _selectionText = null!;
    private AppSettings _settings = null!;
    private LocalizationService _loc = null!;
    private ToolTipCatalog _tooltips = null!;

    public MainWindow()
    {
        AppPaths.EnsureApplicationFolders();
        _settings = SettingsStore.Load();
        _loc = LocalizationService.Load(_settings.Language);
        _tooltips = ToolTipCatalog.Load(_settings.Language, _settings.TooltipsEnabled);

        Title = "Modern Commander Desk v0.4.5 - " + T("window.subtitle", "Dual Pane Commander");
        Width = 1400;
        Height = 850;
        MinWidth = 980;
        MinHeight = 640;
        Background = ThemeBrush("window_bg");
        KeyDown += OnMainKeyDown;
        ApplyThemeFromSettings();
        Content = BuildUi();

        _activePane = _leftPane;
        ActivatePane(_leftPane);
        _leftPane.NavigateTo(FileManager.GetHomePath(), addHistory: false);
        _rightPane.NavigateTo(GetUsefulSecondPath(), addHistory: false);
    }

    private Control BuildUi()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto")
        };

        var menu = BuildMainMenu();
        Grid.SetRow(menu, 0);
        root.Children.Add(menu);

        var commandBar = BuildCommandBar();
        Grid.SetRow(commandBar, 1);
        root.Children.Add(commandBar);

        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,5,*"),
            Margin = new Thickness(10, 8, 10, 8)
        };
        Grid.SetRow(mainGrid, 2);
        root.Children.Add(mainGrid);

        _leftPane = new FilePane(
            "LEFT",
            pane => ActivatePane(pane),
            (_, selected) => UpdateSelection(selected),
            (_, selected) => OpenFileOrFolder(selected),
            async (pane, paths) => await HandleDroppedItemsAsync(pane, paths),
            async pane => await ShowNavigationChooserAsync(pane),
            _loc,
            _tooltips,
            _settings.TooltipsEnabled,
            _settings.Theme);

        _rightPane = new FilePane(
            "RIGHT",
            pane => ActivatePane(pane),
            (_, selected) => UpdateSelection(selected),
            (_, selected) => OpenFileOrFolder(selected),
            async (pane, paths) => await HandleDroppedItemsAsync(pane, paths),
            async pane => await ShowNavigationChooserAsync(pane),
            _loc,
            _tooltips,
            _settings.TooltipsEnabled,
            _settings.Theme);

        Grid.SetColumn(_leftPane.Control, 0);
        mainGrid.Children.Add(_leftPane.Control);

        var splitter = new GridSplitter
        {
            Background = ThemeBrush("splitter"),
            ResizeDirection = GridResizeDirection.Columns,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumn(splitter, 1);
        mainGrid.Children.Add(splitter);

        Grid.SetColumn(_rightPane.Control, 2);
        mainGrid.Children.Add(_rightPane.Control);

        var functionBar = BuildFunctionKeyBar();
        Grid.SetRow(functionBar, 3);
        root.Children.Add(functionBar);

        var status = new Border
        {
            Background = ThemeBrush("status_bg"),
            BorderBrush = ThemeBrush("splitter"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(10, 6)
        };
        var statusGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        _statusText = new TextBlock
        {
            Text = T("status.ready", "Ready."),
            Foreground = ThemeBrush("status_text"),
            VerticalAlignment = VerticalAlignment.Center
        };
        _selectionText = new TextBlock
        {
            Text = T("status.no_selection", "No selection"),
            Foreground = ThemeBrush("muted"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_statusText, 0);
        Grid.SetColumn(_selectionText, 1);
        statusGrid.Children.Add(_statusText);
        statusGrid.Children.Add(_selectionText);
        status.Child = statusGrid;
        Grid.SetRow(status, 4);
        root.Children.Add(status);

        return root;
    }

    private Menu BuildMainMenu()
    {
        var menu = new Menu
        {
            Background = ThemeBrush("menu_bg"),
            Foreground = ThemeBrush("text")
        };

        var file = new MenuItem { Header = T("menu.file", "_File") };
        file.Items.Add(MenuItemFor(T("menu.file.open", "Open / Enter"), () => OpenSelected()));
        file.Items.Add(MenuItemFor(T("menu.file.view", "View  F3"), async () => await ViewSelectedAsync()));
        file.Items.Add(MenuItemFor(T("menu.file.edit", "Edit  F4"), async () => await EditSelectedAsync()));
        file.Items.Add(new Separator());
        file.Items.Add(MenuItemFor(T("menu.file.quit", "Quit  F10"), Close));

        var commands = new MenuItem { Header = T("menu.commands", "_Commands") };
        commands.Items.Add(MenuItemFor(T("menu.commands.copy", "Copy to other panel  F5"), async () => await CopySelectedToPassiveAsync()));
        commands.Items.Add(MenuItemFor(T("menu.commands.move", "Move to other panel  F6"), async () => await MoveSelectedToPassiveAsync()));
        commands.Items.Add(MenuItemFor(T("menu.commands.mkdir", "Make directory  F7"), async () => await CreateFolderInActiveAsync()));
        commands.Items.Add(MenuItemFor(T("menu.commands.delete", "Delete / soft trash  F8"), async () => await SoftDeleteSelectedAsync()));
        commands.Items.Add(new Separator());
        commands.Items.Add(MenuItemFor(T("menu.commands.rename", "Rename  F2"), async () => await RenameSelectedAsync()));
        commands.Items.Add(MenuItemFor(T("menu.commands.copy_path", "Copy path"), async () => await CopyPathSelectedAsync()));
        commands.Items.Add(MenuItemFor(T("menu.commands.swap", "Swap panels"), SwapPanels));

        var navigation = new MenuItem { Header = T("menu.navigate", "_Navigate") };
        navigation.Items.Add(MenuItemFor(T("menu.navigate.up", "Go up  Backspace"), () => ActivePane.NavigateUp()));
        navigation.Items.Add(MenuItemFor(T("menu.navigate.back", "Back"), () => ActivePane.NavigateBack()));
        navigation.Items.Add(MenuItemFor(T("menu.navigate.forward", "Forward"), () => ActivePane.NavigateForward()));
        navigation.Items.Add(MenuItemFor(T("menu.navigate.refresh", "Refresh active  Ctrl+R"), () => ActivePane.Reload()));
        navigation.Items.Add(MenuItemFor(T("menu.navigate.refresh_both", "Refresh both"), RefreshBoth));
        navigation.Items.Add(new Separator());
        navigation.Items.Add(MenuItemFor(T("menu.navigate.home", "Home"), () => ActivePane.NavigateTo(FileManager.GetHomePath())));
        navigation.Items.Add(MenuItemFor(T("menu.navigate.locations", "Locations / drives"), async () => await ShowNavigationChooserAsync(ActivePane)));

        var tools = new MenuItem { Header = T("menu.tools", "_Tools") };
        tools.Items.Add(MenuItemFor(T("menu.tools.terminal", "Terminal in active panel"), async () => await OpenTerminalInActiveAsync()));
        tools.Items.Add(MenuItemFor(T("menu.tools.open_external", "Open active folder externally"), () => PlatformOpen.OpenPath(ActivePane.CurrentPath)));
        tools.Items.Add(new Separator());
        tools.Items.Add(MenuItemFor(T("menu.tools.plugins", "List plugins"), async () => await ShowMessageAsync(T("dialog.plugins", "Plugins"), PluginCatalog.BuildPluginReport())));
        tools.Items.Add(MenuItemFor(T("menu.tools.open_plugins_folder", "Open plugins folder"), () => PlatformOpen.OpenPath(AppPaths.PluginsDirectory)));
        tools.Items.Add(MenuItemFor(T("menu.tools.open_preview_handlers_folder", "Open preview handler folder"), () => PlatformOpen.OpenPath(AppPaths.PreviewHandlersDirectory)));

        var settings = new MenuItem { Header = T("menu.settings", "_Settings") };
        var language = new MenuItem { Header = T("settings.language", "Language") };
        foreach (var code in _loc.GetAvailableLanguages())
        {
            var localCode = code;
            var marker = string.Equals(localCode, _settings.Language, StringComparison.OrdinalIgnoreCase) ? "✓ " : "";
            language.Items.Add(MenuItemFor(marker + _loc.GetLanguageName(localCode), () => SwitchLanguage(localCode)));
        }
        settings.Items.Add(language);

        var appearance = new MenuItem { Header = T("settings.theme", "Visual theme") };
        foreach (var code in ThemeCatalog.Codes)
        {
            var localCode = code;
            var marker = string.Equals(ThemeCatalog.Normalize(localCode), ThemeCatalog.Normalize(_settings.Theme), StringComparison.OrdinalIgnoreCase) ? "✓ " : "";
            appearance.Items.Add(MenuItemFor(marker + ThemeCatalog.DisplayName(localCode, _loc), () => SwitchTheme(localCode)));
        }
        settings.Items.Add(appearance);
        settings.Items.Add(new Separator());
        settings.Items.Add(MenuItemFor(
            _settings.TooltipsEnabled ? T("settings.tooltips.disable", "Disable tooltips") : T("settings.tooltips.enable", "Enable tooltips"),
            ToggleTooltips));
        settings.Items.Add(MenuItemFor(T("settings.open_language_folder", "Open language folder"), () => PlatformOpen.OpenPath(AppPaths.LanguageDirectory)));
        settings.Items.Add(MenuItemFor(T("settings.open_tooltips_folder", "Open tooltip folder"), () => PlatformOpen.OpenPath(AppPaths.TooltipsDirectory)));

        var help = new MenuItem { Header = T("menu.help", "_Help") };
        help.Items.Add(MenuItemFor(T("menu.help.keyboard", "Keyboard help"), async () => await ShowKeyboardHelpAsync()));
        help.Items.Add(MenuItemFor(T("menu.help.help_folder", "Open help folder"), () => PlatformOpen.OpenPath(AppPaths.HelpDirectory)));
        help.Items.Add(MenuItemFor(T("menu.help.about", "About"), async () => await ShowMessageAsync(T("dialog.about", "About"), T("about.text", "Modern Commander Desk v0.4.5\n\nDual-pane commander-style file manager written in C# / Avalonia.\n\nUpdates and follow-up versions: https://github.com/zeittresor"))));

        menu.Items.Add(file);
        menu.Items.Add(commands);
        menu.Items.Add(navigation);
        menu.Items.Add(tools);
        menu.Items.Add(settings);
        menu.Items.Add(help);
        return menu;
    }

    private MenuItem MenuItemFor(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private MenuItem MenuItemFor(string header, Func<Task> action)
    {
        var item = new MenuItem { Header = header };
        item.Click += async (_, _) => await action();
        return item;
    }

    private Control BuildCommandBar()
    {
        var outer = new Border
        {
            Background = ThemeBrush("toolbar_bg"),
            BorderBrush = ThemeBrush("border"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 7)
        };

        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        bar.Children.Add(CommandButton(T("toolbar.back", "← Back"), () => ActivePane.NavigateBack(), "toolbar.back"));
        bar.Children.Add(CommandButton(T("toolbar.forward", "→ Forward"), () => ActivePane.NavigateForward(), "toolbar.forward"));
        bar.Children.Add(CommandButton(T("toolbar.up", "↑ Up"), () => ActivePane.NavigateUp(), "toolbar.up"));
        bar.Children.Add(CommandButton(T("toolbar.locations", "💽 Drives"), async () => await ShowNavigationChooserAsync(ActivePane), "toolbar.locations"));
        bar.Children.Add(CommandButton(T("toolbar.refresh", "⟳ Refresh"), () => ActivePane.Reload(), "toolbar.refresh"));
        bar.Children.Add(CommandButton(T("toolbar.swap", "⇆ Swap"), SwapPanels, "toolbar.swap"));
        bar.Children.Add(CommandButton(T("toolbar.terminal", "⌁ Terminal"), async () => await OpenTerminalInActiveAsync(), "toolbar.terminal"));

        outer.Child = bar;
        return outer;
    }

    private Button CommandButton(string text, Action action, string tooltipKey)
    {
        var button = new Button
        {
            Content = text,
            MinHeight = 32,
            Padding = new Thickness(10, 3),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        ApplyToolTip(button, tooltipKey);
        button.Click += (_, _) => action();
        return button;
    }

    private Button CommandButton(string text, Func<Task> action, string tooltipKey)
    {
        var button = new Button
        {
            Content = text,
            MinHeight = 32,
            Padding = new Thickness(10, 3),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        ApplyToolTip(button, tooltipKey);
        button.Click += async (_, _) => await action();
        return button;
    }

    private Control BuildFunctionKeyBar()
    {
        var outer = new Border
        {
            Background = ThemeBrush("toolbar_bg"),
            BorderBrush = ThemeBrush("border"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(8, 6)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*,*,*")
        };

        AddFunctionButton(grid, 0, T("function.f1", "F1 Help"), async () => await ShowKeyboardHelpAsync(), "function.f1");
        AddFunctionButton(grid, 1, T("function.f2", "F2 Rename"), async () => await RenameSelectedAsync(), "function.f2");
        AddFunctionButton(grid, 2, T("function.f3", "F3 View"), async () => await ViewSelectedAsync(), "function.f3");
        AddFunctionButton(grid, 3, T("function.f4", "F4 Edit"), async () => await EditSelectedAsync(), "function.f4");
        AddFunctionButton(grid, 4, T("function.f5", "F5 Copy"), async () => await CopySelectedToPassiveAsync(), "function.f5");
        AddFunctionButton(grid, 5, T("function.f6", "F6 Move"), async () => await MoveSelectedToPassiveAsync(), "function.f6");
        AddFunctionButton(grid, 6, T("function.f7", "F7 MkDir"), async () => await CreateFolderInActiveAsync(), "function.f7");
        AddFunctionButton(grid, 7, T("function.f8", "F8 Delete"), async () => await SoftDeleteSelectedAsync(), "function.f8");
        AddFunctionButton(grid, 8, T("function.f9", "F9 Menu"), () => SetStatus(T("status.use_menu", "Use the menu bar at the top.")), "function.f9");
        AddFunctionButton(grid, 9, T("function.f10", "F10 Quit"), Close, "function.f10");

        outer.Child = grid;
        return outer;
    }

    private void AddFunctionButton(Grid grid, int column, string text, Action action, string tooltipKey)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(3, 0),
            MinHeight = 32,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        ApplyToolTip(button, tooltipKey);
        button.Click += (_, _) => action();
        Grid.SetColumn(button, column);
        grid.Children.Add(button);
    }

    private void AddFunctionButton(Grid grid, int column, string text, Func<Task> action, string tooltipKey)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(3, 0),
            MinHeight = 32,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        ApplyToolTip(button, tooltipKey);
        button.Click += async (_, _) => await action();
        Grid.SetColumn(button, column);
        grid.Children.Add(button);
    }

    private void OnMainKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            ActivatePane(ActivePane == _leftPane ? _rightPane : _leftPane);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ActivePane.Reload();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.F1:
                _ = ShowKeyboardHelpAsync();
                e.Handled = true;
                break;
            case Key.F2:
                _ = RenameSelectedAsync();
                e.Handled = true;
                break;
            case Key.F3:
                _ = ViewSelectedAsync();
                e.Handled = true;
                break;
            case Key.F4:
                _ = EditSelectedAsync();
                e.Handled = true;
                break;
            case Key.F5:
                _ = CopySelectedToPassiveAsync();
                e.Handled = true;
                break;
            case Key.F6:
                _ = MoveSelectedToPassiveAsync();
                e.Handled = true;
                break;
            case Key.F7:
                _ = CreateFolderInActiveAsync();
                e.Handled = true;
                break;
            case Key.F8:
            case Key.Delete:
                _ = SoftDeleteSelectedAsync();
                e.Handled = true;
                break;
            case Key.F10:
                Close();
                e.Handled = true;
                break;
        }
    }

    private FilePane ActivePane => _activePane;
    private FilePane PassivePane => ActivePane == _leftPane ? _rightPane : _leftPane;

    private void ActivatePane(FilePane pane)
    {
        _activePane = pane;
        _leftPane.SetActive(pane == _leftPane);
        _rightPane.SetActive(pane == _rightPane);
        UpdateSelection(pane.GetSelectedEntry());
        SetStatus($"Active panel: {pane.Title}  •  {pane.CurrentPath}");
    }

    private void UpdateSelection(FileEntry? item)
    {
        _selectionText.Text = item is null
            ? $"Active: {ActivePane.Title}"
            : $"{ActivePane.Title}: {item.Name}  •  {item.TypeText}  •  {item.SizeText}";
    }


    private void OpenSelected()
    {
        var selected = ActivePane.GetSelectedEntry();
        if (selected is not null)
        {
            OpenFileOrFolder(selected);
        }
    }

    private void OpenFileOrFolder(FileEntry item)
    {
        if (item.IsDirectory)
        {
            ActivePane.NavigateTo(item.FullPath);
            return;
        }

        _ = RunSafeAsync(() => PlatformOpen.OpenPath(item.FullPath), "Opened externally.");
    }

    private async Task RenameSelectedAsync()
    {
        var item = ActivePane.GetSelectedEntry();
        if (item is null)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        var newName = await ShowInputAsync("Rename", "New name:", item.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == item.Name)
        {
            return;
        }

        await RunSafeAsync(() =>
        {
            FileManager.Rename(item.FullPath, newName);
            ActivePane.Reload();
        }, "Renamed.");
    }

    private async Task CopySelectedToPassiveAsync()
    {
        var item = ActivePane.GetSelectedEntry();
        if (item is null)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        await RunSafeAsync(() =>
        {
            FileManager.CopyTo(item.FullPath, PassivePane.CurrentPath);
            PassivePane.Reload();
        }, $"Copied to {PassivePane.Title}: {PassivePane.CurrentPath}");
    }

    private async Task MoveSelectedToPassiveAsync()
    {
        var item = ActivePane.GetSelectedEntry();
        if (item is null)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        var ok = await ConfirmAsync("Move", $"Move this item to the passive panel?\n\nFrom:\n{item.FullPath}\n\nTo:\n{PassivePane.CurrentPath}");
        if (!ok)
        {
            return;
        }

        await RunSafeAsync(() =>
        {
            FileManager.MoveTo(item.FullPath, PassivePane.CurrentPath);
            ActivePane.Reload();
            PassivePane.Reload();
        }, $"Moved to {PassivePane.Title}: {PassivePane.CurrentPath}");
    }

    private async Task CreateFolderInActiveAsync()
    {
        var name = await ShowInputAsync("Make directory", "Folder name:", "New Folder");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await RunSafeAsync(() =>
        {
            var path = Path.Combine(ActivePane.CurrentPath, name.Trim());
            Directory.CreateDirectory(path);
            ActivePane.Reload();
        }, "Folder created.");
    }

    private async Task SoftDeleteSelectedAsync()
    {
        var item = ActivePane.GetSelectedEntry();
        if (item is null)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        var ok = await ConfirmAsync("Soft delete", $"Move this item to ~/.ModernCommanderDeskTrash?\n\n{item.FullPath}");
        if (!ok)
        {
            return;
        }

        await RunSafeAsync(() =>
        {
            var target = FileManager.SoftDelete(item.FullPath);
            ActivePane.Reload();
            SetStatus($"Moved to trash folder: {target}");
        }, "Deleted.");
    }

    private async Task CopyPathSelectedAsync()
    {
        var item = ActivePane.GetSelectedEntry();
        if (item is null)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            SetStatus("Clipboard is not available.", isError: true);
            return;
        }

        await clipboard.SetTextAsync(item.FullPath);
        SetStatus("Path copied to clipboard.");
    }

    private async Task ViewSelectedAsync()
    {
        var item = ActivePane.GetSelectedEntry();
        if (item is null)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        if (item.IsDirectory)
        {
            ActivePane.NavigateTo(item.FullPath);
            return;
        }

        await ShowPreviewAsync(item);
    }

    private async Task EditSelectedAsync()
    {
        var item = ActivePane.GetSelectedEntry();
        if (item is null)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        if (item.IsDirectory)
        {
            SetStatus("Folders cannot be edited.", isError: true);
            return;
        }

        if (!FileManager.IsTextPreviewCandidate(item.FullPath))
        {
            PlatformOpen.OpenPath(item.FullPath);
            return;
        }

        await ShowEditorAsync(item);
    }

    private async Task OpenTerminalInActiveAsync()
    {
        await RunSafeAsync(() => PlatformOpen.OpenTerminal(ActivePane.CurrentPath), "Terminal requested.");
    }

    private void SwapPanels()
    {
        var left = _leftPane.CurrentPath;
        var right = _rightPane.CurrentPath;
        _leftPane.NavigateTo(right, addHistory: false);
        _rightPane.NavigateTo(left, addHistory: false);
        SetStatus("Panels swapped.");
    }

    private void RefreshBoth()
    {
        _leftPane.Reload();
        _rightPane.Reload();
        SetStatus("Both panels refreshed.");
    }


    private async Task HandleDroppedItemsAsync(FilePane targetPane, IReadOnlyList<string> sourcePaths)
    {
        var paths = sourcePaths
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToList();

        if (paths.Count == 0)
        {
            SetStatus(T("status.drop_no_valid_paths", "Drop did not contain valid files or folders."), isError: true);
            return;
        }

        ActivatePane(targetPane);
        var targetDirectory = targetPane.CurrentPath;
        if (!Directory.Exists(targetDirectory))
        {
            SetStatus(T("status.drop_target_invalid", "Drop target is not a readable folder."), isError: true);
            return;
        }

        if (paths.All(path => string.Equals(GetParentDirectory(path), NormalizeDirectory(targetDirectory), CurrentPathComparison())))
        {
            SetStatus(T("status.drop_same_folder", "Dropped item is already in the target folder."), isError: true);
            return;
        }

        await ShowDropActionAndExecuteAsync(targetPane, targetDirectory, paths);
    }

    private async Task ShowDropActionAndExecuteAsync(FilePane targetPane, string targetDirectory, IReadOnlyList<string> paths)
    {
        var dialog = new Window
        {
            Title = T("dialog.drop_action", "Drag & Drop action"),
            Width = 720,
            Height = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeBrush("dialog_bg")
        };

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            Margin = new Thickness(16)
        };

        var header = new TextBlock
        {
            Text = string.Format(T("dialog.drop_message", "Copy or move the dropped item(s) to {0}?"), targetPane.Title),
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeBrush("text"),
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var listText = string.Join(Environment.NewLine, paths.Take(12).Select(path => "• " + path));
        if (paths.Count > 12)
        {
            listText += Environment.NewLine + string.Format(T("dialog.drop_more", "… and {0} more"), paths.Count - 12);
        }

        var details = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            Text = T("dialog.drop_target", "Target:") + Environment.NewLine + targetDirectory + Environment.NewLine + Environment.NewLine + listText,
            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(details, 1);
        root.Children.Add(details);

        var progressArea = new StackPanel
        {
            Orientation = Orientation.Vertical,
            IsVisible = false,
            Margin = new Thickness(0, 12, 0, 8)
        };

        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 18
        };
        progressArea.Children.Add(progressBar);

        var progressText = new TextBlock
        {
            Text = string.Empty,
            Foreground = ThemeBrush("muted"),
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        progressArea.Children.Add(progressText);
        Grid.SetRow(progressArea, 2);
        root.Children.Add(progressArea);

        var buttons = DialogButtons();
        var copy = new Button { Content = T("button.copy", "Copy"), Width = 100, IsDefault = true };
        var move = new Button { Content = T("button.move", "Move"), Width = 100 };
        var cancel = new Button { Content = T("button.cancel", "Cancel"), Width = 100, IsCancel = true };

        async Task ExecuteAsync(string action)
        {
            copy.IsEnabled = false;
            move.IsEnabled = false;
            cancel.IsEnabled = false;
            progressArea.IsVisible = true;
            progressBar.IsIndeterminate = true;
            progressText.Text = T("dialog.drop_preparing", "Preparing file operation…");

            var isCopy = action == "copy";
            var startedUtc = DateTime.UtcNow;
            header.Text = isCopy
                ? T("dialog.drop_copying", "Copying dropped item(s)…")
                : T("dialog.drop_moving", "Moving dropped item(s)…");
            details.Text = string.Format(T("dialog.drop_working", "Please wait. Processing {0} item(s) to:\n{1}"), paths.Count, targetDirectory);
            SetStatus(header.Text);

            void ApplyProgress(DropTransferProgress progress)
            {
                progressBar.IsIndeterminate = progress.Indeterminate;
                var percent = CalculateProgressPercent(progress);
                progressBar.Value = percent;

                var eta = EstimateEta(startedUtc, percent);
                var etaText = eta is null
                    ? T("dialog.drop_eta_unknown", "ETA: calculating…")
                    : string.Format(T("dialog.drop_eta", "ETA: {0}"), FormatDuration(eta.Value));

                var current = string.IsNullOrWhiteSpace(progress.CurrentPath)
                    ? string.Empty
                    : Environment.NewLine + string.Format(T("dialog.drop_current", "Current: {0}"), Path.GetFileName(progress.CurrentPath));

                progressText.Text = string.Format(
                    T("dialog.drop_progress", "{0:0.0}% - {1}/{2} items - {3} / {4} - {5}"),
                    percent,
                    Math.Min(progress.CompletedItems, progress.TotalItems),
                    progress.TotalItems,
                    FormatBytes(progress.CompletedBytes),
                    FormatBytes(progress.TotalBytes),
                    etaText) + current;
            }

            var progressReporter = new Progress<DropTransferProgress>(ApplyProgress);

            try
            {
                await Task.Run(() => ExecuteDropTransfer(isCopy, targetDirectory, paths, progressReporter));

                _leftPane.Reload();
                _rightPane.Reload();

                SetStatus(isCopy
                    ? string.Format(T("status.drop_copied", "Copied {0} item(s) to {1}."), paths.Count, targetPane.Title)
                    : string.Format(T("status.drop_moved", "Moved {0} item(s) to {1}."), paths.Count, targetPane.Title));

                dialog.Close(true);
            }
            catch (Exception ex)
            {
                progressBar.IsIndeterminate = false;
                header.Text = T("dialog.drop_failed", "Drag & Drop operation failed.");
                details.Text = ex.Message;
                progressText.Text = ex.Message;
                copy.IsEnabled = true;
                move.IsEnabled = true;
                cancel.IsEnabled = true;
                SetStatus(ex.Message, isError: true);
            }
        }

        copy.Click += async (_, _) => await ExecuteAsync("copy");
        move.Click += async (_, _) => await ExecuteAsync("move");
        cancel.Click += (_, _) => dialog.Close(false);
        buttons.Children.Add(copy);
        buttons.Children.Add(move);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 3);
        root.Children.Add(buttons);

        dialog.Content = root;
        await dialog.ShowDialog<bool>(this);
    }

    private sealed record DropTransferProgress(int TotalItems, int CompletedItems, long TotalBytes, long CompletedBytes, string CurrentPath, bool Indeterminate = false);

    private sealed record DropTransferStats(int Items, long Bytes);

    private static void ExecuteDropTransfer(bool copy, string targetDirectory, IReadOnlyList<string> paths, IProgress<DropTransferProgress> progress)
    {
        var total = CalculateTransferStats(paths);
        var totalItems = Math.Max(1, total.Items);
        var totalBytes = Math.Max(0, total.Bytes);
        var completedItems = 0;
        long completedBytes = 0;

        void Report(long bytesAdded, bool itemFinished, string currentPath, bool indeterminate = false)
        {
            if (bytesAdded > 0)
            {
                completedBytes += bytesAdded;
            }

            if (itemFinished)
            {
                completedItems++;
            }

            progress.Report(new DropTransferProgress(
                totalItems,
                Math.Min(completedItems, totalItems),
                totalBytes,
                Math.Min(completedBytes, totalBytes),
                currentPath,
                indeterminate));
        }

        progress.Report(new DropTransferProgress(totalItems, 0, totalBytes, 0, string.Empty, Indeterminate: true));

        foreach (var sourcePath in paths)
        {
            if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            {
                continue;
            }

            var sourceStats = CalculateTransferStats(new[] { sourcePath });
            var targetPath = GetNonCollidingDropPath(Path.Combine(targetDirectory, GetFileSystemName(sourcePath)));

            if (Directory.Exists(sourcePath) && IsSameOrChildPath(targetDirectory, sourcePath))
            {
                throw new IOException("Cannot copy or move a folder into itself or one of its subfolders.");
            }

            if (!copy)
            {
                try
                {
                    progress.Report(new DropTransferProgress(totalItems, completedItems, totalBytes, completedBytes, sourcePath, Indeterminate: true));
                    if (Directory.Exists(sourcePath))
                    {
                        Directory.Move(sourcePath, targetPath);
                    }
                    else
                    {
                        File.Move(sourcePath, targetPath);
                    }

                    completedBytes += sourceStats.Bytes;
                    completedItems += Math.Max(1, sourceStats.Items);
                    progress.Report(new DropTransferProgress(totalItems, Math.Min(completedItems, totalItems), totalBytes, Math.Min(completedBytes, totalBytes), sourcePath));
                    continue;
                }
                catch (IOException)
                {
                    // Cross-volume moves and some shell-like moves need copy+delete fallback so progress remains visible.
                }
            }

            CopyResolvedPathWithProgress(sourcePath, targetPath, Report);

            if (!copy)
            {
                if (Directory.Exists(sourcePath))
                {
                    Directory.Delete(sourcePath, recursive: true);
                }
                else if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                }
            }
        }

        progress.Report(new DropTransferProgress(totalItems, totalItems, totalBytes, totalBytes, string.Empty));
    }

    private static void CopyResolvedPathWithProgress(string sourcePath, string targetPath, Action<long, bool, string, bool> report)
    {
        if (Directory.Exists(sourcePath))
        {
            CopyDirectoryWithProgress(sourcePath, targetPath, report);
        }
        else if (File.Exists(sourcePath))
        {
            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            CopyFileWithProgress(sourcePath, targetPath, report);
        }
    }

    private static void CopyDirectoryWithProgress(string sourceDirectory, string targetDirectory, Action<long, bool, string, bool> report)
    {
        Directory.CreateDirectory(targetDirectory);
        report(0, true, sourceDirectory, false);

        foreach (var directory in EnumerateDirectoriesSafe(sourceDirectory))
        {
            var relative = Path.GetRelativePath(sourceDirectory, directory);
            var destination = Path.Combine(targetDirectory, relative);
            Directory.CreateDirectory(destination);
            report(0, true, directory, false);
        }

        foreach (var file in EnumerateFilesSafe(sourceDirectory))
        {
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var destination = Path.Combine(targetDirectory, relative);
            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            CopyFileWithProgress(file, destination, report);
        }
    }

    private static void CopyFileWithProgress(string sourceFile, string targetFile, Action<long, bool, string, bool> report)
    {
        const int bufferSize = 1024 * 1024;
        var parent = Path.GetDirectoryName(targetFile);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        using var source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, FileOptions.SequentialScan);
        using var target = new FileStream(targetFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);
        var buffer = new byte[bufferSize];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            target.Write(buffer, 0, read);
            report(read, false, sourceFile, false);
        }

        report(0, true, sourceFile, false);
    }

    private static DropTransferStats CalculateTransferStats(IEnumerable<string> paths)
    {
        var items = 0;
        long bytes = 0;

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                items++;
                bytes += SafeFileLength(path);
            }
            else if (Directory.Exists(path))
            {
                items++;
                foreach (var directory in EnumerateDirectoriesSafe(path))
                {
                    items++;
                }

                foreach (var file in EnumerateFilesSafe(path))
                {
                    items++;
                    bytes += SafeFileLength(file);
                }
            }
        }

        return new DropTransferStats(Math.Max(1, items), bytes);
    }

    private static IEnumerable<string> EnumerateDirectoriesSafe(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            string[] directories;
            try
            {
                directories = Directory.GetDirectories(current);
            }
            catch
            {
                continue;
            }

            foreach (var directory in directories)
            {
                yield return directory;
                stack.Push(directory);
            }
        }
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            string[] files;
            try
            {
                files = Directory.GetFiles(current);
            }
            catch
            {
                files = Array.Empty<string>();
            }

            foreach (var file in files)
            {
                yield return file;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(current);
            }
            catch
            {
                continue;
            }

            foreach (var directory in directories)
            {
                stack.Push(directory);
            }
        }
    }

    private static long SafeFileLength(string path)
    {
        try { return new FileInfo(path).Length; }
        catch { return 0; }
    }

    private static string GetFileSystemName(string path)
        => Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private static string GetNonCollidingDropPath(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var index = 2;
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            index++;
        } while (File.Exists(candidate) || Directory.Exists(candidate));

        return candidate;
    }

    private static bool IsSameOrChildPath(string candidatePath, string parentPath)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var parent = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(parent, comparison);
    }

    private static double CalculateProgressPercent(DropTransferProgress progress)
    {
        if (progress.TotalBytes > 0)
        {
            return Math.Clamp((double)progress.CompletedBytes / progress.TotalBytes * 100.0, 0.0, 100.0);
        }

        if (progress.TotalItems > 0)
        {
            return Math.Clamp((double)progress.CompletedItems / progress.TotalItems * 100.0, 0.0, 100.0);
        }

        return 0;
    }

    private static TimeSpan? EstimateEta(DateTime startedUtc, double percent)
    {
        if (percent < 1.0)
        {
            return null;
        }

        var elapsed = DateTime.UtcNow - startedUtc;
        var fraction = percent / 100.0;
        if (fraction <= 0 || elapsed.TotalSeconds < 1)
        {
            return null;
        }

        var totalSeconds = elapsed.TotalSeconds / fraction;
        var remaining = Math.Max(0, totalSeconds - elapsed.TotalSeconds);
        return TimeSpan.FromSeconds(remaining);
    }

    private static string FormatDuration(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours:0}h {time.Minutes:00}m";
        }

        if (time.TotalMinutes >= 1)
        {
            return $"{(int)time.TotalMinutes:0}m {time.Seconds:00}s";
        }

        return $"{Math.Max(0, (int)time.TotalSeconds):0}s";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.0} {units[unit]}";
    }

    private static string GetParentDirectory(string path)
    {
        var clean = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return NormalizeDirectory(Path.GetDirectoryName(clean) ?? clean);
    }

    private static string NormalizeDirectory(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private async Task ShowNavigationChooserAsync(FilePane targetPane)
    {
        var items = FileManager.BuildNavigationItems();
        if (items.Count == 0)
        {
            SetStatus(T("status.no_locations", "No readable locations or drives found."), isError: true);
            return;
        }

        ActivatePane(targetPane);
        var dialog = new Window
        {
            Title = T("dialog.locations", "Locations / drives"),
            Width = 640,
            Height = 540,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeBrush("dialog_bg")
        };

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(16)
        };

        var header = new TextBlock
        {
            Text = T("dialog.locations_message", "Choose a drive, mount point or common folder for the active panel."),
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeBrush("text"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var list = new ListBox
        {
            ItemsSource = items,
            SelectedIndex = 0
        };
        list.DoubleTapped += (_, _) =>
        {
            if (list.SelectedItem is NavItem item)
            {
                dialog.Close(item.Path);
            }
        };
        Grid.SetRow(list, 1);
        root.Children.Add(list);

        var buttons = DialogButtons();
        var open = new Button { Content = T("button.open", "Open"), Width = 100, IsDefault = true };
        var cancel = new Button { Content = T("button.cancel", "Cancel"), Width = 100, IsCancel = true };
        open.Click += (_, _) =>
        {
            if (list.SelectedItem is NavItem item)
            {
                dialog.Close(item.Path);
            }
        };
        cancel.Click += (_, _) => dialog.Close(null);
        buttons.Children.Add(open);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);

        dialog.Content = root;
        var selectedPath = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        targetPane.NavigateTo(selectedPath, addHistory: true);
        ActivatePane(targetPane);
        SetStatus(string.Format(T("status.location_changed", "{0} switched to {1}."), targetPane.Title, selectedPath));
    }

    private async Task ShowPreviewAsync(FileEntry item)
    {
        try
        {
            var result = await PreviewRegistry.CreatePreviewAsync(item.FullPath);
            var dialog = new Window
            {
                Title = result.Title,
                Width = 1050,
                Height = 760,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = ThemeBrush("window_bg")
            };

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Margin = new Thickness(12)
            };
            Grid.SetRow(result.View, 0);
            root.Children.Add(result.View);

            var buttons = DialogButtons();
            var openExternal = new Button { Content = T("button.open_external", "Open externally"), Width = 140 };
            var close = new Button { Content = T("button.close", "Close"), Width = 100, IsDefault = true };
            openExternal.Click += (_, _) => PlatformOpen.OpenPath(item.FullPath);
            close.Click += (_, _) => dialog.Close();
            buttons.Children.Add(openExternal);
            buttons.Children.Add(close);
            Grid.SetRow(buttons, 1);
            root.Children.Add(buttons);
            dialog.Content = root;
            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(T("dialog.viewer_error", "Viewer error"), ex.Message);
        }
    }

    private async Task ShowViewerAsync(FileEntry item)
    {
        string text;
        try
        {
            text = File.ReadAllText(item.FullPath);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Viewer error", ex.Message);
            return;
        }

        var dialog = new Window
        {
            Title = "View - " + item.Name,
            Width = 1000,
            Height = 720,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeBrush("window_bg")
        };

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Thickness(12)
        };
        var box = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
            Text = text
        };
        Grid.SetRow(box, 0);
        root.Children.Add(box);
        var close = new Button
        {
            Content = "Close",
            Width = 100,
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        close.Click += (_, _) => dialog.Close();
        Grid.SetRow(close, 1);
        root.Children.Add(close);
        dialog.Content = root;
        await dialog.ShowDialog(this);
    }

    private async Task ShowEditorAsync(FileEntry item)
    {
        string text;
        try
        {
            text = File.ReadAllText(item.FullPath);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Editor error", ex.Message);
            return;
        }

        var dialog = new Window
        {
            Title = "Edit - " + item.Name,
            Width = 1000,
            Height = 720,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeBrush("window_bg")
        };

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Thickness(12)
        };
        var box = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
            Text = text
        };
        Grid.SetRow(box, 0);
        root.Children.Add(box);

        var buttons = DialogButtons();
        var save = new Button { Content = "Save", Width = 100, IsDefault = true };
        var cancel = new Button { Content = "Cancel", Width = 100, IsCancel = true };
        save.Click += (_, _) =>
        {
            File.WriteAllText(item.FullPath, box.Text ?? string.Empty);
            dialog.Close(true);
        };
        cancel.Click += (_, _) => dialog.Close(false);
        buttons.Children.Add(save);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 1);
        root.Children.Add(buttons);
        dialog.Content = root;

        var saved = await dialog.ShowDialog<bool>(this);
        if (saved)
        {
            ActivePane.Reload();
            SetStatus("File saved.");
        }
    }

    private async Task ShowKeyboardHelpAsync()
    {
        await ShowMessageAsync(T("dialog.keyboard_help", "Keyboard help"), HelpService.LoadHelp(_settings.Language, "keyboard"));
    }

    private async Task RunSafeAsync(Action action, string successMessage = "")
    {
        try
        {
            action();
            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                SetStatus(successMessage);
            }
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
            await ShowMessageAsync("Error", ex.Message);
        }
    }

    private async Task<string?> ShowInputAsync(string title, string prompt, string defaultText)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeBrush("dialog_bg")
        };

        var root = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10
        };
        root.Children.Add(new TextBlock
        {
            Text = prompt,
            Foreground = ThemeBrush("text")
        });

        var input = new TextBox
        {
            Text = defaultText,
            MinHeight = 34,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        root.Children.Add(input);

        var buttons = DialogButtons();
        var ok = new Button { Content = "OK", Width = 90, IsDefault = true };
        var cancel = new Button { Content = "Cancel", Width = 90, IsCancel = true };
        ok.Click += (_, _) => dialog.Close(input.Text);
        cancel.Click += (_, _) => dialog.Close(null);
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);

        dialog.Content = root;
        return await dialog.ShowDialog<string?>(this);
    }

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 600,
            Height = 270,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeBrush("dialog_bg")
        };

        var root = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };
        root.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrush("text")
        });

        var buttons = DialogButtons();
        var ok = new Button { Content = "Yes", Width = 90, IsDefault = true };
        var cancel = new Button { Content = "Cancel", Width = 90, IsCancel = true };
        ok.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);

        dialog.Content = root;
        return await dialog.ShowDialog<bool>(this);
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 620,
            Height = 330,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ThemeBrush("dialog_bg")
        };

        var root = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };
        root.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeBrush("text")
        });

        var buttons = DialogButtons();
        var ok = new Button { Content = "OK", Width = 90, IsDefault = true };
        ok.Click += (_, _) => dialog.Close();
        buttons.Children.Add(ok);
        root.Children.Add(buttons);

        dialog.Content = root;
        await dialog.ShowDialog(this);
    }

    private static StackPanel DialogButtons() => new()
    {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Spacing = 8,
        Margin = new Thickness(0, 10, 0, 0)
    };

    private void SwitchTheme(string themeCode)
    {
        var normalized = ThemeCatalog.Normalize(themeCode);
        if (string.Equals(ThemeCatalog.Normalize(_settings.Theme), normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var leftPath = _leftPane?.CurrentPath ?? FileManager.GetHomePath();
        var rightPath = _rightPane?.CurrentPath ?? GetUsefulSecondPath();
        var activeWasLeft = _activePane == _leftPane;

        _settings.Theme = normalized;
        SettingsStore.Save(_settings);
        ApplyThemeFromSettings();

        Content = BuildUi();
        _leftPane!.NavigateTo(leftPath, addHistory: false);
        _rightPane!.NavigateTo(rightPath, addHistory: false);
        ActivatePane(activeWasLeft ? _leftPane : _rightPane);
        SetStatus(T("status.theme_changed", "Theme changed."));
    }

    private void ApplyThemeFromSettings()
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = ThemeCatalog.UsesLightFluentVariant(_settings.Theme)
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
        Background = ThemeBrush("window_bg");
    }

    private void SwitchLanguage(string languageCode)
    {
        var leftPath = _leftPane?.CurrentPath ?? FileManager.GetHomePath();
        var rightPath = _rightPane?.CurrentPath ?? GetUsefulSecondPath();
        var activeWasLeft = _activePane == _leftPane;

        _settings.Language = languageCode;
        SettingsStore.Save(_settings);
        _loc = LocalizationService.Load(_settings.Language);
        _tooltips = ToolTipCatalog.Load(_settings.Language, _settings.TooltipsEnabled);
        Title = "Modern Commander Desk v0.4.5 - " + T("window.subtitle", "Dual Pane Commander");

        Content = BuildUi();
        _leftPane!.NavigateTo(leftPath, addHistory: false);
        _rightPane!.NavigateTo(rightPath, addHistory: false);
        ActivatePane(activeWasLeft ? _leftPane : _rightPane);
        SetStatus(T("status.language_changed", "Language changed."));
    }

    private void ToggleTooltips()
    {
        var leftPath = _leftPane?.CurrentPath ?? FileManager.GetHomePath();
        var rightPath = _rightPane?.CurrentPath ?? GetUsefulSecondPath();
        var activeWasLeft = _activePane == _leftPane;

        _settings.TooltipsEnabled = !_settings.TooltipsEnabled;
        SettingsStore.Save(_settings);
        _tooltips = ToolTipCatalog.Load(_settings.Language, _settings.TooltipsEnabled);

        Content = BuildUi();
        _leftPane!.NavigateTo(leftPath, addHistory: false);
        _rightPane!.NavigateTo(rightPath, addHistory: false);
        ActivatePane(activeWasLeft ? _leftPane : _rightPane);
        SetStatus(_settings.TooltipsEnabled ? T("status.tooltips_on", "Tooltips enabled.") : T("status.tooltips_off", "Tooltips disabled."));
    }

    private string T(string key, string fallback) => _loc.T(key, fallback);


    private void ApplyToolTip(Control control, string key)
    {
        if (_settings.TooltipsEnabled)
        {
            ToolTip.SetTip(control, _tooltips.Tip(key));
        }
        else
        {
            ToolTip.SetTip(control, null);
        }
    }

    private void SetStatus(string text, bool isError = false)
    {
        _statusText.Text = text;
        _statusText.Foreground = isError ? ThemeBrush("error") : ThemeBrush("status_text");
    }

    private static string GetUsefulSecondPath()
    {
        var home = FileManager.GetHomePath();
        var downloads = Path.Combine(home, "Downloads");
        return Directory.Exists(downloads) ? downloads : home;
    }

    private static StringComparison CurrentPathComparison()
        => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private IBrush ThemeBrush(string role) => ThemeCatalog.Brush(_settings.Theme, role);
}

internal sealed class FilePane
{
    private readonly ObservableCollection<FileEntry> _entries = [];
    private readonly Stack<string> _backHistory = new();
    private readonly Stack<string> _forwardHistory = new();
    private readonly Action<FilePane> _activate;
    private readonly Action<FilePane, FileEntry?> _selectionChanged;
    private readonly Action<FilePane, FileEntry> _openRequest;
    private readonly Func<FilePane, IReadOnlyList<string>, Task> _dropRequest;
    private readonly Func<FilePane, Task> _locationsRequest;
    private Point? _dragStartPoint;
    private FileEntry? _dragCandidateEntry;
    private bool _dragInProgress;
    public const string DragFormat = "application/x-modern-commander-desk-paths";
    private readonly LocalizationService _loc;
    private readonly ToolTipCatalog _tooltips;
    private readonly bool _tooltipsEnabled;
    private readonly string _theme;
    private readonly Border _outer;
    private readonly TextBlock _titleText;
    private readonly TextBlock _summaryText;
    private readonly TextBox _pathBox;
    private readonly TextBox _filterBox;
    private readonly DataGrid _grid;

    public FilePane(string title,
        Action<FilePane> activate,
        Action<FilePane, FileEntry?> selectionChanged,
        Action<FilePane, FileEntry> openRequest,
        Func<FilePane, IReadOnlyList<string>, Task> dropRequest,
        Func<FilePane, Task> locationsRequest,
        LocalizationService loc,
        ToolTipCatalog tooltips,
        bool tooltipsEnabled,
        string theme)
    {
        _loc = loc;
        _tooltips = tooltips;
        _tooltipsEnabled = tooltipsEnabled;
        _theme = ThemeCatalog.Normalize(theme);
        Title = title == "LEFT" ? _loc.T("pane.left", "LEFT") : _loc.T("pane.right", "RIGHT");
        _activate = activate;
        _selectionChanged = selectionChanged;
        _openRequest = openRequest;
        _dropRequest = dropRequest;
        _locationsRequest = locationsRequest;
        CurrentPath = FileManager.GetHomePath();

        _outer = new Border
        {
            Background = ThemeBrush("panel_bg"),
            BorderBrush = ThemeBrush("border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8)
        };
        _outer.PointerPressed += (_, _) => _activate(this);

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto")
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto,Auto"),
            Margin = new Thickness(0, 0, 0, 6)
        };

        _titleText = new TextBlock
        {
            Text = Title,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeBrush("text"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 10, 0)
        };
        Grid.SetColumn(_titleText, 0);
        header.Children.Add(_titleText);

        _pathBox = new TextBox
        {
            Watermark = _loc.T("pane.path_watermark", "Path"),
            MinHeight = 32,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _pathBox.GotFocus += (_, _) => _activate(this);
        _pathBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(_pathBox.Text))
            {
                NavigateTo(_pathBox.Text.Trim());
                e.Handled = true;
            }
        };
        ApplyToolTip(_pathBox, "pane.path");
        Grid.SetColumn(_pathBox, 1);
        header.Children.Add(_pathBox);

        var locations = SmallButton("💽", "pane.locations", async () => await _locationsRequest(this));
        Grid.SetColumn(locations, 2);
        header.Children.Add(locations);
        var back = SmallButton("←", "pane.back", NavigateBack);
        Grid.SetColumn(back, 3);
        header.Children.Add(back);
        var up = SmallButton("↑", "pane.up", NavigateUp);
        Grid.SetColumn(up, 4);
        header.Children.Add(up);
        var refresh = SmallButton("⟳", "pane.refresh", () => Reload());
        Grid.SetColumn(refresh, 5);
        header.Children.Add(refresh);

        Grid.SetRow(header, 0);
        root.Children.Add(header);

        _filterBox = new TextBox
        {
            Watermark = _loc.T("pane.filter_watermark", "Filter this panel…"),
            MinHeight = 30,
            Margin = new Thickness(0, 0, 0, 6),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _filterBox.GotFocus += (_, _) => _activate(this);
        _filterBox.TextChanged += (_, _) => Reload(quiet: true);
        ApplyToolTip(_filterBox, "pane.filter");
        Grid.SetRow(_filterBox, 1);
        root.Children.Add(_filterBox);

        _grid = new DataGrid
        {
            ItemsSource = _entries,
            AutoGenerateColumns = false,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.None,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            SelectionMode = DataGridSelectionMode.Single,
            CanUserSortColumns = true,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            RowHeight = 32
        };
        _grid.Columns.Add(new DataGridTextColumn { Header = "", Binding = new Binding("Icon"), Width = new DataGridLength(38) });
        _grid.Columns.Add(new DataGridTextColumn { Header = _loc.T("grid.name", "Name"), Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _grid.Columns.Add(new DataGridTextColumn { Header = _loc.T("grid.ext", "Ext"), Binding = new Binding("Extension"), Width = new DataGridLength(70) });
        _grid.Columns.Add(new DataGridTextColumn { Header = _loc.T("grid.size", "Size"), Binding = new Binding("SizeText"), Width = new DataGridLength(105) });
        _grid.Columns.Add(new DataGridTextColumn { Header = _loc.T("grid.modified", "Modified"), Binding = new Binding("ModifiedText"), Width = new DataGridLength(145) });
        _grid.GotFocus += (_, _) => _activate(this);
        _grid.AddHandler(InputElement.PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _grid.AddHandler(InputElement.PointerMovedEvent, OnGridPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _grid.AddHandler(InputElement.PointerReleasedEvent, OnGridPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _grid.SelectionChanged += (_, _) => _selectionChanged(this, GetSelectedEntry());
        _grid.DoubleTapped += (_, _) => OpenSelected();
        _grid.KeyDown += (_, e) =>
        {
            _activate(this);
            if (e.Key == Key.Enter)
            {
                OpenSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                NavigateUp();
                e.Handled = true;
            }
        };

        _grid.ContextMenu = BuildContextMenu();
        EnableDropTarget(_grid);
        EnableDropTarget(_outer);

        Grid.SetRow(_grid, 2);
        root.Children.Add(_grid);

        _summaryText = new TextBlock
        {
            Text = _loc.T("pane.zero_items", "0 items"),
            Foreground = ThemeBrush("muted"),
            Margin = new Thickness(4, 6, 0, 0)
        };
        Grid.SetRow(_summaryText, 3);
        root.Children.Add(_summaryText);

        _outer.Child = root;
        Control = _outer;
    }

    public string Title { get; }
    public string CurrentPath { get; private set; }
    public Control Control { get; }

    public FileEntry? GetSelectedEntry() => _grid.SelectedItem as FileEntry;

    public void SetActive(bool active)
    {
        _outer.BorderBrush = active ? ThemeBrush("active") : ThemeBrush("border");
        _outer.BorderThickness = active ? new Thickness(2) : new Thickness(1);
        _titleText.Foreground = active ? ThemeBrush("active_text") : ThemeBrush("text");
    }

    public void NavigateTo(string path, bool addHistory = true)
    {
        try
        {
            var expandedPath = ExpandPath(path);
            if (File.Exists(expandedPath))
            {
                _openRequest(this, new FileEntry
                {
                    Name = Path.GetFileName(expandedPath),
                    FullPath = expandedPath,
                    IsDirectory = false,
                    SizeBytes = new FileInfo(expandedPath).Length,
                    Modified = File.GetLastWriteTime(expandedPath),
                    Extension = Path.GetExtension(expandedPath)
                });
                return;
            }

            if (!Directory.Exists(expandedPath))
            {
                return;
            }

            var normalized = Path.GetFullPath(expandedPath);
            if (addHistory && !string.Equals(CurrentPath, normalized, CurrentPathComparison()))
            {
                _backHistory.Push(CurrentPath);
                _forwardHistory.Clear();
            }

            CurrentPath = normalized;
            _pathBox.Text = CurrentPath;
            Reload(quiet: true);
        }
        catch
        {
            // Keep the pane stable if a path is inaccessible.
        }
    }

    public void Reload(bool quiet = false)
    {
        try
        {
            var selectedPath = GetSelectedEntry()?.FullPath;
            _entries.Clear();
            foreach (var entry in FileManager.ListDirectory(CurrentPath, _filterBox.Text))
            {
                _entries.Add(entry);
            }

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                var restore = _entries.FirstOrDefault(x => string.Equals(x.FullPath, selectedPath, CurrentPathComparison()));
                if (restore is not null)
                {
                    _grid.SelectedItem = restore;
                }
            }

            var fileCount = _entries.Count(x => !x.IsDirectory);
            var folderCount = _entries.Count(x => x.IsDirectory);
            _summaryText.Text = string.Format(_loc.T("pane.summary", "{0} folders, {1} files"), folderCount, fileCount);
            _selectionChanged(this, GetSelectedEntry());
        }
        catch
        {
            _entries.Clear();
            _summaryText.Text = _loc.T("pane.cannot_read", "Cannot read this folder.");
        }
    }

    public void NavigateBack()
    {
        if (_backHistory.Count == 0)
        {
            return;
        }

        _forwardHistory.Push(CurrentPath);
        NavigateTo(_backHistory.Pop(), addHistory: false);
    }

    public void NavigateForward()
    {
        if (_forwardHistory.Count == 0)
        {
            return;
        }

        _backHistory.Push(CurrentPath);
        NavigateTo(_forwardHistory.Pop(), addHistory: false);
    }

    public void NavigateUp()
    {
        var parent = Directory.GetParent(CurrentPath);
        if (parent is not null)
        {
            NavigateTo(parent.FullName);
            return;
        }

        _ = _locationsRequest(this);
    }


#pragma warning disable CS0618 // Avalonia legacy drag/drop API is kept here for compatibility with the current project target.
    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _activate(this);

        var point = e.GetCurrentPoint(_grid);
        if (!point.Properties.IsLeftButtonPressed)
        {
            ClearDragState();
            return;
        }

        var entry = GetEntryFromPointerEvent(e);
        if (entry is null)
        {
            ClearDragState();
            return;
        }

        _grid.SelectedItem = entry;
        _dragCandidateEntry = entry;
        _dragStartPoint = e.GetPosition(_grid);
        _dragInProgress = false;
    }

    private async void OnGridPointerMoved(object? sender, PointerEventArgs e)
    {
        await StartDragIfNeededAsync(e);
    }

    private void OnGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        ClearDragState();
    }

    private FileEntry? GetEntryFromPointerEvent(PointerEventArgs e)
    {
        if (e.Source is Avalonia.Visual visual)
        {
            var row = visual.GetSelfAndVisualAncestors().OfType<DataGridRow>().FirstOrDefault();
            if (row?.DataContext is FileEntry entry)
            {
                return entry;
            }
        }

        return GetSelectedEntry();
    }

    private void ClearDragState()
    {
        _dragStartPoint = null;
        _dragCandidateEntry = null;
        _dragInProgress = false;
    }

    private async Task StartDragIfNeededAsync(PointerEventArgs e)
    {
        if (_dragStartPoint is null || _dragInProgress)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(_grid);
        if (!pointer.Properties.IsLeftButtonPressed)
        {
            ClearDragState();
            return;
        }

        var current = e.GetPosition(_grid);
        var dx = Math.Abs(current.X - _dragStartPoint.Value.X);
        var dy = Math.Abs(current.Y - _dragStartPoint.Value.Y);
        if (Math.Max(dx, dy) < 6)
        {
            return;
        }

        var item = _dragCandidateEntry ?? GetSelectedEntry();
        if (item is null)
        {
            return;
        }

        _dragInProgress = true;
        var data = new DataObject();
        data.Set(DragFormat, item.FullPath);
        data.Set(DataFormats.Text, item.FullPath);

        try
        {
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
        finally
        {
            ClearDragState();
        }
    }

    private void EnableDropTarget(Control control)
    {
        DragDrop.SetAllowDrop(control, true);
        control.AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        control.AddHandler(DragDrop.DropEvent, async (_, e) => await OnDropAsync(e), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var paths = ExtractDragPaths(e.Data);
        e.DragEffects = paths.Count > 0 ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async Task OnDropAsync(DragEventArgs e)
    {
        var paths = ExtractDragPaths(e.Data);
        if (paths.Count == 0)
        {
            return;
        }

        _activate(this);
        e.Handled = true;
        await _dropRequest(this, paths);
    }

    private static IReadOnlyList<string> ExtractDragPaths(IDataObject data)
    {
        var result = new List<string>();
        if (data.Contains(DragFormat) && data.Get(DragFormat) is string payload)
        {
            result.AddRange(SplitPaths(payload));
        }
        else if (data.Contains(DataFormats.Text) && data.Get(DataFormats.Text) is string text)
        {
            result.AddRange(SplitPaths(text));
        }

        return result
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<string> SplitPaths(string value)
    {
        return value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
#pragma warning restore CS0618

    private void OpenSelected()
    {
        var item = GetSelectedEntry();
        if (item is not null)
        {
            _openRequest(this, item);
        }
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        var open = new MenuItem { Header = _loc.T("context.open", "Open") };
        open.Click += (_, _) => OpenSelected();
        var refresh = new MenuItem { Header = _loc.T("context.refresh_panel", "Refresh panel") };
        refresh.Click += (_, _) => Reload();
        var up = new MenuItem { Header = _loc.T("context.go_up", "Go up") };
        up.Click += (_, _) => NavigateUp();
        var locations = new MenuItem { Header = _loc.T("context.locations", "Locations / drives") };
        locations.Click += async (_, _) => await _locationsRequest(this);
        menu.Items.Add(open);
        menu.Items.Add(new Separator());
        menu.Items.Add(up);
        menu.Items.Add(locations);
        menu.Items.Add(refresh);
        return menu;
    }

    private Button SmallButton(string text, string tooltipKey, Action action)
    {
        var button = new Button
        {
            Content = text,
            Width = 34,
            Height = 32,
            Margin = new Thickness(5, 0, 0, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ApplyToolTip(button, tooltipKey);
        button.Click += (_, _) =>
        {
            _activate(this);
            action();
        };
        return button;
    }


    private Button SmallButton(string text, string tooltipKey, Func<Task> action)
    {
        var button = new Button
        {
            Content = text,
            Width = 34,
            Height = 32,
            Margin = new Thickness(5, 0, 0, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ApplyToolTip(button, tooltipKey);
        button.Click += async (_, _) =>
        {
            _activate(this);
            await action();
        };
        return button;
    }

    private void ApplyToolTip(Control control, string key)
    {
        if (_tooltipsEnabled)
        {
            ToolTip.SetTip(control, _tooltips.Tip(key));
        }
        else
        {
            ToolTip.SetTip(control, null);
        }
    }

    private static string ExpandPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return FileManager.GetHomePath();
        }

        var expanded = path.Trim();
        if (expanded == "~")
        {
            return FileManager.GetHomePath();
        }

        if (expanded.StartsWith("~/") || expanded.StartsWith("~\\"))
        {
            return Path.Combine(FileManager.GetHomePath(), expanded[2..]);
        }

        return Environment.ExpandEnvironmentVariables(expanded);
    }

    private static StringComparison CurrentPathComparison()
        => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private IBrush ThemeBrush(string role) => ThemeCatalog.Brush(_theme, role);
}
