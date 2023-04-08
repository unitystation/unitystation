using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "AlertSOs", menuName = "Singleton/AlertSOs")]
public class AlertSOs : SingletonScriptableObject<AlertSOs>
{
	public List<AlertSO> AllAlertSOs = new List<AlertSO>();


}
