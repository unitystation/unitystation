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


    public AlertSO Bleeding_VeryLow;
    public AlertSO Bleeding_Low;
    public AlertSO Bleeding_Medium;
    public AlertSO Bleeding_High;
    public AlertSO Bleeding_UhOh;

    public AlertSO HasFireStacks;


    public AlertSO Temperature_TooHot;
    public AlertSO Temperature_Hot;
    public AlertSO Temperature_Cold;
    public AlertSO Temperature_TooCold;

    public AlertSO Pressure_TooLow;
    public AlertSO Pressure_Low;
    public AlertSO Pressure_Higher;
    public AlertSO Pressure_TooHigher;

    public AlertSO CellMostlycharge;
    public AlertSO CellMidcharge;
    public AlertSO CellLowcharge;
    public AlertSO CellDeadcharge;

    public AlertSO CellMissing;

    public AlertSO CellCharging;
}
