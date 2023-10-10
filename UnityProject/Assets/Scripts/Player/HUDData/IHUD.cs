using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHUD
{
	public GameObject Prefab { get; set; }

	public GameObject InstantiatedGameObject { get; set; }

	public void SetUp();


	public void SetVisible(bool visible);

}
