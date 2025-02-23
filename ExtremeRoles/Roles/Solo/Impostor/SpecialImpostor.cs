﻿using ExtremeRoles.Module;
using ExtremeRoles.Roles.API;

namespace ExtremeRoles.Roles.Solo.Impostor
{
    public class SpecialImpostor : SingleRoleBase
    {
        public SpecialImpostor(): base(
            ExtremeRoleId.SpecialImpostor,
            ExtremeRoleType.Impostor,
            ExtremeRoleId.SpecialImpostor.ToString(),
            Palette.ImpostorRed,
            true, false, true, true)
        {}

        protected override void CreateSpecificOption(CustomOptionBase parentOps)
        {
            return;
        }
        
        protected override void RoleSpecificInit()
        {
            return;
        }

    }
}
