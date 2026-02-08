using UnityEngine;
using US13.HealthV2.Living;
using US13.HealthV2.Living.CirculatorySystem;
using US13.UI.Systems.Lobby;

namespace US13.Items.Others
{
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
}
