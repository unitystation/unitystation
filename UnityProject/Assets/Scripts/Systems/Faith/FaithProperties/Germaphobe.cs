﻿using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Germaphobe : IFaithProperty
	{
		[SerializeField] private string faithPropertyName = "Germaphobe";
		[SerializeField] private string faithPropertyDesc = "People of this faith cannot stand filth and trash.";
		[SerializeField] private Sprite propertyIcon;
		[SerializeField] private ItemTrait filthTrait;
		[SerializeField] private ItemTrait breadLoafTrait;
		[SerializeField] private List<GameObject> antHills = new List<GameObject>();
		[SerializeField] private List<GameObject> spores = new List<GameObject>();
		[SerializeField] private List<GameObject> KillerBread = new List<GameObject>();

		string IFaithProperty.FaithPropertyName
		{
			get => faithPropertyName;
			set => faithPropertyName = value;
		}

		string IFaithProperty.FaithPropertyDesc
		{
			get => faithPropertyDesc;
			set => faithPropertyDesc = value;
		}

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}

		public FaithData AssociatedFaith { get; set; }

		private int FilthScore = 0;

		public void Setup(FaithData data)
		{
			FaithManager.Instance.FaithPropertiesEventUpdate.Add(CheckFilthLevels);
			CheckFilthLevels();
			AssociatedFaith = data;
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			Chat.AddExamineMsg(newMember.gameObject, "A part of you can't tolerate nearby filth anymore.");
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			Chat.AddExamineMsg(member.gameObject, "A part of you feels indifferent about nearby filth now.");
		}

		public void RandomEvent()
		{
			CheckFilthLevels();
		}

		private void CheckFilthLevels()
		{
			//Only grabs objects that are on main station and have a filth trait.
			//Shuffles the list around so when we spawn stuff, they don't get spawned next to each other.
			var filth =
				ComponentsTracker<Attributes>.Instances.Where(x
					=> x.InitialTraits.Contains(filthTrait) &&
					   x.gameObject.RegisterTile().Matrix == MatrixManager.MainStationMatrix.Matrix).Shuffle().ToList();

			var total = filth.Count();
			if (FilthScore == 0)
			{
				FilthScore = total;
				return;
			}
			//if the total mess of the station has decreased or is still the same,
			//slightly decrease the score penalty and do nothing.
			if (total <= FilthScore)
			{
				FilthScore = (int)(FilthScore / 1.1f);
				return;
			}
			FilthScore = total;
			SpawnStuff(ref filth);
		}

		private void SpawnStuff(ref List<Attributes> filth)
		{
			if (FilthScore > 175)
			{
				SpawnAnts(ref filth);
			}
			if (FilthScore > 400)
			{
				MoldBread();
			}
			if (FilthScore >= 650)
			{
				SpawnSpores(ref filth);
			}
		}

		private void SpawnAnts(ref List<Attributes> filth)
		{
			var spotToPick = filth.PickRandom();
			if (spotToPick is null) return;
			Spawn.ServerPrefab(antHills.PickRandom(), spotToPick.gameObject.AssumedWorldPosServer());
		}

		private void SpawnSpores(ref List<Attributes> filth)
		{
			var spotsToPick = filth.PickRandom(2);
			foreach (var spot in spotsToPick)
			{
				Spawn.ServerPrefab(spores.PickRandom(), spot.gameObject.AssumedWorldPosServer());
			}
		}

		private void MoldBread()
		{
			var bread =
				ComponentsTracker<Attributes>.Instances.Where(x => x.InitialTraits.Contains(breadLoafTrait)).ToList();
			if (bread.Count < 5) return;
			bread = bread.PickRandom(2).ToList();
			foreach (var breadItem in bread)
			{
				Spawn.ServerPrefab(KillerBread.PickRandom(), breadItem.gameObject.AssumedWorldPosServer());
				_ = Despawn.ServerSingle(breadItem.gameObject);
			}
		}

	}
}