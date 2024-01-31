using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
