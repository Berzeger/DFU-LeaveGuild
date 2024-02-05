// Project:         Daggerfall Unity Leave Guild Mod
// Copyright:       Copyright (C) 2024 Filip 'Berzeger' Vondrasek
// Web Site:        https://www.nexusmods.com/daggerfallunity/mods/588
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Berzeger/DFU-LeaveGuild
// Original Author: Berzeger

using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System;
using System.Collections.Generic;
using static Berzeger.LeaveGuildSaveData;
using static DaggerfallConnect.Arena2.FactionFile;
using DaggerfallWorkshop.Game.Serialization;
using System.Reflection;

namespace Berzeger
{
    public class LeaveGuild : MonoBehaviour, IHasModSaveData
    {
        private static Mod _mod;
        public static int ReputationLossForLeaving;
        public static Type QuestOfferLocationsType;

        public LeaveGuildSaveData SavedData { get; private set; }

        Type IHasModSaveData.SaveDataType => typeof(LeaveGuildSaveData);

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            _mod = initParams.Mod;
            var go = new GameObject(_mod.Title);
            go.AddComponent<LeaveGuild>();
        }

        void Awake()
        {
            _mod.SaveDataInterface = this;
            _mod.MessageReceiver = (string message, object data, DFModMessageCallback callback) =>
            {
                HandleMessage(message, data, callback);
            };

            var settings = _mod.GetSettings();
            var reputationLossAsString = settings.GetString("ReputationLossForLeaving", "ReputationLoss");
            int.TryParse(reputationLossAsString, out ReputationLossForLeaving);

            _mod.IsReady = true;
        }

        void Start()
        {
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.GuildServicePopup, typeof(LeaveGuildServicePopupWindow));
            ResolveQuestOfferLocationsModPresence();

        }

        private void ResolveQuestOfferLocationsModPresence()
        {
            Mod questOfferLocationsMod = ModManager.Instance.GetMod("Quest Offer Locations");

            if (questOfferLocationsMod == null)
            {
                return;
            }

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in loadedAssemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    // The Quest Offer Locations assembly is not precompiled - it will be hiding amongst one of the dynamic assemblies.
                    // We need to iterate through all the assemblies and find the popup window in it.
                    if (type.FullName.Contains("DaggerfallWorkshop.Game.UserInterfaceWindows.QuestOfferLocationGuildServicePopUpWindow"))
                    {
                        QuestOfferLocationsType = type;
                        return;
                    }
                }
            }
        }

        void HandleMessage(string message, object data, DFModMessageCallback callback)
        {
            switch (message)
            {
                case "SaveData":
                    var saveData = (SavedGuildData)data;
                    SaveGuildData(saveData.GuildGroup, saveData.BuildingFactionId, saveData.MembershipData);
                    break;

                case "LoadData":
                    var typedPayload = (SavedGuildData)data;
                    saveData = GetSavedDataForGuild(typedPayload.GuildGroup, typedPayload.BuildingFactionId);
                    callback(null, saveData);
                    break;
            }
        }

        object IHasModSaveData.GetSaveData()
        {
            return SavedData;
        }

        object IHasModSaveData.NewSaveData()
        {
            SavedData = new LeaveGuildSaveData
            {
                GuildData = new List<SavedGuildData>()
            };

            return SavedData;
        }

        void IHasModSaveData.RestoreSaveData(object saveData)
        {
            SavedData = (LeaveGuildSaveData)saveData;
        }

        public SavedGuildData GetSavedDataForGuild(GuildGroups guildGroup, int buildingFactionId)
        {
            foreach (var data in SavedData.GuildData)
            {
                if (data.GuildGroup == guildGroup && data.BuildingFactionId == buildingFactionId)
                {
                    return data;
                }
            }

            return null;
        }

        public void SaveGuildData(GuildGroups guildGroup, int buildingFactionId, GuildMembership_v1 membershipData)
        {
            var savedData = GetSavedDataForGuild(guildGroup, buildingFactionId);
            if (savedData != null)
            {
                savedData.MembershipData = membershipData;
            }
            else
            {
                SavedData.GuildData.Add(new SavedGuildData
                {
                    GuildGroup = guildGroup,
                    BuildingFactionId = buildingFactionId,
                    MembershipData = membershipData
                });
            }
        }
    }
}