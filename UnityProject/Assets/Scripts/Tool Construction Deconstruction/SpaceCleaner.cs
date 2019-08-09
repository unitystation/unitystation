using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Pickupable))]
public class SpaceCleaner : NBAimApplyInteractable
{
	int travelDistance = 6;
	public ReagentContainer reagentContainer;
	public RegisterItem registerItem;

	public ParticleSystem particleSystem;
	[SyncVar(hook = nameof(SyncParticles))] public float particleSync;

	protected override bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (interaction.MouseButtonState == MouseButtonState.PRESS)
		{
			return true;
		}
		return false;
	}

	protected override void ServerPerformInteraction(AimApply interaction)
	{
		if (reagentContainer.CurrentCapacity >= 5)
		{
			Vector2 startPos = interaction.Performer.transform.position;
			Vector2 targetPos = new Vector2(Mathf.RoundToInt(interaction.WorldPositionTarget.x), Mathf.RoundToInt(interaction.WorldPositionTarget.y));
			List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
			StartCoroutine(Fire(positionList));

			var angle = Mathf.Atan2(targetPos.y - startPos.y, targetPos.x - startPos.x) * 180 / Mathf.PI;
			particleSync = angle;

			reagentContainer.MoveReagentsTo(5);
			SoundManager.PlayNetworkedAtPos("Spray2", startPos, 1);
		}
	}

	private IEnumerator Fire(List<Vector3Int> positionList)
	{
		for (int i = 0; i < positionList.Count; i++)
		{
			SprayTile(positionList[i]);
			yield return WaitFor.Seconds(0.1f);
		}
	}

	void SprayTile(Vector3Int worldPos)
	{
		var matrix = MatrixManager.AtPoint(worldPos, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPos, matrix);
		matrix.MetaDataLayer.ReagentReact(reagentContainer.Contents, worldPos, localPosInt);
	}

	public void SyncParticles(float value)
	{
		particleSystem.transform.position = registerItem.WorldPositionClient;
		particleSystem.transform.rotation = Quaternion.Euler(0, 0, value);
		var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
		renderer.enabled = true;
		particleSystem.Play();
	}

}
