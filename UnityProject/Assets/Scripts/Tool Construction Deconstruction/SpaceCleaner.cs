using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Pickupable))]
public class SpaceCleaner : NetworkBehaviour, ICheckedInteractable<AimApply>
{
	int travelDistance = 6;
	public ReagentContainer reagentContainer;

	public ParticleSystem particleSystem;
	[SyncVar(hook = nameof(SyncPlayParticles))] public float particleSync;

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
		if (reagentContainer.CurrentCapacity >= 5)
		{
			Vector2 startPos = gameObject.AssumedWorldPosServer();
			Vector2 targetPos = new Vector2(Mathf.RoundToInt(interaction.WorldPositionTarget.x), Mathf.RoundToInt(interaction.WorldPositionTarget.y));
			List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
			StartCoroutine(Fire(positionList));

			var angle = Mathf.Atan2(targetPos.y - startPos.y, targetPos.x - startPos.x) * 180 / Mathf.PI;
			SyncPlayParticles(angle);

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

	public void SyncPlayParticles(float value)
	{
		particleSync = value;
		if (!gameObject.activeInHierarchy) return;

		particleSystem.transform.position = gameObject.AssumedWorldPosServer(); //fixme won't work in mp
		particleSystem.transform.rotation = Quaternion.Euler(0, 0, particleSync);
		var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
		renderer.enabled = true;
		particleSystem.Play();
	}

}
