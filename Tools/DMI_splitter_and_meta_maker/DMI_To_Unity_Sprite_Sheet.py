import os
from PIL import Image #Requires pillow
import time
#import json
import shutil
import simplejson
import to_filename
from decimal import Decimal
#python 3 

path = 'UnityStation' #E.G The name of a folder everything is in Just set it to bypass  The Manual input
if not path: 
    path = input('''The name of the folder with the DMI textures in.
Plonk name of folder here >> ''')

seecool = input(''' see all the cool Numbers and letters Y/N ''')
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
            print(_name)

            DelayIndex = 1
            
            new_file = os.path.join(root, _name)
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
                break
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
            if seecool == 'Y':
                print('Elements', ElementsWidth, ElementsHeight)
            LoadingIndex = False
            Spriteindex = 1
            Loadedindexes = []

            


            for Indexheight in range(0, ElementsHeight):
                for IndexWidth in range(0, ElementsWidth):
                    
                    #cell_Image = Image.new('RGBA', CellSize, color=0)
                    if seecool == 'Y':
                        print(IndexToCoordinates(CellSize,(Indexheight,IndexWidth)))
                    cell_Image = image.crop(IndexToCoordinates(CellSize,(Indexheight,IndexWidth)))
                    if cell_Image.size[0] == 0 or cell_Image.size[1] == 0:
                        print(CellSize,(Indexheight,IndexWidth))
                        print(IndexToCoordinates(CellSize,(Indexheight,IndexWidth)))
                        input("ghf")
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
                            break

                        IndexOfdirs = find_nth(Description,'dirs = ',  Spriteindex)
                        ittn = int(StringToEndOfline(Description,IndexOfdirs, 7 ))
                        if NumberOfFrames > 1 or ittn > 1:
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
                                

                                for IndexWidth in range(0, ittn):
                                    animationDelays.append(odelay)
                
                            #for Indexheight in range(0, ElementsHeight)
                            preNumberOfFrames = NumberOfFrames
                            NumberOfFrames = ittn * NumberOfFrames;
                            LoadingIndex = True
                            WorkingOnIndex['name'] = to_filename.clean_filename(name)
                            if WorkingOnIndex['name'] == '':
                               WorkingOnIndex['name'] = str(time.time())
                            WorkingOnIndex['Covering_Indexes'] = [cell_Image]
                            WorkingOnIndex['Number_Of_Variants'] = ittn
                            WorkingOnIndex['Frames_Of_Animation'] = preNumberOfFrames
                            WorkingOnIndex['Frames_Left'] = NumberOfFrames - 1
                            if len(animationDelays) > 0:
                                #print(animationDelays)
                                WorkingOnIndex['Delays']  = animationDelays

                            
                        else:
                            WorkingOnIndex['name'] = name
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

            
            for D in Loadedindexes:
                if len(D['Covering_Indexes']) > 1:
                    
                    lengthOfImage = len(D['Covering_Indexes']) * D['Covering_Indexes'][0].size[0]
                    cell_Image = Image.new('RGBA', (lengthOfImage, D['Covering_Indexes'][0].size[1]), color=0)
                    c = 0
                    for Lindex in D['Covering_Indexes']:
                        if seecool == 'Y':
                            print('Index location of animated sprite', c)
                            print(Lindex.size)
                            print(IndexToCoordinates(Lindex.size, (0,c)))
                        #input("T")
                        #Lindex.show()
                        #try:
                        cell_Image.paste(Lindex, IndexToCoordinates(Lindex.size, (0,c)))
                        #except ValueError:
                            #cell_Image.show()
                            #Lindex.show()
                            #input("GG")
                        c = c + 1
                    D['Covering_Indexes'] = [cell_Image]

            _name = _name.replace(".dmi", "")
            if seecool == 'Y':
                print(_name)
            
            newFolder = os.path.join(root, _name)
            if not os.path.exists(newFolder):
                os.mkdir(newFolder)
            
            for h in Loadedindexes:
                tim = str(time.time())
                if h['name'] == '\\':
                    h['name'] = 'oh no'

                if 'Delays' in h:
                    #print(h)
                    FilePath =  os.path.join(newFolder, (h['name'] + '.json'))
                    descriptionof = {'Delays':h['Delays']}
                    descriptionof['Number_Of_Variants']= h['Number_Of_Variants']
                    descriptionof['Frames_Of_Animation']= h['Frames_Of_Animation']
                    try:
                        with open(FilePath,'w') as json_data:
                            simplejson.dump(descriptionof, json_data, indent=4)
                            json_data.close()
                    except:
                        FilePath =  os.path.join(newFolder, (tim + '.json'))
                        with open(FilePath,'w') as json_data:
                            simplejson.dump(descriptionof, json_data, indent=4)
                            json_data.close()
                FilePath =  os.path.join(newFolder, (h['name'] + '.png'))
                #print(FilePath)
                try:
                    metaFilePath = os.path.join(newFolder, (h['name'] + '.png.meta'))
                    shutil.copyfile("example.meta", metaFilePath)
                    h['Covering_Indexes'][0].save(FilePath)
                except:
                    
                    metaFilePath = os.path.join(newFolder, (tim + '.png.meta'))
                    FilePath =  os.path.join(newFolder, (tim + '.png'))
                    shutil.copyfile("example.meta", metaFilePath)
                    h['Covering_Indexes'][0].save(FilePath)
                    #pass
            #print(os.path.join(root, _name))
            #os.remove(os.path.join(root, _name + ".dmi"))
     


print("--- %s seconds ---" % (time.time() - start_time)) #Total time











    

