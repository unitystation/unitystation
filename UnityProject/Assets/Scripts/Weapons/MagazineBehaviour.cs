using UnityEngine.Networking;

public class MagazineBehaviour : ManagedNetworkBehaviour
{
	[SyncVar] public int ammoRemains;
	public string ammoType; //SET IT IN INSPECTOR
	public int magazineSize = 20;
	public bool Usable;

	private void Start()
	{
		Usable = true;
		ammoRemains = magazineSize;
	}
	
	public override void UpdateMe()
	{
		if (ammoRemains <= 0)
		{
			Usable = false;
		}
	}
}