using UnityEngine;

public class Temp_ClothPoolTester : MonoBehaviour
{
	public void SpawnCloth()
	{
		ClothFactory.Instance.CreateCloth("", transform.position);
	}
}