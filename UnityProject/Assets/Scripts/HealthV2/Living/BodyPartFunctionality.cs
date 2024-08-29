using System;
using Core.Admin.Logs;
using Mirror;
using Systems.Explosions;

namespace HealthV2
{
	public class BodyPartFunctionality : NetworkBehaviour, IEmpAble
	{

		public BodyPart RelatedPart;
		protected LivingHealthMasterBase LivingHealthMaster => RelatedPart.HealthMaster;


		public virtual void ImplantPeriodicUpdate(){}

		public virtual void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			AdminLogsManager.AddNewLog(
				null,
				$"{gameObject.ExpensiveName()} has been removed from {livingHealth.gameObject.ExpensiveName()}'s body.",
				LogCategory.MobDamage);
		}

		/// <summary>
		/// Warning only add body parts do not remove body parts in this
		/// </summary>
		/// <param name="livingHealth"></param>
		public virtual void OnAddedToBody(LivingHealthMasterBase livingHealth) { }
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
