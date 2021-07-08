using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JobBanListItem : MonoBehaviour
{
	public GameObject bannedStatus = null;
	public GameObject unbannedStatus = null;

	public TMP_Text jobName = null;
	public TMP_Text banTime = null;

	public Toggle toBeBanned = null;
}
