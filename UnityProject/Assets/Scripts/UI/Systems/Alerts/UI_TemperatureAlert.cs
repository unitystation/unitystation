public class UI_TemperatureAlert : TooltipMonoBehaviour
{
	public override string Tooltip => (activeImageIndex < 3) ? "Too Cold" : "Too Hot";

	private int activeImageIndex = -1;
	private SpriteHandler spriteHandler;

	private void Awake()
	{
		spriteHandler = GetComponent<SpriteHandler>();
	}

	public void SetTemperatureSprite(TemperatureAlert temperature)
	{
		switch (temperature)
		{
			case TemperatureAlert.TooCold:
				this.gameObject.SetActive(true);
				SetSprite(0);	//Really cold
				break;
			case TemperatureAlert.Cold:
				this.gameObject.SetActive(true);
				SetSprite(1);	//Cold
				break;
			case TemperatureAlert.None:
				this.gameObject.SetActive(false);
				break;
			case TemperatureAlert.Hot:
				this.gameObject.SetActive(true);
				SetSprite(4);	//Hot
				break;
			case TemperatureAlert.TooHot:
				this.gameObject.SetActive(true);
				SetSprite(5);	 //Too hot
				break;
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

public enum TemperatureAlert
{
	TooCold,
	Cold,
	None,
	Hot,
	TooHot
}
