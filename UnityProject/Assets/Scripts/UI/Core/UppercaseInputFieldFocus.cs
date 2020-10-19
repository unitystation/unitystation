using UnityEngine;

/// <summary>
/// Converts all entered lowercase chars to uppercase
/// </summary>
public class UppercaseInputFieldFocus : InputFieldFocus
{
	protected override void Start()
	{
		base.Start();
		onValidateInput += ( input, index, addedChar ) => char.ToUpper( addedChar );
	}
}