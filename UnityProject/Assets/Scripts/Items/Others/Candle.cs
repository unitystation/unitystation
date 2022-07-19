using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Clothing;

namespace Items.Others
{
	[RequireComponent(typeof(ItemLightControl))]
	public class Candle : NetworkBehaviour, ICheckedInteractable<InventoryApply>, IServerDespawn
	{
		private ItemLightControl lightControl;

		[SerializeField]
		private SpriteHandler spriteHandler = default;

		[SyncVar] public int LifeSpan = 120; //10 minutes
		[SyncVar] public int DecayStage = 0;
		[SyncVar] private bool IsOn = false;

		protected int SpriteIndex => IsOn ? 1 : 0;

		public void Awake()
		{
			lightControl = GetComponent<ItemLightControl>();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			ToggleLight(false);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Degrade);
		}

		#region Interaction

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false || DecayStage >= 4)
			{
				return false;
			}
			if (interaction.UsedObject != null && IsOn == false && interaction.UsedObject.TryGetComponent<FireSource>(out var fire))
			{
				return fire != null;
			}
			else if (interaction.UsedObject == null && IsOn && interaction.Intent != Intent.Help)
			{
				return true;
			}

			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
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

		#endregion Interaction

		private bool TryLightByObject(GameObject usedObject)
		{
			if (!IsOn)
			{
				// check if player tries to light candle with something
				if (usedObject != null)
				{
					// check if it's something like lighter or another candle
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
			if (DecayStage == 4) ToggleLight(false);
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
