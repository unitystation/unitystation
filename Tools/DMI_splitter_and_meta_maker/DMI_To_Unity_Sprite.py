import os
from PIL import Image #Requires pillow
import time
#import json
import shutil #Requires shutil
import simplejson
import to_filename
from decimal import Decimal
#python 3 

StandardInsert = '''
    - serializedVersion: 2
      name: {{{
      rect:
        serializedVersion: 2
        x: ££
        y: 0
        width: **
        height: ##
      alignment: 0
      pivot: {x: 0, y: 0}
      border: {x: 0, y: 0, z: 0, w: 0}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: b16489681e753a24ab39d1f4a26cd82a
      vertices: []
      indices: 
      edges: []
      weights: []'''

path = 'example' #E.G The name of a folder everything is in Just set it to bypass  The Manual input
if not path: 
    path = input('''The name of the folder with the DMI textures in.
Plonk name of folder here >> ''')

Logprint = input(''' see all the cool Numbers and letters Y/N ''')
if Logprint == 'Y':
    Logprint = True
else:
    Logprint = False


Delete_File = input('''Delete DMI files after export Y/N ''')
if Delete_File == 'Y':
    Delete_File = True
else:
    Delete_File = False

    
start_time = time.time()
def find_nth(haystack, needle, n):
    start = haystack.find(needle)
    while start >= 0 and n > 1:
        start = haystack.find(needle, start+len(needle))
        n -= 1
    return start

def StringToEndOfline(string,startingIndex,offset,endCharacter = '\n'):
    addString = ''
    Count = 0
    while string[(startingIndex+offset+Count)] != endCharacter:
        addString = addString + string[(startingIndex+offset+Count)]
        Count = Count + 1
    return addString

def IndexToCoordinates(CellSize,indexLocation):
    Cellheight,Cellwidth = CellSize
    Vertical, Horizontal = indexLocation
    #32,63
    left = (Cellheight*Horizontal)
    top = (Cellwidth*Vertical)
    bottom = (Cellwidth*(1+Vertical))
    right  = (Cellheight*(1+Horizontal))  
    return((left,top,right,bottom))

WorkingOnIndex = {}
    
