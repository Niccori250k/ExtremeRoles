﻿using HarmonyLib;

using UnityEngine;

namespace ExtremeRoles.Patches.MiniGame
{
    [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
    public class EmergencyMinigameUpdatePatch
    {
        public static void Postfix(EmergencyMinigame __instance)
        {
            if (Roles.ExtremeRoleManager.GameRole.Count == 0) { return; }

            if (!Roles.ExtremeRoleManager.GetLocalPlayerRole().CanCallMeeting)
            {
                __instance.StatusText.text = Helper.Translation.GetString("youDonotUse");
                __instance.NumberText.text = string.Empty;
                __instance.ClosedLid.gameObject.SetActive(true);
                __instance.OpenLid.gameObject.SetActive(false);
                __instance.ButtonActive = false;
                return;
            }

            // Handle max number of meetings
            if (__instance.state == 1)
            {
                int localRemaining = PlayerControl.LocalPlayer.RemainingEmergencies;
                int teamRemaining = Mathf.Max(
                    0, OptionHolder.Ship.MaxNumberOfMeeting - ExtremeRolesPlugin.GameDataStore.MeetingsCount);
                int remaining = Mathf.Min(localRemaining, teamRemaining);

                __instance.StatusText.text = string.Concat(
                    "<size=100%>",
                    string.Format(
                        Helper.Translation.GetString("meetingStatus"),
                        PlayerControl.LocalPlayer.name),
                    "</size>");
                __instance.NumberText.text = string.Format(
                    Helper.Translation.GetString("meetingCount"),
                    localRemaining.ToString(),
                    teamRemaining.ToString());
                __instance.ButtonActive = remaining > 0;
                __instance.ClosedLid.gameObject.SetActive(!__instance.ButtonActive);
                __instance.OpenLid.gameObject.SetActive(__instance.ButtonActive);
                return;
            }
        }
    }
}
