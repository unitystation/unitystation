using Shared.Managers;
using UnityEngine;
using UnityEngine.UI;

public class StatsTab : SingletonManager<StatsTab>
{
	public GameObject window;

	[SerializeField] private Text roundStatus = default;
	[SerializeField] private Text currentMap = default;
	[SerializeField] private Text gameMode = default;
	[SerializeField] private Text serverFPS = default;
	[SerializeField] private Text roundTimer;

	void OnEnable()
	{
		EventManager.AddHandler(Event.PreRoundStarted, OnPreRoundStarted);
		EventManager.AddHandler(Event.MatrixManagerInit, OnMapInit);
		EventManager.AddHandler(Event.RoundStarted, OnRoundStarted);
		EventManager.AddHandler(Event.RoundEnded, OnRoundEnded);
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	public override void OnDestroy()
	{
		EventManager.RemoveHandler(Event.PreRoundStarted, OnPreRoundStarted);
		EventManager.RemoveHandler(Event.MatrixManagerInit, OnMapInit);
		EventManager.RemoveHandler(Event.RoundStarted, OnRoundStarted);
		EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnded);
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		base.OnDestroy();
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public void UpdateMe()
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

	public void UpdateRoundStatus(string text)
	{
		if (roundStatus == null) return;

		roundStatus.text = text;
	}

	public void UpdateCurrentMap()
	{
		if (roundStatus == null) return;

		currentMap.text = MatrixManager.MainStationMatrix.GameObject.scene.name;
	}

	public void UpdateGameMode()
	{
		if (roundStatus == null) return;

		gameMode.text = GameManager.Instance.GetGameModeName();
	}

	public void UpdateRoundTime()
	{
		if(roundTimer == null) return;
		roundTimer.text = GameManager.Instance.RoundTime.ToShortTimeString();
	}
}
