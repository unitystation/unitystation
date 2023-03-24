using System;
using Mirror;
using Systems.Explosions;
using NaughtyAttributes;

namespace HealthV2
{
	public class BodyPartFunctionality : NetworkBehaviour, IEmpAble
	{

		[NonSerialized]
		public BodyPart RelatedPart;


		public virtual void ImplantPeriodicUpdate(){}
		public virtual void OnRemovedFromBody(LivingHealthMasterBase livingHealth){}
		public virtual void OnAddedToBody(LivingHealthMasterBase livingHealth){} //Warning only add body parts do not remove body parts in this
		public virtual void SetUpSystems(){}
		public virtual void InternalDamageLogic() {}
		public virtual void OnTakeDamage(BodyPartDamageData data) {}

		public virtual void OnEmp(int strength)
		{

		}

		public virtual void Awake()
		{
			RelatedPart = GetComponent<BodyPart>();
			if (RelatedPart) RelatedPart = GetComponentInParent<BodyPart>();
		}

	}
}
