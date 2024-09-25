using System;
using System.Collections.Generic;
using Core;
using Doors;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;


namespace Systems.MobAIs
{
	public class MobObjective : MonoBehaviour
	{
		[NonSerialized]
		protected UniversalObjectPhysics objectPhysics;
		public UniversalObjectPhysics ObjectPhysics => objectPhysics;
		protected RegisterTile mobTile;
		protected Rotatable rotatable;
		protected MobAI mobAI;

		[Tooltip("Allow the objective to happen when mob is dead")]
		public bool AllowDead = false;

		[Tooltip("Allow the objective to happen when mob is unconscious")]
		public bool AllowUnconscious = false;

		public void Awake()
		{
			mobTile = GetComponent<RegisterTile>();
			rotatable = GetComponent<Rotatable>();
			mobAI = GetComponent<MobAI>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		//The priority that this action should be done next
		public float Priority;

		protected List<Vector3Int> Directions = new List<Vector3Int>()
		{
			new Vector3Int(1, 0, 0),
			new Vector3Int(-1, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(0, -1, 0),
		};

		public void TryAction()
		{
			if(mobAI.IsUnconscious && AllowUnconscious == false) return;

			if(mobAI.IsDead && AllowDead == false) return;

			DoAction();
		}

		public virtual void DoAction() { }

		public virtual void ContemplatePriority() { }

		protected void Move(Vector3Int dirToMove)
		{
			objectPhysics.OrNull()?.TryTilePush(dirToMove.To2Int(), null);

			if (rotatable != null)
			{
				rotatable.SetFaceDirectionLocalVector(dirToMove.To2Int());
			}
		}

		protected Vector3Int ChooseDominantDirection(Vector3 inD)
		{
			if (Mathf.Abs(inD.x) > Mathf.Abs(inD.y))
			{
				if (inD.x > 0)
				{
					return new Vector3Int(1, 0, 0);
				}

				return new Vector3Int(-1, 0, 0);
			}

			if (inD.y > 0)
			{
				return new Vector3Int(0, 1, 0);
			}

			return new Vector3Int(0, -1, 0);
		}
	}
}
