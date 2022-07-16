using System.Collections;
using UnityEngine;
using UI.Core.NetUI;

namespace SyndicateOps
{
	public class GUI_SyndicateOpConsole : NetTab
	{
		private SyndicateOpConsole console;

		[SerializeField]
		private NetLabel timerLabel = null;

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(UpdateTimer, 1f);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateTimer);
			}
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			console = Provider.GetComponentInChildren<SyndicateOpConsole>();
		}

		private void UpdateTimer()
		{
			if (console.Timer == 0)
			{
				if (CustomNetworkManager.IsServer)
				{
					UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateTimer);
				}
			}
			string min = Mathf.FloorToInt((console.Timer) / 60).ToString();
			string sec = ((console.Timer) % 60).ToString();
			sec = sec.Length >= 2 ? sec : "0" + sec;
			timerLabel.SetValueServer($"{min}:{sec}");
		}

		public void ServerDeclareWar(string DeclerationMessage)
		{
			console.AnnounceWar(DeclerationMessage);
		}
	}
}
