using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

///------------
/// CENTRAL COMMAND HQ
///------------
public class CentComm : MonoBehaviour
{
	public GameManager gameManager;

	//Server only:
	private List<Vector2> AsteroidLocations = new List<Vector2>();
	private int PlasmaOrderRequestAmt;

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		AsteroidLocations.Clear();
		if (!scene.name.Contains("Lobby"))
		{
			StartCoroutine(WaitToPrepareReport());
		}
	}

	IEnumerator WaitToPrepareReport()
	{
		yield return YieldHelper.EndOfFrame; //OnStartServer starts one frame after OnLevelFinishedLoading
		//Server only:
		if (!CustomNetworkManager.Instance._isServer)
		{
			yield break;
		}
		//Wait some time after the round has started
		yield return new WaitForSeconds(20f);

		//Gather asteroid locations:
		for (int i = 0; i < gameManager.SpaceBodies.Count; i++)
		{
			var asteroid = gameManager.SpaceBodies[i].GetComponent<Asteroid>();
			if (asteroid != null)
			{
				AsteroidLocations.Add(gameManager.SpaceBodies[i].State.Position);
			}
		}

		//Add in random positions
		int randomPosCount = Random.Range(1, 5);
		for (int i = 0; i <= randomPosCount; i++)
		{
			AsteroidLocations.Add(gameManager.RandomPositionInSolarSystem());
		}

		//Shuffle the list:
		AsteroidLocations = AsteroidLocations.OrderBy(x => Random.value).ToList();

		//Determine Plasma order:
		PlasmaOrderRequestAmt = Random.Range(5, 50);
		SendReportToStation();
	}

	private void SendReportToStation()
	{
		//TODO print report out on all command consoles:
		foreach (Vector2 pos in AsteroidLocations)
		{
			Debug.Log(pos);
		}

		Debug.Log("Plasma Order Amount: " + PlasmaOrderRequestAmt);
	}
}