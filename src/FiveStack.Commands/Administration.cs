using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using FiveStack.entities;
using FiveStack.enums;
using Microsoft.Extensions.Logging;

namespace FiveStack;

public partial class FiveStackPlugin
{
    [ConsoleCommand("get_match", "Gets match information from api")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void get_match(CCSPlayerController? player, CommandInfo command)
    {
        GetMatch();
    }

    private async void GetMatch()
    {
        HttpClient httpClient = new HttpClient();

        string? serverId = Environment.GetEnvironmentVariable("SERVER_ID");
        string? apiPassword = Environment.GetEnvironmentVariable("SERVER_API_PASSWORD");

        Logger.LogInformation($"Server ID: {serverId}");

        if (serverId == null || apiPassword == null)
        {
            Logger.LogWarning("Missing Server ID / API Password");
            await Task.Delay(1000 * 5);
            Server.NextFrame(() =>
            {
                GetMatch();
            });

            return;
        }

        try
        {
            Logger.LogInformation(
                $"Fetching Match Info: https://api.5stack.gg/server/match/{serverId}"
            );

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiPassword);

            string? response = await httpClient.GetStringAsync(
                $"https://api.5stack.gg/server/match/{serverId}"
            );

            Server.NextFrame(() =>
            {
                if (response.Length == 0)
                {
                    Logger.LogWarning("currenlty no match assigned to server");
                    return;
                }

                _matchData = JsonSerializer.Deserialize<Match>(response);

                if (_matchData == null)
                {
                    return;
                }

                if (_currentMapStatus == eMapStatus.Warmup)
                {
                    Message(HudDestination.Alert, "Received Match Data");
                }

                SetupMatch();
            });
        }
        catch (HttpRequestException ex)
        {
            Logger.LogInformation($"HTTP request error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Logger.LogInformation($"JSON deserialization error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"An unexpected error occurred: {ex.Message}");
        }
    }

    [ConsoleCommand("match_state", "Forces a match to update its current state")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void SetMatchState(CCSPlayerController? player, CommandInfo command)
    {
        UpdateMapStatus(MapStatusStringToEnum(command.ArgString));
    }

    [ConsoleCommand("restore_round", "Restores to a previous round")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void RestoreRound(CCSPlayerController? player, CommandInfo command)
    {
        if (_matchData == null)
        {
            return;
        }

        string round = command.ArgByIndex(1);
        string backupRoundFile =
            $"{GetSafeMatchPrefix()}_round{round.ToString().PadLeft(2, '0')}.txt";

        if (!File.Exists(Path.Join(Server.GameDirectory + "/csgo/", backupRoundFile)))
        {
            command.ReplyToCommand($"Unable to restore round, missing file ({backupRoundFile})");
            return;
        }

        SendCommands(new[] { "mp_pause_match", $"mp_backup_restore_load_file {backupRoundFile}" });

        Message(
            HudDestination.Alert,
            $" {ChatColors.Red}Round {round} has been restored (.resume to continue)"
        );
    }

    public void UpdateMapStatus(eMapStatus status)
    {
        if (_matchData == null)
        {
            Logger.LogInformation("missing event data");
            return;
        }

        Logger.LogInformation($"Update Map Status {_currentMapStatus} -> {status}");

        switch (status)
        {
            case eMapStatus.Scheduled:
            case eMapStatus.Warmup:
                status = eMapStatus.Warmup;
                StartWarmup();
                break;
            case eMapStatus.Knife:
                if (!_matchData.knife_round)
                {
                    UpdateMapStatus(eMapStatus.Live);
                    break;
                }

                var currentMap = GetCurrentMap();
                if (currentMap == null)
                {
                    break;
                }

                if (currentMap.order == _matchData.best_of && _matchData.knife_round)
                {
                    StartKnife();
                }

                break;
            case eMapStatus.Live:
                StartLive();
                break;
        }

        _currentMapStatus = status;
    }

    private void _publishGameState(eMapStatus status)
    {
        if (_matchData == null)
        {
            return;
        }

        _redis.PublishMatchEvent(
            _matchData.id,
            new Redis.EventData<Dictionary<string, object>>
            {
                @event = "mapStatus",
                data = new Dictionary<string, object> { { "status", status.ToString() }, }
            }
        );
    }

    public void SetupMatch()
    {
        if (_matchData == null)
        {
            Logger.LogInformation("Missing Match Data");
            return;
        }
        Logger.LogInformation($"Setup Match {_matchData.id}");

        _currentMap = GetCurrentMap();

        if (_currentMap == null)
        {
            Logger.LogWarning("Unable to find map");
            return;
        }

        if (!IsOnMap(_currentMap.map.name))
        {
            ChangeMap(_currentMap.map);
            return;
        }

        SendCommands(new[] { $"sv_password \"{_matchData.password}\"" });

        SetupTeamNames();

        Logger.LogInformation(
            $"Current Game State {_currentMapStatus}:{_currentMap.status}:{_currentMap.map.name}"
        );

        if (MapStatusStringToEnum(_currentMap.status) != _currentMapStatus)
        {
            UpdateMapStatus(MapStatusStringToEnum(_currentMap.status));
        }
    }

    public MatchMap? GetCurrentMap()
    {
        if (_matchData == null || _matchData.current_match_map_id == null)
        {
            return null;
        }

        return _matchData?.match_maps.FirstOrDefault(match_map =>
        {
            return match_map.id == _matchData.current_match_map_id;
        });
    }

    public void SetupTeamNames()
    {
        if (_matchData == null)
        {
            return;
        }

        if (_matchData.lineup_1.name != null)
        {
            SendCommands(new[] { $"mp_teamname_1 {_matchData.lineup_1.name}" });
        }

        if (_matchData.lineup_2.name != null)
        {
            SendCommands(new[] { $"mp_teamname_2 {_matchData.lineup_2.name}" });
        }
    }

    public void ChangeMap(Map map)
    {
        Logger.LogInformation($"Changing Map {map.name}");

        if (map.workshop_map_id == null && Server.IsMapValid(map.name))
        {
            SendCommands(new[] { $"changelevel \"{map.name}\"" });
        }
        else
        {
            SendCommands(new[] { $"host_workshop_map {map.workshop_map_id}" });
        }
    }

    public bool IsOnMap(string map)
    {
        Logger.LogInformation($"Map Check: {_onMap}:{map}");

        return map == _onMap;
    }
}
