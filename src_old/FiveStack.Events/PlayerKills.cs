using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;

namespace FiveStack;

public partial class FiveStackPlugin
{
    [GameEventHandler]
    public HookResult OnPlayerKill(EventPlayerDeath @event, GameEventInfo info)
    {
        if (
            @event.Userid == null
            || !@event.Userid.IsValid
            || _matchData == null
            || _matchData.current_match_map_id == null
            || !IsLive()
        )
        {
            return HookResult.Continue;
        }

        CCSPlayerController attacked = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker.IsValid ? @event.Attacker : null;

        var attackerLocation = attacker?.PlayerPawn?.Value?.AbsOrigin;
        var attackedLocation = attacked?.PlayerPawn?.Value?.AbsOrigin;

        PublishGameEvent(
            "kill",
            new Dictionary<string, object>
            {
                { "time", DateTime.Now },
                { "match_map_id", _matchData.current_match_map_id },
                { "no_scope", @event.Noscope },
                { "blinded", @event.Attackerblind },
                { "thru_smoke", @event.Thrusmoke },
                { "thru_wall", @event.Penetrated > 0 },
                { "headshot", @event.Headshot },
                { "round", _currentRound },
                { "attacker_steam_id", attacker != null ? attacker.SteamID.ToString() : "" },
                { "attacker_team", attacker != null ? $"{TeamNumToString(attacker.TeamNum)}" : "" },
                { "attacker_location", $"{attacker?.PlayerPawn?.Value?.LastPlaceName}" },
                {
                    "attacker_location_coordinates",
                    attackerLocation != null
                        ? $"{Convert.ToInt32(attackerLocation.X)} {Convert.ToInt32(attackerLocation.Y)} {Convert.ToInt32(attackerLocation.Z)}"
                        : ""
                },
                { "weapon", $"{@event.Weapon}" },
                { "hitgroup", $"{HitGroupToString(@event.Hitgroup)}" },
                { "attacked_steam_id", attacked != null ? attacked.SteamID.ToString() : "" },
                { "attacked_team", attacked != null ? $"{TeamNumToString(attacked.TeamNum)}" : "" },
                { "attacked_location", $"{attacked?.PlayerPawn?.Value?.LastPlaceName}" },
                {
                    "attacked_location_coordinates",
                    attackedLocation != null
                        ? $"{Convert.ToInt32(attackedLocation.X)} {Convert.ToInt32(attackedLocation.Y)} {Convert.ToInt32(attackedLocation.Z)}"
                        : ""
                },
            }
        );

        CCSPlayerController? assister = @event.Assister;

        if (attacker != null && attacked != null && assister != null && assister.IsValid)
        {
            if (attacker.TeamNum != attacked.TeamNum)
            {
                PublishGameEvent(
                    "assist",
                    new Dictionary<string, object>
                    {
                        { "time", DateTime.Now },
                        { "match_map_id", _matchData.current_match_map_id },
                        { "match_id", _matchData.id },
                        { "round", _currentRound },
                        { "attacker_steam_id", assister.SteamID.ToString() },
                        { "attacker_team", $"{TeamNumToString(attacker.TeamNum)}" },
                        { "attacked_steam_id", attacked.SteamID.ToString() },
                        { "attacked_team", $"{TeamNumToString(attacked.TeamNum)}" },
                        { "flash", @event.Assistedflash },
                    }
                );
            }
        }

        return HookResult.Continue;
    }
}