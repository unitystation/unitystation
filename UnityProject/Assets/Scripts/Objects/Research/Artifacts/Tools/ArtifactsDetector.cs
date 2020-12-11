using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Basic scanner that prints position of artifacts nearby
/// </summary>
public class ArtifactsDetector : MonoBehaviour, ICheckedInteractable<HandActivate>
{
	[Tooltip("How many artifacts will be printed in report")]
	[SerializeField]
	private int maxArtifactsInReport = 3;

	[Tooltip("Max distance for a scanner to detect artifact")]
	[SerializeField]
	private float scanningRadius = 500f;

	public void ServerPerformInteraction(HandActivate interaction)
	{
		// generate human-readable report about artifacts nearby
		var report = GenerateReport();
		// send report to player as examine message
		Chat.AddExamineMsgFromServer(interaction.Performer, report);
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	private IEnumerable<Artifact> FindArtifacts()
	{
		// get all artifacts on scene
		var allArtifacts = Artifact.ServerSpawnedArtifacts;

		// sort all artifacts by distance from detector
		// exclude that outside scanning radius
		// take only first maxArtifactsInReport count
		var detectorPos = gameObject.AssumedWorldPosServer();
		var sortedArtifacts = allArtifacts
			.Select(art => new { art, distance = Vector3.Distance(art.gameObject.AssumedWorldPosServer(), detectorPos) })
			.Where(art => art.distance < scanningRadius)
			.OrderBy(art => art.distance)
			.Take(maxArtifactsInReport)
			.Select(art => art.art);

		return sortedArtifacts;
	}

	private string GenerateReport()
	{
		var strBuilder = new StringBuilder(); 

		// print user position
		var detectorPos = gameObject.AssumedWorldPosServer().To2Int();
		strBuilder.AppendLine($"Your position: {detectorPos}");

		// print found artifacts positions
		var allArtifacts = FindArtifacts();
		foreach (var art in allArtifacts)
		{
			strBuilder.AppendLine($"Anomaly coordinates: {art.gameObject.AssumedWorldPosServer().To2Int()}");
		}

		return strBuilder.ToString();
	}
}
