using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using HealthV2;
using Messages.Server;
using Objects.Research;

namespace Systems.Research
{
	public class AreaEffectBase : ArtifactEffect
	{
		public EffectShapeType effectShapeType = EffectShapeType.Square;

		public int coolDown = 10;

		[Tooltip("How far this artifact will effect players from")]
		public int AuraRadius = 10;

		[Tooltip("Allows the artifact to effect the head and body differently if they are wearing only partially protective gear")]
		public bool AllowBodySplitting = false;

		public virtual void DoEffectAura(GameObject centeredAround)
		{
			var objCenter = centeredAround.AssumedWorldPosServer().RoundToInt();
			var players = Physics2D.OverlapCircleAll(objCenter.To2(), AuraRadius, LayerMask.NameToLayer("Player"));

			foreach (var player in players)
			{
				if (player.TryGetComponent<PlayerScript>(out var playerScript))
				{
					if (playerScript.IsDeadOrGhost == false)
					{
						bool successful = false;

						//What is this? Particuarly powerful artifacts can rip players apart if they are not careful,
						//if an artifact tries to teleport the head, but the body is resistant to teleporting,
						//it might rip the head off the body. Be careful when enabling body splitting.
						//This can also be used to make artifacts indivdually effect body parts.
						if (AllowBodySplitting) successful = TryEffectParts(playerScript);
						else successful = TryEffectPlayer(playerScript);

						centeredAround.TryGetComponent<Artifact>(out var artifact);
						if (artifact != null) artifact.SpawnClientEffect(playerScript.connectionToClient, successful, playerScript.AssumedWorldPos.To3());
					}
				}
			}
		}

		protected virtual bool TryEffectParts(PlayerScript toEffect)
		{
			foreach (BodyPart part in toEffect.playerHealth.SurfaceBodyParts)
			{
				if (part.BodyPartType == BodyPartType.Chest) continue; //Don't want to dismember torso, only periphirals.

				float totalAnomalyArmour = 0;

				foreach (var anomalyArmour in part.ClothingArmors)
				{
					totalAnomalyArmour += anomalyArmour.Anomaly;
				}

				totalAnomalyArmour = Mathf.Clamp(totalAnomalyArmour, 0, 100);

				if (DMMath.Prob(50 - (totalAnomalyArmour /2)))
				{
					OnEffect(toEffect, part);
					return true;
				}
			}
			return false;
		}

		protected virtual bool TryEffectPlayer(PlayerScript toEffect)
		{
			float totalAnomalyArmour = 0;
			int partCount = 0;

			List<BodyPart> parts = toEffect.playerHealth.SurfaceBodyParts.Shuffle().ToList(); //Without this it will almost always remove the same body part, i.e head

			foreach (BodyPart part in parts)
			{
				float bodyPartTotalArmour = 0;
				foreach (var anomalyArmour in part.ClothingArmors)
				{
					bodyPartTotalArmour += anomalyArmour.Anomaly;
				}

				totalAnomalyArmour += Mathf.Clamp(bodyPartTotalArmour, 0, 100);
				partCount++;
			}

			if(partCount != 0) totalAnomalyArmour /= partCount;

			totalAnomalyArmour = Mathf.Clamp(totalAnomalyArmour, 0, 100);

			if (DMMath.Prob(50 - (totalAnomalyArmour / 2)))
			{
				OnEffect(toEffect);
				return true;
			}

			return false;
		}

		public virtual void OnEffect(PlayerScript player, BodyPart part = null)
		{
			//Area effect here
		}
	}
}
