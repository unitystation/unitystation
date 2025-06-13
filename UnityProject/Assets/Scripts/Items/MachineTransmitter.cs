using Communications;
using Cysharp.Threading.Tasks;
using Mirror;
using Systems.Explosions;

public class MachineTransmitter : SignalEmitter, IEmpAble
{
	[SyncVar] public bool isEMPed = false;

	protected override bool SendSignalLogic()
	{
		if (GameManager.Instance.CommsServers.Count == 0) return false;
		return isEMPed == false;
	}

	/// <summary>
	/// Nothing happens in SS13 when a fail happens so I guess leave it like that
	/// </summary>
	public override void SignalFailed() { }

	public void OnEmp(int EmpStrength)
	{
		if (isEMPed == false) _ = Emp(EmpStrength);
	}

	public async UniTask Emp(int EmpStrength)
	{
		int effectTime = (int)(EmpStrength * 0.75f);
		isEMPed = true;
		await UniTask.Delay(effectTime);
		isEMPed = false;
	}
}
