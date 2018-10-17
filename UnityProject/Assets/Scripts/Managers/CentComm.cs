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
		yield return new WaitForSeconds(60f);

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
		var commConsoles = FindObjectsOfType<CommConsole>();
		foreach (CommConsole console in commConsoles)
		{
			var p = ItemFactory.SpawnItem(ItemFactory.Instance.paper, console.transform.position, console.transform.parent);
			var paper = p.GetComponent<Paper>();
			paper.SetServerString(CreateStartGameReport());
		}
	}

	private string CreateStartGameReport()
	{
		string report = "<size=38>CentComm Report</size> \n __________________________________ \n \n" +
			" <size=26>Asteroid bodies have been sighted in the local area around " +
			"OutpostStation IV. Locate and exploit local sources for plasma deposits.</size>\n \n " +
			"<color=blue><size=32>Crew Objectives:</size></color>\n \n <size=24>- Locate and mine " +
			"local Plasma Deposits\n \n - Fulfill order of " + PlasmaOrderRequestAmt + " Solid Plasma units and dispatch to " +
			"Central Command via Cargo Shuttle</size>\n \n <size=32>Latest Asteroid Sightings:" +
			"</size>\n \n";

		for (int i = 0; i < AsteroidLocations.Count; i++)
		{
			report += " <size=24>" + Vector2Int.RoundToInt(AsteroidLocations[i]).ToString() + "</size> ";
		}

		return report;
	}
}