import json
import os
from PIL import Image #Requires pillow
import time
#import json
import shutil #Requires shutil
import simplejson
from decimal import Decimal

Text_buffer_global = ['List_of_plants = []\nList_of_produce = []\n']
Inherent = {}

Inheritance_Dictionary = {}
        
def file_len(fname):
    with open(fname) as f:
        for i, l in enumerate(f):
            pass
    return i + 1

#for a in range(0,file_len("ethy ithis.txt")):
    #pass
#if "blah" not in somestring: 


#print(file_len("ethy i()this.txt"))

    
def woek_lichgons(f):
    burer = []
    for i, l in enumerate(f):

        p = l.replace('\n','')
        p = p.replace('\t','')
        #print(p)
        if '/obj/item/seeds' in p and not 'list(' in p and not '=' in p and not ',' in p and not ')' in p: 
            burer = [p]
        
        elif not p:
            
            if burer:
                print("\n\n", burer)
                burer_piopes(burer)
                
                burer = []
        elif burer:
            #print("yoyyo \n" + p)
            burer.append(p)


def burer_piopes(burer):
    global Text_buffer_global
    global hierarchy
    global Inherent
    
    Inheritance = False
    Inheritance_default = {}
    topline = ''
    TL_naem = ''
    
    full = ''
    name = ''
    namename = ''
    Description = ''
    Seed_packet_name = ''
    plantname = ''
    lifespan = ''
    endurance = ''
    production = ''
    plant_yield = ''
    potency = ''
    weed_growth_rate = '' 
    weed_resistance = ''

    species = ''
    
    growing_Sprites_folder = '' 
    Grown_Sprite = ''
    dead_Sprite = ''
    genes = ''
    mutates_into = ''
    Chemical_content = ''
    
    #print(burer)
    for line in burer:
        #print(line)
        line = line.split('//')[0]
        if not 'desc' in line:
            line = line.replace('"',"'")

        #print(line)
        if '/obj/item/seeds/' in line and not 'list(' in line and not '=' in line and not ',' in line and not ')' in line:
            name = line
            name = name.replace("/obj/item/seeds/",'')
            topline = name
            name = name.replace("/",'_')
            name = name.replace("seed = ",'')
            
            name = 'name = ' + "'" + name + "'"
            #display_name = display_name.replace('(','')
            #display_name = display_name.replace(')','')
            #display_name = display_name.replace(' ','')
            #display_name = display_name.replace(' ','')
            
        elif 'plantname' in line:
            namename = line
            #description = description.replace("'",'"')
                 
        elif 'desc' in line:
            Description = line
            Description = Description.replace('desc','Description')
            #description = description.replace("'",'"')
            
        elif 'icon_state' in line:
            Seed_packet_name = line

            #materials = materials.replace('MAT_GLASS','Glass')
            #materials = materials.replace('MAT_GLASS','Glass')
            
        elif 'plantname'  in line:
            plantname = line
            #print(category)
