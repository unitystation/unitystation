using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Systems.AdminTools
{
	public class JobBanListItem : MonoBehaviour
	{
		public GameObject bannedStatus = null;
		public GameObject unbannedStatus = null;

		public TMP_Text jobName = null;
		public TMP_Text banTime = null;

		public Toggle toBeBanned = null;
	}
}
