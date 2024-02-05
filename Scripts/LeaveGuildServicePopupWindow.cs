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
using System;
using System.Reflection;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.Guilds;

namespace Berzeger
{
    public class LeaveGuildServicePopupWindow : DaggerfallGuildServicePopupWindow
    {
        protected Button leaveButton = new Button();
        protected Rect leaveButtonRect = new Rect(5f, 33f, 120f, 7f);
        protected new Rect talkButtonRect = new Rect(5f, 15f, 120f, 7f);
        protected new Rect exitButtonRect = new Rect(44f, 42f, 43f, 15f);
        private new const string baseTextureName = "BLANKMENU_4";

        public LeaveGuildServicePopupWindow(IUserInterfaceManager uiManager, StaticNPC npc, FactionFile.GuildGroups guildGroup, int buildingFactionId)
            : base(uiManager, npc, guildGroup, buildingFactionId)
        {
        }

        protected override void Setup()
        {
            ResolveQuestOfferLocationsModCompatibility();

            TextureReplacement.TryImportTexture(baseTextureName, true, out baseTexture);

            // Create interface panel
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            mainPanel.BackgroundTexture = baseTexture;
            mainPanel.Position = new Vector2(0, 50);
            mainPanel.Size = new Vector2(130, 60);

            // Is a member of the guild group (i.e. a temple, a knight order etc.)
            bool isGeneralMember = guildManager.GetGuild(guildGroup).IsMember();

            // The game generally considers the player a member of a guild if they're a member of any guild belonging to the faction.
            // We need to query specifically the one single faction.
            bool isSpecificMember = guild.IsMember();

            // Join Guild button
            joinButton = DaggerfallUI.AddButton(joinButtonRect, mainPanel);
            joinButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GuildsJoin);
            joinButton.Label.Text = "JOIN GUILD";
            joinButton.Label.TextColor = !isGeneralMember ? DaggerfallUI.DaggerfallDefaultTextColor : DaggerfallUI.DaggerfallDisabledTextColor;
            joinButton.Label.HorizontalAlignment = HorizontalAlignment.Center;
            joinButton.Label.Position = new Vector2(0, 1);
            joinButton.Label.ShadowPosition = Vector2.zero;
            if (!isGeneralMember)
            {
                joinButton.OnMouseClick += JoinButton_OnMouseClick;
            }

            // Leave Guild button
            leaveButton = DaggerfallUI.AddButton(leaveButtonRect, mainPanel);
            leaveButton.Label.Text = "LEAVE GUILD";
            leaveButton.Label.TextColor = isSpecificMember? DaggerfallUI.DaggerfallDefaultTextColor : DaggerfallUI.DaggerfallDisabledTextColor;
            leaveButton.Label.HorizontalAlignment = HorizontalAlignment.Center;
            leaveButton.Label.Position = new Vector2(0, 1);
            leaveButton.Label.ShadowPosition = Vector2.zero;
            leaveButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GuildsJoin);
            // Assign the handler only if the button is enabled
            if (isSpecificMember)
            {
                leaveButton.OnMouseClick += LeaveButton_OnMouseClick;
            }

            // Talk button
            talkButton = DaggerfallUI.AddButton(talkButtonRect, mainPanel);
            talkButton.OnMouseClick += TalkButton_OnMouseClick;
            talkButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GuildsTalk);
            talkButton.OnKeyboardEvent += TalkButton_OnKeyboardEvent;
            talkButton.Label.TextColor = DaggerfallUI.DaggerfallDefaultTextColor;
            talkButton.Label.Text = "TALK";
            talkButton.Label.HorizontalAlignment = HorizontalAlignment.Center;
            talkButton.Label.Position = new Vector2(0, 1);
            talkButton.Label.ShadowPosition = Vector2.zero;

            // Service button
            serviceLabel.Position = new Vector2(0, 1);
            serviceLabel.ShadowPosition = Vector2.zero;
            serviceLabel.HorizontalAlignment = HorizontalAlignment.Center;
            serviceLabel.Text = Services.GetServiceLabelText(service).ToUpper();
            serviceButton = DaggerfallUI.AddButton(serviceButtonRect, mainPanel);
            serviceButton.Components.Add(serviceLabel);
            serviceButton.OnMouseClick += ServiceButton_OnMouseClick;
            serviceButton.Hotkey = DaggerfallShortcut.GetBinding(Services.GetServiceShortcutButton(service));
            serviceButton.OnKeyboardEvent += ServiceButton_OnKeyboardEvent;

            // Exit button
            exitButton = DaggerfallUI.AddButton(exitButtonRect, mainPanel);
            exitButton.OnMouseClick += ExitButton_OnMouseClick;
            exitButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.GuildsExit);
            exitButton.OnKeyboardEvent += ExitButton_OnKeyboardEvent;

            NativePanel.Components.Add(mainPanel);
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

        #region Quest Offer Locations mod compatibility
        private object _questOfferLocationsWindow;
        private MethodInfo __OfferQuest_method;
        private MethodInfo __QuestPicker_OnItemPicked_method;
        private MethodInfo __GetQuest_method;

        private void ResolveQuestOfferLocationsModCompatibility()
        {
            if (LeaveGuild.QuestOfferLocationsType != null)
            {
                _questOfferLocationsWindow = Activator.CreateInstance(LeaveGuild.QuestOfferLocationsType, new object[] { uiManager, serviceNPC, guildGroup, buildingFactionId });
                __OfferQuest_method = LeaveGuild.QuestOfferLocationsType.GetMethod("OfferQuest", BindingFlags.Instance | BindingFlags.NonPublic);
                __QuestPicker_OnItemPicked_method = LeaveGuild.QuestOfferLocationsType.GetMethod("QuestPicker_OnItemPicked", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(string) }, null);
                __GetQuest_method = LeaveGuild.QuestOfferLocationsType.GetMethod("GetQuest", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        protected override void OfferQuest()
        {
            if (__OfferQuest_method != null)
            {
                __OfferQuest_method.Invoke(_questOfferLocationsWindow, null);
            }
            else
            {
                base.OfferQuest();
            }
        }

        protected override void QuestPicker_OnItemPicked(int index, string name)
        {
            if (__QuestPicker_OnItemPicked_method != null)
            {
                __QuestPicker_OnItemPicked_method.Invoke(_questOfferLocationsWindow, new object[] { index, name });
            }
            else
            {
                base.QuestPicker_OnItemPicked(index, name);
            }
        }

        protected override void GetQuest()
        {
            if (__GetQuest_method != null)
            {
                __GetQuest_method.Invoke(_questOfferLocationsWindow, null);
            }
            else
            {
                base.GetQuest();
            }
        }
        #endregion
    }
}
