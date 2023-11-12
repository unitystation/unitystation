using System.Collections;
using System.Text;
using Chemistry;
using Chemistry.Components;
using Items;
using Logs;
using TMPro;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects.Medical
{
	public class GUI_ChemMaster : NetTab
	{
		public ChemMaster ChemMaster;

		#region Serialized Fields
		[SerializeField]
		private NetPageSwitcher mainSwitcher;
		[SerializeField]
		private NetPage containerReagentList;
		[SerializeField]
		private NetPage bufferControlList;
		[SerializeField]
		private NetPage productOutList;
		[SerializeField]
		private NetButtonAuth ejectandClear;
		[SerializeField]
		private EmptyItemList containerList;
		[SerializeField]
		private EmptyItemList bufferList;
		[SerializeField]
		private NetPageSwitcher customAmountInputPromptSwitcher;
		[SerializeField]
		private NetPage customAmountInputPromptPage;
		[SerializeField]
		private NetPage noCustomPromptPage;
		[SerializeField]
		private NetText_label customAmountLabel;
		[SerializeField]
		private NetText_label customAmountReagentLabel;
		[SerializeField]
		private NetText_label productReagentList;
		[SerializeField]
		private NetText_label productAmountToDispense;
		[SerializeField]
		private NetText_label productAmountsList;
		[SerializeField]
		private Scrollbar containerScrollbar;
		[SerializeField]
		private Scrollbar bufferScrollbar;
		[SerializeField]
		private NetText_label containerNoReagent;
		[SerializeField]
		private NetText_label bufferNoReagent;
		[SerializeField]
		private NetText_label transferModeButtonLabel;
		[SerializeField]
		private EmptyItemList productList;
		[SerializeField]
		private NetText_label productTypeChoice;
		[SerializeField]
		private NetText_label productMaxAmount;
		[SerializeField]
		private TMP_InputField productNameInputField;
		[SerializeField]
		private GameObject inputFieldBackgroundText;
		[SerializeField]
		private int customProductNameCharacterLimit;

		public NetUIChildActive PillSelectionArea;

		#endregion

		#region Initialization

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			// Makes sure it connects with the machine
			ChemMaster = Provider.GetComponentInChildren<ChemMaster>();
			// Subscribe to change event from ChemMaster.cs
			ChemMaster.changeEvent += UpdateAll;
			UpdateAll();
			containerNoReagent.MasterSetValue("No container");
			bufferNoReagent.MasterSetValue("No reagents in buffer");
			productAmountToDispense.MasterSetValue($"{productDispenseAmount}");
			productTypeChoice.MasterSetValue($"Please select product Type");
			productMaxAmount.MasterSetValue("");
			productNameInputField.characterLimit = customProductNameCharacterLimit;
			UpdateProductOptions();
			PillSelectionArea.MasterNetSetActive(false);
		}
		#endregion

		#region Custom Transfer Amount UI
		private float customTransferAmount;
		private Reagent customTransferReagentCandidate;
		private bool sendCustomAmountToBuffer;
		public void OpenCustomPrompt(Reagent newTransferReagentCandidate, bool toBuffer)
		{
			sendCustomAmountToBuffer = toBuffer;
			customAmountInputPromptSwitcher.SetActivePage(customAmountInputPromptPage);
			customTransferAmount = 0;
			customAmountLabel.MasterSetValue($"---");
			customTransferReagentCandidate = newTransferReagentCandidate;
			customAmountReagentLabel.MasterSetValue($"{customTransferReagentCandidate}");
		}

		public void CloseCustomPrompt()
		{
			customAmountInputPromptSwitcher.SetActivePage(noCustomPromptPage);
			customTransferReagentCandidate = null;
		}

		public void AddTransferAmountDigit(int digit)
		{
			if (customTransferAmount > 0) customTransferAmount = (customTransferAmount * 10) + digit;
			else customTransferAmount = digit;
			if (sendCustomAmountToBuffer)
			{
				if (customTransferAmount > ChemMaster.GetBufferSpace())
					customTransferAmount = ChemMaster.GetBufferSpace();
				if (customTransferAmount > ChemMaster.GetBufferCapacity())
					customTransferAmount = ChemMaster.GetBufferCapacity();
				if (customTransferAmount > ChemMaster.Container.CurrentReagentMix[customTransferReagentCandidate])
					customTransferAmount = ChemMaster.Container.CurrentReagentMix[customTransferReagentCandidate];
			}
			else
			{
				if (customTransferAmount > ChemMaster.Container.MaxCapacity - ChemMaster.Container.ReagentMixTotal)
					customTransferAmount = ChemMaster.Container.MaxCapacity - ChemMaster.Container.ReagentMixTotal;
				if (customTransferAmount > ChemMaster.Container.MaxCapacity)
					customTransferAmount = ChemMaster.Container.MaxCapacity;
				if (customTransferAmount > ChemMaster.GetBufferMix()[customTransferReagentCandidate])
					customTransferAmount = ChemMaster.GetBufferMix()[customTransferReagentCandidate];
			}
			customAmountLabel.MasterSetValue($"{customTransferAmount:F2}u");
		}

		public void RemoveTransferAmountDigit()
		{
			customTransferAmount = (int)(customTransferAmount / 10);
			customAmountLabel.MasterSetValue($"{customTransferAmount:F2}u");
		}

		public void TransferCustomAmount()
		{
			if (sendCustomAmountToBuffer)
			{
				ChemMaster.TransferContainerToBuffer(customTransferReagentCandidate, customTransferAmount);
			}
			else
			{
				ChemMaster.TransferBufferToContainer(customTransferReagentCandidate, customTransferAmount);
			}
			CloseCustomPrompt();
		}

		public void Analyze(Reagent reagent, PlayerInfo player)
		{
			Chat.AddExamineMsg(player.GameObject, $"This is {reagent.Name}. {reagent.description}");
		}
		#endregion

		#region Product Logistics
		private GameObject productChoice = null;
		private int PillproductChoice = 0;

		public void PillChosen(int PillIndex)
		{
			SelectProduct(0, ChemMaster.ChemMasterProducts[0], PillIndex);
		}


		public void SelectProduct(int ChoiceIndex, GameObject choice, int Pillchoice)
		{
			PillproductChoice = Pillchoice;
			productChoice = choice;

			productTypeChoice.MasterSetValue($"{choice.GetComponent<ItemAttributesV2>().InitialName}s");
			productMaxAmount.MasterSetValue($"Max {choice.GetComponent<ReagentContainer>().MaxCapacity}u");
			foreach(var entry in productList.Entries)
			{
				if (entry.transform.GetSiblingIndex() == ChoiceIndex)
				{
					entry.GetComponentInChildren<NetButton>().MasterSetValue($"false");
				}
				else
				{
					entry.GetComponentInChildren<NetButton>().MasterSetValue($"true");
				}
			}
		}

		private int productDispenseAmount=1;

		public void IncrementProductAmount()
		{
			productDispenseAmount = Mathf.Clamp(productDispenseAmount + 1, 1, 10);
			productAmountToDispense.MasterSetValue($"{productDispenseAmount}");
		}

		public void DecrementProductAmount()
		{
			productDispenseAmount = Mathf.Clamp(productDispenseAmount - 1, 1, 10);
			productAmountToDispense.MasterSetValue($"{productDispenseAmount}");
		}

		private string customNameInProgress = "";
		public void InputFieldBackgroundEvaluate(string input)
		{
			if(input == "")
			{
				inputFieldBackgroundText.SetActive(true);
				if (customNameInProgress != "")
				{
					productNameInputField.text = customNameInProgress;
				}
			}
			else
			{
				inputFieldBackgroundText.SetActive(false);
			}
			customNameInProgress = input;
		}

		public void DispenseProduct(string newName)
		{
			if (productChoice != null)
			{
				ChemMaster.DispenseProduct(productChoice, productDispenseAmount,newName, PillproductChoice);
			}
			productDispenseAmount=1;
			productAmountToDispense.MasterSetValue($"{productDispenseAmount}");
			productTypeChoice.MasterSetValue($"Please select");
			productMaxAmount.MasterSetValue("");
			inputFieldBackgroundText.SetActive(true);
			foreach (var entry in productList.Entries)
			{
				entry.GetComponentInChildren<NetButton>().MasterSetValue($"true");
			}
		}
		#endregion

		#region Reagent Management
		public void TransferContainerToBuffer(Reagent reagent, float amount)
		{
			ChemMaster.TransferContainerToBuffer(reagent, amount);
		}
		public void BufferTransfer(Reagent reagent, float amount)
		{
			if (transferBack)
			{
				TransferBufferToContainer(reagent, amount);
			}
			else
			{
				RemoveFromBuffer(reagent, amount);
			}
		}
		public void TransferBufferToContainer(Reagent reagent, float amount)
		{
			ChemMaster.TransferBufferToContainer(reagent, amount);
		}
		public void RemoveFromBuffer(Reagent reagent, float amount)
		{
			ChemMaster.RemoveFromBuffer(reagent, amount);
		}
		public void ClearBuffer()
		{
			ChemMaster.ClearBuffer();
		}

		private bool transferBack = true;
		public void ToggleTransferMode()
		{
			transferBack = !transferBack;
			string temp = transferBack ? "Container" : "Disposal";
			transferModeButtonLabel.MasterSetValue($"Transfering to {temp}");
		}
		#endregion

		#region NetUI Logistics

		/// <summary>
		/// Updates contents of Container Page with current list of reagents and amounts, and provides
		///		feedback if there is a lack of reagents or container
		/// </summary>
		public void DisplayContainerReagents()
		{
			if (ChemMaster.Container)
			{
				if (ChemMaster.Container.CurrentReagentMix.Total != 0)
				{
					ReagentMix tempMix = ChemMaster.Container.CurrentReagentMix;
					containerList.Clear();
					containerList.SetItems(tempMix.reagents.Count);
					int i = 0;
					foreach (Reagent reagent in tempMix.reagents.Keys)
					{
						GUI_ChemContainerEntry thing = containerList.Entries[i] as GUI_ChemContainerEntry;
						thing.ReInit(reagent, tempMix.reagents[reagent], GetComponent<GUI_ChemMaster>());
						i++;
					}
					containerNoReagent.MasterSetValue("");
				}
				else
				{
					containerList.Clear();
					containerNoReagent.MasterSetValue("No Reagents in Container");
				}
				//we have a container that is capable of being ejected
				ejectandClear.MasterSetValue("true");
			}
			else
			{
				containerList.Clear();
				containerNoReagent.MasterSetValue("No container");
				ejectandClear.MasterSetValue("false");
			}
		}

		/// <summary>
		/// Updates contents of Buffer Page with current list of reagents and amounts, and provides
		///		feedback if there is a lack of reagents or containers
		/// </summary>
		public void DisplayBufferReagents()
		{
			if (ChemMaster.GetBufferMix() != null)
			{
				ReagentMix tempMix = ChemMaster.GetBufferMix();
				bufferList.Clear();
				bufferList.SetItems(tempMix.reagents.Count);
				int i = 0;
				foreach (Reagent reagent in tempMix.reagents.Keys)
				{
					GUI_ChemBufferEntry thing = bufferList.Entries[i] as GUI_ChemBufferEntry;
					thing.ReInit(reagent, tempMix.reagents[reagent], GetComponent<GUI_ChemMaster>());
					i++;
				}
				bufferNoReagent.MasterSetValue("");
			}
			else
			{
				bufferList.Clear();
				if (!ChemMaster.BufferslotOne && !ChemMaster.BufferslotTwo)
					bufferNoReagent.MasterSetValue("No containers in buffer");
				else bufferNoReagent.MasterSetValue("No reagents in buffer");
			}
		}

		/// <summary>
		/// Updates contents of Product Page with current list of reagents and amounts, and provides
		///		feedback if there is a lack of reagents
		/// </summary>
		public void DisplayProductReagents()
		{
			if (ChemMaster.GetBufferMix() != null)
			{
				ReagentMix tempMix = ChemMaster.GetBufferMix();
				StringBuilder reagentListStr = new StringBuilder();
				StringBuilder amountsListStr = new StringBuilder();
				foreach (Reagent reagent in tempMix.reagents.Keys)
				{
					reagentListStr.Append($"{reagent.Name}\n");
					amountsListStr.Append($"{tempMix.reagents[reagent]}u\n");
				}
				productReagentList.MasterSetValue(reagentListStr.ToString());
				productAmountsList.MasterSetValue(amountsListStr.ToString());
			}
			else
			{
				productReagentList.MasterSetValue("No reagents in the buffer");
				productAmountsList.MasterSetValue("");
			}
			productNameInputField.text = customNameInProgress;
		}

		/// <summary>
		/// Updates Product Page buttons with available
		/// </summary>
		private void UpdateProductOptions()
		{
			productList.Clear();

			foreach (GameObject listItemin in ChemMaster.ChemMasterProducts)
			{
				if (listItemin == null)
				{
					continue;
				}
				var thing = productList.AddItem().GetComponent<GUI_ChemProductEntry>();
				thing.ReInit(this, listItemin);
			}
		}

		public void SwitchPageToContainer()
		{
			mainSwitcher.SetActivePage(containerReagentList);
		}
		public void SwitchPageToBuffer()
		{
			mainSwitcher.SetActivePage(bufferControlList);
		}
		public void SwitchPageToProducts()
		{
			mainSwitcher.SetActivePage(productOutList);
		}

		public void DisablePlayerForTextInput()
		{
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
		}

		public void EnablePlayerAfterTextInput()
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		public void UpdateAll()
		{
			DisplayContainerReagents();
			DisplayBufferReagents();
			DisplayProductReagents();
			UpdateProductOptions();
		}
		#endregion

		/// <summary>
		/// Ejects input container from ChemMaster into best slot available
		/// </summary>
		/// <param name="player"></param>
		public void EjectContainer(PlayerInfo player)
		{
			if (ChemMaster.Container != null)
			{
				ChemMaster.EjectContainer(player);
			}
			else
			{
				Loggy.LogWarning("Attempted to eject from a ChemMaster without container", Category.Interaction);
			}
		}

		public void OnDestroy()
		{
			ChemMaster.changeEvent -= UpdateAll;
		}
	}
}
