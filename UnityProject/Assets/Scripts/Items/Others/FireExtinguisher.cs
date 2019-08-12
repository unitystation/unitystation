using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Pickupable))]
public class FireExtinguisher : NBAimApplyHandActivateInteractable
{
	bool safety = true;
	int travelDistance = 6;
	public ReagentContainer reagentContainer;
	public RegisterItem registerItem;

	public SpriteRenderer spriteRenderer;
	[SyncVar(hook = nameof(SyncSprite))] public int spriteSync;
	public Sprite[] spriteList;

	public ParticleSystem particleSystem;
	[SyncVar(hook = nameof(SyncParticles))] public float particleSync;

	public override void OnStartClient()
	{
		SyncSprite(spriteSync);
		base.OnStartClient();
	}

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		if (safety)
		{
			safety = false;
			spriteSync = 1;
		}
		else
		{
			safety = true;
			spriteSync = 0;
		}
	}

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
		if(reagentContainer.CurrentCapacity >= 5 && !safety)
		{
			Vector2 startPos = interaction.Performer.transform.position; //TODO: use registeritem position once picked up items get fixed
			Vector2 targetPos = new Vector2(Mathf.RoundToInt(interaction.WorldPositionTarget.x), Mathf.RoundToInt(interaction.WorldPositionTarget.y));
			List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
			StartCoroutine(Fire(positionList));

			var points = GetParallelPoints(startPos, targetPos, true);
			positionList = MatrixManager.GetTiles(points[0], points[1], travelDistance);
			StartCoroutine(Fire(positionList));

			points = GetParallelPoints(startPos, targetPos, false);
			positionList = MatrixManager.GetTiles(points[0], points[1], travelDistance);
			StartCoroutine(Fire(positionList));

			var angle = Mathf.Atan2(targetPos.y - startPos.y, targetPos.x - startPos.x) * 180 / Mathf.PI;
			particleSync = angle;
			SoundManager.PlayNetworkedAtPos("Extinguish", startPos, 1);
			reagentContainer.MoveReagentsTo(5);
		}
	}

	/// <summary>
	/// Returns the vectors that form a line parallel to the arguments
	/// </summary>
	private Vector2[] GetParallelPoints(Vector2 startPos, Vector2 targetPos, bool rightSide)
	{
		Vector2 difference = targetPos - startPos;
		Vector2 rotated = Vector2.Perpendicular(difference).normalized;
		Vector2 paralelStart;
		Vector2 paralelTarget;
		if(rightSide)
		{
			paralelStart = startPos - rotated;
			paralelTarget = startPos - rotated + difference;
		}
		else
		{
			paralelStart = startPos + rotated;
			paralelTarget = startPos + rotated + difference;
		}
		paralelStart = new Vector2(Mathf.RoundToInt(paralelStart.x), Mathf.RoundToInt(paralelStart.y));
		paralelTarget = new Vector2(Mathf.RoundToInt(paralelTarget.x), Mathf.RoundToInt(paralelTarget.y));
		var points = new Vector2[]{paralelStart, paralelTarget };
		return points;
	}

	private IEnumerator Fire(List<Vector3Int> positionList)
	{
		for (int i = 0; i < positionList.Count; i++)
		{
			ExtinguishTile(positionList[i]);
			yield return WaitFor.Seconds(0.1f);
		}
	}

	void ExtinguishTile(Vector3Int worldPos)
	{
		var matrix = MatrixManager.AtPoint(worldPos, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPos, matrix);
		matrix.MetaDataLayer.ReagentReact(reagentContainer.Contents, worldPos, localPosInt);
	}

	public void SyncSprite(int value)
	{
		spriteSync = value;
		spriteRenderer.sprite = spriteList[spriteSync];

		if (UIManager.Hands.CurrentSlot && UIManager.Hands.CurrentSlot.Item == gameObject)
		{
			UIManager.Hands.CurrentSlot.UpdateImage(gameObject);
		}
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
