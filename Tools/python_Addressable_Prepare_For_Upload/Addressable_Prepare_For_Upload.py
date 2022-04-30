import sys 
import os
import json


print("Have you done an addressable bundle build in the editor to ensure its up-to-date? and deleted the old bundle build?")
In = input("y or n \n")

if In != "y":
    print("either you entered something other than 'y' or you haven't done it yet so do it!")
    input("...")
    sys.exit()
else:
    print("good")

paths = os.getcwd()

paths = paths.replace("\\", "/")

paths = paths.replace("Tools/python_Addressable_Prepare_For_Upload", "")
print(paths)

paths = paths + "UnityProject/AddressablePackingProjects/"

print(paths)

print(os.listdir(paths))

newPthas = []

Folders = os.listdir(paths)

for Folder in Folders:
    Folder_Name = Folder
    Folder = paths + Folder  + "/ServerData/"


    Files = os.listdir(Folder)
    print(Folder)
    
    for File in Files:
        if ".json" in File:
            FilePath =  Folder + File
            print(FilePath)
            with open(FilePath, "r+") as f:
                d = json.load(f)
                i = 0

                for InternalIdsin in d["m_InternalIds"]:
                    if "AddressablePackingProjects" in InternalIdsin:
                        InternalIdsin = InternalIdsin.replace("AddressablePackingProjects", "https://unitystationfile.b-cdn.net/Addressables")
                        InternalIdsin = InternalIdsin.replace("/ServerData", "")
                        print(InternalIdsin)
                        d["m_ProviderIds"][i] = InternalIdsin
                    i = i + 1
                f.close()
                
            open(FilePath, "w").close()
            with open(FilePath, "w") as f:
                json.dump(d, f)
                f.close()

        
            txtptahs = Folder + Folder_Name + ".txt"
            print(txtptahs)
            with open(txtptahs, "w+") as f:
                f.write('https://unitystationfile.b-cdn.net/Addressables/' + Folder_Name + "/" + File )
                newPthas.append("https://unitystationfile.b-cdn.net/Addressables/" + Folder_Name + "/" + Folder_Name + ".txt" )
                
                
            
    
#Tools\python_Addressable_Prepare_For_Upload
input("""
========================================================

now go to the respective folders and upload them to the respective folders on the CDN E.G
   unitystation\\UnityProject\\AddressablePackingProjects\\SoundAndMusic\\ServerData\\ to Addressables/SoundAndMusic

  are you done? press enter if you are""")

input("""
========================================================

now go to
https://panel.bunny.net/purge

And in the Purge URL List enter individually, without the quotes! and press the button 

Press enter when done
""" +   str(newPthas))


input("""
========================================================

Good job everything is done press enter to exit bye

""")
