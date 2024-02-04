// Project:         Daggerfall Unity Leave Guild Mod
// Copyright:       Copyright (C) 2024 Filip 'Berzeger' Vondrasek
// Web Site:        https://www.nexusmods.com/daggerfallunity/mods/588
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Berzeger/DFU-LeaveGuild
// Original Author: Berzeger

using UnityEngine;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using static Berzeger.LeaveGuildSaveData;

namespace Berzeger
{
    public class LeaveGuildServicePopupWindow : DaggerfallGuildServicePopupWindow
    {
        protected Button leaveButton = new Button();
        protected Rect leaveButtonRect = new Rect(5, 33, 35, 15);

        public LeaveGuildServicePopupWindow(IUserInterfaceManager uiManager, StaticNPC npc, FactionFile.GuildGroups guildGroup, int buildingFactionId)
            : base(uiManager, npc, guildGroup, buildingFactionId)
        {
        }

        protected override void Setup()
        {
            // Leave Guild button
            // The game generally considers the player a member of a guild if they're a member of any guild belonging to the faction.
            // We need to query specifically the one single faction.
            bool isSpecificMember = guild.IsMember();
            if (isSpecificMember)
            {
                leaveButton = DaggerfallUI.AddButton(leaveButtonRect, mainPanel);
                leaveButton.OnMouseClick += LeaveButton_OnMouseClick;
                leaveButton.Label.Text = "LEAVE GUILD";
                leaveButton.Label.TextColor = DaggerfallUI.DaggerfallDefaultTextColor;

                // we can also reuse the join hotkey - these two buttons are never shown together
                leaveButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GuildsJoin);
            }

            base.Setup();
        }

        protected virtual void LeaveButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            CloseWindow();
            if (guild == null)
            {
                DaggerfallUI.MessageBox("Leaving guild " + guildGroup + " not implemented.");
            }
            else if (guild.IsMember())
            {
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, uiManager.TopWindow);
                TextFile.Token[] leaveTokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    string.Format("Are you sure you want to leave {0}?", guild.GetGuildName()));

                messageBox.SetTextTokens(leaveTokens);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                messageBox.OnButtonClick += ConfirmLeaveGuild_OnButtonClick;

                messageBox.Show();
            }
        }

        protected virtual void ConfirmLeaveGuild_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                // lose reputation for leaving (default value: 0)
                playerEntity.FactionData.ChangeReputation(guild.GetFactionId(), -LeaveGuild.ReputationLossForLeaving, true);

                // save current membership data (rank etc.)
                var membershipData = guild.GetGuildData();
                ModManager.Instance.SendModMessage("Leave Guild", "SaveData", new SavedGuildData
                {
                    BuildingFactionId = buildingFactionId,
                    GuildGroup = guildGroup,
                    MembershipData = membershipData
                });

                guildManager.RemoveMembership(guild);
                int newReputation = guild.GetReputation(playerEntity);

                // Generate the farewell text
                TextFile.Token[] leaveTokens;
                if (newReputation >= 0)
                {
                    string leaveTextPositive = string.Format("We hope to have you back should you change your mind, {0}!", playerEntity.Name);

                    leaveTokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        leaveTextPositive);
                }
                else
                {
                    string leaveTextNegative1 = "Your current reputation does not align with our goals, and therefore, you are no longer welcome here.";
                    string leaveTextNegative2 = string.Format("The situation may change over time; we'll see. Farewell for now, {0}.", playerEntity.Name);

                    leaveTokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        leaveTextNegative1,
                        leaveTextNegative2);
                }

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, uiManager.TopWindow);
                messageBox.SetTextTokens(leaveTokens, guild);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
            }
        }

        protected override void ConfirmJoinGuild_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                SavedGuildData savedGuildData = null;

                var modMessagePayload = new SavedGuildData
                {
                    BuildingFactionId = buildingFactionId,
                    GuildGroup = guildGroup
                };
                ModManager.Instance.SendModMessage("Leave Guild", "LoadData", modMessagePayload, (string message, object data) =>
                {
                    savedGuildData = (SavedGuildData)data;
                });

                guildManager.AddMembership(guildGroup, guild);

                // restore rank and other data
                if (savedGuildData != null)
                {
                    if (savedGuildData.MembershipData.rank != guild.Rank)
                    {
                        guild.RestoreGuildData(savedGuildData.MembershipData);
                    }
                }

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, uiManager.TopWindow);
                messageBox.SetTextTokens(guild.TokensWelcome(), guild);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
            }
        }
    }
}
