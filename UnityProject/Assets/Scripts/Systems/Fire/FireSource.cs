using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects;

/// <summary>
/// Defines fire sources like candles, lighters or activated welder
/// Fire source can lead to plasma fire or used to ignite flamable objects
/// </summary>
public class FireSource : MonoBehaviour, IServerSpawn
{
	[SerializeField]
	[Tooltip("Does this fire emit flame from start?")]
	private bool isBurningOnSpawn = false;

	[SerializeField]
	private float hotspotTemperature = 700;

	[SerializeField]
	[Tooltip("Will change temperature of tile to the hotspotTemperature if this temperature is greater than the current gas mix temperature when true")]
	private bool changeGasMixTemp = false;

	private PushPull pushPull = null;
	private bool isBurning = false;

	/// <summary>
	/// Does this object emit flame?
	/// Flame may lead to ignite flamable objects or plasma fire
	/// </summary>
	public bool IsBurning
	{
		get
		{
			return isBurning;
		}
		set
		{
			if (isBurning == value)
			{
				return;
			}
			isBurning = value;

			// when item emits flame we need to send heat to surroundings
			if (pushPull && CustomNetworkManager.IsServer)
			{
				if (isBurning)
				{
					// subscribe to peropdic update to send heat
					UpdateManager.Add(CreateHotspot, 0.1f);
				}
				else
				{
					// don't need to send heat anymore
					UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CreateHotspot);
				}
			}
		}
	}

	private void Awake()
	{
		pushPull = GetComponent<PushPull>();
	}

	private void OnDisable()
	{
		// unsubscribe hotspot from updates
		if (pushPull && CustomNetworkManager.IsServer)
		{
			if (isBurning)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CreateHotspot);
			}
		}
	}

	private void CreateHotspot()
	{
		// send some heat on firesource position
		var position = pushPull.AssumedWorldPositionServer();
		if (position != TransformState.HiddenPos)
		{
			var registerTile = pushPull.registerTile;
			if (registerTile)
			{
				var reactionManager = registerTile.Matrix.ReactionManager;
				reactionManager.ExposeHotspotWorldPosition(position.To2Int(), hotspotTemperature, changeGasMixTemp);
			}
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		IsBurning = isBurningOnSpawn;
	}
}
