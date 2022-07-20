using System.Linq;
using UnityEngine;
using Clothing;
using System.Collections;
using System.Collections.Generic;
using HealthV2;

namespace Systems.Research
{
	public class AreaArtifactEffect : ArtifactEffect
	{
		public EffectShapeType effectShapeType = EffectShapeType.Square;

		public int coolDown = 10;

		[Tooltip("How far this artifact will effect players from")]
		public int AuraRadius = 10;

		[Tooltip("Wearing this trait protects the user from the area effect")]
		public ItemTrait resistanceTrait = null;

		[Tooltip("Allows the artifact to effect the head and body differently if they are wearing only partially protective gear")]
		public bool AllowBodySplitting = false;

		public virtual void DoEffectAura(GameObject centeredAround)
		{
			var objCenter = centeredAround.AssumedWorldPosServer().RoundToInt();
			var shape = EffectShape.CreateEffectShape(effectShapeType, objCenter, AuraRadius);

			foreach (var pos in shape)
			{
				// check if tile has any alive player
				var players = MatrixManager.GetAt<PlayerScript>(pos, true).Distinct();
				foreach (var player in players)
				{
					if (!player.IsDeadOrGhost)
					{
						//What is this? Particuarly powerful artifacts can rip players apart if they are not careful,
						//if an artifact tries to teleport the head, but the body is resistant to teleporting,
						//it might rip the head off the body. Be careful when enabling body splitting.
						//This can also be used to make artifacts indivdually effect body parts.
						if (AllowBodySplitting) TryEffectParts(player);
						else TryEffectPlayer(player);

					}
				}
			}
		}

		protected virtual void TryEffectParts(PlayerScript toEffect)
		{
			foreach (BodyPart part in toEffect.playerHealth.SurfaceBodyParts)
			{
				if (part.BodyPartType == BodyPartType.Chest) continue; //Don't want to dismember torso, only periphirals.
				float Armour = 0;
				foreach (var armour in part.ClothingArmors)
				{
					Armour += armour.Anomaly;
				}

				Armour += Mathf.Clamp(Armour, 0, 100);

				if (DMMath.Prob(50 - (Armour/2)))
				{
					OnEffect(toEffect, part);
					return;
				}
			}
		}

		protected virtual void TryEffectPlayer(PlayerScript toEffect)
		{
			float Armour = 0;
			int partCount = 0;

			List<BodyPart> parts = toEffect.playerHealth.SurfaceBodyParts.Shuffle().ToList(); //Without this it will almost always remove the same body part, i.e head

			foreach (BodyPart part in parts)
			{
				float A2 = 0;
				foreach (var armour in part.ClothingArmors)
				{
					A2 += armour.Anomaly;
				}

				Armour += Mathf.Clamp(A2, 0, 100);
				partCount++;					
			}
			if(partCount != 0) Armour /= partCount;

			Armour = Mathf.Clamp(Armour, 0, 100);

			if (DMMath.Prob(50 - (Armour/2)))
			{
				OnEffect(toEffect);
			}
		}

		public virtual void OnEffect(PlayerScript player, BodyPart part = null)
		{
			//Area effect here
		}
	}
}
