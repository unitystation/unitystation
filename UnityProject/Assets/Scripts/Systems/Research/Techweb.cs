using System.Collections.Generic;
using Systems.Research.Data;
using Systems.Research.ImporterExporter;
using UnityEngine;

namespace Systems.Research
{

	public class Techweb
	{
		public List<TechWebNode> nodes = new List<TechWebNode>();
		public int researchPoints = 0;

		public List<Technology> ResearchedTech { get; private set; } = new List<Technology>();
		public List<Technology> AvailableTech { get; private set; } = new List<Technology>();
		public List<Technology> FutureTech { get; private set; } = new List<Technology>();

		public List<string> researchedSliverIDs = new List<string>();
		private HashSet<string> researchedTechIDs = new HashSet<string>();

		//UI
		public delegate void UIUpdate();
		public UIUpdate UIupdate;

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
			var importer = new TechwebJSONImporter();
			var techweb = importer.Import(filePath);
			Merge(techweb);
		}

		public List<TechWebNode> GetNodes()
		{
			return nodes;
		}

		public bool ResearchTechology(Technology technologyToResearch)
		{
			if(researchPoints < technologyToResearch.ResearchCosts) return false;

			ResearchedTech.Add(technologyToResearch);
			researchedTechIDs.Add(technologyToResearch.ID);
			researchPoints -= technologyToResearch.ResearchCosts;

			UpdateTechnologyLists();
			UIupdate?.Invoke();

			return true;
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
			List<Technology> possibleTechs = GetTech();

			AvailableTech.Clear();
			FutureTech.Clear();		

			foreach(Technology technology in possibleTechs)
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

		public void AddResearchPoints(int points)
		{
			researchPoints += points;
		}

		public void SubtractResearchPoints(int points)
		{
			researchPoints -= points;
		}
	}
}
