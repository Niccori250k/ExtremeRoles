﻿using System.Linq;

using HarmonyLib;
using UnityEngine;

using ExtremeRoles.Module;
using ExtremeRoles.Roles;

namespace ExtremeRoles.Patches
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    class ShipStatusCalculateLightRadiusPatch
    {
        public static bool Prefix(
            ref float __result,
            ShipStatus __instance,
            [HarmonyArgument(0)] GameData.PlayerInfo playerInfo)
        {
            ISystemType systemType = __instance.Systems.ContainsKey(
                SystemTypes.Electrical) ? __instance.Systems[SystemTypes.Electrical] : null;
            if (systemType == null) { return true; }

            SwitchSystem switchSystem = systemType.TryCast<SwitchSystem>();
            if (switchSystem == null) { return true; }

            var allRole = ExtremeRoleManager.GameRole;
            if (allRole.Count == 0) { return true; }

            float num = (float)switchSystem.Value / 255f;
            float switchVisonMulti = Mathf.Lerp(
                __instance.MinLightRadius,
                __instance.MaxLightRadius, num);

            float baseVison = __instance.MaxLightRadius;

            if (playerInfo == null || playerInfo.IsDead) // IsDead
            {
                __result = baseVison;
            }
            else if (allRole[playerInfo.PlayerId].HasOtherVison)
            {   
                if (allRole[playerInfo.PlayerId].IsApplyEnvironmentVision)
                {
                    baseVison = switchVisonMulti;
                }
                __result = baseVison * allRole[playerInfo.PlayerId].Vison;
            }
            else if (playerInfo.Role.IsImpostor)
            {
                __result = baseVison * PlayerControl.GameOptions.ImpostorLightMod;
            }
            else
            {
                __result = switchVisonMulti * PlayerControl.GameOptions.CrewLightMod;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    class ShipStatusCheckEndCriteriaPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (!GameData.Instance) { return false; };
            if (DestroyableSingleton<TutorialManager>.InstanceExists) { return true; } // InstanceExists | Don't check Custom Criteria when in Tutorial
            if (HudManager.Instance.isIntroDisplayed){ return false; }

            if (AssassinMeeting.AssassinMeetingTrigger) { return false; }

            var statistics = new GameDataContainer.PlayerStatistics();

            if (isSabotageWin(__instance)) { return false; }
            if (isTaskWin(__instance)) { return false; };

            if (isNeutralSpecialWin(__instance)) { return false; };
            if (isNeutralAliveWin(__instance, statistics)) { return false; };

            if (statistics.SeparatedNeutralAlive.Count != 0) { return false; }

            if (isImpostorWin(__instance, statistics)) { return false; };
            if (isCrewmateWin(__instance, statistics)) { return false; };
            
            return false;
        }

        private static void gameIsEnd(
            ref ShipStatus curShip,
            GameOverReason reason,
            bool trigger = false)
        {
            curShip.enabled = false;
            ShipStatus.RpcEndGame(reason, trigger);
        }

        private static bool isCrewmateWin(
            ShipStatus __instance,
            GameDataContainer.PlayerStatistics statistics)
        {
            if (statistics.TeamCrewmateAlive > 0 && 
                statistics.TeamImpostorAlive == 0 && 
                statistics.SeparatedNeutralAlive.Count == 0)
            {
                gameIsEnd(ref __instance, GameOverReason.HumansByVote);
                return true;
            }
            return false;
        }

        private static bool isImpostorWin(
            ShipStatus __instance,
            GameDataContainer.PlayerStatistics statistics)
        {
            bool isGameEnd = false;
            GameOverReason endReason = GameOverReason.HumansDisconnect;

            if (statistics.IsAssassinationMarin)
            {
                isGameEnd = true;
                endReason = (GameOverReason)RoleGameOverReason.AssassinationMarin;
            }

            if (statistics.TeamImpostorAlive >= (statistics.TotalAlive - statistics.TeamImpostorAlive) &&
                statistics.SeparatedNeutralAlive.Count == 0)
            {
                isGameEnd = true;
                switch (TempData.LastDeathReason)
                {
                    case DeathReason.Exile:
                        endReason = GameOverReason.ImpostorByVote;
                        break;
                    case DeathReason.Kill:
                        endReason = GameOverReason.ImpostorByKill;
                        break;
                    default:
                        break;
                }
            }

            if (isGameEnd)
            {

                gameIsEnd(ref __instance, endReason);
                return true;
            }

            return false;

        }

        private static bool isNeutralAliveWin(
            ShipStatus __instance,
            GameDataContainer.PlayerStatistics statistics)
        {
            if (statistics.TeamImpostorAlive > 0) { return false; }
            if (statistics.SeparatedNeutralAlive.Count != 1) { return false; }

            var ((team, id), num) = statistics.SeparatedNeutralAlive.ElementAt(0);

            if (num >= (statistics.TotalAlive - num))
            {
                GameDataContainer.WinGameControlId = id;

                GameOverReason endReason = (GameOverReason)RoleGameOverReason.UnKnown;
                switch (team)
                {
                    case NeutralSeparateTeam.Alice:
                        endReason = (GameOverReason)RoleGameOverReason.AliceKillAllOther;
                        break;
                    case NeutralSeparateTeam.Jackal:
                        endReason = (GameOverReason)RoleGameOverReason.JackalKillAllOther;
                        break;
                    case NeutralSeparateTeam.Lover:
                        endReason = (GameOverReason)RoleGameOverReason.LoverKillAllOther;
                        break;
                    default:
                        break;
                }
                gameIsEnd(ref __instance, endReason);
                return true;
            }
            return false;
        }

        private static bool isNeutralSpecialWin(
            ShipStatus __instance)
        {

            if (OptionsHolder.Ship.DisableNeutralSpecialForceEnd) { return false; }

            foreach(var role in ExtremeRoleManager.GameRole.Values)
            {
                
                if (!role.IsNeutral()) { continue; }
                if (role.IsWin)
                {
                    GameDataContainer.WinGameControlId = role.GameControlId;

                    GameOverReason endReason = (GameOverReason)RoleGameOverReason.UnKnown;

                    switch (role.Id)
                    {
                        case ExtremeRoleId.Alice:
                            endReason = (GameOverReason)RoleGameOverReason.AliceKilledByImposter;
                            break;
                        default :
                            break;
                    }
                    gameIsEnd(ref __instance, endReason);
                    return true;

                }
            }

            return false;
        }

        private static bool isSabotageWin(
            ShipStatus __instance)
        {
            if (__instance.Systems == null) { return false; };
            ISystemType systemType = __instance.Systems.ContainsKey(
                SystemTypes.LifeSupp) ? __instance.Systems[SystemTypes.LifeSupp] : null;
            if (systemType != null)
            {
                LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>();
                if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
                {
                    gameIsEnd(ref __instance, GameOverReason.ImpostorBySabotage);
                    lifeSuppSystemType.Countdown = 10000f;
                    return true;
                }
            }
            ISystemType systemType2 = __instance.Systems.ContainsKey(
                SystemTypes.Reactor) ? __instance.Systems[SystemTypes.Reactor] : null;
            if (systemType2 == null)
            {
                systemType2 = __instance.Systems.ContainsKey(
                    SystemTypes.Laboratory) ? __instance.Systems[SystemTypes.Laboratory] : null;
            }
            if (systemType2 != null)
            {
                ICriticalSabotage criticalSystem = systemType2.TryCast<ICriticalSabotage>();
                if (criticalSystem != null && criticalSystem.Countdown < 0f)
                {
                    gameIsEnd(ref __instance, GameOverReason.ImpostorBySabotage);
                    criticalSystem.ClearSabotage();
                    return true;
                }
            }
            return false;
        }
        private static bool isTaskWin(ShipStatus __instance)
        {
            if (GameData.Instance.TotalTasks > 0 && 
                GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                gameIsEnd(ref __instance, GameOverReason.HumansByTask);
                return true;
            }
            return false;
        }
    }
}
