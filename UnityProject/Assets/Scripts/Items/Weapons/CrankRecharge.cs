using System.Collections;
using AddressableReferences;
using UnityEngine;
using Systems.Construction.Parts;

namespace Weapons
{
	public class CrankRecharge : MonoBehaviour, ICheckedInteractable<HandActivate>, IServerInventoryMove
	{

		[SerializeField] private float crankTime = 2f;
		
		[SerializeField] private string rechargeVerb = "recharging";
		
		[SerializeField] private AddressableAudioSource rechargeSound;
		
		private Gun gunComp;
		
		private GameObject serverHolder;
		
		private void Awake()
		{
			gunComp = GetComponent<Gun>();
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//Gotta exclude attachments or whatever this is applied to wont be able to use em
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponAttachable))
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddActionMsgToChat(serverHolder,
				$"You start {rechargeVerb} the {gameObject.ExpensiveName()}.",
				$"{serverHolder.ExpensiveName()} starts {rechargeVerb} the {gameObject.ExpensiveName()}.");
			
			StartCoroutine(Recharging());
		}
		
		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (gameObject != info.MovedObject.gameObject) return;

			StopAllCoroutines();
			serverHolder = info.ToPlayer != null ? info.ToPlayer.gameObject : null;
		}
		
		private IEnumerator Recharging()
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Wrench, gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
			yield return WaitFor.Seconds(crankTime);
			AddCharge();
		}
		
		private void AddCharge()
		{
			if (serverHolder == null) return;

			var mag = gunComp.magSlot.Item;
			if (mag != null)
			{
				var battery = mag.GetComponent<Battery>();
				if (battery != null)
				{
					battery.Watts = battery.MaxWatts;
				}
				
				var electricalMagazine = mag.GetComponent<ElectricalMagazine>();
				if (electricalMagazine != null)
				{
					electricalMagazine.AddCharge();
				}
			}
			
			if (rechargeSound != null)
			{
				SoundManager.PlayNetworkedAtPos(rechargeSound, gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
			}
			
			Chat.AddActionMsgToChat(serverHolder,
				$"You finish {rechargeVerb} the {gameObject.ExpensiveName()}.",
				$"{serverHolder.ExpensiveName()} finishes {rechargeVerb} the {gameObject.ExpensiveName()}.");
		}
	}
}
