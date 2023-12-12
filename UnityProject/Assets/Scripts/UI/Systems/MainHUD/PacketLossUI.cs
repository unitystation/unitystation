using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using System.Net.NetworkInformation;
using System.Threading;
using Ping = System.Net.NetworkInformation.Ping;

namespace UI.Systems.MainHUD
{
	public class PacketLossUI : MonoBehaviour
	{
		[SerializeField] private Text packetLossText;

		private CancellationToken token;
		private double packetLossPercentage = 0;
		private readonly int pingCount = 10;
		private int pingNumber = 0;
		private bool canPingAgain = true;
		private DateTime startTime;
		private readonly Ping ping = new Ping();

		private int[] attempts = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 , 0 };

		private void Awake()
		{
			CancellationTokenSource source = new CancellationTokenSource();
			token = source.Token;
			ping.PingCompleted += Loss;
		}

		private void Update()
		{
			MeasurePacketLoss();
			UpdateUI();
		}

		private void UpdateUI()
		{
			packetLossPercentage = GetPacketLoss() / pingCount * 100;
			packetLossText.text = packetLossPercentage >= 100 ? "Disconnected?" : $"PL: {packetLossPercentage}%";
			packetLossText.color = packetLossPercentage switch
			{
				> 35 => Color.red,
				> 10 => Color.yellow,
				_ => Color.white
			};
		}

		private void MeasurePacketLoss()
		{
			if (canPingAgain == false) return;
			if (NetworkManager.singleton == null || NetworkManager.singleton.networkAddress.IsNullOrEmpty() ||
			    NetworkManager.singleton.isNetworkActive == false)
			{
				attempts = new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 , 1 };
				return;
			}
			var ip = NetworkManager.singleton.networkAddress == "localhost" ? "127.0.0.1" : NetworkManager.singleton.networkAddress;
			ping.SendAsync(ip, 255, token);
			canPingAgain = false;
			pingNumber++;
			if (pingNumber >= pingCount)
			{
				pingNumber = 0;
			}
		}

		private void Loss(object k, PingCompletedEventArgs e)
		{
			canPingAgain = true;
			if (e == null || e.Error != null || e.Cancelled)
			{
				attempts[pingNumber] = 1;
				return;
			}
			PingReply reply = e.Reply;
			if (reply.Status == IPStatus.Success)
			{
				attempts[pingNumber] = 0;
			}
			else
			{
				attempts[pingNumber] = 1;
			}
		}

		private double GetPacketLoss()
		{
			var fails = attempts.Where(i => i == 1);
			return fails.Sum();
		}
	}
}