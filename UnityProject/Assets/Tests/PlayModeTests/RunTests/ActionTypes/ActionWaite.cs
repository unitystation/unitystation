using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction {

  public bool ShowActionWaite => SpecifiedAction == ActionType.ActionWaite;

  [AllowNesting]
  [ShowIf("ShowActionWaite")]
  public ActionWaite DataActionWaite;

  [System.Serializable]
  public class ActionWaite {
    public bool WaitForFrame = false;
    public int WaitForSeconds = 0;

    public bool Initiate(TestRunSO TestRunSO) {
      TestRunSO.BoolYieldInstruction = true;

      if (WaitForFrame) {
        TestRunSO.YieldInstruction = null;
      }

      if (WaitForSeconds != 0) {
        TestRunSO.YieldInstruction = WaitFor.Seconds(WaitForSeconds);
      }

      return true;
    }
  }

  public bool InitiateActionWaite(TestRunSO TestRunSO) {
    return DataActionWaite.Initiate(TestRunSO);
  }
}
