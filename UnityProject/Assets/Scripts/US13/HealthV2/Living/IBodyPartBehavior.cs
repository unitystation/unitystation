using UnityEngine;
using US13.HealthV2.Living.BodyParts;

namespace US13.HealthV2.Living
{
	public interface IBodyPartBehavior
	{
		public void Initialize(BodyPartBehaviors owner);
		public void OnInitialize();
		public void OnBehaviorStarted();
		public void OnBehaviorEnabled();
		public void OnBehaviorDisabled();
		public void OnBehaviorDestroyed();
		public BodyPart RelatedPart { get; set; }
		public LivingHealthMasterBase LivingHealthMaster => RelatedPart?.HealthMaster;
		public void ImplantPeriodicUpdate();
		public void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null);
		public void OnAddedToBody(LivingHealthMasterBase livingHealth);
		public void SetUpSystems();
		public void OnTakeDamage(BodyPartDamageData data);
	}
}
