public class UI_PressureAlert : TooltipMonoBehaviour
{
	public override string Tooltip => (activeImageIndex < 2) ? "Low Pressure" : "High Pressure";

	private int activeImageIndex = -1;
	private SpriteHandler spriteHandler;

	private void Awake()
	{
		spriteHandler = GetComponent<SpriteHandler>();
	}

	public void SetPressureSprite(float pressure)
	{
		if (pressure < 50)
		{
			if (pressure > 20)
			{
				SetSprite(1);	//low pressure
			}
			else
			{
				SetSprite(0);	//really low pressure
			}
		}
		else
		{
			if (pressure > 550)
			{
				SetSprite(3);	//really high pressure
			}
			else
			{
				SetSprite(2);	//high pressure
			}
		}
	}

	void SetSprite(int index)
	{
		if (index == activeImageIndex)
		{
			return;
		}

		activeImageIndex = index;
		spriteHandler.ChangeSprite(index, false);
	}
}
