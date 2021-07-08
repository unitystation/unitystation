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

Logprint = input(''' see all the cool Numbers and letters Y/N ''')
if Logprint == 'Y':
    Logprint = True
else:
    Logprint = False


start_time = time.time()

AID = 0
Name_store = set([])

for root, dirs, files in os.walk(path):
    #print(root)
    for name in files:
        if ".png" in name:
            #if name in Name_store:
            Edited_Name = name.replace('.png','.json')
            jsonFilePath = os.path.join(root, (Edited_Name))
            print(name + os.path.basename(root) )
            if os.path.exists(jsonFilePath):
                new_file = os.path.join(root,os.path.basename(root) + "_" + Edited_Name)
                #print(new_file)
                os.rename(jsonFilePath,new_file)
            #print(os.path.basename(root))
            old_file = os.path.join(root, name)
            new_file = os.path.join(root,os.path.basename(root)+ "_" + name)
            os.rename(old_file,new_file)
            #print(new_file)
            #else:
                #Name_store.add(name)

print(AID)

print("--- %s seconds ---" % (time.time() - start_time)) #Total time
