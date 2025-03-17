﻿using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using SillySCP.API.Features;

namespace SillySCP.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class Human : ICommand
    {
        public string Command { get; } = "human";

        public string[] Aliases { get; } = new [] { "h" };

        public string Description { get; } = "Change into a human, if you're an SCP.";

        public bool Execute(
            ArraySegment<string> arguments,
            ICommandSender sender,
            out string response
        )
        {
            Player.TryGet(sender, out Player player);
            
            if (player.Role == RoleTypeId.Scp0492)
            {
                VolunteerSystem.NewVolunteer(player.Role,original:player);
                response = "Opening a replacement for SCP-049-2!";
                return true;
            }
            
            if (!VolunteerSystem.ReadyVolunteers)
            {
                response = "You can not change into a human after the volunteer period is over!";
                return false;
            }
            
            
            if (!player.IsScp)
            {
                response = "Only SCPs can use this command!";
                return false;
            }
            
            RoleTypeId role = HumanSpawner.NextHumanRoleToSpawn;
            VolunteerSystem.NewVolunteer(player.Role);
            player.Role.Set(role, SpawnReason.None);
            response = $"You have been changed into {role.GetFullName()}!";
            return true;
        }
    }
}