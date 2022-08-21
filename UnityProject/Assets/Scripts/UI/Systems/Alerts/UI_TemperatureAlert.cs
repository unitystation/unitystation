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
		if(temperature < 260)
		{
			if(temperature > 210)
			{
				SetSprite(2);	// a bit cold
			}
			else if(temperature > 160)
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
			if(temperature > 460)
			{
				SetSprite(5);	// superhot
			}
			else if(temperature > 410)
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
