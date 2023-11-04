using System.Collections.Generic;
using Systems.Research.Data;
using Systems.Research.ImporterExporter;
using System;
using UnityEngine.Events;

namespace Systems.Research
{

	public class Techweb
	{
		public List<string> TestedPrefabs = new List<string>();

		public List<TechWebNode> nodes = new List<TechWebNode>();
		public int researchPoints = 0;

		public List<Technology> ResearchedTech { get; private set; } = new List<Technology>();
		public List<Technology> AvailableTech { get; private set; } = new List<Technology>();
		public List<Technology> FutureTech { get; private set; } = new List<Technology>();

		public List<string> AvailableDesigns { get; private set; } = new List<string>();

		public List<string> researchedSliverIDs = new List<string>();
		private HashSet<string> researchedTechIDs = new HashSet<string>();

		public TechType ResearchFocus { get; private set; } = TechType.None;
		private const float FOCUS_DISCOUNT = 0.25f; //25% discount on focused technologies

		//UI
		public delegate void UIUpdate();
		public UIUpdate UIupdate;

		[NonSerialized] public Action<int, List<string>> TechWebDesignUpdateEvent;

		public void Merge(Techweb techWebToMerge, bool mergeResearchPoints = false)
		{
			nodes.AddRange(techWebToMerge.nodes);
			ResearchedTech.AddRange(techWebToMerge.ResearchedTech);
			GenerateResearchedIDList();
			UpdateTechnologyLists();
			if (mergeResearchPoints) researchPoints += techWebToMerge.researchPoints;
		}

		public void LoadTechweb(string filePath)
		{
			researchPoints = 0;
			var importer = new TechwebJSONImporter();
			var techweb = importer.Import(filePath);
			Merge(techweb);
		}

		public List<TechWebNode> GetNodes()
		{
			return nodes;
		}

		public void SetResearchFocus(TechType focus)
		{
			ResearchFocus = focus;

			foreach(TechWebNode node in nodes)
			{
				if (node.technology.techType == ResearchFocus) node.technology.ResearchCosts =
					(int)(node.technology.ResearchCosts * (1 - FOCUS_DISCOUNT)); //Discounts technology by discount amount
			}

			UpdateTechnologyLists();
		}

		public bool ResearchTechology(Technology technologyToResearch, bool updateUI = true)
		{
			if(researchPoints < technologyToResearch.ResearchCosts) return false;
			UnlockTechnology(technologyToResearch, updateUI);
			researchPoints -= technologyToResearch.ResearchCosts;

			return true;
		}

		public void UnlockTechnology(Technology technologyToResearch, bool updateUI = true)
		{
			ResearchedTech.Add(technologyToResearch);
			researchedTechIDs.Add(technologyToResearch.ID);
			UpdateTechnologyLists();
			UpdateAvailableDesigns();
			if(updateUI) UIupdate?.Invoke();

		}

		private void GenerateResearchedIDList()
		{
			researchedTechIDs.Clear();
			foreach (Technology technology in ResearchedTech)
			{
				researchedTechIDs.Add(technology.ID);
			}
		}

		private void UpdateTechnologyLists()
		{
			AvailableTech.Clear();
			FutureTech.Clear();

			foreach(Technology technology in GetTech())
			{
				if (researchedTechIDs.Contains(technology.ID)) continue;

				bool hasNeededTech = true;
				foreach(string prereq in technology.RequiredTechnologies)
				{
					if (researchedTechIDs.Contains(prereq)) continue;

					hasNeededTech = false;
					break;
				}
				if(hasNeededTech == true) AvailableTech.Add(technology);
				else FutureTech.Add(technology);
			}
		}

		public List<Technology> GetTech()
		{
			List<Technology> technologies = new List<Technology>();
			foreach (var techWebNode in nodes)
			{
				technologies.Add(techWebNode.technology);
			}
			return technologies;
		}

		public List<string> UpdateAvailableDesigns()
		{
			List<string> availableDesigns = AvailableDesigns;

			foreach (Technology tech in ResearchedTech)
			{
				foreach (string str in tech.DesignIDs)
				{
					if (availableDesigns.Contains(str) == false)
					{
						availableDesigns.Add(str);
					}
				}
			}

			AvailableDesigns = availableDesigns;
			TechWebDesignUpdateEvent?.Invoke(1, AvailableDesigns);

			return availableDesigns;
		}

		public void AddResearchPoints(int points)
		{
			researchPoints += points;
		}

		public void SubtractResearchPoints(int points)
		{
			researchPoints = Math.Max(researchPoints - points, 0);
		}
	}
}
