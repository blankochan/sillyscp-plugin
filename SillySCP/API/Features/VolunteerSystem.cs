﻿using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using SillySCP.API.EventArgs;
using SillySCP.API.Modules;

namespace SillySCP.API.Features
{
    public static class VolunteerSystem
    {
        public static bool ReadyVolunteers => Round.ElapsedTime.TotalSeconds < Plugin.Instance.Config.VolunteerTime;
        public static List<Volunteers> Volunteers = new();
        
        public static event EventHandler VolunteerPeriodEnd;
        public static event EventHandler<VolunteerEventArgs> Volunteer;
        public static event EventHandler<VolunteerCreatedEventArgs> VolunteerCreated;
        public static event EventHandler<VolunteerChosenEventArgs> VolunteerChosen;

        public static Dictionary<string, RoleTypeId> VaildScps { get; set; } = new ()
        {
            { "173", RoleTypeId.Scp173 },
            { "peanut", RoleTypeId.Scp173 },
            { "939", RoleTypeId.Scp939 },
            { "079", RoleTypeId.Scp079 },
            { "79", RoleTypeId.Scp079 },
            { "computer", RoleTypeId.Scp079 },
            { "106", RoleTypeId.Scp106 },
            { "larry", RoleTypeId.Scp106 },
            { "096", RoleTypeId.Scp096 },
            { "96", RoleTypeId.Scp096 },
            { "shyguy", RoleTypeId.Scp096 },
            { "049", RoleTypeId.Scp049 },
            { "49", RoleTypeId.Scp049 },
            { "doctor", RoleTypeId.Scp049 },
            { "0492", RoleTypeId.Scp0492 },
            { "049-2", RoleTypeId.Scp0492 },
            { "492", RoleTypeId.Scp0492 },
            { "zombie", RoleTypeId.Scp0492 },
        };
        
        public static void NewVolunteer(RoleTypeId role)
        {
            Volunteers volunteer = new ()
            {
                Replacement = role,
                Players = new()
            };
            Volunteers ??= new();
            Volunteers.Add(volunteer);
            Timing.RunCoroutine(ChooseVolunteers(volunteer));
            
            string annoucement =                                      // if we add support for other roles this ↓ will need to be changed
                $"{role.GetFullName()} has left the game\nPlease run .volunteer {role.GetFullName().Substring(4)} to volunteer to be the SCP";
            
            if (role == RoleTypeId.Scp0492)
            {
                foreach (Player player in Player.List)
                {
                    if (player.IsAlive) continue;
                    
                    player.Broadcast(10, annoucement);
                }
                return;
            }
            Map.Broadcast(10, annoucement);
            
            VolunteerCreated?.Invoke(null, new (volunteer));
        }
        public static IEnumerator<float> DisableVolunteers()
        {
            yield return Timing.WaitForSeconds(120);
            List<Player> scps = Player.List.Where(p => p.IsScp).ToList();
            if(scps.Count == 1 && scps.First().Role.Type == RoleTypeId.Scp079 && !ReadyVolunteers)
                Scp079Recontainment.Recontain();
            VolunteerPeriodEnd?.Invoke(null, null);
        }

        public static IEnumerator<float> ChooseVolunteers(Volunteers volunteer)
        {
            yield return Timing.WaitForSeconds(15);
            volunteer = Volunteers.FirstOrDefault(v => v.Replacement == volunteer.Replacement);
            if (volunteer == null)
                yield break;
            Volunteers volunteerClone = new ()
            {
                Replacement = volunteer.Replacement,
                Players = volunteer.Players
            };
            Volunteers.Remove(volunteer);
            if (volunteerClone.Players.Count == 0) yield break;
            Player replacementPlayer = volunteerClone.Players.GetRandomValue();
            replacementPlayer.Role.Set(volunteerClone.Replacement);
            Map.Broadcast(10, volunteerClone.Replacement.GetFullName() + " has been replaced!",
                Broadcast.BroadcastFlags.Normal, true);
            VolunteerChosen?.Invoke(null, new (replacementPlayer, volunteerClone));
            yield return 0;
        }
        
        public static void RaiseVolunteerEvent(Player player, Volunteers volunteer)
        {
            Volunteer?.Invoke(null, new (player, volunteer));
        }
    }
}