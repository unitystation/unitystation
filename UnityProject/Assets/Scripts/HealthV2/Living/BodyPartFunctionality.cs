using System;
using Mirror;
using Systems.Explosions;

namespace HealthV2
{
	public class BodyPartFunctionality : NetworkBehaviour, IEmpAble
	{

		[NonSerialized] public BodyPart RelatedPart;
		protected LivingHealthMasterBase LivingHealthMaster => RelatedPart.HealthMaster;


		public virtual void ImplantPeriodicUpdate(){}
		public virtual void OnRemovedFromBody(LivingHealthMasterBase livingHealth){}

		/// <summary>
		/// Warning only add body parts do not remove body parts in this
		/// </summary>
		/// <param name="livingHealth"></param>
		public virtual void OnAddedToBody(LivingHealthMasterBase livingHealth){}
		public virtual void SetUpSystems(){}
		public virtual void InternalDamageLogic() {}
		public virtual void OnTakeDamage(BodyPartDamageData data) {}

		public virtual void OnEmp(int strength)
		{

		}

		public virtual void Awake()
		{
			RelatedPart = GetComponent<BodyPart>();
			if (RelatedPart == null) RelatedPart = GetComponentInParent<BodyPart>();
		}

	}
}
