// Project:         Daggerfall Unity Leave Guild Mod
// Copyright:       Copyright (C) 2024 Filip 'Berzeger' Vondrasek
// Web Site:        https://www.nexusmods.com/daggerfallunity/mods/588
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Berzeger/DFU-LeaveGuild
// Original Author: Berzeger

using DaggerfallWorkshop.Game.Serialization;
using System.Collections.Generic;
using static DaggerfallConnect.Arena2.FactionFile;

namespace Berzeger
{
    [FullSerializer.fsObject("v1")]
    public class LeaveGuildSaveData
    {
        public List<SavedGuildData> GuildData;

        [FullSerializer.fsObject("v1")]
        public class SavedGuildData
        {
            public GuildGroups GuildGroup;
            public int BuildingFactionId;
            public GuildMembership_v1 MembershipData;
        }
    }
}
