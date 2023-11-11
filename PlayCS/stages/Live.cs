using CounterStrikeSharp.API.Modules.Utils;
using PlayCS.enums;

namespace PlayCs;

/**
 * TODO - check is live if plugin restarts
 */
public partial class PlayCsPlugin
{
    public async void StartLive()
    {
        if (_matchData == null || (_currentPhase != ePhase.Warmup && _currentPhase != ePhase.Knife))
        {
            return;
        }

        SendCommands(
            new[]
            {
                "mp_autokick 0",
                "mp_autoteambalance 0",
                "mp_warmup_end",
                $"mp_backup_round_file ${_matchData.id}",
                "mp_round_restart_delay 3",
                "mp_free_armor 0",
                "mp_give_player_c4 1",
                "mp_maxmoney 16000",
                "mp_roundtime 1.92",
                "mp_roundtime_defuse 1.92",
                "mp_freezetime 15",
                "mp_startmoney 800",
                "mp_ct_default_secondary weapon_hkp2000",
                "mp_t_default_secondary weapon_glock",
                "mp_spectators_max 0",
                "sv_disable_teamselect_menu 1",
                // OT settings
                $"mp_overtime_enable {_matchData.overtime}",
                "mp_overtime_startmoney 10000",
                "mp_overtime_maxrounds 6",
                "mp_overtime_halftime_pausetimer 0",
                "cash_team_bonus_shorthanded 0",
                // MR settings
                $"mp_maxrounds {_matchData.mr * 2}",
                "mp_restartgame 1"
            }
        );

        UpdateCurrentRound();

        await Task.Delay(3000);
        Message(HudDestination.Alert, "LIVE LIVE LIVE!");
    }
}
