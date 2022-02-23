using UnityEngine;
using System.Collections.Generic;
using Systems.Research.Data;
using Systems.Research.ImporterExporter;

namespace Systems.Research
{

	public class Techweb : MonoBehaviour
	{
		public List<TechWebNode> nodes = new List<TechWebNode>();
		public int researchPoints = 10000;
		public List<Technology> researchedTech = new List<Technology>();


		public void Merge(Techweb techwebtToMerge, bool mergeResearchPoints = false)
		{
			nodes.AddRange(techwebtToMerge.nodes);
			researchedTech.AddRange(techwebtToMerge.researchedTech);
			if (mergeResearchPoints) researchPoints += techwebtToMerge.researchPoints;
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
