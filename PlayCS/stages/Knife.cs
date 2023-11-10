using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PlayCs;

public partial class PlayCsPlugin
{
    private CsTeam KnifeWinningTeam;

    public async void startKnife()
    {
        if (CurrentPhase != ePhase.Warmup)
        {
            return;
        }

        if (Captains[CsTeam.Terrorist] == null)
        {
            AutoSelectCaptain(CsTeam.Terrorist);
        }

        if (Captains[CsTeam.CounterTerrorist] == null)
        {
            AutoSelectCaptain(CsTeam.CounterTerrorist);
        }

        SendCommands(
            new[]
            {
                "mp_maxrounds 12",
                "mp_roundtime 60",
                "mp_roundtime_defuse 60",
                "mp_ct_default_secondary ''",
                "mp_t_default_secondary ''",
                "mp_free_armor 1",
                "mp_give_player_c4 0",
                "mp_maxmoney 0",
                "mp_freezetime 0",
                "mp_respawn_immunitytime 0",
                "mp_respawn_on_death_ct 0",
                "mp_respawn_on_death_t 0",
                "mp_team_intro_time 0",
                "mp_round_restart_delay 0",
                "mp_warmup_end",
            }
        );

        await Task.Delay(1000);
        Message(HudDestination.Alert, "KNIFE KNIFE KNIFE!");
    }

    public void NotifyCaptainSideSelection()
    {
        if (KnifeWinningTeam == null || Captains[KnifeWinningTeam] == null)
        {
            Console.WriteLine("CAPTAIN IS MISSING..?");

            return;
        }

        Message(
            HudDestination.Chat,
            $"As the captain you must select to {ChatColors.Green}!stay {ChatColors.Default} or {ChatColors.Green}!switch",
            Captains[KnifeWinningTeam]
        );
        Message(
            HudDestination.Alert,
            $"{(KnifeWinningTeam == CsTeam.Terrorist ? "Terrorist" : "CT")} - Captain is Picking Sides!"
        );
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

        CCSPlayerController? player = players[Random.Shared.Next(players.Count)];

        ClaimCaptain(
            team,
            player,
            $" {(team == CsTeam.Terrorist ? ChatColors.Gold : ChatColors.Blue)}{TeamNumToString((int)team)}'s {ChatColors.Default}captain was auto selected to be {ChatColors.Red}{player.PlayerName}"
        );
    }
}