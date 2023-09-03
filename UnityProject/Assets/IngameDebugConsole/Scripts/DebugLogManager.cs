using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using SecureStuff;
using UnityEngine.UI;

namespace IngameDebugConsole
{
	/// <summary>
	/// An enum to represent filtered log types
	/// </summary>
	public enum DebugLogFilter
	{
		None = 0,
		Info = 1,
		Warning = 2,
		Error = 4,
		All = 7
	}

	/// <summary>
	/// Receives debug entries and custom events (e.g. Clear, Collapse, Filter by Type)
	/// and notifies the recycled list view of changes to the list of debug entries
	/// </summary>
	/// <remarks>
	/// - Vocabulary -
	/// Debug/Log entry: a Debug.Log/LogError/LogWarning/LogException/LogAssertion request made by
	///                   the client and intercepted by this manager object
	/// Debug/Log item: a visual (uGUI) representation of a debug entry
	///
	/// There can be a lot of debug entries in the system but there will only be a handful of log items
	/// to show their properties on screen (these log items are recycled as the list is scrolled)
	/// </remarks>
	public class DebugLogManager : MonoBehaviour
	{
		private static DebugLogManager instance = null;

		/// <summary>
		/// Debug console will persist between scenes
		/// </summary>
		[Header("Properties")]
		[SerializeField]
		private bool singleton = true;

		/// <summary>
		/// Minimum height of the console window
		/// </summary>
		[SerializeField]
		private float minimumHeight = 200f;

		[SerializeField]
		private bool clearCommandAfterExecution = true;

		[Header("Visuals")]
		[SerializeField]
		private DebugLogItem logItemPrefab = null;

		/// <summary>
		/// Visuals for info log type
		/// </summary>
		[SerializeField]
		private Sprite infoLog = null;

		/// <summary>
		/// Visuals for warning log type
		/// </summary>
		[SerializeField]
		private Sprite warningLog = null;

		/// <summary>
		/// Visuals for error log type
		/// </summary>
		[SerializeField]
		private Sprite errorLog = null;

		private Dictionary<LogType, Sprite> logSpriteRepresentations;

		//Default value warning
#pragma warning disable CS0649
		[SerializeField]
		private Color collapseButtonNormalColor;
		[SerializeField]
		private Color collapseButtonSelectedColor;

		[SerializeField]
		private Color filterButtonsNormalColor;
		[SerializeField]
		private Color filterButtonsSelectedColor;
#pragma warning restore CS0649

		[Header("Internal References")]
		[SerializeField]
		private RectTransform logWindowTR = null;

		private RectTransform canvasTR;

		[SerializeField]
		private RectTransform logItemsContainer = null;

		[SerializeField]
		private InputField commandInputField = null;

		[SerializeField]
		private Image collapseButton = null;

		[SerializeField]
		private Image filterInfoButton = null;
		[SerializeField]
		private Image filterWarningButton = null;
		[SerializeField]
		private Image filterErrorButton = null;

		[SerializeField]
		private Text infoEntryCountText = null;
		[SerializeField]
		private Text warningEntryCountText = null;
		[SerializeField]
		private Text errorEntryCountText = null;

		[SerializeField]
		private GameObject snapToBottomButton = null;
		public FPSCounter fpsCounter;

		/// <summary>
		/// Number of entries filtered by their types
		/// </summary>
		private int infoEntryCount = 0, warningEntryCount = 0, errorEntryCount = 0;

		/// <summary>
		/// Canvas group to modify visibility of the log window
		/// </summary>
		[SerializeField]
		private CanvasGroup logWindowCanvasGroup = null;

		private bool isLogWindowVisible = false;
		private bool screenDimensionsChanged = false;

		[SerializeField]
		private DebugLogPopup popupManager = null;

		[SerializeField]
		private ScrollRect logItemsScrollRect = null;

		/// <summary>
		/// Recycled list view to handle the log items efficiently
		/// </summary>
		[SerializeField]
		private DebugLogRecycledListView recycledListView = null;

