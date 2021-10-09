using System;
using UnityEngine;
using UnityEngine.UI;

public class StatsTab : MonoBehaviour
{
	public GameObject window;

	[SerializeField]
	private Text roundStatus = default;
	[SerializeField]
	private Text currentMap = default;
	[SerializeField]
	private Text gameMode = default;
	[SerializeField]
	private Text serverFPS = default;

	void OnEnable()
	{
		Invoke("SetScrollToTop",0.1f);

		EventManager.AddHandler(Event.PreRoundStarted, OnPreRoundStarted);
		EventManager.AddHandler(Event.MatrixManagerInit, OnMapInit);
		EventManager.AddHandler(Event.RoundStarted, OnRoundStarted);
		EventManager.AddHandler(Event.RoundEnded, OnRoundEnded);
	}

	public void Update()
	{
		serverFPS.text = $"{GameManager.Instance.ServerCurrentFPS} - avg: {GameManager.Instance.ServerAverageFPS}";
	}

	private void OnPreRoundStarted()
	{
		UpdateRoundStatus("starting soon");
	}

	private void OnMapInit()
	{
		UpdateCurrentMap();
	}

	private void OnRoundStarted()
	{
		UpdateRoundStatus("started");
		UpdateGameMode();
	}

	private void OnRoundEnded()
	{
		UpdateRoundStatus("ended, restarting soon");
	}

	private void UpdateRoundStatus(string text)
	{
		if (roundStatus == null) return;

		roundStatus.text = text;
	}

	private void UpdateCurrentMap()
	{
		if (roundStatus == null) return;

		currentMap.text = MatrixManager.MainStationMatrix.GameObject.scene.name;
	}

	private void UpdateGameMode()
	{
		if (roundStatus == null) return;

		gameMode.text = GameManager.Instance.GetGameModeName();
	}
}
