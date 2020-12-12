using UnityEngine;
using UnityEngine.UI;

public class PlayerListUI : MonoBehaviour
{
	public Text nameList;
	public GameObject window;

	public ScrollRect scrollRect;

	[SerializeField]
	private Text roundStatus = default;
	[SerializeField]
	private Text currentMap = default;
	[SerializeField]
	private Text gameMode = default;

	void OnEnable()
	{
		Invoke("SetScrollToTop",0.1f);

		EventManager.AddHandler(EVENT.PreRoundStarted, OnPreRoundStarted);
		EventManager.AddHandler(EVENT.MatrixManagerInit, OnMapInit);
		EventManager.AddHandler(EVENT.RoundStarted, OnRoundStarted);
		EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnded);
	}

	void SetScrollToTop()
	{
		scrollRect.verticalScrollbar.value = 1f;
	}

	private void OnPreRoundStarted()
	{
		UpdateRoundStatus();
	}

	private void OnMapInit()
	{
		UpdateCurrentMap();
	}

	private void OnRoundStarted()
	{
		UpdateRoundStatus();
		UpdateGameMode();
	}

	private void OnRoundEnded()
	{
		UpdateRoundStatus();
	}

	private void UpdateRoundStatus()
	{
		if (roundStatus == null) return;

		roundStatus.text = GameManager.Instance.CurrentRoundState.ToString();
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
