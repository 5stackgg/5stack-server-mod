using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PlayCs;

public partial class PlayCsPlugin
{
    private Dictionary<int, bool> ReadyPlayers = new Dictionary<int, bool>();

    public void startWarmup()
    {
        if (Phase != ePhase.Unknown && Phase != ePhase.Knife && Phase != ePhase.Scheduled)
        {
            return;
        }

        Message(HudDestination.Center, "[PlayCS] Starting Warmup!");

        SendCommands(
            new[]
            {
                "mp_autokick 0",
                "mp_limitteams 0",
                "mp_autoteambalance 0",
                "sv_disable_teamselect_menu 0",
                "mp_warmuptime_all_players_connected 0",
                "mp_roundtime 60",
                "mp_roundtime_defuse 0",
                "mp_ct_default_secondary weapon_hkp2000",
                "mp_t_default_secondary weapon_glock",
                "mp_give_player_c4 0",
                "mp_warmup_pausetimer 1",
                "mp_maxmoney 60000",
                "mp_startmoney 60000",
                "mp_free_armor 1",
                // TODO - we have issues because we cant accurately detect if in a warmup status
                "mp_warmup_start",
            }
        );

        UpdatePhase(ePhase.Warmup);

        // await this.setupTeamNames(matchId);
    }

    private bool IsWarmup()
    {
        CCSGameRules? rules = GameRules();

        if (rules == null)
        {
            return false;
        }

        return rules.WarmupPeriod;
    }

    private int TotalReady()
    {
        return ReadyPlayers.Count(pair => pair.Value);
    }

    private CCSGameRules? GameRules()
    {
        return Utilities
            .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .First()
            .GameRules;
    }
}
