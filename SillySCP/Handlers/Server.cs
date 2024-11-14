﻿using Exiled.API.Enums;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.EventArgs.Server;
using MEC;
using Respawning;
using Scp914;
using SillySCP.API;
using UnityEngine;
using Features = Exiled.API.Features;

namespace SillySCP.Handlers
{
    public class Server : IInittable
    {
        public void Init()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += OnRoundRestart;
            Exiled.Events.Handlers.Server.RespawningTeam += OnRespawn;
            Exiled.Events.Handlers.Scp914.UpgradingPickup += OnScp914UpgradePickup;
        }

        public void Unregister()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound -= OnRoundRestart;
            Exiled.Events.Handlers.Server.RespawningTeam -= OnRespawn;
            Exiled.Events.Handlers.Scp914.UpgradingPickup -= OnScp914UpgradePickup;
        }
        
        private void OnWaitingForPlayers()
        {
            Plugin.Instance.Scp106 = null;
            DiscordBot.Instance.SetStatus();
        }

        private void OnRoundEnded(RoundEndedEventArgs _)
        {
            var highestKills = Plugin
                .Instance.PlayerStats.Where(p => p.Kills > 0).OrderByDescending(p => p.Kills)
                .ToList();
            var scpHighestKills = Plugin
                .Instance.PlayerStats.Where(p => p.ScpKills > 0).OrderByDescending(p => p.ScpKills)
                .ToList();
            var highestKiller = highestKills.FirstOrDefault();
            var scpHighestKiller = scpHighestKills.FirstOrDefault();
            Features.Server.FriendlyFire = true;
            
            var normalMvpMessage = highestKiller != null ? $"Highest kills from a human for this round is {highestKiller.Player.Nickname} with {highestKiller.Kills}" : null;
            var scpMvpMessage = scpHighestKiller != null ? $"Highest kills from an SCP for this round is {scpHighestKiller.Player.Nickname} with {scpHighestKiller.ScpKills}" : null;
            var message = $"{normalMvpMessage}\n{scpMvpMessage}".Trim();
            
            Features.Map.Broadcast(
                10,
                message
            );

            var discMessage = "Round has ended with the following people:\n```";
            discMessage += string.Join("\n", Features.Player.List.Select(player => $"{player.Nickname} ({player.UserId})"));
            discMessage += "```";
            DiscordBot.Instance.ConnectionChannel.SendMessageAsync(discMessage);
        }
        
        private void OnRoundStarted()
        {
            Features.Server.FriendlyFire = false;
            Plugin.Instance.PlayerStats = new List<PlayerStat>();
            Plugin.Instance.Volunteers = new List<Volunteers>();
            Timing.RunCoroutine(Plugin.Instance.DisableVolunteers());
            DiscordBot.Instance.SetStatus(true);
            // var eventRound = Plugin.Instance.RoundEvents.EventRound();
            // if (eventRound)
            // {
            //     var eventChosen = Plugin.Instance.RoundEvents.PlayRandomEvent();
            //     Plugin.Instance.ChosenEvent = eventChosen.Item1;
            //     Map.Broadcast(10, eventChosen.Item2);
            // }
            // else
            // {
            //     Plugin.Instance.ChosenEvent = null;
            // }

            var message = "Round has started with the following people:\n```";
            message += string.Join("\n", Features.Player.List.Select(player => $"{player.Nickname} ({player.UserId})"));
            message += "```";
            DiscordBot.Instance.ConnectionChannel.SendMessageAsync(message);
            DiscordBot.Instance.DeathChannel.SendMessageAsync("New round");
            Timing.RunCoroutine(Plugin.Instance.HeartAttack());
            Timing.RunCoroutine(CheckNukeRoom());
        }

        private void OnRoundRestart()
        {
            Features.Server.FriendlyFire = false;
            DiscordBot.Instance.SetStatus();
        }

        private void OnRespawn(RespawningTeamEventArgs ev)
        {
            if (ev.NextKnownTeam == SpawnableTeamType.None)
                return;

            ev.Players.ForEach(p =>
            {
                p.ShowHint("", int.MaxValue);
                PlayerStat playerStats = PlayerStatUtils.FindPlayerStat(p);
                if (playerStats == null) return;
                playerStats.Spectating = null;
            });
        }
        
        private void OnScp914UpgradePickup(UpgradingPickupEventArgs ev)
        {
            if (ev.KnobSetting == Scp914KnobSetting.Fine && ev.Pickup.Type == ItemType.Coin)
            {
                var randomNum = UnityEngine.Random.Range(1, 3);
                switch (randomNum)
                {
                    case 1:
                    {
                        ev.Pickup.Destroy();
                        Pickup.CreateAndSpawn(ItemType.Flashlight, ev.OutputPosition, Quaternion.identity);
                        break;
                    }
                    case 2:
                    {
                        ev.Pickup.Destroy();
                        Pickup.CreateAndSpawn(ItemType.Radio, ev.OutputPosition, Quaternion.identity);
                        break;
                    }
                    case 3:
                    {
                        ev.Pickup.Destroy();
                        Pickup.CreateAndSpawn(ItemType.KeycardJanitor, ev.OutputPosition, Quaternion.identity);
                        break;
                    }
                }
            }
        }
        
        private IEnumerator<float> CheckNukeRoom()
        {
            while (Features.Round.IsStarted && !Features.Round.IsEnded)
            {
                var players = Features.Player.List;
                foreach (var player in players)
                {
                    if (!player.IsAlive) continue;
                    if (player.CurrentRoom.Type == RoomType.HczNuke)
                    {
                        Timing.RunCoroutine(Plugin.Instance.PlayerHandler.StartNukeDamage(player));
                    }
                }

                yield return Timing.WaitForSeconds(60);
            }

            yield return 0;
        }
    }
}