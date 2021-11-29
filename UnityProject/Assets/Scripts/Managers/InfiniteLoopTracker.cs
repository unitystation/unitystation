﻿using System;
using System.Text;
using System.Threading;
using Debug = UnityEngine.Debug;
using System.IO;

namespace Managers
{
	public class InfiniteLoopTracker : SingletonManager<InfiniteLoopTracker>
	{
		private Thread thread;
		private StreamWriter streamWriter;

		private int frameNumber;

		//miliseconds
        private int sleepDuration = 1000;
        private int reportTimeFrame = 60000;

        private void Start()
        {
	        thread = new Thread (OverwatchMainThread);
	        thread.Start();
	        streamWriter = File.AppendText("Logs/InfiniteLoopTracker.txt");
        }

        private void OnDisable()
        {
	        if (streamWriter != null)
	        {
		        streamWriter.Close();
		        thread.Abort();
	        }
        }

        private void Update()
        {
            frameNumber++;
        }

        private void OverwatchMainThread()
        {
	        var lastFrame = 0;
            var currentFrameLenght = 0;
            while (true)
            {
                if (frameNumber > lastFrame)
                {
                    //new frame on main thread, reset
                    lastFrame = frameNumber;
                    currentFrameLenght = 0;
                }
                else
                {
                    //mainthread is still on the same frame
                    currentFrameLenght += sleepDuration;
                    if (currentFrameLenght > reportTimeFrame)
                    {
	                    ReportLastCheckpoint();
	                    currentFrameLenght = 0;
                    }
                }
                Thread.Sleep(sleepDuration);
            }
        }

        private void ReportLastCheckpoint()
        {
	        var stringBuilder = new StringBuilder();
	        stringBuilder.Append($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} possible infinite loop detected on frame: {frameNumber}");
	        if (UpdateManager.Instance.MidInvokeCalls)
	        {
		        var lastInvoked = UpdateManager.Instance.LastInvokedAction.Method.ReflectedType.ToString();
		        var methodName = UpdateManager.Instance.LastInvokedAction.Method.Name;
		        stringBuilder.AppendLine($" - UpdateManager invoke - type: {lastInvoked} - method: {methodName}");
	        }
	        Log(stringBuilder.ToString());
        }

        private void Log(string aText)
        {
	        Debug.LogError(aText); //in case of case positives we make a normal log
            streamWriter.WriteLine(aText);
            streamWriter.Flush();
        }
	}
}