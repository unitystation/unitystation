using UnityEngine.Networking;

public class MagazineBehaviour : NetworkBehaviour
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

	//FIXME: this should be moved to an UpdateMe approach. 
	// 1 manager that updates a list of UpdateMe actions as there may be many magazines
	// in game at some point
	private void Update()
	{
		if (ammoRemains <= 0)
		{
			Usable = false;
		}
	}
}