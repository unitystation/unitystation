using UnityEngine;

namespace US13.Items.Weapons
{
	public class BulletAmmo : MonoBehaviour
	{
		public AmmoType ammoType; //SET IT IN INSPECTOR
		public GameObject initalProjectile;
		public int ProjectilesFired = -1;
	}
}
