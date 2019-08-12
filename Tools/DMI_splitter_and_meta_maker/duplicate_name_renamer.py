import os
from PIL import Image #Requires pillow
import time
#import json
import shutil #Requires shutil
import simplejson
import to_filename
from decimal import Decimal
#python 3 


path = 'example' #E.G The name of a folder everything is in Just set it to bypass  The Manual input
if not path: 
    path = input('''The name of the folder with the DMI textures in.
Plonk name of folder here >> ''')

Logprint = input(''' see all the cool Numbers and letters Y/N ''')
if Logprint == 'Y':
    Logprint = True
else:
    Logprint = False


start_time = time.time()


Name_store = set([])
    
for root, dirs, files in os.walk(path):
    print(root)
    for name in files:
        if name in Name_store:
            #renam stuff
        else:
            Name_store.Add(name)
        
        

print(Name_store)

print("--- %s seconds ---" % (time.time() - start_time)) #Total time











    