##            category = category.replace('list (','[')
##            category = category.replace('list(','[')
##            category = category.replace(')',']')
            #prereq_ids = prereq_ids.replace('(','[')


            
            
        elif 'lifespan'  in line:
            lifespan = line
        elif 'endurance'  in line:
            endurance = line
        elif 'production'  in line:
            production = line
            
        elif 'yield'  in line:
            plant_yield = line
            plant_yield = plant_yield.replace('yield','plant_yield')
        elif 'potency'  in line:
            potency = line

        elif 'growing_icon'  in line:
            growing_Sprites_folder = line
            growing_Sprites_folder = growing_Sprites_folder.replace("icons/obj/hydroponics/",'')
            growing_Sprites_folder = growing_Sprites_folder.replace(".dmi",'')
        elif 'icon_grow'  in line:
            Grown_Sprite = line
            Grown_Sprite = Grown_Sprite.replace('icon_grow','Grown_Sprite')
        elif 'icon_dead'  in line:
            dead_Sprite = line
            dead_Sprite = dead_Sprite.replace('icon_dead','dead_Sprite')
        
        elif 'genes' in line:
            
            genes = line
            genes = genes.replace('/datum/plant_gene/trait/repeated_harvest','"Perennial_Growth"')
            genes = genes.replace('/datum/plant_gene/trait/plant_type/fungal_metabolism','"Fungal Vitality"')
            genes = genes.replace('/datum/plant_gene/trait/squash','"Liquid Contents"')
            genes = genes.replace('/datum/plant_gene/trait/slip','"Slippery Skin"')
            genes = genes.replace('/datum/plant_gene/trait/teleport','"Bluespace Activity"')
            genes = genes.replace('/datum/plant_gene/trait/maxchem','"Densified Chemicals"')
            genes = genes.replace('/datum/plant_gene/trait/battery','"Capacitive Cell Production"')
            genes = genes.replace('/datum/plant_gene/trait/plant_type/weed_hardy','"Weed Adaptation"')
            genes = genes.replace('/datum/plant_gene/trait/stinging','"Hypodermic Prickles"')
            genes = genes.replace('/datum/plant_gene/trait/glow/shadow','"Shadow Emission"')
            genes = genes.replace('/datum/plant_gene/trait/glow/red','"Red Electrical Glow"')
            genes = genes.replace('/datum/plant_gene/trait/cell_charge','"Electrical Activity"')#
            genes = genes.replace('/datum/plant_gene/trait/glow/berry','"Strong Bioluminescence"')
            genes = genes.replace('/datum/plant_gene/trait/glow','"Bioluminescence"')
            genes = genes.replace('/datum/plant_gene/trait/noreact','"Separated Chemicals"')
            genes = genes.replace('] | [',',')
            genes = genes.replace('list(','[')
            genes = genes.replace(')',']')
            
        elif 'mutatelist' in line:
            mutates_into = line
            mutates_into = mutates_into.replace('mutatelist','mutates_into')
            mutates_into = mutates_into.replace('/obj/item/seeds/','')
            mutates_into = mutates_into.replace('/','_')
            mutates_into = mutates_into.replace(', ',',')
            mutates_into = mutates_into.replace(',','","')
            mutates_into = mutates_into.replace('list(','["')
            mutates_into = mutates_into.replace(')','"]')
       

        elif 'reagents_add' in line:
            #print(line)
            Chemical_content = line
            Chemical_content = Chemical_content.replace('= list','@list')
            Chemical_content = Chemical_content.replace('list','')
            Chemical_content = Chemical_content.replace(')','}')
            Chemical_content = Chemical_content.replace('(','{')
            Chemical_content = Chemical_content.replace('=','\':')

            Chemical_content = Chemical_content.replace('/datum/reagent/medicine/','\'')
            Chemical_content = Chemical_content.replace('/datum/reagent/drug/','\'')
            Chemical_content = Chemical_content.replace('/datum/reagent/consumable/nutriment/','\'')
            Chemical_content = Chemical_content.replace('/datum/reagent/consumable/','\'')
            Chemical_content = Chemical_content.replace('/datum/reagent/','\'')

            
            Chemical_content = Chemical_content.replace(':@','=')
            Chemical_content = Chemical_content.replace('@','=')
            Chemical_content = Chemical_content.replace('={','= {')
            Chemical_content = Chemical_content.replace(' : ','\':')
            Chemical_content = Chemical_content.replace(' \'','\'')
            
        elif 'species' in line:
            species = line
            


    if not "/" in topline:
        Inheritance = True
    else:
        Inheritance = False
        Inheritance_default = Inherent[topline.split('/')[0]] 
        
      
    if name:

        #print(name,'yooooooooooooooooo')
        class_name = name.replace("'",' ')
        class_name = class_name.replace("name = ",'')
        class_name = class_name.replace(":",'')
        class_name = class_name.replace("\ ",'')
        class_name = class_name.replace('(','')
        class_name = class_name.replace(')','')
        class_name = class_name.replace('-','_')
        class_name = class_name.replace('_ ','')
        class_name = class_name.replace(' ','')
        if Inheritance:
            Inherent[class_name] = {}
            TL_naem = class_name
        class_name = 'class ' + class_name +'():\n'
        full = class_name
        #print(class_name)
 
 
    if name:
        name = '\t'+name+'\n'
        full = full + name
    

    if "namename" in Inheritance_default and not namename:
        namename = Inheritance_default["namename"]
    
    if namename:
        if Inheritance:
            Inherent[TL_naem]["namename"] = namename 
            
        namename = '\t'+namename+'\n'
        full = full + namename

    if "Description" in Inheritance_default and not Description:
        Description = Inheritance_default["Description"]

    
    if Description:
        if Inheritance:
            Inherent[TL_naem]["Description"] = Description 
        Description = '\t'+Description+'\n'
        full = full + Description
 
    if "Seed_packet_name" in Inheritance_default and not Seed_packet_name:
        Seed_packet_name = Inheritance_default["Seed_packet_name"]
        
    if Seed_packet_name:
        if Inheritance:
            Inherent[TL_naem]["Seed_packet_name"] = Seed_packet_name 
        Seed_packet_name = '\t'+Seed_packet_name+'\n'
        full = full + Seed_packet_name

    if "plantname" in Inheritance_default and not plantname:
        plantname = Inheritance_default["plantname"]

    if plantname:
        if Inheritance:
            Inherent[TL_naem]["plantname"] = plantname 
        plantname = '\t'+plantname+'\n'
        full = full + plantname



    if "lifespan" in Inheritance_default and not lifespan:
        lifespan = Inheritance_default["lifespan"]

    if not lifespan:
        lifespan = 'lifespan = 25'

    if lifespan:
        if Inheritance:
            Inherent[TL_naem]["lifespan"] = lifespan 
        lifespan = '\t'+lifespan+'\n'
        full = full + lifespan


    if "endurance" in Inheritance_default and not endurance:
        endurance = Inheritance_default["endurance"]

    if not endurance:
        endurance = 'endurance = 15'
    
    if endurance:
        if Inheritance:
            Inherent[TL_naem]["endurance"] = endurance 
        endurance = '\t'+endurance+'\n'
        full = full + endurance


    if "production" in Inheritance_default and not production:
        production = Inheritance_default["production"]

    if not production:
        production = 'production = 6'

    if production:
        if Inheritance:
            Inherent[TL_naem]["production"] = production 
        production = '\t'+production+'\n'
        full = full + production








    if "plant_yield" in Inheritance_default and not plant_yield:
        plant_yield = Inheritance_default["plant_yield"]

    if not plant_yield:
        plant_yield = 'plant_yield = 3'

    if plant_yield:
        if Inheritance:
            Inherent[TL_naem]["plant_yield"] = plant_yield 
        plant_yield = '\t'+plant_yield+'\n'
        full = full + plant_yield

    if "potency" in Inheritance_default and not potency:
        potency = Inheritance_default["potency"]

    if not potency:
        potency = 'potency = 10'
        
    if potency:
        if Inheritance:
            Inherent[TL_naem]["potency"] = potency 
        potency = '\t'+potency+'\n'
        full = full + potency

    if "weed_growth_rate" in Inheritance_default and not weed_growth_rate:
        weed_growth_rate = Inheritance_default["weed_growth_rate"]  

    if not weed_growth_rate:
        weed_growth_rate = 'weed_growth_rate = 1' 

    if weed_growth_rate:
        if Inheritance:
            Inherent[TL_naem]["weed_growth_rate"] = weed_growth_rate 
        weed_growth_rate = '\t'+weed_growth_rate+'\n'
        full = full + weed_growth_rate

    if "weed_resistance" in Inheritance_default and not weed_resistance:
        weed_resistance = Inheritance_default["weed_resistance"]

    if not weed_resistance:
        weed_resistance = 'weed_resistance = 5'
    
    if weed_resistance:
        if Inheritance:
            Inherent[TL_naem]["weed_resistance"] = weed_resistance 
        weed_resistance = '\t'+weed_resistance+'\n'
        full = full + weed_resistance

    if "growing_Sprites_folder" in Inheritance_default and not growing_Sprites_folder:
        growing_Sprites_folder = Inheritance_default["growing_Sprites_folder"]

    if growing_Sprites_folder:
        if Inheritance:
            Inherent[TL_naem]["growing_Sprites_folder"] = growing_Sprites_folder 
        growing_Sprites_folder = '\t'+growing_Sprites_folder+'\n'
        full = full + growing_Sprites_folder

    if "Grown_Sprite" in Inheritance_default and not Grown_Sprite:
        Grown_Sprite = Inheritance_default["Grown_Sprite"]
        
    if Grown_Sprite:
        if Inheritance:
            Inherent[TL_naem]["Grown_Sprite"] = Grown_Sprite 
        Grown_Sprite = '\t'+Grown_Sprite+'\n'
        full = full + Grown_Sprite

    if "dead_Sprite" in Inheritance_default and not dead_Sprite:
        dead_Sprite = Inheritance_default["dead_Sprite"]

    if dead_Sprite:
        if Inheritance:
            Inherent[TL_naem]["dead_Sprite"] = dead_Sprite 
        dead_Sprite = '\t'+dead_Sprite+'\n'
        full = full + dead_Sprite

    if "genes" in Inheritance_default and not genes:
        genes = Inheritance_default["genes"]

    if genes:
        if Inheritance:
            Inherent[TL_naem]["genes"] = genes 
        genes = '\t'+genes+'\n'
        full = full + genes


        
    if mutates_into:
        mutates_into = '\t'+mutates_into+'\n'
        full = full + mutates_into
        
    if "Chemical_content" in Inheritance_default and not Chemical_content:
        Chemical_content = Inheritance_default["Chemical_content"]
    
    if Chemical_content:
        if Inheritance:
            Inherent[TL_naem]["Chemical_content"] = Chemical_content 
        Chemical_content = '\t'+Chemical_content+'\n'
        full = full + Chemical_content

    if species:
        species = '\t'+species+'\n'
        full = full + species


    
