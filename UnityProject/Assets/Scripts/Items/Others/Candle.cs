using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Clothing;

namespace Items.Others
{
	[RequireComponent(typeof(ItemLightControl))]
	public class Candle : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerDespawn
	{
		[SerializeField]
		private ItemLightControl lightControl;

		[SerializeField]
		private SpriteHandler spriteHandler = default;

		[SyncVar] public int LifeSpan = 120; //10 minutes
		[SyncVar] public int DecayStage = 0;
		[SyncVar] private bool IsOn = false;
		protected int SpriteIndex => IsOn ? 1 : 0;
		
		private ItemTrait lighterTrait;

		private void Awake()
		{
			lighterTrait = CommonTraits.Instance.everyTraitOutThere[313];
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			ToggleLight(false);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Degrade);
		}

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false || DecayStage >= 4)
			{
				return false;
			}

			return CheckInteraction(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.UsedObject == null)
			{
				ToggleLight(false);
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You blow out the {gameObject.ExpensiveName()}!",
					$"{interaction.Performer.name} blows out the {gameObject.ExpensiveName()}!");
			}
			else
			{
				if (TryLightByObject(interaction.UsedObject))
				{
					ToggleLight(true);
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You light the {gameObject.ExpensiveName()}!",
						$"{interaction.Performer.name} lights the {gameObject.ExpensiveName()}!");
				}
			}
		}

		private bool CheckInteraction(Interaction interaction, NetworkSide side)
		{
			if (interaction.UsedObject != null && IsOn == false && interaction.UsedObject.TryGetComponent<FireSource>(out var fire))
			{
				return fire != null;
			}
			else if(interaction.UsedObject == null && IsOn)
			{
				return true;
			}

			return false;
		}

		#endregion Interaction

		private bool TryLightByObject(GameObject usedObject)
		{
			if (!IsOn)
			{
				// check if player tries to lit candle with something
				if (usedObject != null)
				{
					// check if it's something like lighter or candle
					var fireSource = usedObject.GetComponent<FireSource>();
					if (fireSource  != null && fireSource.IsBurning)
					{
						return true;
					}
				}
			}

			return false;
		}

		public void ToggleLight(bool lit)
		{
			IsOn = lit;
			lightControl.Toggle(lit);

			UpdateSprite();

			if (TryGetComponent<FireSource>(out var fire))
			{
				fire.IsBurning = IsOn;
			}
			if(IsOn)
			{
				UpdateManager.Add(Degrade, 5f);
			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Degrade);
			}
		}

		void Degrade()
		{
			LifeSpan--;
			DecayStage = 4 - Mathf.CeilToInt((LifeSpan / 30f));
			if (DecayStage == 4)
			{
				lightControl.Toggle(false);
				if (TryGetComponent<FireSource>(out var fire))
				{
					fire.IsBurning = IsOn;
				}
			}
			UpdateSprite();
		}

		void UpdateSprite()
		{
			if (TryGetComponent<ClothingV2>(out var clothing))
			{
				clothing.ChangeSprite(lightControl.IsOn ? 1 + (DecayStage * 2) : 0 + (DecayStage * 2));
			}
			spriteHandler.ChangeSprite(SpriteIndex + (DecayStage * 2));
		}
	}
}
