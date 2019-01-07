using UnityEngine;

public enum TortureSeverity
{
	S = 1, M = 2, L = 3, XL = 4, XXL = 5
}

/// Various punishments for misbehaving players
public class TortureChamber
{
	public static void Torture(GameObject player, TortureSeverity severity = TortureSeverity.M)
	{
		var ps = player.GetComponent<PlayerScript>();
		if ( !ps )
		{
			Logger.LogWarning("Cannot torture :( not a player", Category.Security);
			return;
		}
		Torture(ps, severity);
	}

	public static void Torture(PlayerScript ps, TortureSeverity severity = TortureSeverity.M)
	{
		Logger.Log($"Player {ps.gameObject} is now being tortured with '{severity}' severity. Enjoy", Category.Security);
		//todo: torture sequences
		Bleed(ps, severity);
		DropShit(ps, severity);
		if ( severity >= TortureSeverity.L )
		{
			RandomTeleport(ps, severity);
		}
	}

	private static void Bleed(PlayerScript ps, TortureSeverity severity)
	{
		ps.playerHealth.bloodSystem.AddBloodLoss(( int ) Mathf.Pow(2, ( float ) severity));
	}
	private static void DropShit(PlayerScript ps, TortureSeverity severity)
	{
		if ( severity >= TortureSeverity.L )
		{
			ps.playerNetworkActions.DropAll();
			return;
		}
		for ( int i = 0; i < (int)severity; i++ )
		{
			ps.playerNetworkActions.DropItem();
		}
	}
	private static void RandomTeleport(PlayerScript ps, TortureSeverity severity)
	{
		int randX = (int)severity * 100 * Random.Range(-5, 5);
		int randY = (int)severity * 100 * Random.Range(-5, 5);
		ps.PlayerSync.SetPosition(new Vector2(randX,randY));
	}

}
