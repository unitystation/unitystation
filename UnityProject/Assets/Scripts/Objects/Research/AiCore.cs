using Systems.Ai;
using Mirror;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// This script controls the AI core object, for core AI job logic see AiPlayer.cs
	/// </summary>
	public class AiCore : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private AiPlayer linkedPlayer;
		public AiPlayer LinkedPlayer => linkedPlayer;

		[Server]
		public void SetLinkedPlayer(AiPlayer aiPlayer)
		{
			linkedPlayer = aiPlayer;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.HandApply(interaction, side) == false) return false;

			//if (Validations.HasItemTrait(interaction, CommonTraits.Instance)) return false;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			throw new System.NotImplementedException();
		}
	}
}
