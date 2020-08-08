using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines fire sources like candles, lighters or activated welder
/// Fire source can lead to plasma fire or used to ignite flamable objects
/// </summary>
public class FireSource : MonoBehaviour, IServerSpawn
{
	[SerializeField]
	[Tooltip("Does this fire emit flame from start?")]
	private bool isBurningOnSpawn;

	[Temperature]
	[SerializeField]
	[Tooltip("Flame temperature in Kelvins")]
	private float flameTemperature = 700f;

	[SerializeField]
	[Tooltip("Volume of flamed gas in m3")]
	public float flameVolume = 0.005f;

	private PushPull pushPull;

	/// <summary>
	/// Does this object emit flame?
	/// Flame may lead to ignite flamable objects or plasma fire
	/// </summary>
	public bool IsBurning { get; set; }

	private void Awake()
	{
		pushPull = GetComponent<PushPull>();
	}

	private void Update()
	{
		if (!CustomNetworkManager.IsServer || !pushPull)
		{
			return;
		}

		if (IsBurning)
		{
			var position = pushPull.AssumedWorldPositionServer();
			if (position != TransformState.HiddenPos)
			{
				var registerTile = pushPull.registerTile;
				if (registerTile)
				{
					var reactionManager = registerTile.Matrix.ReactionManager;
					reactionManager.ExposeHotspotWorldPosition(position.To2Int(), flameTemperature, flameVolume);
				}
			}
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		IsBurning = isBurningOnSpawn;
	}
}
