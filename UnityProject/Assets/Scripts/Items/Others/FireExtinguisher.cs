using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Chemistry.Components;

[RequireComponent(typeof(Pickupable))]
public class FireExtinguisher : NetworkBehaviour, IServerSpawn,
	IInteractable<HandActivate>,
	ICheckedInteractable<AimApply>
{
	bool safety = true;
	public int travelDistance = 6;
	private float travelTime => 1f / travelDistance;
	public ReagentContainer reagentContainer;
	public RegisterItem registerItem;
	public Pickupable pickupable;

	[SerializeField]
	[Range(1, 50)]
	private int reagentsPerUse = 1;

	public SpriteRenderer spriteRenderer;
	[SyncVar(hook = nameof(SyncSprite))] public int spriteSync;
	public Sprite[] spriteList;

	private DateTime clientLastInteract = DateTime.Now;

	public override void OnStartClient()
	{
		EnsureInit();
		SyncSprite(spriteSync, spriteSync);
	}

	public void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (!pickupable)
		{
			pickupable = GetComponent<Pickupable>();
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		safety = true;
		SyncSprite(spriteSync, 0);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (safety)
		{
			safety = false;
			SyncSprite(spriteSync, 1);
		}
		else
		{
			safety = true;
			SyncSprite(spriteSync, 0);
		}
	}

	public bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)
		    || (!IsCoolDown() && !isServer)) return false;
		return true;
	}

	public void ServerPerformInteraction(AimApply interaction)
	{
		if (reagentContainer.ReagentMixTotal < reagentsPerUse || safety) return;


		Vector2 startPos = gameObject.AssumedWorldPosServer();
		Vector2 targetPos = interaction.WorldPositionTarget.To2Int();
		List<Vector3Int> positionList = CheckPassableTiles(startPos, targetPos);
		StartCoroutine(Fire(positionList));

		var points = GetParallelPoints(startPos, targetPos, true);
		positionList = CheckPassableTiles(points[0], points[1]);
		StartCoroutine(Fire(positionList));

		points = GetParallelPoints(startPos, targetPos, false);
		positionList = CheckPassableTiles(points[0], points[1]);
		StartCoroutine(Fire(positionList));

		Effect.PlayParticleDirectional(this.gameObject, interaction.TargetVector);

		SoundManager.PlayNetworkedAtPos("Extinguish", startPos, 1, sourceObj: interaction.Performer);

		interaction.Performer.Pushable()?.NewtonianMove((-interaction.TargetVector).NormalizeToInt());
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
		if (rightSide)
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
		var points = new Vector2[] { paralelStart, paralelTarget };
		return points;
	}

	private IEnumerator Fire(List<Vector3Int> positionList)
	{
		for (int i = 0; i < positionList.Count; i++)
		{
			ExtinguishTile(positionList[i]);
			yield return WaitFor.Seconds(travelTime);
		}
	}

	void ExtinguishTile(Vector3Int worldPos)
	{
		reagentContainer.Spill(worldPos, reagentsPerUse);
	}

	public void SyncSprite(int oldValue, int value)
	{
		EnsureInit();
		spriteSync = value;
		spriteRenderer.sprite = spriteList[spriteSync];

		pickupable.RefreshUISlotImage();
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

	private bool IsCoolDown()
	{
		var totalSeconds = (DateTime.Now - clientLastInteract).TotalSeconds;
		if (totalSeconds < 1f)
		{
			return false;
		}

		clientLastInteract = DateTime.Now;
		return true;
	}
}