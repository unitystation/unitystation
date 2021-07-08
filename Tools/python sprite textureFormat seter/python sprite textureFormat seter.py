import os
from PIL import Image #Requires pillow
import time
#import json
import shutil #Requires shutil
import simplejson
from decimal import Decimal
#python 3


path = 'textures' #E.G The name of a folder everything is in Just set it to bypass  The Manual input
if not path:
    path = input('''The name of the folder with the DMI textures in.
Plonk name of folder here >> ''')


isto = input(''' What is the current value of  textureFormat: just put int value ''')
setto = input(''' textureFormat: Set what value just put int value ''')



start_time = time.time()

AID = 0
Name_store = set([])

for root, dirs, files in os.walk(path):
    print(root)
    for name in files:
        if ".png.meta" in name:
            #if name in Name_store:
            metaFilePath = os.path.join(root, (name))
            with open(metaFilePath, "r+") as RmetaFile:
                Data = RmetaFile.read()
                RmetaFile.close()
            metaFile = open(metaFilePath,"w+")
            metaFile.write(Data.replace('textureFormat: '+ isto,"textureFormat: "+ setto))
            metaFile.close()
            AID = AID + 1

print(AID, " Files")

print("--- %s seconds ---" % (time.time() - start_time)) #Total time
