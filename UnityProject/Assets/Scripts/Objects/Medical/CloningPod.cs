using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Machines;
using UnityEngine;
using Mirror;
using Objects.Machines;
using UnityEngine.Serialization;

namespace Objects.Medical
{
	public class CloningPod : NetworkBehaviour, IRefreshParts
	{
		[SyncVar(hook = nameof(SyncSprite))] public CloningPodStatus statusSync;
		public SpriteRenderer spriteRenderer;
		public Sprite cloningSprite;
		public Sprite emptySprite;
		public string statusString;
		public CloningConsole console;

		[FormerlySerializedAs("LimbCloningDamage"), SerializeField] private float internalLimbCloningDamage = 25;

		[SerializeField] private float internalCloningTime = 60;


		private float LimbCloningDamage = 25;

		private float CloningTime = 60;

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

		public override void OnStartClient()
		{
			SyncSprite(statusSync, statusSync);
		}

		public void ServerStartCloning(CloningRecord record)
		{
			statusSync = CloningPodStatus.Cloning;
			statusString = "Cloning cycle in progress.";
			StartCoroutine(ServerProcessCloning(record));
		}

		private IEnumerator ServerProcessCloning(CloningRecord record)
		{
			yield return WaitFor.Seconds(CloningTime);
			statusSync = CloningPodStatus.Empty;
			statusString = "Cloning process complete.";
			if (console)
			{
				console.UpdateDisplay();
			}
			if (record.mind.IsOnline())
			{
				var playerBody = PlayerSpawn.ServerClonePlayer(record.mind, transform.position.CutToInt()).GetComponent<LivingHealthMasterBase>();
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

		public void SyncSprite(CloningPodStatus oldValue, CloningPodStatus value)
		{
			statusSync = value;
			if (value == CloningPodStatus.Empty)
			{
				spriteRenderer.sprite = emptySprite;
			}
			else
			{
				spriteRenderer.sprite = cloningSprite;
			}
		}

		public void RefreshParts(IDictionary<PartReference, int> partsInFrame)
		{
			var Multiplier = Machine.GetCertainPartMultiplier(UpgradePart);
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
