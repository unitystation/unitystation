using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects;

//Gateways
public partial class SubSceneManager
{
	private List<StationGateway> stationGatewayCache = new List<StationGateway>();
	private List<WorldGateway> worldGatewayCache = new List<WorldGateway>();
	public Dictionary<StationGateway, WorldGateway> gatewayLinks = new Dictionary<StationGateway, WorldGateway>();

	public static WorldGateway RequestRandomAwayWorldLink(StationGateway requestee, bool requestNewLink = false)
	{
		if (Instance.gatewayLinks.ContainsKey(requestee))
		{
			if(!requestNewLink) return null;

			Instance.gatewayLinks.Remove(requestee);
		}
		var destination = Instance.worldGatewayCache.PickRandom();
		if (destination == null) return null; // Additional scenes were likely disabled on this build - logged in caller
		Instance.gatewayLinks.Add(requestee, destination);

		return Instance.gatewayLinks[requestee];
	}

	public static void RegisterStationGateway(StationGateway gateway)
	{
		Instance.stationGatewayCache.Add(gateway);
	}

	public static void RegisterWorldGateway(WorldGateway gateway)
	{
		Instance.worldGatewayCache.Add(gateway);
	}
}
