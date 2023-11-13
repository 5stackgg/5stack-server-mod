using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using PlayCs.entities;

namespace PlayCs;

public partial class PlayCsPlugin
{
    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
    {
        if (
            @event.Userid == null
            || !@event.Userid.IsValid
            || @event.Userid.IsBot
            || _matchData == null
        )
        {
            return HookResult.Continue;
        }

        CCSPlayerController player = @event.Userid;

        _redis.PublishMatchEvent(
            _matchData.id,
            new Redis.EventData<Dictionary<string, object>>
            {
                @event = "player",
                data = new Dictionary<string, object>
                {
                    { "player_name", player.PlayerName },
                    { "steam_id", player.SteamID.ToString() },
                }
            }
        );

        MatchMember? foundMatchingMember = _matchData
            .members
            .Find(member =>
            {
                if (member.steam_id == null)
                {
                    return member.name.StartsWith(player.PlayerName);
                }

                Console.WriteLine(
                    $"MEMBER HAS STEMA ID {member.steam_id.ToString()}: {player.SteamID.ToString()}"
                );
                return member.steam_id == player.SteamID.ToString();
            });

        if (foundMatchingMember != null)
        {
            MatchTeam? team = _matchData
                .teams
                .Find(team =>
                {
                    return team.id == foundMatchingMember.team_id;
                });

            if (team != null)
            {
                CsTeam startingSide = TeamStringToCsTeam(team.starting_side);
                if (TeamNumToCSTeam(player.TeamNum) != startingSide)
                {
                    Console.WriteLine($"Switching {player.PlayerName} to {team.starting_side}");
                    // TODO - this works but you get stuck in limbo
                    player.SwitchTeam(CsTeam.Spectator);
                    player.SwitchTeam(startingSide);
                }
            }
        }

        return HookResult.Continue;
    }
}
