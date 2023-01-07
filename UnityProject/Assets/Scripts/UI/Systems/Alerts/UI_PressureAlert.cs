public class UI_PressureAlert : TooltipMonoBehaviour
{
	public override string Tooltip => (activeImageIndex < 2) ? "Low Pressure" : "High Pressure";

	private int activeImageIndex = -1;
	private SpriteHandler spriteHandler;

	private void Awake()
	{
		spriteHandler = GetComponent<SpriteHandler>();
	}

	public void SetPressureSprite(PressureAlert pressure)
	{
		switch (pressure)
		{
			case PressureAlert.PressureTooLow:
				this.gameObject.SetActive(true);
				SetSprite(0);	//really low pressure
				break;
			case PressureAlert.PressureLow:
				this.gameObject.SetActive(true);
				SetSprite(1);	//low pressurec
				break;
			case PressureAlert.None:
				this.gameObject.SetActive(false);
				break;
			case PressureAlert.PressureHigher:
				this.gameObject.SetActive(true);
				SetSprite(2);	//high pressure
				break;
			case PressureAlert.PressureTooHigher:
				this.gameObject.SetActive(true);
				SetSprite(3);	 //really high pressure
				break;
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

public enum PressureAlert
{
	None,
	PressureTooLow,
	PressureLow,
	PressureHigher,
	PressureTooHigher
}
