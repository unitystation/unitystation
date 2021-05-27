using System.Collections;
using System.Collections.Generic;
using Systems.Ai;
using Messages.Client;
using Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

	public void SetUp(AiPlayer player)
	{
		aiPlayer = player;
		controller = aiPlayer.GetComponent<AiMouseInputController>();
	}

	public void JumpToCore()
	{
		if (aiPlayer == null) return;

		aiPlayer.CmdTeleportToCore();
	}

	public void ToggleLights()
	{
		aiPlayer.CmdToggleCameraLights(!aiPlayer.CoreCamera.LightOn);
	}

	public void ToggleFloorBolts()
	{
		aiPlayer.CmdToggleFloorBolts();
	}



	#region Laws

	public void OpenLaws()
	{
		aiLawsTab.SetActive(true);
		aiLawsTabDummyLaw.SetActive(false);

		//Clear old laws
		foreach (Transform child in aiLawsTabContents)
		{
			//Dont destroy dummy
			if(child.gameObject.activeSelf == false) continue;

			GameObject.Destroy(child.gameObject);
		}

		foreach (var law in aiPlayer.AiLaws)
		{
			var newChild = Instantiate(aiLawsTabDummyLaw, aiLawsTabContents);
			newChild.GetComponent<TMP_Text>().text = law;
			newChild.SetActive(true);
		}
	}

	public void StateLaws()
	{
		StartCoroutine(StateLawsRoutine());
	}

	private IEnumerator StateLawsRoutine()
	{
		foreach (Transform child in aiLawsTabContents)
		{
			if(child.gameObject.activeSelf == false) continue;

			if(child.TryGetComponent<TMP_Text>(out var text) == false) continue;

			PostToChatMessage.Send(text.text, ChatChannel.Local | ChatChannel.Common);

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
		aiPlayer.CmdCallShuttle(callReasonInputField.text);
		callShuttleTab.SetActive(false);
	}

	#endregion
}
