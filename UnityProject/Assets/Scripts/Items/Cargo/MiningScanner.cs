using System.Collections;
using Core.Lighting;
using Mirror;
using UnityEngine;

namespace Items.Cargo
{
	public class MiningScanner : NetworkBehaviour, IInteractable<HandActivate>
	{

		public bool IsAdvanced = false;
		public float NormalScanCooldown = 10f;
		public float AdvancedScanCooldown = 4f;

		private bool isOnCooldown = false;


		[TargetRpc]
		public void TargetStartScanning(NetworkConnection target)
		{
			Debug.Log("scanning");
			HighlightScanManager.Instance.Highlight();
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (isOnCooldown)
			{
				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} is still recharging!");
				return;
			}

			if (CustomNetworkManager.IsHeadless)
			{
				TargetStartScanning(interaction.PerformerPlayerScript.connectionToClient);
			}
			else
			{
				HighlightScanManager.Instance.Highlight();
			}
			StartCoroutine(Cooldown());
			Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} makes a buzzing sound as it scans the area around you for things that can be highlighted.");
		}

		private IEnumerator Cooldown()
		{
			isOnCooldown = true;
#if UNITY_EDITOR
			yield return WaitFor.Seconds(0.5f);
#else
			yield return WaitFor.Seconds(IsAdvanced ? AdvancedScanCooldown : NormalScanCooldown);
#endif
			isOnCooldown = false;
		}
	}
}