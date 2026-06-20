using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.HealthV2.Living;
using US13.Managers.UpdateManager;
using US13.Objects.Engineering;
using US13.Systems.Electricity.Interfaces;
using Util;

namespace US13.Objects.Medical
{
	public class MedBed : MonoBehaviour, IAPCPowerable
	{
		[System.Serializable]
		public class ReagentReGenAndCap
		{
			public Reagent Reagent;
			public  float ReagentRegenerationSecond;
			[field: SerializeField] public virtual float ReagentCap { get; set; }
			[field: SerializeField] public virtual float  CurrentReagents { get; set; }
		}

		[System.Serializable]
		public class ReagentContainerReGenAndCap : ReagentReGenAndCap
		{
			public ReagentContainer ReagentContainer;
			public float ReagentRegenerationSecond;
			public override float ReagentCap => ReagentContainer.MaxCapacity;
			public override float  CurrentReagents => ReagentContainer.Total;
		}

		public List<ReagentReGenAndCap> ReagentsGen = new List<ReagentReGenAndCap>();

		public ReagentContainer CustomBuffer;


		public LivingHealthMasterBase LivingHealthMasterBase;

		public GUI_MedBed GUI_MedBed;

		public ObjectContainer ObjectContainer;

		public bool power = false;

		public void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 1f);

		}

		public void PowerNetworkUpdate(float voltage)
		{

		}

		public void StateUpdate(PowerState state)
		{
			switch (state)
			{

				case PowerState.Disconnected:
				case PowerState.Off:
				case PowerState.LowVoltage:
					power = false;
					break;
				case PowerState.On:
				case PowerState.OverVoltage:
					power = true;
					break;
			}
		}

		public void OnDisable()
		{
			UpdateManager.Remove( CallbackType.PERIODIC_UPDATE,  UpdateMe);
		}

		public void RegisterGUI(GUI_MedBed GUI_MedBed)
		{
			this.GUI_MedBed = GUI_MedBed;
		}


		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			CustomBuffer = this.GetCachedComponent<ReagentContainer>();
			ObjectContainer = this.GetCachedComponent<ObjectContainer>();
			ObjectContainer.OnObjectStored.AddListener(StoreObject);
			ObjectContainer.OnObjectRetrieved.AddListener(LossObject);
			ReagentsGen.Add(new ReagentContainerReGenAndCap()
			{
				ReagentContainer = CustomBuffer
			});

		}

		public void StoreObject(GameObject GameObject)
		{
			var Health = GameObject.GetComponent<LivingHealthMasterBase>();
			if (LivingHealthMasterBase == null)
			{
				LivingHealthMasterBase = Health;
			}
		}

		public void LossObject(GameObject GameObject)
		{
			var Health = GameObject.GetComponent<LivingHealthMasterBase>();

			if (LivingHealthMasterBase == Health)
			{
				LivingHealthMasterBase = null;
			}
		}

		// Update is called once per frame
		void UpdateMe()
		{
			if (power)
			{
				foreach (var ReagentGen in ReagentsGen)
				{
					if (ReagentGen.Reagent == null) continue;
					if ((ReagentGen.CurrentReagents >= ReagentGen.ReagentCap) == false)
					{
						ReagentGen.CurrentReagents += ReagentGen.ReagentRegenerationSecond;
					}
				}

			}

			GUI_MedBed?.UpdateDisplay();
		}

		public void InjectReagent(ReagentReGenAndCap ReagentReGenAndCap, float ToInject)
		{
			if (ReagentReGenAndCap.Reagent == null)
			{
				if (ToInject > ReagentReGenAndCap.CurrentReagents)
				{
					ToInject =  ReagentReGenAndCap.CurrentReagents;
				}

				var Reagents = CustomBuffer.TakeReagents(ToInject);
				if (LivingHealthMasterBase != null)
				{
					LivingHealthMasterBase.reagentPoolSystem.BloodPool.Add(Reagents);
				}
				else
				{
					if (ToInject > 0)
					{
						Chat.AddActionMsgToChat(this.gameObject, "Leaks chemicals all over the medical bed");
					}

				}

			}
			else
			{
				if (ToInject > ReagentReGenAndCap.CurrentReagents)
				{
					ToInject =  ReagentReGenAndCap.CurrentReagents;
					ReagentReGenAndCap.CurrentReagents = 0;
				}
				else
				{
					ReagentReGenAndCap.CurrentReagents -= ToInject;
				}

				if (LivingHealthMasterBase != null)
				{
					LivingHealthMasterBase.reagentPoolSystem.BloodPool.Add(ReagentReGenAndCap.Reagent, ToInject);
				}
				else
				{
					if (ToInject > 0)
					{
						Chat.AddActionMsgToChat(this.gameObject, "Leaks chemicals all over the medical bed");
					}

				}
			}

			GUI_MedBed?.UpdateDisplay();
		}
	}
}

