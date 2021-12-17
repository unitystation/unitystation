using System;
using System.Text;
using System.Threading;
using Debug = UnityEngine.Debug;
using System.IO;
using Mirror.RemoteCalls;

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

        //checkpoints for game messages
        public static bool gameMessageProcessing;
        public static string lastGameMessage;

        private void Start()
        {
	        thread = new Thread (OverwatchMainThread);
	        thread.Start();
	        Directory.CreateDirectory("Logs");
	        streamWriter = File.AppendText("Logs/InfiniteLoopTracker.txt");
        }

        private void OnEnable()
        {
	        UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
        }

        private void OnDisable()
        {
	        if (streamWriter != null)
	        {
		        streamWriter.Close();
		        thread.Abort();
	        }
	        UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
        }

        private void UpdateMe()
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

	        //update manager checkpoints
	        if (UpdateManager.Instance.MidInvokeCalls)
	        {
		        var className = UpdateManager.Instance.LastInvokedAction.Method.ReflectedType.ToString();
		        var methodName = UpdateManager.Instance.LastInvokedAction.Method.Name;
		        stringBuilder.AppendLine($" - UpdateManager invoke - class: {className} - method: {methodName}");
	        }

	        //cmd and rcp checkpoints
	        if (RemoteCallHelper.mirrorProcessingCMD)
	        {
		        var className = RemoteCallHelper.mirrorLastInvoker.invokeClass.FullName;
		        var methodName = RemoteCallHelper.mirrorLastInvoker.invokeFunction.Method.Name;
		        var invokeType = RemoteCallHelper.mirrorLastInvoker.invokeType;
		        stringBuilder.AppendLine($" - Mirror {invokeType} - class: {className} - method: {methodName}");
	        }

	        //game message checkpoints
	        if (gameMessageProcessing)
	        {
		        stringBuilder.AppendLine($" - GameMessage - class: {lastGameMessage}");
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