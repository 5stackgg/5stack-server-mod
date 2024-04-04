using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using FiveStack.enums;

namespace FiveStack;

public partial class FiveStackPlugin
{
    [ConsoleCommand("css_stay", "")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnStay(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null || KnifeWinningTeam == null || !IsKnife())
        {
            return;
        }

        if (_captains[(CsTeam)KnifeWinningTeam]?.SteamID != player.SteamID)
        {
            Message(HudDestination.Chat, $" {ChatColors.Red}You are not the captain!", player);
            return;
        }

        Message(
            HudDestination.Alert,
            $"captain picked to {ChatColors.Red}stay {ChatColors.Default}sides"
        );

        UpdateMapStatus(eMapStatus.Live);
    }

    [ConsoleCommand("css_switch", "")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSwitch(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null || _matchData == null || KnifeWinningTeam == null || !IsKnife())
        {
            return;
        }

        if (_captains[(CsTeam)KnifeWinningTeam]?.SteamID != player.SteamID)
        {
            Message(HudDestination.Chat, $" {ChatColors.Red}You are not the captain!", player);
            return;
        }

        Message(
            HudDestination.Alert,
            $"captain picked to {ChatColors.Red}swap {ChatColors.Default}sides"
        );

        PublishGameEvent("switch", new Dictionary<string, object>());

        SendCommands(new[] { "mp_swapteams" });

        UpdateMapStatus(eMapStatus.Live);
    }

    [ConsoleCommand("skip_knife", "Skips knife round")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void OnSkipKnife(CCSPlayerController? player, CommandInfo? command)
    {
        if (!IsKnife())
        {
            return;
        }

        Message(HudDestination.Center, $"Skipping Knife.", player);

        UpdateMapStatus(eMapStatus.Live);
    }
}