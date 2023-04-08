using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "CommonAlertSOs", menuName = "Singleton/CommonAlertSOs")]
public class CommonAlertSOs : SingletonScriptableObject<CommonAlertSOs>
{
	public AlertSO Full;
	public AlertSO Hungry;
    public AlertSO Malnourished;
    public AlertSO Starving;
}
