﻿using System.Collections.Generic;

using ExtremeRoles.Module;
using ExtremeRoles.Module.RoleAbilityButton;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

namespace ExtremeRoles.Roles.Solo.Neutral
{
    public class Jester : SingleRoleBase, IRoleAbility
    {

        public enum JesterOption
        {
            OutburstDistance,
        }

        public RoleAbilityButtonBase Button
        { 
            get => this.outburstButton;
            set
            {
                this.outburstButton = value;
            }
        }

        private float outburstDistance;
        private PlayerControl tmpTarget;
        private PlayerControl outburstTarget;
        private RoleAbilityButtonBase outburstButton;

        public Jester(): base(
            ExtremeRoleId.Jester,
            ExtremeRoleType.Neutral,
            ExtremeRoleId.Jester.ToString(),
            ColorPalette.JesterPink,
            false, false, false, true)
        { }

        public static void OutburstKill(
            byte outburstTargetPlayerId, byte killTargetPlayerId)
        {
            if (outburstTargetPlayerId != PlayerControl.LocalPlayer.PlayerId) { return; }

            PlayerControl killer = Helper.Player.GetPlayerControlById(outburstTargetPlayerId);
            PlayerControl target = Helper.Player.GetPlayerControlById(killTargetPlayerId);

            byte killerId = killer.PlayerId;
            byte targetId = target.PlayerId;

            var killerRole = ExtremeRoleManager.GameRole[killerId];
            var targetRole = ExtremeRoleManager.GameRole[targetId];

            if (!killer.CanMove) { return; }

            bool canKill = killerRole.TryRolePlayerKillTo(
                killer, target);
            if (!canKill) { return; }

            canKill = targetRole.TryRolePlayerKilledFrom(
                target, killer);
            if (!canKill) { return; }

            var bodyGuard = ExtremeRolesPlugin.GameDataStore.ShildPlayer.GetBodyGuardPlayerId(
                targetId);

            PlayerControl prevTarget = target;

            if (bodyGuard != byte.MaxValue)
            {
                target = Helper.Player.GetPlayerControlById(bodyGuard);
                if (target == null)
                {
                    target = prevTarget;
                }
                else if (target.Data.IsDead || target.Data.Disconnected)
                {
                    target = prevTarget;
                }
            }

            if (AmongUsClient.Instance.IsGameOver) { return; }
            if (killer == null ||
                killer.Data == null ||
                killer.Data.IsDead ||
                killer.Data.Disconnected) { return; }
            if (target == null ||
                target.Data == null ||
                target.Data.IsDead ||
                target.Data.Disconnected) { return; }

            byte animate = byte.MaxValue;

            if (target.PlayerId != prevTarget.PlayerId)
            {
                animate = 0;
            }

            RPCOperator.Call(
                PlayerControl.LocalPlayer.NetId,
                RPCOperator.Command.UncheckedMurderPlayer,
                new List<byte> { killerId, target.PlayerId, animate });
            RPCOperator.UncheckedMurderPlayer(
                killerId,
                target.PlayerId,
                animate);
        }

        public void CreateAbility()
        {
            this.CreateAbilityCountButton(
                Helper.Translation.GetString("outburst"),
                Loader.CreateSpriteFromResources(
                    Path.JesterOutburst),
                abilityCleanUp:CleanUp);
        }

        public override bool IsSameTeam(SingleRoleBase targetRole)
        {
            var multiAssignRole = targetRole as MultiAssignRoleBase;

            if (multiAssignRole != null)
            {
                if (multiAssignRole.AnotherRole != null)
                {
                    return this.IsSameTeam(multiAssignRole.AnotherRole);
                }
            }
            if (OptionHolder.Ship.IsSameNeutralSameWin)
            {
                return this.Id == targetRole.Id;
            }
            else
            {
                return (this.Id == targetRole.Id) && this.IsSameControlId(targetRole);
            }
        }

        public bool IsAbilityUse()
        {
            this.tmpTarget = Helper.Player.GetPlayerTarget(
                PlayerControl.LocalPlayer, this,
                this.outburstDistance);
            return this.IsCommonUse() && this.tmpTarget != null;
        }

        public override void ExiledAction(GameData.PlayerInfo rolePlayer)
        {
            this.IsWin = true;
        }

        public bool UseAbility()
        {
            this.outburstTarget = this.tmpTarget;
            return true;
        }
        public void CleanUp()
        {
            if (this.outburstTarget == null) { return; }
            if (this.outburstTarget.Data.IsDead || this.outburstTarget.Data.Disconnected) { return; }
            if (ExtremeRoleManager.GameRole.Count == 0) { return; }

            var role = ExtremeRoleManager.GameRole[this.outburstTarget.PlayerId];
            if (!role.CanKill) { return; }

            PlayerControl killTarget = this.outburstTarget.FindClosestTarget(
                !role.IsImpostor());

            if (killTarget == null) { return; }
            if (killTarget.Data.IsDead || killTarget.Data.Disconnected) { return; }
            if (killTarget.PlayerId == PlayerControl.LocalPlayer.PlayerId) { return; }
            
            RPCOperator.Call(
                PlayerControl.LocalPlayer.NetId,
                RPCOperator.Command.JesterOutburstKill,
                new List<byte> { this.outburstTarget.PlayerId, killTarget.PlayerId });
            OutburstKill(this.outburstTarget.PlayerId, killTarget.PlayerId);
        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            CustomOption.Create(
                GetRoleOptionId((int)JesterOption.OutburstDistance),
                string.Concat(
                    this.RoleName,
                    JesterOption.OutburstDistance.ToString()),
                1.0f, 0.0f, 2.0f, 0.1f,
                parentOps);

            this.CreateAbilityCountOption(
                parentOps, 5, 100, 2.0f);
        }

        protected override void RoleSpecificInit()
        {
            this.outburstDistance = OptionHolder.AllOption[
                GetRoleOptionId((int)JesterOption.OutburstDistance)].GetValue();
            this.RoleAbilityInit();
        }

        public void RoleAbilityResetOnMeetingStart()
        {
            return;
        }

        public void RoleAbilityResetOnMeetingEnd()
        {
            return;
        }
    }
}
