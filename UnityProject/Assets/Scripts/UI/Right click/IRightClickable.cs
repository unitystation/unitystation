
using System;
using System.Collections.Generic;

/// <summary>
/// Indicates that a component has some right click menu options that might be generated
/// when its object is right clicked.
/// </summary>
public interface IRightClickable
{
	/// <summary>
	/// Generate the right click menu options currently available to be performed. Invoked
	/// when this component's object is right clicked to determine current available actions.
	/// </summary>
	/// <returns>a dictionary from option to the action that should be invoked when the option
	/// is selected.</returns>
	Dictionary<RightClickOption, Action> GenerateRightClickOptions();

}
