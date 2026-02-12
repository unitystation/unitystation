using System.Collections;
using Shared.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Chat;
using US13.Core.Cooldowns;
using US13.Items.Implants.Organs;
using US13.Messages.Client;
using US13.Player;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
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
			PostToChatMessage.Send("Current active laws: ", US13.Core.Chat.ChatChannel.Local | US13.Core.Chat.ChatChannel.Common , Voice: "", Loudness.NORMAL);

			yield return WaitFor.Seconds(1.5f);

			foreach (Transform child in aiLawsTabContents)
			{
				if(child.gameObject.activeSelf == false) continue;

				if(child.TryGetComponent<TMP_Text>(out var text) == false) continue;

				var toggle = child.GetComponentInChildren<Toggle>();
				if(toggle == null || toggle.isOn == false) continue;

				PostToChatMessage.Send(text.text, US13.Core.Chat.ChatChannel.Local | US13.Core.Chat.ChatChannel.Common,  Voice: "", Loudness.NORMAL);

				yield return WaitFor.Seconds(1.5f);
			}
		}
	}
}
