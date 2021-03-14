﻿using Mirror;
using ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Objects.Construction
{

	/// <summary>
	/// Component for construction of light fixtures
	/// </summary>
	public class LightFixtureConstruction : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		public enum State { initial, wiresAdded, ready }
				
		[SerializeField]
		private SpriteHandler spriteHandler;
		[SerializeField]
		private SpriteDataSO initialStateSprites;
		[SerializeField]
		private SpriteDataSO wiresAddedStateSprites;
		[SerializeField]
		private GameObject FixtureFrameItemPrefab;

		[SyncVar]
		private State constructionState = State.ready;

		private Directional directional;
		private LightSource lightSource;

		private void Awake()
		{
			directional = GetComponent<Directional>();
			lightSource = GetComponent<LightSource>();
		}

		public bool isFullyBuilt()
		{
			return constructionState == State.ready;
		}

		internal void ServerSetState(State state)
		{
			constructionState = state;

			switch (constructionState)
			{
				case State.initial:
					spriteHandler.SetSpriteSO(initialStateSprites, NewvariantIndex: SpritesDirectional.OrientationIndex(directional.CurrentDirection.AsEnum()));
					break;
				case State.wiresAdded:
					lightSource.ServerChangeLightState(LightMountState.None);
					spriteHandler.SetSpriteSO(wiresAddedStateSprites, NewvariantIndex: SpritesDirectional.OrientationIndex(directional.CurrentDirection.AsEnum()));
					break;
				case State.ready:
				default:
					lightSource.ServerChangeLightState(LightMountState.MissingBulb);
					break;
			}

		}

		#region IExaminable

		public string Examine(Vector3 worldPos = default)
		{
			switch (constructionState)
			{
				case State.initial:
					return "Add wires or use a wrench to remove from the wall";
				case State.wiresAdded:
					return "Use a screwdriver to finish construction or use wirecutters to cut the exposed wires";
				case State.ready:
				default:
					if(lightSource.HasBulb())
						return "Remove the bulb and use a screwdriver to expose the wires";
					else
						return "Use a screwdriver to expose the wires";
			}
		}

		#endregion

		#region ICheckedInteractable

		public void ServerPerformInteraction(HandApply interaction)
		{
			switch (constructionState)
			{
				case State.initial:
					InitialStateInteract(interaction);
					break;

				case State.wiresAdded:
					WiresAddedStateInteract(interaction);
					break;

				case State.ready:
				default:
					ReadyStateInteract(interaction);
					break;
			}
		}		

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			switch (constructionState)
			{
				case State.initial:
					return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) || Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench);

				case State.wiresAdded:
					return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter) || Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver);

				case State.ready:
				default:
					return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) && !lightSource.HasBulb();
			}
		}

		#endregion

		#region Interactions

		private void InitialStateInteract(HandApply interaction)
		{
			if(Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start adding cables to the frame...",
					$"{interaction.Performer.ExpensiveName()} starts adding cables to the frame...",
					"You add cables to the frame.",
					$"{interaction.Performer.ExpensiveName()} adds cables to the frame.",
					() =>
					{
						Inventory.ServerConsume(interaction.HandSlot, 1);
						ServerSetState(State.wiresAdded);
					});
			}
			else if(Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				Spawn.ServerPrefab(FixtureFrameItemPrefab, transform.position, interaction.Performer.transform.parent, spawnItems: false);
				Despawn.ServerSingle(gameObject);
			}
		}

		private void WiresAddedStateInteract(HandApply interaction)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter))
			{
				ServerSetState(State.initial);
			}
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				ServerSetState(State.ready);
			}
		}


		private void ReadyStateInteract(HandApply interaction)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) && !lightSource.HasBulb())
			{								
				ServerSetState(State.wiresAdded);
			}
		}

		#endregion
	}
}