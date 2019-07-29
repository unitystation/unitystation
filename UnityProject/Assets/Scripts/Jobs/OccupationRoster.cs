using UnityEngine;
using System.Collections.Generic;

public class OccupationRoster : MonoBehaviour
{
	public int limit;
	public int jobsTaken;
	public int priority = 99;
	public JobType jobType;

	public string accessory;
	public List<Access> allowedAccess;

	public string head;
	public string glasses;
	public string ears;
	public string mask;
	public string neck;
	public string exosuit;
	public string suitStorage;
	public string uniform;
	public string leftPocket;
	public string rightPocket;
	public string belt;
	public string gloves;
	public string leftHand;
	public string shoes;

	public string backpack;
	public string duffelbag;
	public string satchel;
	public string box;
	public List<string> backpack_contents = new List<string>();
}