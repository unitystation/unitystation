using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
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
		[SyncVar] public int InitialLifeSpan = 120;
		[SyncVar] public int LifeSpan = 120; //10 minutes
		[SyncVar] public int DecayStage = 0;
		[SyncVar] private bool IsOn = false;

		private bool Updating = false;

		protected int SpriteIndex => IsOn ? 1 : 0;

		public void Awake()
		{
			lightControl = GetComponent<ItemLightControl>();
		}

		public void Start()
		{
			LifeSpan = InitialLifeSpan;
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			ToggleLight(false);
			if (Updating)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Degrade);
				Updating = false;
			}


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
			else if (interaction.UsedObject == null && IsOn)
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
				if (Updating ==false)
				{
					UpdateManager.Add(Degrade, 5f);
					Updating = true;

				}

			}
			else
			{
				if (Updating)
				{
					UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Degrade);
					Updating = false;
				}

			}
		}

		void Degrade()
		{
			LifeSpan--;

			if (LifeSpan < 0)
			{
				LifeSpan = 0;
			}

			var  Percentage = (float) LifeSpan / (float) InitialLifeSpan;


			//0 == full
			//1 = A bit dead
			//2 = Nearly dead
			//3 = dead
			switch (Percentage)
			{
				case > 0.666f:
					DecayStage = 0;
					break;
				case > 0.3333f:
					DecayStage = 1;
					break;
				case > 0:
					DecayStage = 2;
					break;
				default:
					DecayStage = 3;
					break;
			}
			if (DecayStage == 3) ToggleLight(false);
			UpdateSprite();
		}

		void UpdateSprite()
		{
			if (TryGetComponent<ClothingV2>(out var clothing))
			{
				clothing.ChangeSprite(lightControl.IsOn ? 1 + (DecayStage * 2) : 0 + (DecayStage * 2));
			}

			spriteHandler.SetCatalogueIndexSprite(SpriteIndex + (DecayStage * 2));
		}
	}
}
