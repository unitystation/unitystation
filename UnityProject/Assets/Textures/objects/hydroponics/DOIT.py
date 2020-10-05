import os
#python 3 
print('hey Make sure to make a backup of the icons and Make sure this file is in the root of TG station Should work with other code bases as long as you pointed to the folder with all the textures ')
print('[WARNING!] Will rename all Dmi files to PNG losing all Dmi metadata (it should technically keep it but if you open it with a image editor and save it will lose it then)')
path = '' #E.G The name of a folder everything is in Just set it to bypass  The Manual input
if not path: 
    path = input('''The name of the folder with the textures in.
Plonk name of folder here >> ''')
    
for root, dirs, files in os.walk(path):
    print(root)
    for name in files:
        if not "seeds_" in name:
            print(name)
            #if ".meta" not in name:
                #name = name.replace('.asset','')
                #
            nameold = name
            
            name =  "seeds_" + name
            old_file = os.path.join(root, nameold)
            new_file = os.path.join(root, name)
            os.rename(old_file,new_file)