		/// <summary>
		/// Determine whether Debug Log is collapsed
		/// </summary>
		private bool isCollapseOn = true;

		/// <summary>
		/// Filters to apply to the list of debug entries to show
		/// </summary>
		private DebugLogFilter logFilter = DebugLogFilter.Warning | DebugLogFilter.Error;

		/// <summary>
		/// If the last log item is completely visible (scrollbar is at the bottom),
		/// scrollbar will remain at the bottom when new debug entries are received
		/// </summary>
		private bool snapToBottom = true;

		/// <summary>
		/// List of unique debug entries (duplicates of entries are not kept)
		/// </summary>
		private List<DebugLogEntry> collapsedLogEntries;

		/// <summary>
		/// Dictionary to quickly find if a log already exists in collapsedLogEntries
		/// </summary>
		private Dictionary<DebugLogEntry, int> collapsedLogEntriesMap;

		/// <summary>
		/// The order the collapsedLogEntries are received
		/// (duplicate entries have the same index (value))
		/// </summary>
		private DebugLogIndexList uncollapsedLogEntriesIndices;

		/// <summary>
		/// Filtered list of debug entries to show
		/// </summary>
		private DebugLogIndexList indicesOfListEntriesToShow;

		private List<DebugLogItem> pooledLogItems;

		/// <summary>
		/// Required in ValidateScrollPosition() function
		/// </summary>
		private PointerEventData nullPointerEventData;

		private void OnEnable()
		{
			// Only one instance of debug console is allowed
			if (instance == null)
			{
				instance = this;
				pooledLogItems = new List<DebugLogItem>();

				canvasTR = (RectTransform)transform;

				// Associate sprites with log types
				logSpriteRepresentations = new Dictionary<LogType, Sprite>
				{ { LogType.Log, infoLog },
					{ LogType.Warning, warningLog },
					{ LogType.Error, errorLog },
					{ LogType.Exception, errorLog },
					{ LogType.Assert, errorLog }
				};

				// Set initial button colors
				collapseButton.color = isCollapseOn ? collapseButtonSelectedColor : collapseButtonNormalColor;
				filterInfoButton.color = (logFilter & DebugLogFilter.Info) == DebugLogFilter.Info
						? filterButtonsSelectedColor : filterButtonsNormalColor;
				filterWarningButton.color = (logFilter & DebugLogFilter.Warning) == DebugLogFilter.Warning
						? filterButtonsSelectedColor : filterButtonsNormalColor;
				filterErrorButton.color = (logFilter & DebugLogFilter.Error) == DebugLogFilter.Error
						? filterButtonsSelectedColor : filterButtonsNormalColor;

				collapsedLogEntries = new List<DebugLogEntry>(128);
				collapsedLogEntriesMap = new Dictionary<DebugLogEntry, int>(128);
				uncollapsedLogEntriesIndices = new DebugLogIndexList();
				indicesOfListEntriesToShow = new DebugLogIndexList();

				recycledListView.Initialize(this, collapsedLogEntries, indicesOfListEntriesToShow, logItemPrefab.Transform.sizeDelta.y);
				recycledListView.SetCollapseMode(isCollapseOn);
				recycledListView.UpdateItemsInTheList(true);

				nullPointerEventData = new PointerEventData(null);

				// If it is a singleton object, don't destroy it between scene changes
				if (singleton)
					DontDestroyOnLoad(gameObject);
			}
			else if (this != instance)
			{
				Destroy(gameObject);
				return;
			}

			// Intercept debug entries
			Application.logMessageReceived -= ReceivedLog;
			Application.logMessageReceived += ReceivedLog;

			// Listen for entered commands
			commandInputField.onValidateInput -= OnValidateCommand;
			commandInputField.onValidateInput += OnValidateCommand;

			if (minimumHeight < 200f)
				minimumHeight = 200f;

			//Debug.LogAssertion( "assert" );
			//Debug.LogError( "error" );
			//Debug.LogException( new System.IO.EndOfStreamException() );
			//Debug.LogWarning( "warning" );
			//Debug.Log( "log" );

			//FPS Counter on for mobile only
			if(Application.isMobilePlatform){
				fpsCounter.enabled = true;
			} else {
				fpsCounter.enabled = false;
			}
		}

