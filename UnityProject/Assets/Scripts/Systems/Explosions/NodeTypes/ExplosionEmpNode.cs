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
using Cysharp.Threading.Tasks;
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

		public ExplosionEmpNode(Vector3 _explosionStartWorldPosition) : base(_explosionStartWorldPosition)
		{
		}

		public override async UniTask Process()
		{
			float damageDealt = AngleAndIntensity.magnitude;
			if (damageDealt <= 0)
			{
				return;
			}

			if (matrix.MetaTileMap == null)
			{
				return;
			}

			if (damageDealt > 0)
			{
				//(Max): This is a terrible name. Whoever named it this way should be ashamed.
				//I have no clue what's the context of this vector. Is it local position? Is it world position? Is it a direction? Who knows!
				//Keep gatekeeping the codebase, it's not like there are other people working on this project..
				var v3int = new Vector3Int(Location.x, Location.y, 0);
				await ReguralProcessingToTilesOnly(damageDealt, v3int);
			}
		}

		public override float DoDamageToTiles(Matrix matrix, float damageDealt, Vector3Int v3int, MetaTileMap tileMap)
		{
			EmpThings(v3int, (int)damageDealt);
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
			return new ExplosionEmpNode(ExplosionStartWorldPosition);
		}
	}
}
