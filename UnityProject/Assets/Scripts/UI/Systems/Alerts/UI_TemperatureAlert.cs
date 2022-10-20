public class UI_TemperatureAlert : TooltipMonoBehaviour
{
	public override string Tooltip => (activeImageIndex < 3) ? "Too Cold" : "Too Hot";

	private int activeImageIndex = -1;
	private SpriteHandler spriteHandler;

	private void Awake()
	{
		spriteHandler = GetComponent<SpriteHandler>();
	}

	public void SetTemperatureSprite(float temperature)
	{
		if(temperature < AtmosConstants.BARELY_COLD_HEAT)
		{
			if(temperature > AtmosConstants.ABIT_COLD_HEAT)
			{
				SetSprite(2);	// a bit cold
			}
			else if(temperature > AtmosConstants.COLD_HEAT)
			{
				SetSprite(1);	// cold
			}
			else
			{
				SetSprite(0);	// really cold
			}
		}
		else
		{
			if(temperature > AtmosConstants.MELTING_HEAT)
			{
				SetSprite(5);	// superhot
			}
			else if(temperature > AtmosConstants.HOT_HEAT)
			{
				SetSprite(4);	// hot
			}
			else
			{
				SetSprite(3);	// a bit hot
			}
		}
	}

	void SetSprite(int index)
	{
		if(index == activeImageIndex){
			return;
		}

		activeImageIndex = index;
		spriteHandler.ChangeSprite(index, false);
	}
}
