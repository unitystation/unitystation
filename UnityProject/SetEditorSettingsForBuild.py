import os
#python 3
isto = "3"
setto = "4"
with open('./UnityProject/ProjectSettings/EditorSettings.asset', "r+") as RmetaFile:
    Data = RmetaFile.read()
    RmetaFile.close()
    print(Data)
    metaFile = open('./UnityProject/ProjectSettings/EditorSettings.asset',"w+")
    Data = Data.replace('m_SpritePackerMode: '+ isto,"m_SpritePackerMode: "+ setto)
    print(Data)
    metaFile.write(Data)
    metaFile.close()