		void Start()
		{
			popupManager.Hide();
		}

		private void OnDisable()
		{
			// Stop receiving debug entries
			Application.logMessageReceived -= ReceivedLog;

			// Stop receiving commands
			commandInputField.onValidateInput -= OnValidateCommand;
		}

		/// <summary>
		/// Window is resized, update the list
		/// </summary>
		private void OnRectTransformDimensionsChange()
		{
			screenDimensionsChanged = true;
		}

		/// <summary>
		/// If snapToBottom is enabled, force the scrollbar to the bottom
		/// </summary>
		private void LateUpdate()
		{
			Debug.developerConsoleVisible = false;	//makes the default unity console disappear, rather hacky but unity doesnt give us a way to properly disable it

			if (screenDimensionsChanged)
			{
				// Update the recycled list view
				if (isLogWindowVisible)
					recycledListView.OnViewportDimensionsChanged();
				else
					popupManager.OnViewportDimensionsChanged();

				screenDimensionsChanged = false;
			}

			if (snapToBottom)
			{
				logItemsScrollRect.verticalNormalizedPosition = 0f;

				if (snapToBottomButton.activeSelf)
					snapToBottomButton.SetActive(false);
			}
			else
			{
				float scrollPos = logItemsScrollRect.verticalNormalizedPosition;
				if (snapToBottomButton.activeSelf != (scrollPos > 1E-6f && scrollPos < 0.9999f))
					snapToBottomButton.SetActive(!snapToBottomButton.activeSelf);
			}

			// Hide/Show the debugger windows when user presses F5
			if (CommonInput.GetKeyDown(KeyCode.F5))
			{
				if (popupManager.isLogPopupVisible || isLogWindowVisible)
				{
					Hide();
					// popupManager.Hide();
				}
				else
				{
					// popupManager.ShowWithoutReset();
					Show();
				}

			}
		}

		/// <summary>
		/// Command field input is changed, check if command is submitted
		/// </summary>
		/// <param name="text">Text sent to command field</param>
		/// <param name="charIndex">Index of current entry in command field</param>
		/// <param name="addedChar">Character of current entry in command field</param>
		/// <returns></returns>
		public char OnValidateCommand(string text, int charIndex, char addedChar)
		{
			// If command is submitted
			if (addedChar == '\n')
			{
				// Clear the command field
				if (clearCommandAfterExecution)
					commandInputField.text = "";

				if (text.Length > 0)
				{
					// Execute the command
					DebugLogConsole.ExecuteCommand(text);

					// Snap to bottom and select the latest entry
					SetSnapToBottom(true);
				}

				return '\0';
			}

			return addedChar;
		}

