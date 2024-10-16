using HealthV2;
using UnityEngine;

public class SetBodyType : MonoBehaviour
{
	public BodyType ToSetTo;
	private BodyPart bodyPart;

	public void Awake()
	{
		bodyPart = GetComponent<BodyPart>();
		bodyPart.OnAddedToBody += UpdateBodyType;
	}

	public void Start()
	{
		UpdateBodyType(bodyPart.HealthMaster);
	}

	public void UpdateBodyType(LivingHealthMasterBase livingHealth)
	{
		if (livingHealth == null) return;

		var sprites = livingHealth.playerSprites;
		sprites.SetAllBodyType(ToSetTo);
	}
}
