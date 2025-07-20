using UnityEngine;

[System.Serializable]
public class TAGType
{
	[SerializeField] private string tagValue;

	public string Value => tagValue;

	public void SetValue(string value)
	{
		tagValue = value;
	}
}