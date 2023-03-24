using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Mirror;

namespace Items
{
	/// <summary>
	/// Emag charges handler
	/// </summary>
	public class Emag : NetworkBehaviour, IServerSpawn, IExaminable
	{
		private SpriteHandler spriteHandler;

		[Tooltip("Number of charges emags start with")] [SerializeField]
		public int startCharges = 3;

		[Tooltip("Number of seconds it takes to regenerate 1 charge")] [SerializeField]
		public float rechargeTimeInSeconds = 10f;

		[SyncVar(hook = nameof(SyncCharges))] private int charges;

		/// <summary>
		/// Number of charges left on emag
		/// </summary>
		public int Charges => charges;

		public AddressableAudioSource OutOfChargesSFXA;

		#region SyncVarFuncs

		void Awake()
		{
			charges = startCharges;
			spriteHandler = gameObject.transform.Find("Charges").GetComponent<SpriteHandler>();
		}

		public void OnDisable()
		{
			if (isServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RegenerateCharge);
			}
		}

		public override void OnStartClient()
		{
			SyncCharges(Charges, charges);
			base.OnStartClient();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SyncCharges(startCharges, startCharges);
		}

		#endregion

		private void SyncCharges(int oldCharges, int newCharges)
		{
			charges = newCharges;
		}

		public string Examine(Vector3 worldPos)
		{
			return "The charge indicator shows " + Charges.ToString() + "/" + startCharges.ToString();
		}

		///<summary>
		///Used to scale charges if starting charges > 3 so it will show proper pips
		///</summary>
		private int ScaleChargesToSpriteIndex()
		{
			int output = Mathf.CeilToInt(((float) Charges / (float) startCharges) * 3f) - 1;
			return output;
		}

		/// <summary>
		/// Checks if the Emag has charges
		/// </summary>
		public bool EmagHasCharges()
		{
			return Charges > 0;
		}

		/// <summary>
		/// Uses one charge from the emag, returns true if successful
		/// </summary>
		public bool UseCharge(HandApply interaction)
		{
			return UseCharge(interaction.TargetObject, interaction.Performer);
		}

		public bool UseCharge(GameObject TargetObject, GameObject Performer)
		{
			Chat.AddActionMsgToChat(Performer,
				$"You wave the Emag over the {TargetObject.ExpensiveName()}'s electrical panel.",
				$"{Performer.ExpensiveName()} waves something over the {TargetObject.ExpensiveName()}'s electrical panel.");
			return UseChargeLogic(Performer);
		}

		private bool UseChargeLogic(GameObject Performer)
		{
			if (Charges > 0)
			{
				//if this is the first charge taken off, add recharge loop
				if (Charges >= startCharges)
				{
					UpdateManager.Add(RegenerateCharge, rechargeTimeInSeconds);
				}

				SyncCharges(Charges, Charges - 1);
				if (Charges > 0)
				{
					spriteHandler.ChangeSprite(ScaleChargesToSpriteIndex());
				}
				else
				{
					SoundManager.PlayNetworkedForPlayer(recipient: Performer, OutOfChargesSFXA, sourceObj: gameObject);
					spriteHandler.Empty();
				}

				return true;
			}

			return false;
		}

		private void RegenerateCharge()
		{
			if (Charges < startCharges)
			{
				AddCharges(1);
				spriteHandler.ChangeSprite(ScaleChargesToSpriteIndex());
			}

			if (Charges >= startCharges)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RegenerateCharge);
			}
		}

		public void AddCharges(int incharges)
		{
			SyncCharges(Charges, Charges + incharges);
		}


		public static Emag GetEmagInDynamicItemStorage(DynamicItemStorage dynamicItemStorage)
		{
			if (dynamicItemStorage == null) return null;
			Emag emagInHand = dynamicItemStorage.OrNull()?.GetActiveHandSlot()?.Item.OrNull()?.gameObject.OrNull()
				?.GetComponent<Emag>()?.OrNull();

			if (emagInHand != null)
			{
				return emagInHand;
			}

			foreach (var item in dynamicItemStorage.GetNamedItemSlots(NamedSlot.id))
			{
				Emag emagInIdSlot = item?.Item.OrNull()?.gameObject.GetComponent<Emag>()?.OrNull();
				if (emagInIdSlot != null)
				{
					return emagInIdSlot;
				}
			}

			return null;
		}
	}
}