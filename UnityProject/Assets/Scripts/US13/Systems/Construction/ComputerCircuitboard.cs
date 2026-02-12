using System.Collections.Generic;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.Traits;
using Util;

namespace US13.Systems.Construction
{
	/// <summary>
	/// Allows an object to function as a circuitboard for a computer, being placed into a computer frame and
	/// causing a particular computer to be spawned on completion.
	/// </summary>
	public class ComputerCircuitboard : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("Computer which should be spawned when this circuitboard's frame is constructed.")]
		[SerializeField]
		private GameObject computerToSpawn = null;

		[SerializeField] private ItemTrait screwdriver;
		[SerializeField] private List<GameObject> customTypes = new List<GameObject>();

		private int currentIndex = 0;

		/// <summary>
		/// Computer which should be spawned when this circuitboard's frame is constructed
		/// </summary>
		public GameObject ComputerToSpawn => computerToSpawn;

		private void Awake()
		{
			if(customTypes.Count == 0 || computerToSpawn != null) return;
			computerToSpawn = customTypes[currentIndex];
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (customTypes.Count == 0 || screwdriver == null) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return interaction.HandObject.Item().HasTrait(screwdriver);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			currentIndex++;
			if (currentIndex >= customTypes.Count) currentIndex = 0;
			computerToSpawn = customTypes[currentIndex];
			Chat.AddExamineMsg(interaction.Performer, $"You tune the board to create a {customTypes[currentIndex].name}");
		}
	}
}
