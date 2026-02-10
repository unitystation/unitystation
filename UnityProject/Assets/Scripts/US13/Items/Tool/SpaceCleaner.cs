using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Mirror;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Addressables.Types;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.HealthV2;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Messages.Server.SoundMessages;
using US13.Systems.Inventory;
using Util;
using Effect = US13.Effects.Effect;

namespace US13.Items.Tool
{
	[RequireComponent(typeof(Pickupable))]
	public class SpaceCleaner : NetworkBehaviour, ICheckedInteractable<AimApply>
	{
		public int travelDistance = 6;
		[SerializeField] private AddressableAudioSource Spray2 = null;

		private float travelTime => 1f / travelDistance;

		[SerializeField] [Range(1, 50)] private int reagentsPerUse = 1;

		private ReagentContainer reagentContainer;

		private void Awake()
		{
			reagentContainer = GetComponent<ReagentContainer>();
		}

		public bool WillInteract(AimApply interaction, NetworkSide side)
		{
			if (interaction.MouseButtonState == MouseButtonState.PRESS)
			{
				return true;
			}

			return false;
		}

		public void ServerPerformInteraction(AimApply interaction)
		{
			//just in case
			if (reagentContainer == null) return;

			if (reagentContainer.ReagentMixTotal < reagentsPerUse)
			{
				return;
			}

			Vector2 startPos = gameObject.AssumedWorldPosServer();
			Vector2 targetPos = new Vector2(Mathf.RoundToInt(interaction.WorldPositionTarget.x),
				Mathf.RoundToInt(interaction.WorldPositionTarget.y));
			List<Vector3Int> positionList = CheckPassableTiles(startPos, targetPos);
			StartCoroutine(Fire(positionList, interaction.TargetBodyPart));

			Effect.PlayParticleDirectional(this.gameObject, interaction.TargetVector);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1);
			SoundManager.PlayNetworkedAtPos(Spray2, startPos, audioSourceParameters, sourceObj: interaction.Performer);

			interaction.PerformerPlayerScript.ObjectPhysics.NewtonianPush((-interaction.TargetVector).NormalizeToInt(),
				speed: 1f);
		}

		private IEnumerator Fire(List<Vector3Int> positionList, BodyPartType bodyPartAim)
		{
			if (reagentContainer != null && reagentContainer.ReagentMixTotal > 0.1)
			{
				var Taking = reagentContainer.TakeReagents(reagentsPerUse);
				Taking.Divide(positionList.Count);
				for (int i = 0; i < positionList.Count; i++)
				{
					SprayTile(positionList[i], Taking.Clone(), bodyPartAim);
					yield return WaitFor.Seconds(travelTime);
				}
			}
		}

		void SprayTile(Vector3Int worldPos, ReagentMix ToReact, BodyPartType bodyPartAim  )
		{
			MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
			Vector3Int localPos = MatrixManager.WorldToLocalInt(worldPos, matrixInfo);

			if (reagentContainer.MajorMixReagent?.name == "SpaceCleaner")
			{
				matrixInfo.MetaDataLayer.Clean(worldPos, localPos, false);
			}
			else if (reagentContainer.MajorMixReagent?.name == "Water")
			{
				matrixInfo.MetaDataLayer.Clean(worldPos, localPos, true);
			}
			else
			{
				MatrixManager.ReagentReact(ToReact, worldPos, bodyPartAim : bodyPartAim);
			}
		}

		private List<Vector3Int> CheckPassableTiles(Vector2 startPos, Vector2 targetPos)
		{
			List<Vector3Int> passableTiles = new List<Vector3Int>();
			List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
			for (int i = 0; i < positionList.Count; i++)
			{
				if (!MatrixManager.IsAtmosPassableAt(positionList[i], true))
				{
					return passableTiles;
				}

				passableTiles.Add(positionList[i]);
			}

			return passableTiles;
		}
	}
}