		/// <summary>
		/// A debug entry is received
		/// </summary>
		/// <param name="logString">The Debug.Log/LogError/LogWarning/LogException/LogAssertion string</param>
		/// <param name="stackTrace">The debug entry trace</param>
		/// <param name="logType">Error, warning, informational message</param>
		private void ReceivedLog(string logString, string stackTrace, LogType logType)
		{
			DebugLogEntry logEntry = new DebugLogEntry(logString, stackTrace, null);

			// Check if this entry is a duplicate (i.e. has been received before)
			int logEntryIndex;
			bool isEntryInCollapsedEntryList = collapsedLogEntriesMap.TryGetValue(logEntry, out logEntryIndex);
			if (isEntryInCollapsedEntryList == false)
			{
				// It is not a duplicate,
				// add it to the list of unique debug entries
				logEntry.logTypeSpriteRepresentation = logSpriteRepresentations[logType];

				if (RconManager.Instance != null)
				{
					RconManager.AddLog(logString);
				}

				logEntryIndex = collapsedLogEntries.Count;
				collapsedLogEntries.Add(logEntry);
				collapsedLogEntriesMap[logEntry] = logEntryIndex;
			}
			else
			{
				// It is a duplicate,
				// increment the original debug item's collapsed count
				logEntry = collapsedLogEntries[logEntryIndex];
				logEntry.count++;
			}

			// Add the index of the unique debug entry to the list
			// that stores the order the debug entries are received
			uncollapsedLogEntriesIndices.Add(logEntryIndex);

			// If this debug entry matches the current filters,
			// add it to the list of debug entries to show
			Sprite logTypeSpriteRepresentation = logEntry.logTypeSpriteRepresentation;
			if (isCollapseOn && isEntryInCollapsedEntryList)
			{
				if (isLogWindowVisible)
					recycledListView.OnCollapsedLogEntryAtIndexUpdated(logEntryIndex);
			}
			else if (logFilter == DebugLogFilter.All ||
				(logTypeSpriteRepresentation == infoLog && ((logFilter & DebugLogFilter.Info) == DebugLogFilter.Info)) ||
				(logTypeSpriteRepresentation == warningLog && ((logFilter & DebugLogFilter.Warning) == DebugLogFilter.Warning)) ||
				(logTypeSpriteRepresentation == errorLog && ((logFilter & DebugLogFilter.Error) == DebugLogFilter.Error)))
			{
				indicesOfListEntriesToShow.Add(logEntryIndex);

				if (isLogWindowVisible)
					recycledListView.OnLogEntriesUpdated(false);
			}

			if (logType == LogType.Log)
			{
				infoEntryCount++;
				infoEntryCountText.text = infoEntryCount.ToString();

				// If debug popup is visible, notify it of the new debug entry
				if (isLogWindowVisible == false && CustomNetworkManager.IsHeadless == false)
					popupManager.NewInfoLogArrived();
			}
			else if (logType == LogType.Warning)
			{
				warningEntryCount++;
				warningEntryCountText.text = warningEntryCount.ToString();

				// If debug popup is visible, notify it of the new debug entry
				if (isLogWindowVisible == false && CustomNetworkManager.IsHeadless == false)
					popupManager.NewWarningLogArrived();
			}
			else
			{
				errorEntryCount++;
				errorEntryCountText.text = errorEntryCount.ToString();

				// If debug popup is visible, notify it of the new debug entry
				if (isLogWindowVisible == false && CustomNetworkManager.IsHeadless == false)
					popupManager.NewErrorLogArrived();
			}
		}

		/// <summary>
		/// Value of snapToBottom is changed (user scrolled the list manually)
		/// </summary>
		/// <param name="snapToBottom">Set whether scrollbar will stay at bottom</param>
		public void SetSnapToBottom(bool snapToBottom)
		{
			this.snapToBottom = snapToBottom;
		}

		/// <summary>
		/// Make sure the scroll bar of the scroll rect is adjusted properly
		/// </summary>
		public void ValidateScrollPosition()
		{
			logItemsScrollRect.OnScroll(nullPointerEventData);
		}

		/// <summary>
		/// Show the log window
		/// </summary>
		public void Show()
		{
			// Update the recycled list view (in case new entries were
			// intercepted while log window was hidden)
			recycledListView.OnLogEntriesUpdated(true);

			logWindowCanvasGroup.interactable = true;
			logWindowCanvasGroup.blocksRaycasts = true;
			logWindowCanvasGroup.alpha = 1f;

			isLogWindowVisible = true;
		}

		/// <summary>
		/// Hide the log window
		/// </summary>
		public void Hide()
		{
			logWindowCanvasGroup.interactable = false;
			logWindowCanvasGroup.blocksRaycasts = false;
			logWindowCanvasGroup.alpha = 0f;

			isLogWindowVisible = false;
		}

		/// <summary>
		/// Hide button is clicked
		/// </summary>
		public void HideButtonPressed()
		{
			Hide();
		}

