﻿using HarmonyLib;

namespace ExtremeSkins.Patches.AmongUs
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    public class AmongUsClientOnPlayerJoinedPatch
    {
        public static void Postfix()
        {
            if (PlayerControl.LocalPlayer != null)
            {
                VersionManager.ShareVersion();
            }
        }
    }
}
