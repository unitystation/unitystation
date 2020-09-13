using UnityEngine;
using System.Collections;
using DentedPixel;

public class TestingPunch : MonoBehaviour {

    public AnimationCurve exportCurve;
    public float overShootValue = 1f;

    private LTDescr descr;
	
	void Start () {
	   //LeanTween.rotateAround(gameObject, gameObject.transform.rotation.eulerAngles, 360f, 5f).setDelay(1f).setEase(LeanTweenType.easeOutBounce);
        Debug.Log( "exported curve:" + curveToString(exportCurve) );
	}

	void Update () 
    {        
        LeanTween.dtManual = Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            //LeanTween.scale(this.gameObject, Vector3.one*3f, 1.0f).setEase(LeanTweenType.easeSpring).setUseManualTime(true);            
            //print("scale punch time independent!");

            LeanTween.moveLocalX(gameObject, 5, 1).setOnComplete( () => {
            Debug.Log("on complete move local X");
            }).setOnCompleteOnStart(true);

            GameObject light = GameObject.Find("DirectionalLight");
            Light lt = light.GetComponent<Light>(); 
        
            LeanTween.value(lt.gameObject, lt.intensity, 0.0f, 1.5f)
              .setEase(LeanTweenType.linear)
              .setLoopPingPong()
              .setRepeat(-1)
              .setOnUpdate((float val)=>{
                
            lt.intensity = val;
            });
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            print("scale punch!");

            tweenStatically( this.gameObject );

            LeanTween.scale(this.gameObject, new Vector3(1.15f, 1.15f, 1.15f), 0.6f);

            LeanTween.rotateAround(this.gameObject, Vector3.forward, -360f, 0.3f).setOnComplete(() =>
            {
                LeanTween.rotateAround(this.gameObject, Vector3.forward, -360f, 0.4f).setOnComplete(() =>
                {
                    LeanTween.scale(this.gameObject, new Vector3(1f, 1f, 1f), 0.1f);

                    LeanTween.value(this.gameObject, (v) =>
                    {
                        
                    }, 0, 1, 0.3f).setDelay(1);

                });

            });

            
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Vector3[] pts = new Vector3[] {new Vector3(-1f,0f,0f), new Vector3(0f,0f,0f), new Vector3(4f,0f,0f), new Vector3(20f,0f,0f)};
            descr = LeanTween.move(gameObject, pts, 15f).setOrientToPath(true).setDirection(1f).setOnComplete( ()=>{
                Debug.Log("move path finished");
            });
        }

        if (Input.GetKeyDown(KeyCode.Y)) // Reverse the move path
        {
            descr.setDirection(-descr.direction);
        }
 
        if (Input.GetKeyDown(KeyCode.R))
        {
            // LeanTween.rotate(this.gameObject, Vector3.one, 1.0f).setEase(LeanTweenType.punch);
            LeanTween.rotateAroundLocal(this.gameObject, this.transform.forward, -80f, 5.0f).setPoint(new Vector3(1.25f, 0f, 0f));
            print("rotate punch!");
        }
 
        if (Input.GetKeyDown(KeyCode.M))
        {
            // LeanTween.move(this.gameObject, new Vector3(0f,0f,1f), 1.0f).setEase(LeanTweenType.punch);
            print("move punch!");
            Time.timeScale = 0.25f;
            float start = Time.realtimeSinceStartup;
            LeanTween.moveX( this.gameObject, 1f, 1f).setOnComplete( destroyOnComp ).setOnCompleteParam( this.gameObject ).setOnComplete( ()=>{
                float end = Time.realtimeSinceStartup;
                float diff = end - start;
                Debug.Log("start:"+start+" end:"+end+" diff:"+diff+" x:"+this.gameObject.transform.position.x);
            }).setEase(LeanTweenType.easeInBack).setOvershoot( overShootValue ).setPeriod(0.3f);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            LeanTween.color( this.gameObject, new Color(1f, 0f, 0f, 0.5f), 1f);

            Color to = new Color(Random.Range(0f,1f),0f,Random.Range(0f,1f),0.0f);
            GameObject l = GameObject.Find("LCharacter");
            LeanTween.color( l, to, 4.0f ).setLoopPingPong(1).setEase(LeanTweenType.easeOutBounce);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            LeanTween.delayedCall(gameObject,0.3f, delayedMethod).setRepeat(4).setOnCompleteOnRepeat(true).setOnCompleteParam( "hi" );
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            LeanTween.value( gameObject, updateColor, new Color(1.0f,0.0f,0.0f,1.0f), Color.blue, 4.0f );//.setRepeat(2).setLoopPingPong().setEase(LeanTweenType.easeOutBounce);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            LeanTween.delayedCall(0.05f, enterMiniGameStart).setOnCompleteParam( new object[]{""+5} );
        }

        if(Input.GetKeyDown(KeyCode.U)){
            #if !UNITY_FLASH
            LeanTween.value(gameObject, (Vector2 val)=>{
                // Debug.Log("tweening vec2 val:"+val);
                transform.position = new Vector3(val.x, transform.position.y, transform.position.z);
            }, new Vector2(0f,0f), new Vector2(5f,100f), 1f ).setEase(LeanTweenType.easeOutBounce);

            GameObject l = GameObject.Find("LCharacter");
            Debug.Log("x:"+l.transform.position.x+" y:"+l.transform.position.y);
            LeanTween.value(l, new Vector2( l.transform.position.x, l.transform.position.y), new Vector2( l.transform.position.x, l.transform.position.y+5), 1f ).setOnUpdate( 
            (Vector2 val)=>{
                Debug.Log("tweening vec2 val:"+val);
                 l.transform.position = new Vector3(val.x, val.y, transform.position.z);
            }

            );
            #endif
        }
	}

    static void tweenStatically( GameObject gameObject ){
        Debug.Log("Starting to tween...");
        LeanTween.value(gameObject, (val)=>{
            Debug.Log("tweening val:"+val);
        }, 0f, 1f, 1f);
    }

    void enterMiniGameStart( object val ){
        object[] arr = (object [])val;
        int lvl = int.Parse((string)arr[0]);
        Debug.Log("level:"+lvl);
    }

    void updateColor( Color c ){
        GameObject l = GameObject.Find("LCharacter");
        // Debug.Log("new col:"+c);
        l.GetComponent<Renderer>().material.color = c;
    }

    void delayedMethod( object myVal ){
        string castBack = myVal as string;
        Debug.Log("delayed call:"+Time.time +" myVal:"+castBack);
    }

    void destroyOnComp( object p ){
      GameObject g = (GameObject)p;
      Destroy( g );
    }

    string curveToString( AnimationCurve curve) {
        string str = "";
        for(int i = 0; i < curve.length; i++){
            str += "new Keyframe("+curve[i].time+"f, "+curve[i].value+"f)";
            if(i<curve.length-1)
                str += ", ";
        }
        return "new AnimationCurve( "+str+" )";
    }
}