		/// <summary>
		/// Clear button is clicked
		/// </summary>
		public void ClearButtonPressed()
		{
			snapToBottom = true;

			infoEntryCount = 0;
			warningEntryCount = 0;
			errorEntryCount = 0;

			infoEntryCountText.text = "0";
			warningEntryCountText.text = "0";
			errorEntryCountText.text = "0";

			collapsedLogEntries.Clear();
			collapsedLogEntriesMap.Clear();
			uncollapsedLogEntriesIndices.Clear();
			indicesOfListEntriesToShow.Clear();

			recycledListView.DeselectSelectedLogItem();
			recycledListView.OnLogEntriesUpdated(true);
		}

		/// <summary>
		/// Collapse button is clicked
		/// </summary>
		public void CollapseButtonPressed()
		{
			// Swap the value of collapse mode
			isCollapseOn = !isCollapseOn;

			snapToBottom = true;
			collapseButton.color = isCollapseOn ? collapseButtonSelectedColor : collapseButtonNormalColor;
			recycledListView.SetCollapseMode(isCollapseOn);

			// Determine the new list of debug entries to show
			FilterLogs();
		}

		/// <summary>
		/// Filtering mode of info logs has been changed
		/// </summary>
		public void FilterLogButtonPressed()
		{
			logFilter = logFilter ^ DebugLogFilter.Info;

			if ((logFilter & DebugLogFilter.Info) == DebugLogFilter.Info)
				filterInfoButton.color = filterButtonsSelectedColor;
			else
				filterInfoButton.color = filterButtonsNormalColor;

			FilterLogs();
		}

		/// <summary>
		/// Filtering mode of warning logs has been changed
		/// </summary>
		public void FilterWarningButtonPressed()
		{
			logFilter = logFilter ^ DebugLogFilter.Warning;

			if ((logFilter & DebugLogFilter.Warning) == DebugLogFilter.Warning)
				filterWarningButton.color = filterButtonsSelectedColor;
			else
				filterWarningButton.color = filterButtonsNormalColor;

			FilterLogs();
		}

		/// <summary>
		/// Filtering mode of error logs has been changed
		/// </summary>
		public void FilterErrorButtonPressed()
		{
			logFilter = logFilter ^ DebugLogFilter.Error;

			if ((logFilter & DebugLogFilter.Error) == DebugLogFilter.Error)
				filterErrorButton.color = filterButtonsSelectedColor;
			else
				filterErrorButton.color = filterButtonsNormalColor;

			FilterLogs();
		}

		/// <summary>
		/// Creates a local profile
		/// </summary>
		public void LocalProfileButtonPressed()
		{
			if (Debug.isDebugBuild)
			{
				SafeProfileManager.Instance.StartProfile(30);
				Loggy.Log("Running a local profile, saving on installation folder", Category.DebugConsole);
			}
			else
			{
				Loggy.Log("Unable to run local profile, the build needs to be in development mode", Category.DebugConsole);
			}
		}

		/// <summary>
		/// Creates a local memory profile
		/// </summary>
		public void LocalMemoryProfileButtonPressed()
		{
			if (Debug.isDebugBuild)
			{
				SafeProfileManager.Instance.RunMemoryProfile();
				Loggy.Log("Running a local memory profile, saving on installation folder", Category.DebugConsole);
			}
			else
			{
				Loggy.Log("Unable to run local profile, the build needs to be in development mode", Category.DebugConsole);
			}
		}

		/// <summary>
		/// Debug window is being resized,
		/// Set the sizeDelta property of the window accordingly while
		/// preventing window dimensions from going below the minimum dimensions
		/// </summary>
		public void Resize(BaseEventData dat)
		{
			PointerEventData eventData = (PointerEventData)dat;

			// Grab the resize button from top; 36f is the height of the resize button
			float newHeight = (eventData.position.y - logWindowTR.position.y) / -canvasTR.localScale.y + 36f;
			if (newHeight < minimumHeight)
				newHeight = minimumHeight;

			Vector2 anchorMin = logWindowTR.anchorMin;
			anchorMin.y = Mathf.Max(0f, 1f - newHeight / canvasTR.sizeDelta.y);
			logWindowTR.anchorMin = anchorMin;

			// Update the recycled list view
			recycledListView.OnViewportDimensionsChanged();
		}