##    name = ''
##    Description = ''
##    Seed_packet_name = ''
##    plantname = ''
##    lifespan = 'lifespan = 25'
##    endurance = 'endurance = 15'
##    production = 'production = 6'
##    plant_yield = 'plant_yield = 3'
##    potency = 'potency = 10'
##    weed_growth_rate = 'weed_growth_rate = 1' 
##    weed_resistance = 'weed_resistance = 5'
##    
##    growing_Sprites_folder = '' 
##    Grown_Sprite = ''
##    dead_Sprite = ''
##    genes = ''
##    mutates_into = ''
##    Chemical_content = ''

    full = full + '\n' + 'List_of_plants.append(' + class_name.replace('():\n','').replace('class ','') + ')' + '\n'
    Text_buffer_global.append(full)
    print(full)

def read_produce(f):
    burer = []
    for i, l in enumerate(f):

        p = l.replace('\n','')
        p = p.replace('\t','')
        #print(p)
        if '/obj/item/reagent_containers' in p and not 'list(' in p and not '=' in p and not ',' in p and not ')' in p: 
            burer = [p]
        
        elif not p:
            
            if burer:
                print("\n\n", burer)
                create_produce(burer)
                
                burer = []
        elif burer:
            burer.append(p)

def create_produce(burer):
    global Text_buffer_global
    global hierarchy
    global Inherent
    
    Inheritance = False
    Inheritance_default = {}
    topline = ''
    TL_naem = ''
    
    name = ''
    pretty_name = ''
    description = ''
    
    #print(burer)
    for line in burer:
        #print(line)
        line = line.split('//')[0]
        if not 'desc' in line:
            line = line.replace('"',"'")

        #print(line)
        if '/obj/item/reagent_containers/' in line and not 'list(' in line and not '=' in line and not ',' in line and not ')' in line:
            name = line
            name = name.replace("/obj/item/reagent_containers/food/snacks/grown/",'')
            topline = name
            name = name.replace("/",'_')
            name = name.replace("seed = ",'')
            
            name = 'name = ' + "'" + name + "'"
        elif 'name'  in line:
            pretty_name = line
        elif 'desc'  in line:
            description = line
            
        
            
        
      
    if name:
        class_name = name.replace("'",' ')
        class_name = class_name.replace("name = ",'')
        class_name = class_name.replace(":",'')
        class_name = class_name.replace("\ ",'')
        class_name = class_name.replace('(','')
        class_name = class_name.replace(')','')
        class_name = class_name.replace('-','_')
        class_name = class_name.replace('_ ','')
        class_name = class_name.replace(' ','')

        class_name = 'class ' + class_name +'():\n'
        full = class_name

    if name:
        name = '\t'+name+'\n'
        full = full + name
    if pretty_name:
        pretty_name = pretty_name.replace('name','pretty_name')
        pretty_name = '\t'+pretty_name+'\n'
        full = full + pretty_name
    if description:
        description = '\t'+description+'\n'
        full = full + description
        

##    name = ''

    full = full + '\n' + 'List_of_produce.append(' + class_name.replace('():\n','').replace('class ','') + ')' + '\n'
    Text_buffer_global.append(full)
    print(full)



print("List_of_plants = []")
for root, dirs, files in os.walk("plants"):
    
    #for name in files:
    #    if ".dm" in name:
    #        
    #        f = open("plants/"+  name,"r+")
    #        #create plants for file
    #        woek_lichgons(f)
    #        
    #
    #        f.close()
    for name in files:
        if ".dm" in name:
            f = open("plants/"+  name,"r+")
            read_produce(f)
            f.close()

Filenamepy = 'produce.py'
PY_File = open(Filenamepy,'w+')
PY_File.writelines(Text_buffer_global)
PY_File.close()


