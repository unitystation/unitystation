using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropDownSelectorItem : MonoBehaviour
{
	public TMP_Text Text;

	public MultiDropDownSelector Master;

	public GameObject Tick;

	public void Setup(MultiDropDownSelector inMaster, string INText)
	{
		Master = inMaster;
		Text.text = INText;
		Tick.SetActive(false);
	}

	public void ToggleValue()
	{
		Tick.SetActive(!Tick.activeSelf);
		Master.ValueChanged();
	}

	public void SetValue(bool TheValue)
	{
		Tick.SetActive(TheValue);
	}

	public bool IsActive()
	{
		return Tick.activeSelf;
	}

}
