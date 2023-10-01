using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Systems.Ai;
using Systems.MobAIs;
using Systems.Teleport;
using Messages.Client;
using Objects;
using TMPro;
using UI.Core.GUI.Components;
using UI.Core.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.MainHUD.UI_Bottom
{
	public class UI_Ai : MonoBehaviour
	{
		[HideInInspector]
		public AiPlayer aiPlayer = null;

		[HideInInspector]
		public AiMouseInputController controller = null;

		//Laws Tab Stuff
		[SerializeField]
		private GameObject aiLawsTab = null;

		[SerializeField]
		private Transform aiLawsTabContents = null;

		[SerializeField]
		private GameObject aiLawsTabDummyLaw = null;

		[SerializeField]
		private TMP_Text amountOfLawsText = null;

		//Slider Stuff
		[SerializeField]
		private Slider powerSlider = null;

		[SerializeField]
		private Slider integritySlider = null;

		//Call Shuttle Stuff
		[SerializeField]
		private GameObject callShuttleTab = null;

		[SerializeField]
		private TMP_InputField callReasonInputField = null;

		//Camera Teleport Screen
		[SerializeField]
		private TeleportWindow teleportWindow = null;

		//Camera Save
		[SerializeField]
		private GameObject cameraSaveDummy = null;

		[SerializeField]
		private Transform cameraSaveContents = null;

		//First is the UI button, second is the associated security camera object
		private Dictionary<GameObject, GameObject> savedCameras = new Dictionary<GameObject, GameObject>();

		//Number of cameras
		[SerializeField]
		private TMP_Text numberOfCameras = null;

		private bool focusCheck;

		private CooldownInstance stateCooldown = new CooldownInstance (5f);

		private void OnEnable()
		{
			teleportWindow.onTeleportRequested += OnTeleportButtonPress;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			teleportWindow.onTeleportRequested += OnTeleportButtonPress;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);

		}

		#region focus Check
		void UpdateMe()
		{
			if (callReasonInputField.isFocused && focusCheck == false)
			{
				InputFocus();
			}
			else if (callReasonInputField.isFocused == false && focusCheck)
			{
				InputUnfocus();
			}
		}

		private void InputFocus()
		{
			focusCheck = true;
			//disable keyboard commands while input is focused
			UIManager.IsInputFocus = true;
		}
		private void InputUnfocus()
		{
			focusCheck = false;
			//disable keyboard commands while input is focused
			UIManager.IsInputFocus = false;
		}

		#endregion

		public void SetUp(AiPlayer player)
		{
			aiPlayer = player;
			controller = aiPlayer.GetComponent<AiMouseInputController>();
		}

		public void JumpToCore()
		{
			if (aiPlayer == null) return;

			if(aiPlayer.OnCoolDown(NetworkSide.Client)) return;
			aiPlayer.StartCoolDown(NetworkSide.Client);

			aiPlayer.CmdTeleportToCore();
		}

		public void ToggleLights()
		{
			if(aiPlayer.OnCoolDown(NetworkSide.Client)) return;
			aiPlayer.StartCoolDown(NetworkSide.Client);

			if (aiPlayer.CoreCamera == null)
			{
				Chat.AddExamineMsgToClient("Unable to change light state when not in core");
				return;
			}

			aiPlayer.CmdToggleCameraLights(!aiPlayer.CoreCamera.LightOn);
		}

		public void ToggleFloorBolts()
		{
			if(aiPlayer.OnCoolDown(NetworkSide.Client)) return;
			aiPlayer.StartCoolDown(NetworkSide.Client);

			aiPlayer.CmdToggleFloorBolts();
		}

		public void OpenCameraTeleportScreen()
		{
			teleportWindow.SetWindowTitle("Move Camera");
			teleportWindow.gameObject.SetActive(true);
			teleportWindow.GenerateButtons(TeleportUtils.GetCameraDestinations());
		}

		public void OpenTrackPlayerScreen()
		{
			teleportWindow.SetWindowTitle("Track Lifeforms");
			teleportWindow.gameObject.SetActive(true);
			teleportWindow.GenerateButtons(TeleportUtils.GetCameraTrackPlayerDestinations());
		}

		private void OnTeleportButtonPress(TeleportInfo info)
		{
			if(aiPlayer.OnCoolDown(NetworkSide.Client)) return;
			aiPlayer.StartCoolDown(NetworkSide.Client);

			if (info.gameObject.GetComponent<PlayerScript>() != null || info.gameObject.GetComponent<MobAI>() != null)
			{
				aiPlayer.CmdTrackObject(info.gameObject);
				return;
			}

			aiPlayer.CmdTeleportToCamera(info.gameObject, true);
		}

		public void SetNumberOfCameras(uint newNumber)
		{
			numberOfCameras.text = newNumber.ToString();
		}

		#region Laws

		public void OpenLaws()
		{
			if (aiPlayer == null)
			{
				aiPlayer = PlayerManager.LocalPlayerObject.OrNull()?.GetComponent<AiPlayer>();
			}

			if (aiPlayer == null)
			{
				Loggy.LogError("Failed to find AiPlayer for player");
				return;
			}

			aiLawsTab.SetActive(true);
			aiLawsTabDummyLaw.SetActive(false);

			//Clear old laws
			foreach (Transform child in aiLawsTabContents)
			{
				//Dont destroy dummy
				if(child.gameObject.activeSelf == false) continue;

				GameObject.Destroy(child.gameObject);
			}

			// 0 laws first, freeform last
			var laws = aiPlayer?.GetLaws();


			if (laws == null)
			{
				laws = PlayerManager.LocalMindScript.PossessingObject.GetComponent<BrainLaws>().GetLaws();
			}




			amountOfLawsText.text = $"You have <color=orange>{laws.Count}</color> law{(laws.Count == 1 ? "" : "s")}\nYou Must Follow Them";

			foreach (var law in laws)
			{
				var newChild = Instantiate(aiLawsTabDummyLaw, aiLawsTabContents);
				newChild.GetComponent<TMP_Text>().text = law;
				newChild.SetActive(true);
			}
		}

		public void StateLaws()
		{
			if(Cooldowns.TryStartClient(aiPlayer.PlayerScript, stateCooldown) == false) return;

			StartCoroutine(StateLawsRoutine());
		}

		private IEnumerator StateLawsRoutine()
		{
			PostToChatMessage.Send("Current active laws: ", ChatChannel.Local | ChatChannel.Common, Loudness.NORMAL);

			yield return WaitFor.Seconds(1.5f);

			foreach (Transform child in aiLawsTabContents)
			{
				if(child.gameObject.activeSelf == false) continue;

				if(child.TryGetComponent<TMP_Text>(out var text) == false) continue;

				var toggle = child.GetComponentInChildren<Toggle>();
				if(toggle == null || toggle.isOn == false) continue;

				PostToChatMessage.Send(text.text, ChatChannel.Local | ChatChannel.Common, Loudness.NORMAL);

				yield return WaitFor.Seconds(1.5f);
			}
		}

		#endregion

		#region Sidebar

		public void SetPowerLevel(float newLevel)
		{
			Mathf.Clamp(newLevel, 0, 100);

			//0 check
			if (newLevel.Approx(0))
			{
				powerSlider.value = 0;
				return;
			}

			// 0 to 1
			powerSlider.value = newLevel / 100;
		}

		public void SetIntegrityLevel(float newLevel)
		{
			Mathf.Clamp(newLevel, 0, 100);

			//0 check
			if (newLevel.Approx(0))
			{
				integritySlider.value = 0;
				return;
			}

			// 0 to 1
			integritySlider.value = newLevel / 100;
		}

		#endregion

		#region Shuttle

		public void OpenCallShuttleTab()
		{
			callShuttleTab.SetActive(true);
		}

		public void CallShuttleButton()
		{
			if(aiPlayer.OnCoolDown(NetworkSide.Client)) return;
			aiPlayer.StartCoolDown(NetworkSide.Client);

			aiPlayer.CmdCallShuttle(callReasonInputField.text);
			callShuttleTab.SetActive(false);
		}

		#endregion

		#region Camera Saving

		public void OnCameraSave()
		{
			var newCamera = aiPlayer.CameraLocation.OrNull()?.gameObject;
			var newCameraText = newCamera.OrNull()?.GetComponent<SecurityCamera>().OrNull()?.CameraName;

			if (newCameraText == null)
			{
				return;
			}

			if (savedCameras.Any(x => x.Value == newCamera))
			{
				Chat.AddExamineMsg(aiPlayer.gameObject, "Camera has already been saved");
				return;
			}

			//Add new camera button
			var newChild = Instantiate(cameraSaveDummy, cameraSaveContents);
			newChild.GetComponentInChildren<TMP_Text>().text = newCameraText;
			newChild.name = newCameraText;

			//Set up click interactions
			//Left click to go to, right click delete
			var click = newChild.GetComponent<GUI_ClickController>();
			click.onLeft.AddListener(OnCameraClick);
			click.onRight.AddListener(OnCameraRemove);

			newChild.SetActive(true);

			savedCameras.Add(newChild, aiPlayer.CameraLocation.gameObject);
		}

		private void OnCameraRemove(GameObject cameraToRemove)
		{
			savedCameras.Remove(cameraToRemove);
			Destroy(cameraToRemove);
		}

		private void OnCameraClick(GameObject cameraClicked)
		{
			if(aiPlayer.OnCoolDown(NetworkSide.Client)) return;
			aiPlayer.StartCoolDown(NetworkSide.Client);

			if (savedCameras.TryGetValue(cameraClicked, out var secCame) == false) return;

			aiPlayer.CmdTeleportToCamera(secCame, true);
		}

		#endregion
	}
}
