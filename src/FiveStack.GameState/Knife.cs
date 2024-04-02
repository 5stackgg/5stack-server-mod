using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using FiveStack.enums;

namespace FiveStack;

public partial class FiveStackPlugin
{
    public async void StartKnife()
    {
        if (_matchData == null || IsKnife())
        {
            return;
        }

        if (_captains[CsTeam.Terrorist] == null)
        {
            AutoSelectCaptain(CsTeam.Terrorist);
        }

        if (_captains[CsTeam.CounterTerrorist] == null)
        {
            AutoSelectCaptain(CsTeam.CounterTerrorist);
        }

        SendCommands(new[] { "exec knife" });

        PublishMapStatus(eMapStatus.Knife);

        SendCommands(new[] { "mp_warmup_end", "mp_restartgame 0" });

        await Task.Delay(1000);
        Server.NextFrame(() =>
        {
            Message(HudDestination.Alert, "KNIFE KNIFE KNIFE!");
        });
    }

    private void AutoSelectCaptain(CsTeam team)
    {
        List<CCSPlayerController> players = Utilities
            .GetPlayers()
            .FindAll(player =>
            {
                return player.TeamNum == (int)team && player.SteamID != 0;
            });

        if (players.Count == 0)
        {
            return;
        }

        CCSPlayerController player = players[Random.Shared.Next(players.Count)];

        ClaimCaptain(
            team,
            player,
            $" {(team == CsTeam.Terrorist ? ChatColors.Gold : ChatColors.Blue)}{TeamNumToString((int)team)}'s {ChatColors.Default}captain was auto selected to be {ChatColors.Red}{player.PlayerName}"
        );
    }
}
