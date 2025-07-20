using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Machines;
using UnityEngine;
using Mirror;
using Objects.Machines;
using SecureStuff;
using UnityEngine.Serialization;

namespace Objects.Medical
{
	public class CloningPod : NetworkBehaviour, IRefreshParts
	{
		public CloningPodStatus statusSync;
		public SpriteHandler SpriteHandler;
		[PlayModeOnly] public string statusString;
		public CloningConsole console;

		[FormerlySerializedAs("LimbCloningDamage"), SerializeField] private float internalLimbCloningDamage = 25;

		[SerializeField] private float internalCloningTime = 180;


		private float LimbCloningDamage = 25;

		private float CloningTime = 180;

		[SerializeField] private ItemTrait UpgradePart;

		private Machine Machine;

		public enum CloningPodStatus
		{
			Empty,
			Cloning
		}

		public override void OnStartServer()
		{
			statusString = "Inactive.";
		}


		public void ServerStartCloning(CloningRecord record)
		{
			statusSync = CloningPodStatus.Cloning;
			SpriteHandler.SetCatalogueIndexSprite(1);
			statusString = "Cloning cycle in progress.";
			StartCoroutine(ServerProcessCloning(record));
		}

		private IEnumerator ServerProcessCloning(CloningRecord record)
		{
			yield return WaitFor.Seconds(CloningTime);
			statusSync = CloningPodStatus.Empty;
			SpriteHandler.SetCatalogueIndexSprite(0);
			statusString = "Cloning process complete.";
			if (console)
			{
				console.UpdateDisplay();
			}
			if (record.mind.IsOnline())
			{
				var playerBody = PlayerSpawn.ClonePlayerAt(record.mind,  record.mind.occupation,  record.mind.CurrentCharacterSettings, transform.position.CutToInt()).GetComponent<LivingHealthMasterBase>();
				playerBody.ApplyDamageAll(this.gameObject, LimbCloningDamage, AttackType.Internal, DamageType.Clone, false);
			}

		}

		public bool CanClone()
		{
			return statusSync == CloningPodStatus.Empty;
		}

		/// <summary>
		/// Updates the cloning pod's status string according to a mind's state
		/// </summary>
		public void UpdateStatusString(CloneableStatus status)
		{
			statusString = statusStrings[status];
		}

		private static Dictionary<CloneableStatus, string> statusStrings =
			new Dictionary<CloneableStatus, string>
			{
			{ CloneableStatus.Cloneable, "Cloning will commence shortly." },
			{ CloneableStatus.OldRecord, "Outdated record." },
			{ CloneableStatus.DenyingCloning, "Spirit is denying cloning." },
			{ CloneableStatus.StillAlive, "Person is still alive." },
			{ CloneableStatus.Offline, "Spirit cannot be found." }
			};


		public void RefreshParts(IDictionary<PartReference, int> partsInFrame, Machine Frame)
		{
			var Multiplier = Frame.GetCertainPartMultiplier(UpgradePart);
			var DamageMultiplier = 1f / Multiplier;
			if (DamageMultiplier == 0.25f)
			{
				LimbCloningDamage = 0;
			}
			else
			{
				LimbCloningDamage = DamageMultiplier * internalLimbCloningDamage;
			}

			CloningTime = DamageMultiplier * internalCloningTime;
		}
	}
}
