using System;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Player;
using US13.Projectiles.Behaviours;

public class ProjectileTeleportRandom : MonoBehaviour, IOnHit
{
	public bool OnHit(MatrixManager.CustomPhysicsHit hit)
	{
		if (hit.CollisionHit.GameObject == null)
		{
			return true;
		}
		else
		{
			int maxRange = 11;
			int potencyStrength = (int)Math.Round((100 * .01f) * maxRange, 0);
			TeleportUtils.ServerTeleportRandom(hit.CollisionHit.GameObject, 0, potencyStrength, false, true);
		}

		return true;
	}
}
