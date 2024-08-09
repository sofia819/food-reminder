using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace FoodReminder.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly MainTab MainTab;

    private readonly ContentTab ContentTab;

    private readonly StyleTab StyleTab;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin)
        : base("FoodReminder###FoodReminderConfig")
    {
        Flags =
            ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(200, 230);
        SizeCondition = ImGuiCond.Always;

        MainTab = new MainTab(plugin.Configuration);
        ContentTab = new ContentTab(plugin.Configuration);
        StyleTab = new StyleTab(plugin.Configuration);
    }

    public void Dispose() { }

    public override void PreDraw() { }

    public override void Draw()
    {
        ImGui.BeginTabBar("Settings");

        MainTab.Draw();
        ContentTab.Draw();
        StyleTab.Draw();

        ImGui.EndTabBar();
    }
}
