using System.Collections.Generic;
using UnityEngine;
using Light2D;
using HealthV2;
using Systems.Pipes;
using Items;
using Items.Others;
using Objects.Machines;
using Doors;
using Systems.Electricity;


namespace Systems.Explosions
{
	public class ExplosionNode
	{
		public int EmpStrength = 0;

		public Vector3Int Location;
		public Matrix matrix;

		public HashSet<ExplosionPropagationLine> PresentLines = new HashSet<ExplosionPropagationLine>();
		public Vector2 AngleAndIntensity;

		public List<PipeNode> SavedPipes = new List<PipeNode>();

		public void Initialise(Vector3Int Loc, Matrix Inmatrix)
		{
			Location = Loc;
			matrix = Inmatrix;
		}

		public void Process()
		{
			float Damagedealt = AngleAndIntensity.magnitude;
			float EnergyExpended = 0;
			var v3int = new Vector3Int(Location.x, Location.y, 0);

			var metaTileMap = matrix.MetaTileMap;

			if (Damagedealt <= 0 && EmpStrength <= 0)
			{
				return;
			}

			if (metaTileMap == null)
			{
				return;
			}

			if (EmpStrength > 0)
			{
				foreach (var thing in matrix.Get<Integrity>(v3int, true))
				{
					EmpThing(thing.gameObject, EmpStrength);
				}

				foreach (var thing in matrix.Get<LivingHealthMasterBase>(v3int, true))
				{
					EmpThing(thing.gameObject, EmpStrength);
				}

				EmpStrength = 0;
			}

			if(Damagedealt > 0)
			{
				EnergyExpended = metaTileMap.ApplyDamage(v3int, Damagedealt,
			MatrixManager.LocalToWorldInt(v3int, matrix.MatrixInfo), AttackType.Bomb);

				if (Damagedealt > 100)
				{
					var Node = matrix.GetMetaDataNode(v3int);
					if (Node != null)
					{
						foreach (var electricalData in Node.ElectricalData)
						{
							electricalData.InData.DestroyThisPlease();
						}

						SavedPipes.Clear();
						SavedPipes.AddRange(Node.PipeData);
						foreach (var Pipe in SavedPipes)
						{
							Pipe.pipeData.DestroyThis();
						}
					}
				}


				foreach (var integrity in matrix.Get<Integrity>(v3int, true))
				{
					//Throw items
					if (integrity.GetComponent<ItemAttributesV2>() != null)
					{

						integrity.GetComponent<UniversalObjectPhysics>().NewtonianPush(AngleAndIntensity.Rotate90(),1,inaim: BodyPartType.Chest,  inthrownBy : integrity.gameObject );
					}

					//And do damage to objects
					integrity.ApplyDamage(Damagedealt, AttackType.Bomb, DamageType.Brute);
				}

				foreach (var player in matrix.Get<PlayerHealthV2>(v3int, ObjectType.Player, true))
				{

					// do damage
					player.ApplyDamageAll(null, Damagedealt, AttackType.Bomb, DamageType.Brute);

				}

				foreach (var line in PresentLines)
				{
					line.ExplosionStrength -= EnergyExpended * (line.ExplosionStrength / Damagedealt);
				}
				AngleAndIntensity = Vector2.zero;
			}
		}

		private void EmpThing(GameObject thing, int EmpStrength)
		{
			if (thing != null)
			{
				if (isEmpAble(thing))
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

		private bool isEmpAble(GameObject thing)
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
	}
}
