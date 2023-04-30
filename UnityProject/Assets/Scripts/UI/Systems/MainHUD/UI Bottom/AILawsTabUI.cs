using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Shared.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AILawsTabUI : SingletonManager<AILawsTabUI>
{

	[SerializeField]
	private Transform aiLawsTabContents = null;

	[SerializeField]
	private TMP_Text amountOfLawsText = null;

	[SerializeField]
	private GameObject aiLawsTabDummyLaw = null;

	private CooldownInstance stateCooldown = new CooldownInstance (5f);

	public override void Awake()
	{
		base.Awake();
		this.gameObject.SetActive(false);
	}

	public void OpenLaws()
	{
		this.gameObject.SetActive(true);

		//Clear old laws
		foreach (Transform child in aiLawsTabContents)
		{
			//Dont destroy dummy
			if(child.gameObject.activeSelf == false) continue;

			GameObject.Destroy(child.gameObject);
		}


		var laws = PlayerManager.LocalMindScript.PossessingObject.GetComponent<BrainLaws>().GetLaws();




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
		if(Cooldowns.TryStartClient(PlayerManager.LocalMindScript.CurrentPlayScript, stateCooldown) == false) return;

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
}
