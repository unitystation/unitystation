import os
#python 3
isto = "3"
setto = "4"
with open('./UnityProject/ProjectSettings/EditorSettings.asset', "r+") as RmetaFile:
    Data = RmetaFile.read()
    RmetaFile.close()
    metaFile = open('./UnityProject/ProjectSettings/EditorSettings.asset',"w+")
    metaFile.write(Data.replace('m_SpritePackerMode: '+ isto,"m_SpritePackerMode: "+ setto))
    metaFile.close()
