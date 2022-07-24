using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items.Weapons;
using Systems.Electricity;
using Systems.ObjectConnection;
using Systems.Research.Objects;
using UI.Core.Net;
using UnityEngine;

namespace Research
{
	public class BlastYieldDetector : MonoBehaviour, IAPCPowerable, IMultitoolSlaveable, ICanOpenNetTab
	{
		public ResearchServer researchServer;

		public float range;
		[SerializeField] public OrientationEnum coneDirection;

		/// <summary>
		/// Randomized blast yield target for awarding maximum points, initialized from research server
		/// </summary>
		private int maxPointYieldTarget;

		/// <summary>
		/// Randomized blast yield target for awarding easier points, initialized from research server
		/// </summary>
		private int easyPointYieldTarget;

		/// <summary>
		/// Points awardable for reaching the more difficult blast yield target
		/// </summary>
		public int maxPointsValue;

		/// <summary>
		/// Points awardable for reaching the easier blast yield target
		/// </summary>
		public int easyPointsValue;

		/// <summary>
		/// Data structure for blast data, sorted so that scrolling through the graph makes sense
		/// </summary>
		public SortedList<float,float> blastData;

		protected RegisterObject registerObject;

		public static int highestExplosionPointValueCurrent;

		public delegate void ChangeEvent();

		public static event ChangeEvent changeEvent;

		private void UpdateGui()
		{
			// Change event runs UpdateNodes in GUI_BlastYieldDetector
			if (changeEvent != null)
			{
				changeEvent();
			}
		}

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();

			ExplosiveBase.ExplosionEvent.AddListener(DetectBlast);
			blastData = new SortedList<float, float>();
			GetYieldTargets();
		}

		/// <summary>
		/// Obtains Yield targets from Research.
		/// </summary>
		private void GetYieldTargets()
		{
			researchServer.SetBlastYieldTargets();
			maxPointYieldTarget = researchServer.hardBlastYieldDetectorTarget;
			easyPointYieldTarget = researchServer.easyBlastYieldDetectorTarget;
		}

		/// <summary>
		/// Checks if an explosion happens within the detection cone. If the explosion takes
		/// place within the cone, the point value is checked and compared to previous alloted points.
		/// </summary>
		/// <param name="pos">Position of given explosion to check.</param>
		/// <param name="explosiveStrength">Blast yield of the given explosion.</param>
		private void DetectBlast(Vector3Int pos, float explosiveStrength)
		{
			Vector2 thisMachine = registerObject.WorldPosition.To2Int();

			float distance = Vector2.Distance(pos.To2Int(), thisMachine);
			//Distance is checked first to potentially avoid calculations.
			if (distance > range) return;

			//Math to check for if our explosion falls within a certain angle away from the center of the cone
			Vector2 coneToQuery = pos.To2Int() - thisMachine;
			coneToQuery.Normalize();

			Vector2 coneCenterVector = coneDirection.ToLocalVector2Int();
			coneCenterVector.Normalize();

			float angle = Math.Abs((Mathf.Acos(Vector2.Dot(coneToQuery, coneCenterVector)) * 180) / Mathf.PI);

			int points = calculateResearchPoints(explosiveStrength);

			if (angle <= 45)
			{
				blastData.Add(explosiveStrength, points);
				AwardResearchPoints(points);
			}

			UpdateGui();
		}

		/// <summary>
		/// Awards points by how much higher current explosion points are than the current highest. E.G. if current
		/// highest is 35 and a new explosion reaches 40 points, then 5 points are awarded. This puts an effective cap on
		/// possible points gained from ordnance, based on points put in from the formula in calculateResearchPoints().
		/// </summary>
		/// <param name="points"></param>
		private void AwardResearchPoints(int points)
		{
			if (points > highestExplosionPointValueCurrent)
			{
				Chat.AddLocalMsgToChat($"Research points awarded: {points.ToString()}", gameObject);
				researchServer.AddResearchPoints(points - highestExplosionPointValueCurrent);
				highestExplosionPointValueCurrent = points;
			}
			else
			{
				Chat.AddLocalMsgToChat(
					$"Explosion strength not close enough to yield target to award additional research.",
					gameObject);
			}
		}

		/// <summary>
		/// Determines the research point value of an explosion based on the following formula.
		/// [ max(e^(-(x - a1)^2/1250000)×140, e^(-(x - a2)^2/5000000)×70) ]
		/// a1 and a2 are the per round randomised values set between 1000 and 19000, modelled after minimum and maximum
		/// values for ExplosionBase ExplosionStrengths, that are targets to reach for blast yield.
		/// </summary>
		/// <param name="explosiveStrength"></param>
		/// <returns></returns>
		private int calculateResearchPoints(float explosiveStrength)
		{
			float term1 = (float)Math.Exp(-Math.Pow(explosiveStrength - maxPointYieldTarget, 2) / 1250000) *
			              maxPointsValue;
			float term2 = (float)Math.Exp(-Math.Pow(explosiveStrength - easyPointYieldTarget, 2) / 5000000) *
			              easyPointsValue;
			return (int)Mathf.Max(term1, term2);
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.ResearchServer;
		IMultitoolMasterable IMultitoolSlaveable.Master => researchServer;
		bool IMultitoolSlaveable.RequireLink => false;

		bool IMultitoolSlaveable.TrySetMaster(PositionalHandApply interaction, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			if (master is ResearchServer server)
			{
				SubscribeToServerEvent(server);
			}
			else if (researchServer != null)
			{
				UnSubscribeFromServerEvent();
			}
		}

		private void SubscribeToServerEvent(ResearchServer server)
		{
			UnSubscribeFromServerEvent();
			ExplosiveBase.ExplosionEvent.AddListener(DetectBlast);
			researchServer = server;
			GetYieldTargets();
		}

		private void UnSubscribeFromServerEvent()
		{
			if (researchServer == null) return;
			ExplosiveBase.ExplosionEvent.RemoveListener(DetectBlast);
			researchServer = null;
			easyPointYieldTarget = 0;
			maxPointYieldTarget = 0;
		}

		#endregion

		#region IAPCPowerable, ICanOpenNetTab

		public PowerState PoweredState;

		public void PowerNetworkUpdate(float voltage)
		{
		}

		public void StateUpdate(PowerState state)
		{
			if (state == PowerState.Off)
			{
				//Machine loses connection to server on power loss
				UnSubscribeFromServerEvent();
			}

			PoweredState = state;
		}

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			if (PoweredState == PowerState.Off)
			{
				Chat.AddExamineMsgFromServer(playerObject, $"{gameObject.ExpensiveName()} is unpowered");
				return false;
			}
			return true;
		}

		#endregion

	}
}