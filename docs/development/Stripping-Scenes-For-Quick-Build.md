<h1>First way:</h1>
1. Navigate to Tools/QuickBuild in a toolbar

![image](https://user-images.githubusercontent.com/19672958/151049515-226bd02b-03aa-4258-8d32-689f2564ad13.png)

2. Pick settings you need and press Build.

![image](https://user-images.githubusercontent.com/19672958/151050881-190bfb81-17e9-4d2f-99d7-99c9657b8750.png)


<br>

<h1>Second way:</h2>

1. Go to UnityProject\Assets\ScriptableObjects\SubScenes
2. Open AwayWorldList and remove all scenes from there 
3. Do the same for AsteroidListSO
4. Open AdditionalSceneList and remove all the scenes, except CentCom
5. Open MainStationList and remove all stations except one you will be using


<h2>Note:</h2>
Map is picked at round start based on current player count. Values for that are specified in maps.json

![image](https://user-images.githubusercontent.com/19672958/151050113-f69ee28d-620f-485f-93f0-ad1d87719dfe.png)

You wont be able to start a round if current player amount is less than PopMinLimit, so I suggest using **FallStation** or **SquareStation** for testing.
