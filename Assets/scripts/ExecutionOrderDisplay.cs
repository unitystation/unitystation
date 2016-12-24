using UnityEngine;

[ExecuteInEditMode]
public class ExecutionOrderDisplay: MonoBehaviour {
    private string id;

    private string checkMode() {
        if(Application.isPlaying)
            return "PlayMode";
        else
            return "EditMode";
    }

    void Awake() {
        id = GetInstanceID() + "";

        Debug.Log(checkMode() + "::ExecutionOrderDisplay::" + id + "::" + name + "::Awake()");
    }

    void OnEnable() {
        Debug.Log(checkMode() + "::ExecutionOrderDisplay::" + id + "::" + name + "::OnEnable()");
    }

    void Start() {
        Debug.Log(checkMode() + "::ExecutionOrderDisplay::" + id + "::" + name + "::Start()");
    }

    void Update() {
        // enabling the next line clogs the console, obviously
        // Debug.Log(checkMode()+"::ExecutionOrderDisplay::"+id+"::"+name+"::Update()");
    }

    void OnDisable() {
        Debug.Log(checkMode() + "::ExecutionOrderDisplay::" + id + "::" + name + "::OnDisable()");
    }

    void OnDestroy() {
        Debug.Log(checkMode() + "::ExecutionOrderDisplay::" + id + "::" + name + "::OnDestroy()");
    }
}