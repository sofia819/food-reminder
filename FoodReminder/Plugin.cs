using System.IO;
using System.Text.RegularExpressions;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FoodReminder.Windows;
using ContentFinderCondition = Lumina.Excel.GeneratedSheets.ContentFinderCondition;

namespace FoodReminder;

public sealed class Plugin : IDalamudPlugin
{
    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("FoodReminder");

    private const string CommandName = "/food";
    private IPlayerCharacter? PlayerCharacter { get; set; }
    private ConfigWindow ConfigWindow { get; init; }
    private Overlay Overlay { get; init; }

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IDutyState DutyState { get; private set; } = null!;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        var newGameFontHandle =
            PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 46));
        var iconPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "icon.png");
        ConfigWindow = new ConfigWindow(this);
        Overlay = new Overlay(this, Configuration, newGameFontHandle, iconPath);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(Overlay);
        CommandManager.AddHandler(CommandName,
                                  new CommandInfo(OnCommand) { HelpMessage = "Opens Food Reminder config" });
        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Framework.Update += CheckFood;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        Overlay.Dispose();
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our config UI
        ToggleConfigUI();
    }

    private unsafe void CheckFood(IFramework framework)
    {
        // Check if enabled
        if (!Configuration.IsEnabled)
        {
            ToggleOverlayOff();
            return;
        }

        // Make sure there is a player
        PlayerCharacter = ClientState.LocalPlayer;
        if (PlayerCharacter == null)
        {
            ToggleOverlayOff();
            return;
        }

        // Make sure duty is ready
        if (!DutyState.IsDutyStarted) return;

        // Whether to show in combat
        if (Configuration.HideInCombat && (PlayerCharacter.StatusFlags & StatusFlags.InCombat) != 0)
        {
            ToggleOverlayOff();
            return;
        }

        var currentContent =
            DataManager.GetExcelSheet<ContentFinderCondition>(ClientLanguage.English)!.GetRow(
                GameMain.Instance()->CurrentContentFinderConditionId);
        if (currentContent == null)
        {
            ToggleOverlayOff();
            return;
        }

        // Only show if level synced
        if (Configuration.ShowIfLevelSynced && currentContent.ClassJobLevelSync != PlayerCharacter.Level)
        {
            ToggleOverlayOff();
            return;
        }

        // Check duty if specific filter applied
        if (!Configuration.EnableAll)
        {
            // Check Content Type By Name
            var contentName = currentContent.Name.RawString;

            // If not all enabled, only check these content names
            var validContentNames =
                Regex.IsMatch(contentName, "(Minstrel's Ballad|\\(Extreme\\)|\\(Savage\\)|\\(Ultimate\\))");
            if (!validContentNames)
            {
                ToggleOverlayOff();
                return;
            }

            if (!Configuration.ShowInExtreme &&
                (contentName.Contains("(Extreme)") || contentName.Contains("Minstrel's Ballad")))
            {
                ToggleOverlayOff();
                return;
            }

            if (!Configuration.ShowInSavage && contentName.Contains("(Savage)"))
            {
                ToggleOverlayOff();
                return;
            }

            if (!Configuration.ShowInUltimate && contentName.Contains("(Ultimate)"))
            {
                ToggleOverlayOff();
                return;
            }
        }

        // Check if well-fed
        var playerCharacterStatusList = PlayerCharacter.StatusList;
        var hasFood = false;
        foreach (var status in playerCharacterStatusList)
            if (status.StatusId == 48 && status.RemainingTime > Configuration.RemainingTimeInSeconds)
                hasFood = true;

        /*
         * Opens overlay if player has no food
         * Closes overlay if player has food
         */
        Overlay.IsOpen = !hasFood;
    }

    private void ToggleOverlayOff()
    {
        Overlay.IsOpen = false;
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void ToggleConfigUI()
    {
        ConfigWindow.Toggle();
    }
}
