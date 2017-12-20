using UnityEngine;

[ExecuteInEditMode]
public class FloorTile : MonoBehaviour
{
	public GameObject ambientTile;
	public GameObject fireScorch;

	public void AddFireScorch()
	{
		if (fireScorch == null)
		{
			//Do poolspawn here
			fireScorch = EffectsFactory.Instance.SpawnScorchMarks(transform);
		}
	}

	private void Start()
	{
		CheckAmbientTile();
	}

	public void CheckAmbientTile()
	{
		if (ambientTile == null)
		{
			ambientTile = Instantiate(Resources.Load("AmbientTile") as GameObject, transform.position,
				Quaternion.identity, transform);
		}
	}

	public void CleanTile()
	{
		if (fireScorch != null)
		{
			fireScorch.transform.parent = null;
			PoolManager.Instance.PoolClientDestroy(fireScorch);
		}
	}
}