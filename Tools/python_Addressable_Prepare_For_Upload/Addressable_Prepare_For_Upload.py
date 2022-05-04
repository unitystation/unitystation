import sys 
import os
import json
import shutil



print("Have you done an addressable bundle build in the editor to ensure its up-to-date? and deleted the old bundle build?")
In = "y" # input("y or n \n")

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

Addressable_path = os.getcwd()

Addressable_path = Addressable_path.replace("\\", "/")

Addressable_path = Addressable_path.replace("Tools/python_Addressable_Prepare_For_Upload", "AddressableContent/")


if os.path.isdir(Addressable_path):
    shutil.rmtree(Addressable_path)
    os.mkdir(Addressable_path)
else:
    os.mkdir(Addressable_path)





newPthas = []

Folders = os.listdir(paths)

for Folder in Folders:
    Folder_Name = Folder
    Folder = paths + Folder  + "/ServerData/"
    Local_Addressable_path = Addressable_path  + "/" + Folder_Name + "/"
    
    if os.path.isdir(Local_Addressable_path):
        shutil.rmtree(Local_Addressable_path)
        os.mkdir(Local_Addressable_path)
    else:
        os.mkdir(Local_Addressable_path)
    
    Files = os.listdir(Folder)
    for File in Files:
        FilePath =  Folder + File
        shutil.copy(FilePath, Local_Addressable_path)

print("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")
print(Addressable_path)
Folders = os.listdir(Addressable_path)
print(Folders)
for Folder in Folders:
    Folder_Name = Folder
    Folder = Addressable_path + Folder+ "/"

    Files = os.listdir(Folder)
    
    print(Folder)
    
    for File in Files:
        if ".json" in File:
            FilePath = Folder + File
            print(FilePath)
            with open(FilePath, "r+") as f:
                d = json.load(f)
                i = 0

                for InternalIdsin in d["m_InternalIds"]:
                    if "AddressablePackingProjects" in InternalIdsin:
                        InternalIdsin = InternalIdsin.replace("AddressablePackingProjects", "https://raw.githubusercontent.com/unitystation/unitystation/AddressableContent")
                        InternalIdsin = InternalIdsin.replace("/ServerData", "")
                        print(InternalIdsin)
                        d["m_ProviderIds"][i] = InternalIdsin
                    i = i + 1
                f.close()

 
            with open(FilePath, "w") as f:
                json.dump(d, f)
                f.close()

        
            txtptahs = Folder + Folder_Name + ".txt"
            print(txtptahs)
            with open(txtptahs, "w+") as f:
                f.write('https://raw.githubusercontent.com/unitystation/unitystation/AddressableContent/' + Folder_Name + "/" + File )
                newPthas.append("https://raw.githubusercontent.com/unitystation/unitystation/AddressableContent/" + Folder_Name + "/" + Folder_Name + ".txt" )
                
print(str(newPthas))
            
    
#Tools\python_Addressable_Prepare_For_Upload
input("""
========================================================

now Commit your changes and make a PR

  are you done? press enter if you are""")



input("""
========================================================

Good job just have to wait for the PR to be merged, press enter to exit bye

""")
