/// <summary>
/// Implement this on a component to cause that component to do something when electrocuted.
/// It's up to the component what it does in response to electrocution.
/// </summary>
public interface IElectrocutable
{
	/// <summary>
	/// Expose this component to electrocution.
	/// </summary>
	/// <param name="exposure">information on the exposure</param>
	ElectrocutionSeverity Electrocute(Electrocution exposure);
}
