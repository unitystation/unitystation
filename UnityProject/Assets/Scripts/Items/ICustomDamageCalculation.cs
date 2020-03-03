
/// <summary>
/// Base for customized damage calculations when performing a melee attack.
/// Is called when ItemAttributesV2 gets the damage of an item.
/// </summary>
public interface ICustomDamageCalculation
{

	//Set new damage.
	int ServerPerformDamageCalculation();
}
