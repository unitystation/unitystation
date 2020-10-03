
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
	/// <returns>a RightClickableResult indicating the right click elements that
	/// should be displayed, which may be null or empty if there is nothing to display.</returns>
	RightClickableResult GenerateRightClickOptions();

}