		/// <summary>
		/// Determine the filtered list of debug entries to show on screen
		/// </summary>
		private void FilterLogs()
		{
			if (logFilter == DebugLogFilter.None)
			{
				// Show no entry
				indicesOfListEntriesToShow.Clear();
			}
			else if (logFilter == DebugLogFilter.All)
			{
				if (isCollapseOn)
				{
					// All the unique debug entries will be listed just once.
					// So, list of debug entries to show is the same as the
					// order these unique debug entries are added to collapsedLogEntries
					indicesOfListEntriesToShow.Clear();
					for (int i = 0; i < collapsedLogEntries.Count; i++)
						indicesOfListEntriesToShow.Add(i);
				}
				else
				{
					indicesOfListEntriesToShow.Clear();
					for (int i = 0; i < uncollapsedLogEntriesIndices.Count; i++)
						indicesOfListEntriesToShow.Add(uncollapsedLogEntriesIndices[i]);
				}
			}
			else
			{
				// Show only the debug entries that match the current filter
				bool isInfoEnabled = (logFilter & DebugLogFilter.Info) == DebugLogFilter.Info;
				bool isWarningEnabled = (logFilter & DebugLogFilter.Warning) == DebugLogFilter.Warning;
				bool isErrorEnabled = (logFilter & DebugLogFilter.Error) == DebugLogFilter.Error;

				if (isCollapseOn)
				{
					indicesOfListEntriesToShow.Clear();
					for (int i = 0; i < collapsedLogEntries.Count; i++)
					{
						DebugLogEntry logEntry = collapsedLogEntries[i];
						if (logEntry.logTypeSpriteRepresentation == infoLog && isInfoEnabled)
							indicesOfListEntriesToShow.Add(i);
						else if (logEntry.logTypeSpriteRepresentation == warningLog && isWarningEnabled)
							indicesOfListEntriesToShow.Add(i);
						else if (logEntry.logTypeSpriteRepresentation == errorLog && isErrorEnabled)
							indicesOfListEntriesToShow.Add(i);
					}
				}
				else
				{
					indicesOfListEntriesToShow.Clear();
					for (int i = 0; i < uncollapsedLogEntriesIndices.Count; i++)
					{
						DebugLogEntry logEntry = collapsedLogEntries[uncollapsedLogEntriesIndices[i]];
						if (logEntry.logTypeSpriteRepresentation == infoLog && isInfoEnabled)
							indicesOfListEntriesToShow.Add(uncollapsedLogEntriesIndices[i]);
						else if (logEntry.logTypeSpriteRepresentation == warningLog && isWarningEnabled)
							indicesOfListEntriesToShow.Add(uncollapsedLogEntriesIndices[i]);
						else if (logEntry.logTypeSpriteRepresentation == errorLog && isErrorEnabled)
							indicesOfListEntriesToShow.Add(uncollapsedLogEntriesIndices[i]);
					}
				}
			}

			// Update the recycled list view
			recycledListView.DeselectSelectedLogItem();
			recycledListView.OnLogEntriesUpdated(true);

			ValidateScrollPosition();
		}

		/// <summary>
		/// Pool an unused log item
		/// </summary>
		public void PoolLogItem(DebugLogItem logItem)
		{
			logItem.gameObject.SetActive(false);
			pooledLogItems.Add(logItem);
		}

		/// <summary>
		/// Fetch a log item from the pool
		/// </summary>
		/// <returns>The log item</returns>
		public DebugLogItem PopLogItem()
		{
			DebugLogItem newLogItem;

			// If pool is not empty, fetch a log item from the pool,
			// create a new log item otherwise
			if (pooledLogItems.Count > 0)
			{
				newLogItem = pooledLogItems[pooledLogItems.Count - 1];
				pooledLogItems.RemoveAt(pooledLogItems.Count - 1);
				newLogItem.gameObject.SetActive(true);
			}
			else
			{
				newLogItem = Instantiate<DebugLogItem>(logItemPrefab, logItemsContainer, false);
				newLogItem.Initialize(recycledListView);
			}

			return newLogItem;
		}
	}
}