for root, dirs, files in os.walk(path):
    print(root)
    for _name in files:
        
        if _name.endswith((".dmi")):
            
            File_Time = time.time()
            DelayIndex = 1
            
            new_file = os.path.join(root, _name)
            print(new_file)
            #os.rename(old_file,new_file)

            image = Image.open(new_file)
            #print(image.info['Description'] )
            #input("yo")

            Width = 0
            Height = 0

            FoundLocation = image.info['Description'].find('width')
            Count = 0
            offset = 8
            addString = ''
            while image.info['Description'][(FoundLocation+offset+Count)] != '\n':
                addString = addString + image.info['Description'][(FoundLocation+offset+Count)]
                Count = Count + 1
            if addString == ' DMI':
                continue
            Width = int(addString)
            Count = 0
            offset = 8
            addString = ''

            FoundLocation = image.info['Description'].find('height')
            while image.info['Description'][(FoundLocation+offset+Count)] != '\n':
                addString = addString + image.info['Description'][(FoundLocation+offset+Count)]
                Count = Count + 1

            Height = int(addString)
            IMwidth, IMheight = image.size

            CellSize = Width,Height

            Description = image.info['Description']

            TotalElementIndex = 0

            ElementsWidth = int(IMwidth/Width)
            ElementsHeight = int(IMheight/Height)
            if Logprint:
                print('Elements', ElementsWidth, ElementsHeight)
            LoadingIndex = False
            Spriteindex = 1
            Loadedindexes = []

            


            for Indexheight in range(0, ElementsHeight):
                for IndexWidth in range(0, ElementsWidth):
                    
                    #cell_Image = Image.new('RGBA', CellSize, color=0)
                    if Logprint:
                        print(IndexToCoordinates(CellSize,(Indexheight,IndexWidth)))
                    cell_Image = image.crop(IndexToCoordinates(CellSize,(Indexheight,IndexWidth)))
                    #print(IndexToCoordinates(CellSize,(Indexheight,IndexWidth)))
                    #cell_Image.show()
                    #Dictionary_images[(Indexheight, IndexWidth)] = cell_Image
                    if not LoadingIndex:
                        StringIndex = find_nth(Description,'state = "',  Spriteindex)
                        
                        name = StringToEndOfline(Description,StringIndex, 9 )
                        name = name.replace('"','')
                        
                        #if name == 'DMI':
                            #Spriteindex = Spriteindex + 1
                            #break
                        #print(name)
                        IndexOfFrames = find_nth(Description,'frames = ',  Spriteindex)
                        try:
                            NumberOfFrames = int(StringToEndOfline(Description,IndexOfFrames, 9 ))
                        except:
                            Spriteindex = Spriteindex + 1
                            continue

                        IndexOfdirs = find_nth(Description,'dirs = ',  Spriteindex)
                        number_of_variants = int(StringToEndOfline(Description,IndexOfdirs, 7 ))
                        if NumberOfFrames > 1 or number_of_variants > 1:
                            animationDelays = []
                            #cell_Image.show()
                            if NumberOfFrames > 1:
                                IndexOfDelays = find_nth(Description,'delay = ',  DelayIndex)
                                delay = StringToEndOfline(Description,IndexOfDelays, 8 ).split(",")
                                #print(name)
                                #print(delay)
                                DelayIndex = DelayIndex + 1
                            #print(delay);
                                odelay = []
                                for _delay in delay:
                                    if not " DMI" == _delay:
                                        odelay.append(Decimal(_delay)/10)
                                #print(odelay)
                                

                                for IndexWidth in range(0, number_of_variants):
                                    animationDelays.append(odelay)
   

                            #for Indexheight in range(0, ElementsHeight)
                            preNumberOfFrames = NumberOfFrames
                            NumberOfFrames = number_of_variants * NumberOfFrames;
                            LoadingIndex = True
                            WorkingOnIndex['name'] = to_filename.clean_filename(name, replace=' .')
                            if WorkingOnIndex['name'] == '':
                               WorkingOnIndex['name'] = str(time.time())
                            WorkingOnIndex['Covering_Indexes'] = [cell_Image]
                            WorkingOnIndex['Number_Of_Variants'] = number_of_variants
                            WorkingOnIndex['Frames_Of_Animation'] = preNumberOfFrames
                            WorkingOnIndex['Total_Sprites'] = NumberOfFrames
                            WorkingOnIndex['Frames_Left'] = NumberOfFrames - 1
                            if number_of_variants > 1 or preNumberOfFrames > 1:
                                #print(animationDelays)
                                WorkingOnIndex['Delays']  = animationDelays
                        else:
                            WorkingOnIndex['name'] = to_filename.clean_filename(name, replace=' .')
                            if WorkingOnIndex['name'] == '':
                               WorkingOnIndex['name'] = str(time.time())
                            WorkingOnIndex['Covering_Indexes'] = [cell_Image]
                            Loadedindexes.append(WorkingOnIndex.copy())
                            WorkingOnIndex = {}  
                        Spriteindex = Spriteindex + 1
                            
                            
                    elif 'Frames_Left' in WorkingOnIndex: 
                        WorkingOnIndex['Covering_Indexes'].append(cell_Image)
                        WorkingOnIndex['Frames_Left'] = WorkingOnIndex['Frames_Left']  - 1
                        if WorkingOnIndex['Frames_Left'] == 0:
                            Loadedindexes.append(WorkingOnIndex.copy())
                            WorkingOnIndex = {} 
                            LoadingIndex = False
                    TotalElementIndex = TotalElementIndex + 1

            
            for IndividualSprite in Loadedindexes:
                if len(IndividualSprite['Covering_Indexes']) > 1:
                    
                    lengthOfImage = len(IndividualSprite['Covering_Indexes']) * IndividualSprite['Covering_Indexes'][0].size[0]
                    cell_Image = Image.new('RGBA', (lengthOfImage, IndividualSprite['Covering_Indexes'][0].size[1]), color=0)
                    c = 0
                    for Lindex in IndividualSprite['Covering_Indexes']:
                        if Logprint:
                            print('Index location of animated sprite', c)
                            print(Lindex.size)
                            print(IndexToCoordinates(Lindex.size, (0,c)))
                        cell_Image.paste(Lindex, IndexToCoordinates(Lindex.size, (0,c)))

                        c = c + 1
                    IndividualSprite['Covering_Indexes'] = [cell_Image]

            _name = _name.replace(".dmi", "")
            if Logprint:
                print(_name)
            
            newFolder = os.path.join(root, _name)
            if not os.path.exists(newFolder):
                os.mkdir(newFolder)
            
            for IndividualSprite in Loadedindexes:
                tim = str(time.time())
    
                if 'Delays' in IndividualSprite:
                    FilePath =  os.path.join(newFolder, (IndividualSprite['name'] + '.json'))
                    descriptionof = {'Delays':IndividualSprite['Delays']}
                    descriptionof['Number_Of_Variants']= IndividualSprite['Number_Of_Variants']
                    descriptionof['Frames_Of_Animation']= IndividualSprite['Frames_Of_Animation']
                    with open(FilePath,'w') as json_data:
                        simplejson.dump(descriptionof, json_data, indent=4)
                        json_data.close()
                        
                FilePath =  os.path.join(newFolder, (IndividualSprite['name'] + '.png'))
                metaFilePath = os.path.join(newFolder, (IndividualSprite['name'] + '.png.meta'))
                if 'Total_Sprites' in IndividualSprite:
                    if IndividualSprite['Total_Sprites'] > 1:
                        toaddto = []
                        for MultiSpriteIndex in range(0, IndividualSprite['Total_Sprites']):
                            newinsert = StandardInsert
                            newinsert = newinsert.replace('££',str(int(MultiSpriteIndex*Width)))
                            newinsert = newinsert.replace('**',str(Width)) 
                            newinsert = newinsert.replace('##',str(Height)) 
                            newinsert = newinsert.replace('{{{',IndividualSprite['name']+"_"+str(MultiSpriteIndex))
                            toaddto.append(newinsert)
                        Ttotal = ''
                        for bob in toaddto:
                            Ttotal = Ttotal + bob
                        shutil.copyfile("SpriteSheetMeta.meta", metaFilePath)
                        f = open(metaFilePath, 'r+')
                        contents = f.read()
                        f.close()
                        metaFile = open(metaFilePath,"w+")
                        contents = contents.replace('##',Ttotal)
                        metaFile.write(contents)
                        metaFile.close()
                else:
                    shutil.copyfile("SingleSpriteMeta.meta", metaFilePath)
                
                IndividualSprite['Covering_Indexes'][0].save(FilePath)
            if Delete_File:
                os.remove(os.path.join(root, _name + ".dmi"))
                


print("--- %s seconds ---" % (time.time() - start_time)) #Total time











    

