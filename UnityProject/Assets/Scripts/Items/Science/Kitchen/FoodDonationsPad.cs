using System.Collections.Generic;
using Logs;
using Managers;
using Systems.Cargo;
using UnityEngine;

namespace Items.Science.Kitchen
{
	/// <summary>
	/// A component that spawns food objects periodically from a list of prefabs.
	/// </summary>
	public class FoodDonationsPad : MonoBehaviour
	{
		[SerializeField] private List<GameObject> foodDonations = new List<GameObject>();
		[SerializeField] private float timeBetweenDonationsInSeconds = 550f;
		[SerializeField] private float balanceCheckCargoCredits = 9000f;

		private const float DONATION_CHANCE_CHECK = 65f;

		private void Awake()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Add(DonationCheck, timeBetweenDonationsInSeconds);
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DonationCheck);
		}

		private void DonationCheck()
		{
			if (CargoManager.Instance.Credits > balanceCheckCargoCredits && CargoManager.Instance.CargoOffline == false
			    || GameManager.Instance.CentComm.CurrentAlertLevel == CentComm.AlertLevel.Green)
			{
				if(DMMath.Prob(DONATION_CHANCE_CHECK)) return;
			}

			GameObject randomDonationFromAcrossTheUniverse = foodDonations.PickRandom();

			if (randomDonationFromAcrossTheUniverse == null)
			{
				Loggy.LogError("[FoodDonationsPad/DonationsCheck] - You forget to fill the possible prefabs that could spawn.");
				return;
			}

			var donation = Spawn.ServerPrefab(randomDonationFromAcrossTheUniverse, gameObject.AssumedWorldPosServer(), scatterRadius: 0.5f);
			Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} spits out a {donation.GameObject.ExpensiveName()}.", gameObject);
			Chat.AddLocalMsgToChat("Please accept this generous donation from the temple of frisking spirits.",
				gameObject, null, "Food Donations Pad", true);
		}
	}
}