using System;
using AdminTools;
using ImGuiNET;
using Newtonsoft.Json;
using UImGui;
using UnityEngine;
using Logs;
using Messages.Client.Admin;
using Shared.Managers;
using Systems.Character;
using Util;

namespace UI.Admin.DIMGUI.Characters
{
	public class CharacterSheetEditor : SingletonManager<CharacterSheetEditor>
	{
		[SerializeField] private PlayerManagePage playerManagePage;
		private AdminPlayerEntry playerEntry => playerManagePage.PlayerEntry;
		private PlayerInfo currentSelectedPlayer;
		private bool windowOpened = false;
		private CharacterSheet characterSheet; // Save a local copy for reference

		private bool errorOccuredWhileSaving = false;
		private bool saving = false;
		private int savingWaitCount = 0;
		private const int maxSavingWaitCount = 650;

		private void OnDisable()
		{
			HideUI();
		}

		public override void OnDestroy()
		{
			HideUI();
			base.OnDestroy();
		}

		public void NetMessage_SuccessEvent()
		{
			saving = false;
			HideUI();
		}

		public void NetMessage_FailEvent()
		{
			saving = false;
			errorOccuredWhileSaving = true;
		}

		public void ShowUI()
		{
			if (windowOpened)
			{
				Loggy.Log("[CharacterSheetEditor] UI already open");
				return;
			}
			saving = false;
			windowOpened = true;
			UImGuiUtility.Layout += OnLayout;
			UImGuiUtility.OnInitialize += OnInitialize;
			UImGuiUtility.OnDeinitialize += OnDeinitialize;
		}

		public void HideUI()
		{
			if (windowOpened == false)
			{
				Loggy.Log("[CharacterSheetEditor] UI already closed");
				return;
			}
			windowOpened = false;
			UImGuiUtility.Layout -= OnLayout;
			UImGuiUtility.OnInitialize -= OnInitialize;
			UImGuiUtility.OnDeinitialize -= OnDeinitialize;
		}

		private void OnInitialize(UImGui.UImGui obj)
		{
			// runs after UImGui.OnEnable();
		}

		private void OnDeinitialize(UImGui.UImGui obj)
		{
			// runs after UImGui.OnDisable();
		}

		private void OnLayout(UImGui.UImGui obj)
		{
			ImGui.Begin("Character Sheet Editor");
			if (saving)
			{
				SavingPage();
				ImGui.End();
				return;
			}
			GrabPlayerObject();
			if (playerEntry == null || currentSelectedPlayer == null)
			{
				ShowErrorPage();
				ImGui.End();
				return;
			}

			EditPage();
			ImGui.End();
		}

		//THIS ONLY WORKS ON THE SERVER!!!!
		private void GrabPlayerObject()
		{
			if (playerEntry == null) return;
			currentSelectedPlayer ??= PlayerList.Instance.GetPlayerByID(playerEntry.PlayerData.uid);
			if (playerEntry.PlayerData.uid != currentSelectedPlayer.AccountId)
			{
				currentSelectedPlayer = PlayerList.Instance.GetPlayerByID(playerEntry.PlayerData.uid);
				characterSheet = currentSelectedPlayer.Mind.CurrentCharacterSettings;
			}
			characterSheet ??= currentSelectedPlayer.Mind.CurrentCharacterSettings;
		}

		private void EditPage()
		{
			if (errorOccuredWhileSaving)
			{
				ImGui.TextColored(new Vector4(1, 0, 0, 1), "Error: Something went wrong while saving..");
				ImGui.Text(gameObject.NetId().ToString());
			}
			GrabPlayerObject();
			if (characterSheet == null) return;
			ImGui.Text("Player Account ID: " + currentSelectedPlayer.AccountId);
			ImGui.Separator();
			IMGUIHelper.DrawObjectFields(characterSheet);
			// TODO: networking
			if (ImGui.Button("Save Changes"))
			{
				SaveChanges();
			}
			if (ImGui.Button("Generate Random Character Sheet"))
			{
				characterSheet = CharacterSheet.GenerateRandomCharacter(RaceSOSingleton.GetPlayerSpecies());
			}
		}

		private void SavingPage()
		{
			savingWaitCount++;
			ImGui.TextColored(new Vector4(0.949f, 0.89f, 0, 1), "Saving...");
			if (savingWaitCount >= maxSavingWaitCount)
			{
				NetMessage_FailEvent();
			}
		}

		private void SaveChanges()
		{
			errorOccuredWhileSaving = false;
			saving = true;
			string updatedDataJson = JsonConvert.SerializeObject(characterSheet);
			RequestUpdatePlayerCharacterSheet.SendSheetUpdate(currentSelectedPlayer.AccountId, updatedDataJson);
		}

		private void ShowErrorPage()
		{
			ImGui.Text("No player selected, or missing player data");
		}
	}
}
