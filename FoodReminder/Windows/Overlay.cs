using System;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace FoodReminder.Windows;

public class Overlay
    : Window, IDisposable
{
    private const int ImageWidth = 128;

    private const int ImageHeight = 128;

    private readonly Configuration configuration;

    private readonly IFontHandle font;

    private DateTime lastVisibleTime;

    private readonly TimeSpan flashTimeSpan = TimeSpan.FromSeconds(1);

    private bool visible;

    private string iconPath;

    public Overlay(Plugin plugin, Configuration configuration, IFontHandle font, string iconPath) : base(
        "FoodReminder###Overlay")
    {
        this.configuration = configuration;
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        Size = new Vector2(1040, 400);
        SizeCondition = ImGuiCond.Always;

        this.configuration = plugin.Configuration;
        this.font = font;
        this.iconPath = iconPath;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (configuration.IsOverlayLocked)
        {
            Flags |= ImGuiWindowFlags.NoInputs;
            Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoInputs;
            Flags &= ~ImGuiWindowFlags.NoMove;
        }

        Flags |= ImGuiWindowFlags.NoTitleBar;
        Flags |= ImGuiWindowFlags.NoBackground;
    }

    public override void Draw()
    {
        var eatFood = "EAT FOOD";
        var currentTime = DateTime.Now;

        if (configuration.IsFlashingEffectEnabled && currentTime - lastVisibleTime > flashTimeSpan)
        {
            visible = !visible;
            lastVisibleTime = currentTime;
        }

        var topLeft = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
        var imDrawListPtr = ImGui.GetWindowDrawList();

        var image = Plugin.TextureProvider.GetFromFile(iconPath).GetWrapOrDefault();

        if (configuration.ShowIcon && image != null)
        {
            // Leave some padding
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMin().X + (14 * configuration.OverlayScale));
            ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMin().Y + (14 * configuration.OverlayScale));
            ImGui.Image(image.ImGuiHandle,
                        new Vector2((ImageWidth * configuration.OverlayScale) - (14 * configuration.OverlayScale),
                                    (ImageHeight * configuration.OverlayScale) - (14 * configuration.OverlayScale)));
        }

        var imageEdge = new Vector2(topLeft.X + (ImageWidth * configuration.OverlayScale),
                                    topLeft.Y + (ImageHeight * configuration.OverlayScale));

        imDrawListPtr.AddRectFilled(
            new Vector2(imageEdge.X + (6 * configuration.OverlayScale), topLeft.Y + (24 * configuration.OverlayScale)),
            new Vector2(imageEdge.X + (210 * configuration.OverlayScale),
                        topLeft.Y + (100 * configuration.OverlayScale)),
            ImGui.GetColorU32(configuration.BackgroundColor));
        font.Push();
        ImGui.SetWindowFontScale(configuration.OverlayScale);
        imDrawListPtr.AddText(
            new Vector2(imageEdge.X + (20 * configuration.OverlayScale), topLeft.Y + (40 * configuration.OverlayScale)),
            !configuration.IsFlashingEffectEnabled || visible
                ? ImGui.GetColorU32(configuration.PrimaryTextColor)
                : ImGui.GetColorU32(configuration.SecondaryTextColor),
            eatFood);
        font.Pop();
    }
}
