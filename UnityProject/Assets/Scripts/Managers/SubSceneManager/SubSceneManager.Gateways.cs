using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

		Instance.gatewayLinks.Add(requestee,
			Instance.worldGatewayCache[Random.Range(0, Instance.worldGatewayCache.Count)]);

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

