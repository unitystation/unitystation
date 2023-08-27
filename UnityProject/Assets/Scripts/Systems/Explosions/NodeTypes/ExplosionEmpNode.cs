using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Systems.Explosions;
using HealthV2;
using Items;
using Items.Others;
using Objects.Machines;
using Doors;
using AddressableReferences;
using TileManagement;

namespace Systems.Explosions
{
	public class ExplosionEmpNode : ExplosionNode
	{
		public override string EffectName
		{
			get { return "EMPEffect"; }
		}
		public override OverlayType EffectOverlayType
		{
			get { return OverlayType.EMP; }
		}
		public override AddressableAudioSource CustomSound
		{
			get { return CommonSounds.Instance.Empulse; }
		}

		public override float DoDamage(Matrix matrix, float DamageDealt, Vector3Int v3int)
		{
			EmpThings(v3int, (int)DamageDealt);
			return 10.0f; //magic number
		}

		public override void DoInternalDamage(float strength, BodyPart bodyPart)
		{
			return; //todo: add damage to prosthetics and augs
		}

		private void EmpThings(Vector3Int worldPosition, int damage)
		{
			foreach (var thing in MatrixManager.GetAt<Integrity>(worldPosition, true).Distinct())
			{
				EmpThing(thing.gameObject, damage);
			}

			foreach (var thing in MatrixManager.GetAt<LivingHealthMasterBase>(worldPosition, true).Distinct())
			{
				EmpThing(thing.gameObject, damage);
			}
		}

		private void EmpThing(GameObject thing, int EmpStrength)
		{
			if (thing != null)
			{
				if (IsEmpAble(thing))
				{
					if (thing.TryGetComponent<ItemStorage>(out var storage))
					{
						foreach (var slot in storage.GetItemSlots())
						{
							EmpThing(slot.ItemObject, EmpStrength);
						}
					}

					if (thing.TryGetComponent<DynamicItemStorage>(out var dStorage))
					{
						foreach (var slot in dStorage.GetItemSlots())
						{
							EmpThing(slot.ItemObject, EmpStrength);
						}
					}

					var interfaces = thing.GetComponents<IEmpAble>();

					foreach (var EMPAble in interfaces)
					{
						EMPAble.OnEmp(EmpStrength);
					}
				}
			}
		}

		private bool IsEmpAble(GameObject thing)
		{
			if (thing.TryGetComponent<Machine>(out var machine))
			{
				if (machine.isEMPResistant) return false;
			}

			if (thing.TryGetComponent<ItemAttributesV2>(out var attributes))
			{
				if (Validations.HasItemTrait(thing.gameObject, CommonTraits.Instance.EMPResistant)) return false;
			}

			return true;
		}

		public override ExplosionNode GenInstance()
		{
			return new ExplosionEmpNode();
		}
	}
}
