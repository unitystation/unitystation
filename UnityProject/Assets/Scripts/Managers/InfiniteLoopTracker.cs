using System;
using System.Text;
using System.Threading;
using SecureStuff;
using Initialisation;
using Mirror;
using Mirror.RemoteCalls;
using Newtonsoft.Json;
using Shared.Managers;
using Debug = UnityEngine.Debug;

namespace Managers
{
	public class InfiniteLoopTracker : SingletonManager<InfiniteLoopTracker>
	{
		private Thread thread;

		private int frameNumber;

		//miliseconds
        private int sleepDuration = 1000;
        private int reportTimeFrame = 60000;

        //checkpoints for game messages
        public static bool gameMessageProcessing;
        public static string lastGameMessage;
        public static NetworkMessage NetNetworkMessage;

        public override void Start()
        {
	        base.Start();
	        thread = new Thread (OverwatchMainThread);
	        thread.Start();
	        AccessFile.Delete("InfiniteLoopTracker.txt",FolderType.Logs);
	        AccessFile.AppendAllText("InfiniteLoopTracker.txt", "", FolderType.Logs);
        }

        private void OnEnable()
        {
	        UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
        }

        private void OnDisable()
        {
	        if (thread != null)
	        {
		        thread.Abort();
		        UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	        }
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
		        if (LoadManager.Instance.IsExecuting)
		        {
			        if (LoadManager.Instance.IsExecutingGeneric)
			        {
				        var className = (LoadManager.Instance.LastInvokedAction.Target as Action).Method.ReflectedType.ToString();
				        var methodName = (LoadManager.Instance.LastInvokedAction.Target as Action).Method.Name;
				        stringBuilder.AppendLine($" - LoadManager invoke - class: {className} - method: {methodName}");
			        }
			        else
			        {
				        var className = LoadManager.Instance.LastInvokedAction.Method.ReflectedType.ToString();
				        var methodName = LoadManager.Instance.LastInvokedAction.Method.Name;
				        stringBuilder.AppendLine($" - LoadManager invoke - class: {className} - method: {methodName}");
			        }
		        }
		        else
		        {
			        var className = UpdateManager.Instance.LastInvokedAction.Method.ReflectedType.ToString();
			        var methodName = UpdateManager.Instance.LastInvokedAction.Method.Name;
			        stringBuilder.AppendLine($" - UpdateManager invoke - class: {className} - method: {methodName}");
		        }


	        }

	        //Something within load manager
	        if (LoadManager.Instance.IsExecuting)
	        {
		        if (LoadManager.Instance.IsExecutingGeneric)
		        {
			        var className = (LoadManager.Instance.LastInvokedAction.Target as Action).Method.ReflectedType.ToString();
			        var methodName = (LoadManager.Instance.LastInvokedAction.Target as Action).Method.Name;
			        stringBuilder.AppendLine($" - LoadManager invoke - class: {className} - method: {methodName}");
		        }
		        else
		        {
			        var className = LoadManager.Instance.LastInvokedAction.Method.ReflectedType.ToString();
			        var methodName = LoadManager.Instance.LastInvokedAction.Method.Name;
			        stringBuilder.AppendLine($" - LoadManager invoke - class: {className} - method: {methodName}");
		        }
	        }

	        //cmd and rcp checkpoints
	        if (RemoteProcedureCalls.mirrorProcessingCMD)
	        {
		        var className = RemoteProcedureCalls.mirrorLastInvoker.function.Method.DeclaringType.Name;
				var methodName = RemoteProcedureCalls.mirrorLastInvoker.function.Method.Name;
				stringBuilder.AppendLine($" - Mirror invoke - class: {className} - method: {methodName}");
	        }

	        //game message checkpoints
	        if (gameMessageProcessing)
	        {
		        try
		        {
			        stringBuilder.AppendLine($" - GameMessage - class: {lastGameMessage} data : {JsonConvert.SerializeObject(NetNetworkMessage, new JsonSerializerSettings() {ReferenceLoopHandling = ReferenceLoopHandling.Ignore})}");

		        }
		        catch (Exception e)
		        {
			        stringBuilder.AppendLine($" - GameMessage - class: {lastGameMessage} data : Error {e}");
		        }
	        }

	        Log(stringBuilder.ToString());
        }

        private void Log(string aText)
        {
	        Debug.LogError(aText); //in case of case positives we make a normal log
	        AccessFile.AppendAllText("InfiniteLoopTracker.txt", aText, FolderType.Logs);
        }
	}
}