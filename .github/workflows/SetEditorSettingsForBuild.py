import os
#python 3
#This is needed to prevent unity from crashing for some reason when Unity is trying to build This only happens when
#Atlas packing mode is set to builds only
isto = "3"
setto = "4"
with open('./UnityProject/ProjectSettings/EditorSettings.asset', "r+") as RmetaFile:
    Data = RmetaFile.read()
    RmetaFile.close()
    if not "m_SpritePackerMode: "+isto in Data:
        print("error, Sprite packing mode is already set or Ivor set to a different valuemaking the script redundant")
    
    metaFile = open('./UnityProject/ProjectSettings/EditorSettings.asset',"w+")
    Data = Data.replace('m_SpritePackerMode: '+ isto,"m_SpritePackerMode: "+ setto)
    metaFile.write(Data)
    metaFile.close()
