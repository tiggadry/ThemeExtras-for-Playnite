using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Extras
{
    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public class GamePropertyAttribute : DontSerializeAttribute { }

    public class GameProperties : ObservableObject
    {
        public static readonly GameProperties Instance = new GameProperties();

        private GameProperties() { }

        private bool hidden;
        public bool Hidden
        {
            get => hidden;
            set => SetValue(ref hidden, value);
        }

        private string notes;
        public string Notes
        {
            get => notes;
            set => SetValue(ref notes, value);
        }

        private bool favorite;
        public bool Favorite
        {
            get => favorite;
            set => SetValue(ref favorite, value);
        }

        private string completion;
        public string Completion
        {
            get => completion;
            set => SetValue(ref completion, value);
        }
    }

    public class ExtrasSettings : ObservableObject
    {
        private bool enableGameMenuRating = false;
        public bool EnableGameMenuRating
        {
            get => enableGameMenuRating;
            set => SetValue(ref enableGameMenuRating, value);
        }

        private bool enableSelectionPreservation = true;
        public bool EnableSelectionPreservation
        {
            get => enableSelectionPreservation;
            set => SetValue(ref enableSelectionPreservation, value);
        }

        private bool backupAndRestore = true;
        public bool BackupAndRestore
        {
            get => backupAndRestore;
            set => SetValue(ref backupAndRestore, value);
        }

        private string lastThemeId = null;
        public string LastThemeId
        {
            get => lastThemeId;
            set => SetValue(ref lastThemeId, value);
        }

        private bool applyThemeIconOnChange = false;
        public bool ApplyThemeIconOnChange
        {
            get => applyThemeIconOnChange;
            set => SetValue(ref applyThemeIconOnChange, value);
        }

        private Dictionary<string, Dictionary<string, string>> persistentResources =
            new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, Dictionary<string, string>> PersistentResources
        {
            get => persistentResources;
            set => SetValue(ref persistentResources, value);
        }

        [DontSerialize]
        public CommandSettings Commands { get; } = CommandSettings.Instance;

        [DontSerialize]
        public GameProperties Game { get; } = GameProperties.Instance;

        [DontSerialize]
        public Menus Menus { get; } = Menus.Instance;

        [DontSerialize]
        public ObservableCollection<Game> RunningGames { get; } = new ObservableCollection<Game>();

        private bool isAnyGameRunning = false;

        [DontSerialize]
        public bool IsAnyGameRunning
        {
            get => isAnyGameRunning;
            set => SetValue(ref isAnyGameRunning, value);
        }

        [DontSerialize]
        public IValueConverter IntToRatingBrushConverter { get; } =
            new Converters.IntToRatingBrushConverter();

        private readonly IPlayniteAPI PlayniteApi;
        public Game SelectedGame { get; set; }

        public ExtrasSettings() { }

        public ExtrasSettings(IPlayniteAPI api)
        {
            PlayniteApi = api;
        }
    }

    public class ExtrasSettingsViewModel : ObservableObject, ISettings
    {
        private readonly ThemeExtras plugin;
        private ExtrasSettings editingClone { get; set; }

        private ExtrasSettings settings;

        public ExtrasSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public ViewModels.ThemeExtrasManifestViewModel ExtendedThemesViewModel { get; set; }

        public ICommand OpenUserLinkIconDir =>
            new RelayCommand(() =>
                System.Diagnostics.Process.Start(ThemeExtras.Instance.UserLinkIconDir)
            );

        public ICommand OpenBannersDirectory =>
            new RelayCommand(() =>
                System.Diagnostics.Process.Start(
                    Path.Combine(
                        ThemeExtras.Instance.GetPluginUserDataPath(),
                        ThemeExtras.BannersDirectoryName
                    )
                )
            );

        public ExtrasSettingsViewModel(ThemeExtras plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<ExtrasSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new ExtrasSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }

    public class Menus : ObservableObject
    {
        public static Menus Instance = new Menus();

        private bool isOpen;
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                SetValue(ref isOpen, value);
                if (value)
                {
                    OnPropertyChanged(nameof(EMLGameMenuItems));
                    OnPropertyChanged(nameof(BackgroundChangerGameMenuItems));
                    OnPropertyChanged(nameof(UniPlaySongGameMenuItems));
                    OnPropertyChanged(nameof(ScreenshotsVisualizerGameMenuItems));
                }
            }
        }

        public IEnumerable<object> EMLGameMenuItems
        {
            get
            {
                var api = API.Instance;
                var id = "705fdbca-e1fc-4004-b839-1d040b8b4429";
                if (IsOpen && (api.MainView.SelectedGames?.Any() ?? false))
                    if (
                        api.Addons?.Plugins?.FirstOrDefault(p =>
                            string.Equals(
                                p.Id.ToString(),
                                id,
                                System.StringComparison.InvariantCultureIgnoreCase
                            )
                        )
                        is Plugin plugin
                    )
                    {
                        try
                        {
                            var items = CreateGameMenuItems(api, plugin);
                            var settingsItem = new MenuItem
                            {
                                Header = ResourceProvider.GetString("LOCSettingsLabel"),
                                Command = ThemeExtras
                                    .Instance
                                    .Settings
                                    .Commands
                                    .OpenPluginSettingsCommand,
                                CommandParameter = id,
                            };
                            if (items.Count > 0)
                            {
                                items.Insert(0, new Separator());
                            }
                            items.Insert(0, settingsItem);
                            return items;
                        }
                        catch (System.Exception ex)
                        {
                            ThemeExtras.logger.Error(
                                ex,
                                $"Failed to create ExtraMetadata menu items."
                            );
                        }
                    }
                return null;
            }
        }

        public IEnumerable<object> BackgroundChangerGameMenuItems
        {
            get
            {
                var api = API.Instance;
                var id = "3afdd02b-db6c-4b60-8faa-2971d6dfad2a";
                if (IsOpen && (api.MainView.SelectedGames?.Any() ?? false))
                    if (
                        api.Addons?.Plugins?.FirstOrDefault(p =>
                            string.Equals(
                                p.Id.ToString(),
                                id,
                                System.StringComparison.InvariantCultureIgnoreCase
                            )
                        )
                        is Plugin plugin
                    )
                    {
                        try
                        {
                            var items = CreateGameMenuItems(api, plugin);
                            var settingsItem = new MenuItem
                            {
                                Header = ResourceProvider.GetString("LOCSettingsLabel"),
                                Command = ThemeExtras
                                    .Instance
                                    .Settings
                                    .Commands
                                    .OpenPluginSettingsCommand,
                                CommandParameter = id,
                            };
                            if (items.Count > 0)
                            {
                                items.Insert(0, new Separator());
                            }
                            items.Insert(0, settingsItem);
                            return items;
                        }
                        catch (System.Exception ex)
                        {
                            ThemeExtras.logger.Error(
                                ex,
                                $"Failed to create BackgroundChanger menu items."
                            );
                        }
                    }
                return null;
            }
        }

        public IEnumerable<object> UniPlaySongGameMenuItems
        {
            get
            {
                var api = API.Instance;
                var id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
                if (IsOpen && (api.MainView.SelectedGames?.Any() ?? false))
                    if (
                        api.Addons?.Plugins?.FirstOrDefault(p =>
                            string.Equals(
                                p.Id.ToString(),
                                id,
                                System.StringComparison.InvariantCultureIgnoreCase
                            )
                        )
                        is Plugin plugin
                    )
                    {
                        try
                        {
                            var items = CreateGameMenuItems(api, plugin);
                            var settingsItem = new MenuItem
                            {
                                Header = ResourceProvider.GetString("LOCSettingsLabel"),
                                Command = ThemeExtras
                                    .Instance
                                    .Settings
                                    .Commands
                                    .OpenPluginSettingsCommand,
                                CommandParameter = id,
                            };
                            if (items.Count > 0)
                            {
                                items.Insert(0, new Separator());
                            }
                            items.Insert(0, settingsItem);
                            return items;
                        }
                        catch (System.Exception ex)
                        {
                            ThemeExtras.logger.Error(
                                ex,
                                $"Failed to create UniPlaySong menu items."
                            );
                        }
                    }
                return null;
            }
        }

        public IEnumerable<object> ScreenshotsVisualizerGameMenuItems
        {
            get
            {
                var api = API.Instance;
                var id = "c6c8276f-91bf-48e5-a1d1-4bee0b493488";
                if (IsOpen && (api.MainView.SelectedGames?.Any() ?? false))
                    if (
                        api.Addons?.Plugins?.FirstOrDefault(p =>
                            string.Equals(
                                p.Id.ToString(),
                                id,
                                System.StringComparison.InvariantCultureIgnoreCase
                            )
                        )
                        is Plugin plugin
                    )
                    {
                        try
                        {
                            var items = CreateGameMenuItems(api, plugin);
                            var settingsItem = new MenuItem
                            {
                                Header = ResourceProvider.GetString("LOCSettingsLabel"),
                                Command = ThemeExtras
                                    .Instance
                                    .Settings
                                    .Commands
                                    .OpenPluginSettingsCommand,
                                CommandParameter = id,
                            };
                            if (items.Count > 0)
                            {
                                items.Insert(0, new Separator());
                            }
                            items.Insert(0, settingsItem);
                            return items;
                        }
                        catch (System.Exception ex)
                        {
                            ThemeExtras.logger.Error(
                                ex,
                                $"Failed to create ScreenshotsVisualizer menu items."
                            );
                        }
                    }
                return null;
            }
        }

        private static IList<object> CreateGameMenuItems(IPlayniteAPI api, Plugin plugin)
        {
            var menuItems = new ObservableCollection<object>();
            List<Game> games = api.MainView.SelectedGames?.Take(1).ToList() ?? new List<Game>();
            foreach (
                var item in plugin.GetGameMenuItems(new GetGameMenuItemsArgs() { Games = games })
            )
            {
                var path = item
                    .MenuSection.Replace("@", "")
                    .Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1)
                    .ToList();
                var currentMenuItems = menuItems;
                for (int i = 0; i < path.Count; ++i)
                {
                    var section = path[i];
                    if (
                        currentMenuItems
                            .OfType<MenuItem>()
                            .FirstOrDefault(mi => mi.Header as string == section)
                        is MenuItem menuItem
                    )
                    {
                        currentMenuItems = menuItem.ItemsSource as ObservableCollection<object>;
                    }
                    else
                    {
                        var itemList = new ObservableCollection<object>();
                        currentMenuItems.Add(
                            new MenuItem { Header = section, ItemsSource = itemList }
                        );
                        currentMenuItems = itemList;
                    }
                }
                if (item.Description is "-")
                {
                    currentMenuItems.Add(new Separator());
                }
                else
                {
                    currentMenuItems.Add(
                        new MenuItem
                        {
                            Header = item.Description,
                            Command = new RelayCommand<GameMenuItemActionArgs>(item.Action),
                            CommandParameter = new GameMenuItemActionArgs
                            {
                                Games = games,
                                SourceItem = item,
                            },
                        }
                    );
                }
            }
            return menuItems;
        }
    }

    public class CommandSettings : ObservableObject
    {
        public static readonly CommandSettings Instance = new CommandSettings();

        // Cache pro SaveFileView plugin výsledky
        private static readonly Dictionary<
            string,
            (bool result, DateTime timestamp)
        > saveFileViewCache = new Dictionary<string, (bool result, DateTime timestamp)>();
        private static readonly TimeSpan cacheTimeout = TimeSpan.FromMilliseconds(500);

        // Additional protection against rapid multiple calls
        private static readonly HashSet<string> pendingChecks = new HashSet<string>();

        // Cache pro uložení menuItems z SaveFileView pluginu
        private static readonly Dictionary<
            string,
            (IEnumerable<GameMenuItem> menuItems, DateTime timestamp)
        > menuItemsCache =
            new Dictionary<string, (IEnumerable<GameMenuItem> menuItems, DateTime timestamp)>();

        private CommandSettings() { }

        public ICommand SwitchToDetailsViewCommand { get; } =
            new RelayCommand<Game>(SwitchToDetailsView);

        private static void SwitchToDetailsView(Game game)
        {
            SwitchViewAndSelect(game, DesktopView.Details);
        }

        public ICommand SwitchToGridViewCommand { get; } = new RelayCommand<Game>(SwitchToGridView);

        private static void SwitchToGridView(Game game)
        {
            SwitchViewAndSelect(game, DesktopView.Grid);
        }

        public ICommand SwitchToListViewCommand { get; } = new RelayCommand<Game>(SwitchToListView);

        private static void SwitchToListView(Game game)
        {
            SwitchViewAndSelect(game, DesktopView.List);
        }

        private static void SwitchViewAndSelect(Game game, DesktopView view)
        {
            if (API.Instance.MainView.ActiveDesktopView != view)
            {
                if (game is Game)
                {
                    if (API.Instance.MainView.SelectedGames?.FirstOrDefault() != game)
                    {
                        API.Instance.MainView.SelectGame(game.Id);
                    }
                }
                API.Instance.MainView.ActiveDesktopView = view;
            }
        }

        private static void OpenUrl(object param)
        {
            try
            {
                string url = null;
                switch (param)
                {
                    case string urlString:
                        url = urlString;
                        break;
                    case Uri uri:
                        url = uri.ToString();
                        break;
                    case Link link:
                        url = link.Url;
                        break;
                    default:
                        break;
                }
                if (!string.IsNullOrEmpty(url))
                {
                    System.Diagnostics.Process.Start(url);
                }
            }
            catch (Exception ex)
            {
                ThemeExtras.logger.Error(ex, $"Failed to open url {param?.ToString()}.");
            }
        }

        public ICommand OpenUrlCommand { get; } = new RelayCommand<object>(OpenUrl);

        public static void DiscardNotification(NotificationMessage notificationMessage)
        {
            if (notificationMessage?.Id is string)
            {
                API.Instance.Notifications.Remove(notificationMessage.Id);
                if (API.Instance.Notifications.Count == 0)
                {
                    var mainWindow = Application
                        .Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.Name == "WindowMain");
                    if (mainWindow is Window)
                    {
                        var notifcationPanel = PlayniteCommon
                            .UI.UiHelper.FindVisualChildren<FrameworkElement>(
                                mainWindow,
                                "PART_Notifications"
                            )
                            .FirstOrDefault();
                        if (notifcationPanel is FrameworkElement)
                        {
                            if (
                                PlayniteCommon
                                    .UI.UiHelper.FindVisualChildren<Button>(
                                        notifcationPanel,
                                        "PART_ButtonClose"
                                    )
                                    .FirstOrDefault()
                                is Button closeButton
                            )
                            {
                                if (closeButton.Command?.CanExecute(null) ?? false)
                                {
                                    closeButton.Command.Execute(null);
                                }
                            }
                        }
                    }
                }
            }
        }

        public ICommand DiscardNotificationCommand { get; } =
            new RelayCommand<NotificationMessage>(DiscardNotification);

        public static void UpdateGames(object sender, EventArgs args)
        {
            API.Instance.Database.Games.Update(API.Instance.MainView.SelectedGames);
        }

        public ICommand UpdateGamesCommand { get; } =
            new RelayCommand(
                () =>
                {
                    UpdateGames(null, null);
                },
                () => API.Instance?.MainView?.SelectedGames?.Count() > 0
            );

        public ICommand ResetScoreCommand { get; } =
            new RelayCommand<string>(
                kinds =>
                {
                    foreach (var game in API.Instance.MainView.SelectedGames)
                    {
                        if (kinds.Contains("User"))
                        {
                            game.UserScore = null;
                        }
                        if (kinds.Contains("Community"))
                        {
                            game.CommunityScore = null;
                        }
                        if (kinds.Contains("Critic"))
                        {
                            game.CriticScore = null;
                        }
                    }
                    API.Instance.Database.Games.Update(API.Instance.MainView.SelectedGames);
                },
                _ => API.Instance?.MainView?.SelectedGames?.Count() > 0
            );

        public ICommand OpenGameAssetFolderCommand { get; } =
            new RelayCommand<Game>(
                game =>
                {
                    string path = null;
                    var api = API.Instance;
                    if (game is Game)
                    {
                        path = Path.Combine(api.Database.DatabasePath, "files", game.Id.ToString());
                    }
                    else
                    {
                        if (api?.MainView?.SelectedGames?.FirstOrDefault() is Game selected)
                        {
                            path = Path.Combine(
                                api.Database.DatabasePath,
                                "files",
                                selected.Id.ToString()
                            );
                        }
                    }
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        System.Diagnostics.Process.Start(path);
                    }
                },
                game =>
                {
                    string path = null;
                    var api = API.Instance;
                    if (game is Game)
                    {
                        path = Path.Combine(api.Database.DatabasePath, "files", game.Id.ToString());
                    }
                    else
                    {
                        if (api?.MainView?.SelectedGames?.FirstOrDefault() is Game selected)
                        {
                            path = Path.Combine(
                                api.Database.DatabasePath,
                                "files",
                                selected.Id.ToString()
                            );
                        }
                    }
                    return !string.IsNullOrEmpty(path) && Directory.Exists(path);
                }
            );

        // Helper method to get game from various contexts
        private static Game GetGameFromParameter(object parameter)
        {
            // If parameter is directly a game
            if (parameter is Game directGame)
                return directGame;

            // If parameter has DataContext property (like DetailsViewGameOverview)
            var dataContextProperty = parameter?.GetType().GetProperty("DataContext");
            if (dataContextProperty?.GetValue(parameter) is Game contextGame)
                return contextGame;

            // Fallback to selected game
            return API.Instance?.MainView?.SelectedGames?.FirstOrDefault();
        }

        public ICommand OpenExtraMetadataFolderCommand { get; } =
            new RelayCommand<object>(
                parameter =>
                {
                    try
                    {
                        var game = GetGameFromParameter(parameter);
                        if (game == null)
                        {
                            API.Instance.Dialogs.ShowMessage("No game selected.");
                            return;
                        }

                        var extraMetadataPath = Path.Combine(
                            API.Instance.Paths.ConfigurationPath,
                            "ExtraMetadata",
                            "games",
                            game.Id.ToString()
                        );

                        if (Directory.Exists(extraMetadataPath))
                        {
                            System.Diagnostics.Process.Start(
                                "explorer.exe",
                                $"\"{extraMetadataPath}\""
                            );
                        }
                        else
                        {
                            API.Instance.Dialogs.ShowMessage(
                                $"Extra Metadata folder not found:\n{extraMetadataPath}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        ThemeExtras.logger.Error(ex, "Error opening Extra Metadata folder");
                        API.Instance.Dialogs.ShowMessage(
                            $"Error opening Extra Metadata folder: {ex.Message}"
                        );
                    }
                },
                parameter =>
                {
                    var game = GetGameFromParameter(parameter);
                    if (game == null)
                        return false;

                    var extraMetadataPath = Path.Combine(
                        API.Instance.Paths.ConfigurationPath,
                        "ExtraMetadata",
                        "games",
                        game.Id.ToString()
                    );

                    if (!Directory.Exists(extraMetadataPath))
                        return false;

                    try
                    {
                        return Directory
                            .EnumerateFiles(extraMetadataPath, "*", SearchOption.AllDirectories)
                            .Any();
                    }
                    catch
                    {
                        return false;
                    }
                }
            );

        public ICommand OpenMusicFolderCommand { get; } =
            new RelayCommand<object>(
                parameter =>
                {
                    try
                    {
                        var game = GetGameFromParameter(parameter);
                        if (game == null)
                        {
                            API.Instance.Dialogs.ShowMessage("No game selected.");
                            return;
                        }

                        var musicPath = Path.Combine(
                            API.Instance.Paths.ConfigurationPath,
                            "ExtraMetadata",
                            "UniPlaySong",
                            "Games",
                            game.Id.ToString()
                        );

                        if (Directory.Exists(musicPath))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"\"{musicPath}\"");
                        }
                        else
                        {
                            API.Instance.Dialogs.ShowMessage(
                                $"Music folder not found for '{game.Name}':\n{musicPath}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        ThemeExtras.logger.Error(ex, "Error opening music folder");
                        API.Instance.Dialogs.ShowMessage(
                            $"Error opening music folder: {ex.Message}"
                        );
                    }
                },
                parameter =>
                {
                    var game = GetGameFromParameter(parameter);
                    if (game == null)
                        return false;

                    var musicPath = Path.Combine(
                        API.Instance.Paths.ConfigurationPath,
                        "ExtraMetadata",
                        "UniPlaySong",
                        "Games",
                        game.Id.ToString()
                    );

                    if (!Directory.Exists(musicPath))
                        return false;

                    try
                    {
                        return Directory
                            .EnumerateFiles(musicPath, "*", SearchOption.AllDirectories)
                            .Any();
                    }
                    catch
                    {
                        return false;
                    }
                }
            );

        public ICommand OpenBackgroundChangerMetadataFolderCommand { get; } =
            new RelayCommand<object>(
                parameter =>
                {
                    try
                    {
                        var game = GetGameFromParameter(parameter);
                        if (game == null)
                        {
                            API.Instance.Dialogs.ShowMessage("No game selected.");
                            return;
                        }

                        var backgroundPath = Path.Combine(
                            API.Instance.Paths.ConfigurationPath,
                            "ExtensionsData",
                            "3afdd02b-db6c-4b60-8faa-2971d6dfad2a",
                            "Images",
                            game.Id.ToString()
                        );

                        if (Directory.Exists(backgroundPath))
                        {
                            System.Diagnostics.Process.Start(
                                "explorer.exe",
                                $"\"{backgroundPath}\""
                            );
                        }
                        else
                        {
                            API.Instance.Dialogs.ShowMessage(
                                $"BackgroundChanger folder not found for '{game.Name}':\n{backgroundPath}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        ThemeExtras.logger.Error(ex, "Error opening background folder");
                        API.Instance.Dialogs.ShowMessage(
                            $"Error opening background folder: {ex.Message}"
                        );
                    }
                },
                parameter =>
                {
                    var game = GetGameFromParameter(parameter);
                    if (game == null)
                        return false;

                    var backgroundPath = Path.Combine(
                        API.Instance.Paths.ConfigurationPath,
                        "ExtensionsData",
                        "3afdd02b-db6c-4b60-8faa-2971d6dfad2a",
                        "Images",
                        game.Id.ToString()
                    );

                    if (!Directory.Exists(backgroundPath))
                        return false;

                    try
                    {
                        return Directory
                            .EnumerateFiles(backgroundPath, "*", SearchOption.AllDirectories)
                            .Any();
                    }
                    catch
                    {
                        return false;
                    }
                }
            );

        public ICommand OpenScreenshotsFolderCommand { get; } =
            new RelayCommand<object>(
                parameter =>
                {
                    try
                    {
                        var game = GetGameFromParameter(parameter);
                        if (game == null)
                        {
                            API.Instance.Dialogs.ShowMessage("No game selected.");
                            return;
                        }

                        var screenshotPaths = GetScreenshotPathsFromConfig(game);

                        if (screenshotPaths.Any())
                        {
                            int openedCount = 0;
                            foreach (var path in screenshotPaths)
                            {
                                if (Directory.Exists(path))
                                {
                                    System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
                                    openedCount++;
                                }
                            }

                            if (openedCount == 0)
                            {
                                API.Instance.Dialogs.ShowMessage(
                                    $"Screenshot paths found in config for '{game.Name}', but none of the directories exist:\n"
                                        + string.Join("\n", screenshotPaths)
                                );
                            }
                        }
                        else
                        {
                            API.Instance.Dialogs.ShowMessage(
                                $"No screenshot folders found for '{game.Name}'.\n\n"
                                    + "Make sure the ScreenshotsVisualizer plugin is configured or Playnite's screenshots folder contains a subfolder matching the game name."
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        ThemeExtras.logger.Error(ex, "Error opening screenshots folder");
                        API.Instance.Dialogs.ShowMessage($"Error: {ex.Message}");
                    }
                },
                parameter =>
                {
                    var game = GetGameFromParameter(parameter);
                    if (game == null)
                        return false;

                    if (!HasScreenshotsVisualizerPlugin())
                        return false;

                    var screenshotPaths = GetScreenshotPathsFromConfig(game);
                    if (!screenshotPaths?.Any() == true)
                        return false;

                    return screenshotPaths.Any(path =>
                    {
                        try
                        {
                            return Directory.Exists(path);
                        }
                        catch
                        {
                            return false;
                        }
                    });
                }
            );

        public ICommand OpenGameInstallLocationCommand { get; } =
            new RelayCommand<object>(
                parameter =>
                {
                    try
                    {
                        var game = GetGameFromParameter(parameter);
                        if (game == null)
                        {
                            API.Instance.Dialogs.ShowMessage("No game selected.");
                            return;
                        }

                        if (
                            !string.IsNullOrEmpty(game.InstallDirectory)
                            && Directory.Exists(game.InstallDirectory)
                        )
                        {
                            System.Diagnostics.Process.Start(
                                "explorer.exe",
                                $"\"{game.InstallDirectory}\""
                            );
                        }
                        else
                        {
                            API.Instance.Dialogs.ShowMessage(
                                $"Install directory not found or not set for '{game.Name}'.\n\nGame install directory: {game.InstallDirectory ?? "Not set"}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        ThemeExtras.logger.Error(ex, "Error opening install directory");
                        API.Instance.Dialogs.ShowMessage(
                            $"Error opening install directory: {ex.Message}"
                        );
                    }
                },
                parameter =>
                {
                    var game = GetGameFromParameter(parameter);
                    return game != null
                        && !string.IsNullOrEmpty(game.InstallDirectory)
                        && Directory.Exists(game.InstallDirectory);
                }
            );

        public ICommand OpenGameSaveDirectoriesCommand { get; } =
            new RelayCommand<object>(
                parameter =>
                {
                    try
                    {
                        var game = GetGameFromParameter(parameter);
                        if (game == null)
                        {
                            API.Instance.Dialogs.ShowMessage("No game selected.");
                            return;
                        }

                        // Try to use SaveFileView plugin if available
                        if (TryOpenWithSaveFileViewPlugin(game, "save"))
                        {
                            return;
                        }

                        // If SaveFileView plugin not available or no folders found, show message
                        API.Instance.Dialogs.ShowMessage(
                            $"No save folders detected for '{game.Name}'."
                        );
                    }
                    catch (Exception ex)
                    {
                        ThemeExtras.logger.Error(ex, "Error opening save directories");
                        API.Instance.Dialogs.ShowMessage(
                            $"Error opening save directories: {ex.Message}"
                        );
                    }
                },
                parameter =>
                {
                    var game = GetGameFromParameter(parameter);
                    if (game == null)
                        return false;

                    return CheckSaveFileViewPlugin(game, "save");
                }
            );

        public ICommand OpenGameConfigDirectoriesCommand { get; } =
            new RelayCommand<object>(
                parameter =>
                {
                    try
                    {
                        var game = GetGameFromParameter(parameter);
                        if (game == null)
                        {
                            API.Instance.Dialogs.ShowMessage("No game selected.");
                            return;
                        }

                        // Try to use SaveFileView plugin if available
                        if (TryOpenWithSaveFileViewPlugin(game, "config"))
                        {
                            return;
                        }

                        // If SaveFileView plugin not available or no folders found, show message
                        API.Instance.Dialogs.ShowMessage(
                            $"No config folders detected for '{game.Name}'."
                        );
                    }
                    catch (Exception ex)
                    {
                        ThemeExtras.logger.Error(ex, "Error opening config directories");
                        API.Instance.Dialogs.ShowMessage(
                            $"Error opening config directories: {ex.Message}"
                        );
                    }
                },
                parameter =>
                {
                    var game = GetGameFromParameter(parameter);
                    if (game == null)
                        return false;

                    return CheckSaveFileViewPlugin(game, "config");
                }
            );

        // Helper methods to integrate with SaveFileView plugin
        private static bool CheckSaveFileViewPlugin(Game game, string pathType)
        {
            try
            {
                var cacheKey = $"{game?.Id}:{pathType}";
                var now = DateTime.Now;

                // Check if this exact request is already being processed
                lock (pendingChecks)
                {
                    if (pendingChecks.Contains(cacheKey))
                    {
                        return false; // Avoid multiple concurrent calls for same key
                    }
                }

                if (saveFileViewCache.TryGetValue(cacheKey, out var cachedEntry))
                {
                    var timeDiff = now - cachedEntry.timestamp;

                    if (timeDiff < cacheTimeout)
                    {
                        return cachedEntry.result;
                    }
                    else
                    {
                        saveFileViewCache.Remove(cacheKey);
                    }
                }

                // Mark this request as being processed
                lock (pendingChecks)
                {
                    pendingChecks.Add(cacheKey);
                }

                if (saveFileViewCache.Count > 20)
                {
                    var keysToRemove = saveFileViewCache
                        .Where(kvp => now - kvp.Value.timestamp >= cacheTimeout)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        saveFileViewCache.Remove(key);
                    }
                }

                var api = API.Instance;

                var saveFileViewPlugin = api.Addons?.Plugins?.FirstOrDefault(p =>
                    p.Id.ToString()
                        .Equals(
                            "f68f302b-9799-4b77-a982-4bfca97130e2",
                            StringComparison.OrdinalIgnoreCase
                        )
                    || p.Id.ToString()
                        .Equals(
                            "SaveFileView_f68f302b-9799-4b77-a982-4bfca97130e2",
                            StringComparison.OrdinalIgnoreCase
                        )
                    || p.GetType().Name.ToLower().Contains("savefileview")
                );

                if (saveFileViewPlugin == null)
                {
                    saveFileViewCache[cacheKey] = (false, now);
                    return false;
                }

                var menuItems = GetCachedMenuItems(game, saveFileViewPlugin);

                var relevantItem = menuItems?.FirstOrDefault(item =>
                {
                    var desc = item.Description?.ToLower() ?? "";

                    if (pathType == "save")
                        return desc.Contains("save")
                            && (
                                desc.Contains("open")
                                || desc.Contains("directory")
                                || desc.Contains("folder")
                            );
                    else
                        return desc.Contains("config")
                            && (
                                desc.Contains("open")
                                || desc.Contains("directory")
                                || desc.Contains("folder")
                            );
                });

                bool hasPluginData = relevantItem != null;

                if (!hasPluginData)
                {
                    saveFileViewCache[cacheKey] = (false, now);
                    return false;
                }

                bool foldersExistOnDisk = CheckActualFoldersExist(game, pathType);

                bool result = hasPluginData && foldersExistOnDisk;

                saveFileViewCache[cacheKey] = (result, now);

                return result;
            }
            catch (Exception ex)
            {
                ThemeExtras.logger.Error(
                    ex,
                    $"Failed to check SaveFileView plugin for {pathType} paths"
                );
                return false;
            }
            finally
            {
                // Always remove from pending checks
                lock (pendingChecks)
                {
                    pendingChecks.Remove($"{game?.Id}:{pathType}");
                }
            }
        }

        // Helper metoda pro získání menu items s cachováním
        private static IEnumerable<GameMenuItem> GetCachedMenuItems(
            Game game,
            Plugin saveFileViewPlugin
        )
        {
            var cacheKey = $"menuItems:{game?.Id}";
            var now = DateTime.Now;

            if (menuItemsCache.TryGetValue(cacheKey, out var cachedEntry))
            {
                var timeDiff = now - cachedEntry.timestamp;
                if (timeDiff < cacheTimeout)
                {
                    return cachedEntry.menuItems;
                }
                else
                {
                    menuItemsCache.Remove(cacheKey);
                }
            }

            // Cache cleanup
            if (menuItemsCache.Count > 10)
            {
                var keysToRemove = menuItemsCache
                    .Where(kvp => now - kvp.Value.timestamp >= cacheTimeout)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    menuItemsCache.Remove(key);
                }
            }

            // Get fresh menu items
            var menuItems = saveFileViewPlugin.GetGameMenuItems(
                new GetGameMenuItemsArgs { Games = new List<Game> { game } }
            );

            // Cache the result
            menuItemsCache[cacheKey] = (menuItems, now);

            return menuItems;
        }

        private static bool TryOpenWithSaveFileViewPlugin(Game game, string pathType)
        {
            try
            {
                var api = API.Instance;

                var saveFileViewPlugin = api.Addons?.Plugins?.FirstOrDefault(p =>
                    p.Id.ToString()
                        .Equals(
                            "f68f302b-9799-4b77-a982-4bfca97130e2",
                            StringComparison.OrdinalIgnoreCase
                        )
                );

                if (saveFileViewPlugin != null)
                {
                    var menuItems = GetCachedMenuItems(game, saveFileViewPlugin);

                    var relevantItem = menuItems?.FirstOrDefault(item =>
                    {
                        var desc = item.Description?.ToLower() ?? "";
                        if (pathType == "save")
                            return desc.Contains("save")
                                && (
                                    desc.Contains("open")
                                    || desc.Contains("directory")
                                    || desc.Contains("folder")
                                );
                        else
                            return desc.Contains("config")
                                && (
                                    desc.Contains("open")
                                    || desc.Contains("directory")
                                    || desc.Contains("folder")
                                );
                    });

                    if (relevantItem != null)
                    {
                        relevantItem.Action?.Invoke(
                            new GameMenuItemActionArgs { Games = new List<Game> { game } }
                        );
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ThemeExtras.logger.Error(
                    ex,
                    $"Failed to use SaveFileView plugin for {pathType} paths"
                );
            }

            return false;
        }

        public static void OpenPlayniteSettings(object sender, EventArgs args)
        {
            if (
                Application
                    .Current?.Windows?.OfType<Window>()
                    .FirstOrDefault(w => w.Name == "WindowMain")
                is Window mainWindow
            )
            {
                if (
                    mainWindow
                        .InputBindings.OfType<KeyBinding>()
                        .FirstOrDefault(b => b.Key == Key.F4)
                    is KeyBinding binding
                )
                {
                    if (binding.Command.CanExecute(null))
                    {
                        binding.Command.Execute(null);
                    }
                }
            }
        }

        public ICommand OpenPlayniteSettingsCommand { get; } =
            new RelayCommand(
                () =>
                {
                    OpenPlayniteSettings(null, null);
                },
                () => true
            );

        public ICommand OpenAddonWindowCommand { get; } =
            new RelayCommand(
                () =>
                {
                    if (
                        Application
                            .Current?.Windows?.OfType<Window>()
                            .FirstOrDefault(w => w.Name == "WindowMain")
                        is Window mainWindow
                    )
                    {
                        if (
                            mainWindow
                                .InputBindings.OfType<KeyBinding>()
                                .FirstOrDefault(b => b.Key == Key.F9)
                            is KeyBinding binding
                        )
                        {
                            if (binding.Command.CanExecute(null))
                            {
                                binding.Command.Execute(null);
                            }
                        }
                    }
                },
                () => true
            );

        public ICommand SwitchModeCommand { get; } =
            new RelayCommand(
                () =>
                {
                    if (
                        Application
                            .Current?.Windows?.OfType<Window>()
                            .FirstOrDefault(w => w.Name == "WindowMain")
                        is Window mainWindow
                    )
                    {
                        if (
                            mainWindow
                                .InputBindings.OfType<KeyBinding>()
                                .FirstOrDefault(b => b.Key == Key.F11)
                            is KeyBinding binding
                        )
                        {
                            if (binding.Command.CanExecute(null))
                            {
                                binding.Command.Execute(null);
                            }
                        }
                    }
                },
                () => true
            );

        public ICommand OpenPluginSettingsCommand { get; } =
            new RelayCommand<string>(
                id =>
                {
                    var api = API.Instance;
                    if (
                        api.Addons?.Plugins?.FirstOrDefault(p =>
                            string.Equals(
                                p.Id.ToString(),
                                id,
                                System.StringComparison.InvariantCultureIgnoreCase
                            )
                        )
                        is Plugin plugin
                    )
                    {
                        try
                        {
                            plugin.OpenSettingsView();
                        }
                        catch (System.Exception ex)
                        {
                            ThemeExtras.logger.Error(
                                ex,
                                $"Failed to open plugin settings for plugin with Id: {id}"
                            );
                        }
                    }
                },
                id =>
                {
                    var api = API.Instance;
                    if (
                        api.Addons?.Plugins?.FirstOrDefault(p =>
                            string.Equals(
                                p.Id.ToString(),
                                id,
                                System.StringComparison.InvariantCultureIgnoreCase
                            )
                        )
                        is Plugin plugin
                    )
                    {
                        if (plugin is GenericPlugin genericPlugin)
                            return genericPlugin.Properties?.HasSettings ?? false;
                        if (plugin is LibraryPlugin libraryPlugin)
                            return libraryPlugin.Properties?.HasSettings ?? false;
                        if (plugin is MetadataPlugin metadataPlugin)
                            return metadataPlugin.Properties?.HasSettings ?? false;
                    }
                    return false;
                }
            );

        public ICommand OpenPluginConfigDirCommand { get; } =
            new RelayCommand<string>(
                id =>
                {
                    var api = API.Instance;
                    if (
                        api.Addons?.Plugins?.FirstOrDefault(p =>
                            string.Equals(
                                p.Id.ToString(),
                                id,
                                System.StringComparison.InvariantCultureIgnoreCase
                            )
                        )
                        is Plugin plugin
                    )
                    {
                        var configPath = Path.Combine(
                            api.Paths.ConfigurationPath,
                            "Extensions",
                            id
                        );
                        if (Directory.Exists(configPath))
                        {
                            System.Diagnostics.Process.Start(configPath);
                        }
                        else
                        {
                            api.Dialogs.ShowMessage(
                                $"Plugin configuration directory does not exist.\n{configPath}"
                            );
                        }
                    }
                },
                id => true
            );

        public ICommand BackCommand { get; } =
            new RelayCommand(
                () =>
                {
                    if (
                        Application
                            .Current?.Windows?.OfType<Window>()
                            .FirstOrDefault(w => w.Name == "WindowMain")
                        is Window mainWindow
                    )
                    {
                        if (
                            mainWindow
                                .InputBindings.OfType<KeyBinding>()
                                .FirstOrDefault(b =>
                                    b.Key == Key.BrowserBack
                                    || (b.Key == Key.Left && b.Modifiers == ModifierKeys.Alt)
                                )
                            is KeyBinding binding
                        )
                        {
                            if (binding.Command.CanExecute(null))
                            {
                                binding.Command.Execute(null);
                            }
                        }
                    }
                },
                () => true
            );

        public ICommand ForwardCommand { get; } =
            new RelayCommand(
                () =>
                {
                    if (
                        Application
                            .Current?.Windows?.OfType<Window>()
                            .FirstOrDefault(w => w.Name == "WindowMain")
                        is Window mainWindow
                    )
                    {
                        if (
                            mainWindow
                                .InputBindings.OfType<KeyBinding>()
                                .FirstOrDefault(b =>
                                    b.Key == Key.BrowserForward
                                    || (b.Key == Key.Right && b.Modifiers == ModifierKeys.Alt)
                                )
                            is KeyBinding binding
                        )
                        {
                            if (binding.Command.CanExecute(null))
                            {
                                binding.Command.Execute(null);
                            }
                        }
                    }
                },
                () => true
            );

        private static bool HasScreenshotsVisualizerPlugin()
        {
            try
            {
                var api = API.Instance;
                return api.Addons?.Plugins?.Any(p =>
                        p.Id.ToString()
                            .Equals(
                                "c6c8276f-91bf-48e5-a1d1-4bee0b493488",
                                StringComparison.OrdinalIgnoreCase
                            ) || p.GetType().Name.ToLower().Contains("screenshotsvisualizer")
                    ) ?? false;
            }
            catch
            {
                return false;
            }
        }

        // Simple helper to get screenshot paths from ScreenshotsVisualizer config
        private static List<string> GetScreenshotPathsFromConfig(Game game)
        {
            var screenshotPaths = new List<string>();

            try
            {
                var configPath = Path.Combine(
                    API.Instance.Paths.ConfigurationPath,
                    "ExtensionsData",
                    "c6c8276f-91bf-48e5-a1d1-4bee0b493488",
                    "config.json"
                );

                if (File.Exists(configPath))
                {
                    var configContent = File.ReadAllText(configPath);

                    var configJson = JToken.Parse(configContent);
                    var gameIdStr = game.Id.ToString();

                    JArray gamesArray = null;

                    if (configJson is JObject configObj)
                    {
                        var possibleArrays = configObj
                            .Properties()
                            .Where(p => p.Value is JArray)
                            .Select(p => p.Value as JArray)
                            .ToList();

                        gamesArray = possibleArrays.FirstOrDefault(arr =>
                            arr.Count > 0 && arr[0] is JObject firstItem && firstItem["Id"] != null
                        );

                        if (gamesArray == null)
                        {
                            return AddPlayniteScreenshotFolders(game, screenshotPaths);
                        }
                    }
                    else if (configJson is JArray directArray)
                    {
                        gamesArray = directArray;
                    }
                    else
                    {
                        return AddPlayniteScreenshotFolders(game, screenshotPaths);
                    }

                    var gameData = gamesArray
                        .OfType<JObject>()
                        .FirstOrDefault(g =>
                            g["Id"]
                                ?.ToString()
                                .Equals(gameIdStr, StringComparison.OrdinalIgnoreCase) == true
                        );

                    if (gameData != null)
                    {
                        var screenshotsFolders = gameData["ScreenshotsFolders"] as JArray;
                        if (screenshotsFolders != null)
                        {
                            foreach (var folderObj in screenshotsFolders.OfType<JObject>())
                            {
                                var screenshotsFolder = folderObj["ScreenshotsFolder"]?.ToString();
                                if (!string.IsNullOrEmpty(screenshotsFolder))
                                {
                                    screenshotPaths.Add(screenshotsFolder);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThemeExtras.logger.Error(ex, "Error reading ScreenshotsVisualizer config");
            }

            return AddPlayniteScreenshotFolders(game, screenshotPaths);
        }

        private static List<string> AddPlayniteScreenshotFolders(
            Game game,
            List<string> screenshotPaths
        )
        {
            foreach (var path in GetPlayniteScreenshotFolders(game))
            {
                if (
                    !screenshotPaths.Any(p =>
                        p.Equals(path, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    screenshotPaths.Add(path);
                }
            }

            return screenshotPaths;
        }

        private static IEnumerable<string> GetPlayniteScreenshotFolders(Game game)
        {
            var roots = new List<string>();

            try
            {
                var configPath = Path.Combine(
                    API.Instance.Paths.ConfigurationPath,
                    "config.json"
                );

                if (File.Exists(configPath))
                {
                    var configContent = File.ReadAllText(configPath);
                    var configJson = JToken.Parse(configContent);
                    var configContainer = configJson as JContainer;
                    if (configContainer != null)
                    {
                        foreach (
                            var prop in configContainer.DescendantsAndSelf().OfType<JProperty>()
                        )
                        {
                            if (
                                prop.Name.IndexOf(
                                    "screenshot",
                                    StringComparison.OrdinalIgnoreCase
                                ) < 0
                            )
                            {
                                continue;
                            }

                            if (prop.Value.Type == JTokenType.String)
                            {
                                var value = prop.Value.ToString();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    roots.Add(Environment.ExpandEnvironmentVariables(value));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThemeExtras.logger.Error(ex, "Error reading Playnite screenshot paths");
            }

            roots.Add(Path.Combine(API.Instance.Paths.ConfigurationPath, "Screenshots"));
            roots.Add(Path.Combine(API.Instance.Paths.ApplicationPath, "_screenshots"));
            roots.Add(Path.Combine(API.Instance.Paths.ApplicationPath, "Screenshots"));

            var uniqueRoots = roots
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(path => Directory.Exists(path));

            var nameCandidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(game?.Name))
            {
                nameCandidates.Add(game.Name);
                nameCandidates.Add(SanitizePathSegment(game.Name));
            }

            if (game != null)
            {
                nameCandidates.Add(game.Id.ToString());
            }

            foreach (var root in uniqueRoots)
            {
                foreach (var name in nameCandidates.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var candidate = Path.Combine(root, name);
                    if (Directory.Exists(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        private static string SanitizePathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var buffer = new char[value.Length];

            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                buffer[i] = invalidChars.Contains(ch) ? '_' : ch;
            }

            return new string(buffer);
        }

        // Helper metoda pro kontrolu skutečné existence save/config složek na disku
        private static bool CheckActualFoldersExist(Game game, string pathType)
        {
            try
            {
                var pluginDataPath = Path.Combine(
                    API.Instance.Paths.ConfigurationPath,
                    "ExtensionsData",
                    "f68f302b-9799-4b77-a982-4bfca97130e2", // SaveFileView plugin ID
                    $"{game.Id}.json"
                );

                if (!File.Exists(pluginDataPath))
                {
                    return false; // Žádná data z PCGamingWiki - stejně jako SaveFileView
                }

                var data = Serialization.FromJsonFile<GameDirectoriesData>(pluginDataPath);
                if (data?.PathsData?.Any() != true)
                {
                    return false;
                }

                var replacementDict = GetSaveFileViewReplacementDict(game);
                var pathTypeEnum = pathType == "save" ? PathType.Save : PathType.Config;

                var availableDirs = GetAvailableDirsFromSaveFileViewData(
                    game,
                    replacementDict,
                    data,
                    pathTypeEnum
                );

                return availableDirs.Any();
            }
            catch (Exception ex)
            {
                ThemeExtras.logger.Error(
                    ex,
                    $"Failed to check actual folder existence for {pathType} paths using SaveFileView data"
                );
                return false;
            }
        }

        private static Dictionary<string, string> GetSaveFileViewReplacementDict(Game game)
        {
            var replacementDict = new Dictionary<string, string>
            {
                [@"{{p|uid}}"] = @"*", // Wildcard pro user ID
                [@"{{p|username}}"] = Environment.UserName,
                [@"{{p|userprofile}}"] = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile
                ),
                [@"{{p|userprofile\documents}}"] = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments
                ),
                [@"{{p|userprofile\appdata\locallow}}"] = Environment
                    .GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    .Replace("Roaming", "LocalLow"),
                [@"{{p|appdata}}"] = Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData
                ),
                [@"{{p|localappdata}}"] = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                [@"{{p|public}}"] = Path.GetDirectoryName(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)
                ),
                [@"{{p|allusersprofile}}"] = Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData
                ),
                [@"{{p|programdata}}"] = Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData
                ),
                [@"{{p|windir}}"] = Environment.GetFolderPath(Environment.SpecialFolder.System),
                [@"{{p|syswow64}}"] = Environment.GetFolderPath(
                    Environment.SpecialFolder.SystemX86
                ),
                [@"<code>"] = @"*",
            };

            try
            {
                var steamPath = GetSteamInstallationDirectory();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    replacementDict[@"{{p|steam}}"] = steamPath;
                }

                var uplayPath = GetUbisoftInstallationDirectory();
                if (!string.IsNullOrEmpty(uplayPath))
                {
                    replacementDict[@"{{p|uplay}}"] = uplayPath;
                }
            }
            catch
            {
                // Ignorujeme chyby při získávání Steam/Uplay cest
            }

            return replacementDict;
        }

        private static List<string> GetAvailableDirsFromSaveFileViewData(
            Game game,
            Dictionary<string, string> replacementDict,
            GameDirectoriesData data,
            PathType pathType
        )
        {
            var dirDefinitions = new List<string>();
            var pathDefinitions = new List<string>();

            var pcgwRegistryVariables = new List<string>
            {
                @"{{p|hkcu}}",
                @"{{p|hklm}}",
                @"{{p|wow64}}",
            };

            foreach (var pathData in data.PathsData)
            {
                if (pathData.Type != pathType)
                {
                    continue;
                }

                var path = pathData.Path;

                if (
                    pcgwRegistryVariables.Any(x =>
                        path.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0
                    )
                )
                {
                    continue;
                }

                var pathDef = path;

                if (!string.IsNullOrEmpty(game.InstallDirectory))
                {
                    pathDef = path.Replace(@"{{p|game}}", game.InstallDirectory);
                }

                foreach (var kv in replacementDict)
                {
                    pathDef = ReplaceCaseInsensitive(pathDef, kv.Key, kv.Value);
                }

                if (!pathDef.Contains("*"))
                {
                    pathDefinitions.Add(pathDef);
                }
                else
                {
                    foreach (var matchingPath in GetAllMatchingPaths(pathDef))
                    {
                        if (!string.IsNullOrEmpty(matchingPath))
                        {
                            pathDefinitions.Add(matchingPath);
                        }
                    }
                }
            }

            foreach (var path in pathDefinitions.Distinct())
            {
                if (Directory.Exists(path))
                {
                    dirDefinitions.Add(path);
                }
                else if (File.Exists(path))
                {
                    dirDefinitions.Add(Path.GetDirectoryName(path));
                }
            }

            return dirDefinitions;
        }

        private static string ReplaceCaseInsensitive(
            string input,
            string search,
            string replacement
        )
        {
            return Regex.Replace(input, Regex.Escape(search), replacement, RegexOptions.IgnoreCase);
        }

        private static IEnumerable<string> GetAllMatchingPaths(string pattern)
        {
            try
            {
                char separator = Path.DirectorySeparatorChar;
                string[] parts = pattern.Split(separator);

                if (parts[0].Contains('*') || parts[0].Contains('?'))
                {
                    return new string[0];
                }

                var startPattern = string.Join(separator.ToString(), parts.Skip(1));
                return GetAllMatchingPathsInternal(startPattern, parts[0]);
            }
            catch
            {
                return new string[0];
            }
        }

        private static IEnumerable<string> GetAllMatchingPathsInternal(string pattern, string root)
        {
            try
            {
                char separator = Path.DirectorySeparatorChar;
                string[] parts = pattern.Split(separator);

                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Contains('*') || parts[i].Contains('?'))
                    {
                        var combined =
                            root + separator + string.Join(separator.ToString(), parts.Take(i));
                        if (!Directory.Exists(combined))
                        {
                            return new string[0];
                        }

                        if (i == parts.Length - 1)
                        {
                            return Directory
                                .EnumerateFiles(combined, parts[i], SearchOption.TopDirectoryOnly)
                                .Concat(
                                    Directory.EnumerateDirectories(
                                        combined,
                                        parts[i],
                                        SearchOption.TopDirectoryOnly
                                    )
                                );
                        }
                        else
                        {
                            var directories = Directory.EnumerateDirectories(
                                combined,
                                parts[i],
                                SearchOption.TopDirectoryOnly
                            );
                            return directories.SelectMany(dir =>
                                GetAllMatchingPathsInternal(
                                    string.Join(separator.ToString(), parts.Skip(i + 1)),
                                    dir
                                )
                            );
                        }
                    }
                }

                var absolute = root + separator + string.Join(separator.ToString(), parts);
                if (File.Exists(absolute) || Directory.Exists(absolute))
                {
                    return new[] { absolute };
                }

                return new string[0];
            }
            catch
            {
                return new string[0];
            }
        }

        // Helper metody pro získání Steam a Uplay cest
        private static string GetSteamInstallationDirectory()
        {
            try
            {
                using (
                    var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\WOW6432Node\Valve\Steam"
                    )
                )
                {
                    return key?.GetValue("InstallPath")?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetUbisoftInstallationDirectory()
        {
            try
            {
                using (
                    var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\WOW6432Node\Ubisoft\Launcher"
                    )
                )
                {
                    return key?.GetValue("InstallDir")
                            ?.ToString()
                            ?.Replace('/', '\\')
                            ?.Replace("Ubisoft Game Launcher\\", "Ubisoft Game Launcher")
                        ?? @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher";
                }
            }
            catch
            {
                return @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher";
            }
        }
    }

    // Models pro integrace se SaveFileView pluginem
    public enum PathType
    {
        Save = 1,
        Config = 2,
    }

    public class GameDirectoriesData
    {
        [SerializationPropertyName("PcgwPageId")]
        public string PcgwPageId { get; set; }

        [SerializationPropertyName("PathsData")]
        public List<PathData> PathsData { get; set; }
    }

    public class PathData
    {
        [SerializationPropertyName("Path")]
        public string Path { get; set; }

        [SerializationPropertyName("PathType")]
        public PathType Type { get; set; }
    }
}
