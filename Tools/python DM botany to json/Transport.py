import os

text_buffer_global = ['List_of_plants = []\nList_of_produce = []\n']
inherent_global = {}

inheritance_Dictionary = {}

def file_len(fname):
    with open(fname) as opened_file:
        return sum(1 for _ in opened_file)

#for a in range(0,file_len("ethy ithis.txt")):
    #pass
#if "blah" not in somestring:


#print(file_len("ethy i()this.txt"))
def name_replace(class_name):
    class_name = class_name.replace("'",' ')
    class_name = class_name.replace("name = ",'')
    class_name = class_name.replace(":",'')
    class_name = class_name.replace("\\ ",'')
    class_name = class_name.replace('(','')
    class_name = class_name.replace(')','')
    class_name = class_name.replace('-','_')
    class_name = class_name.replace('_ ','')
    return class_name.replace(' ','')

def woek_lichgons(opened_file, text_buffer, inherent):
    burer = []
    for line in opened_file:
        line = line.replace('\n','')
        line = line.replace('\t','')
        #print(p)
        if '/obj/item/seeds' in line and all(substr not in line for substr in
                ('list(', '=', ',', ')')):
            burer = [line]

        elif not line:

            if burer:
                print("\n\n", burer)
                burer_piopes(burer,text_buffer, inherent)

                burer = []
        elif burer:
            #print("yoyyo \n" + p)
            burer.append(line)

def genes_replace(line):
    genes = line
    genes = genes.replace('/datum/plant_gene/trait/repeated_harvest','"Perennial_Growth"')
    genes = genes.replace('/datum/plant_gene/trait/plant_type/fungal_metabolism',
            '"Fungal Vitality"')
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
    return genes.replace(')',']')

def mutates_replace(line):
    mutates_into = line
    mutates_into = mutates_into.replace('mutatelist','mutates_into')
    mutates_into = mutates_into.replace('/obj/item/seeds/','')
    mutates_into = mutates_into.replace('/','_')
    mutates_into = mutates_into.replace(', ',',')
    mutates_into = mutates_into.replace(',','","')
    mutates_into = mutates_into.replace('list(','["')
    return mutates_into.replace(')','"]')


def chemical_replace(line):
    chemical_content = line
    chemical_content = chemical_content.replace('= list','@list')
    chemical_content = chemical_content.replace('list','')
    chemical_content = chemical_content.replace(')','}')
    chemical_content = chemical_content.replace('(','{')
    chemical_content = chemical_content.replace('=','\':')

    chemical_content = chemical_content.replace('/datum/reagent/medicine/','\'')
    chemical_content = chemical_content.replace('/datum/reagent/drug/','\'')
    chemical_content = chemical_content.replace('/datum/reagent/consumable/nutriment/','\'')
    chemical_content = chemical_content.replace('/datum/reagent/consumable/','\'')
    chemical_content = chemical_content.replace('/datum/reagent/','\'')


    chemical_content = chemical_content.replace(':@','=')
    chemical_content = chemical_content.replace('@','=')
    chemical_content = chemical_content.replace('={','= {')
    chemical_content = chemical_content.replace(' : ','\':')
    return chemical_content.replace(' \'','\'')


def add_trait_to_full(full, trait, trait_name, default, inheritance,
        inherent, tl_naem, inheritance_default):

    if trait_name in inheritance_default and not trait:
        trait = inheritance_default[trait_name]

    if default and not trait:
        trait = f'{trait_name} = {default}'

    if trait:
        if inheritance:
            inherent[tl_naem][trait_name] = trait

        trait = F"\t{trait}\n"
        full = full + trait

    return full, trait

def initialize_data():
    data = {}
    data['name'] = ''
    data['namename'] = ''
    data['description'] = ''
    data['seed_packet_name'] = ''
    data['plantname'] = ''
    data['lifespan'] = ''
    data['endurance'] = ''
    data['production'] = ''
    data['plant_yield'] = ''
    data['potency'] = ''
    data['weed_growth_rate'] = ''
    data['weed_resistance'] = ''

    data['species'] = ''

    data['growing_sprites_folder'] = ''
    data['grown_sprite'] = ''
    data['dead_sprite'] = ''
    data['genes'] = ''
    data['mutates_into'] = ''
    data['chemical_content'] = ''
    return data

def continue_process_burer(line, data):
    if 'production' in line:
        data['production'] = line

    elif 'yield' in line:
        plant_yield = line
        data['plant_yield'] = plant_yield.replace('yield', 'plant_yield')
    elif 'potency' in line:
        data['potency'] = line

    elif 'growing_icon' in line:
        growing_sprites_folder = line
        growing_sprites_folder = growing_sprites_folder.replace(
            "icons/obj/hydroponics/", '')
        data['growing_sprites_folder'] = growing_sprites_folder.replace(
            ".dmi", '')
    elif 'icon_grow' in line:
        grown_sprite = line
        data['grown_sprite'] = grown_sprite.replace('icon_grow',
                                                    'Grown_Sprite')
    elif 'icon_dead' in line:
        dead_sprite = line
        data['dead_sprite'] = dead_sprite.replace('icon_dead', 'dead_Sprite')

    elif 'genes' in line:
        data['genes'] = genes_replace(line)

    elif 'mutatelist' in line:
        data['mutates_into'] = mutates_replace(line)

    elif 'reagents_add' in line:
        # print(line)
        data['chemical_content'] = chemical_replace(line)

    elif 'species' in line:
        data['species'] = line

def process_burer(line, data):
    # print(line)
    line = line.split('//')[0]
    if not 'desc' not in line:
        line = line.replace('"', "'")

    # print(line)
    if '/obj/item/seeds/' in line and all(substr not in line for substr in
                                          ('list(', '=', ',', ')')):
        name = line
        name = name.replace("/obj/item/seeds/", '')
        data['topline'] = name
        name = name.replace("/", '_')
        name = name.replace("seed = ", '')

        data['name'] = F"name = '{name}'"
        # display_name = display_name.replace('(','')
        # display_name = display_name.replace(')','')
        # display_name = display_name.replace(' ','')
        # display_name = display_name.replace(' ','')

    elif 'plantname' in line:
        data['namename'] = line
        # description = description.replace("'",'"')

    elif 'desc' in line:
        description = line
        data['description'] = description.replace('desc', 'Description')
        # description = descriptionreplace("'", '"')

    elif 'icon_state' in line:
        data['seed_packet_name'] = line

        # materials = materials.replace('MAT_GLASS', 'Glass')
        # materials = materials.replace('MAT_GLASS', 'Glass')

    elif 'plantname' in line:
        data['plantname'] = line
        # print(category)
        # category = category.replace('list (','[')
        # category = category.replace('list(','[')
        # category = category.replace(')',']')
        # prereq_ids = prereq_ids.replace('(','[')

    elif 'lifespan' in line:
        data['lifespan'] = line
    elif 'endurance' in line:
        data['endurance'] = line

    else:
        continue_process_burer(line, data)


def burer_piopes(burer, text_buffer, inherent):
    inheritance = False
    inheritance_default = {}
    topline = ''
    tl_naem = ''
    full = ''

    data = initialize_data()

    # print(burer)
    for line in burer:
        process_burer(line, data)

    name = data['name']
    namename = data['namename']
    description = data['description']
    seed_packet_name = data['seed_packet_name']
    plantname = data['plantname']
    lifespan = data['lifespan']
    endurance = data['endurance']
    production = data['production']
    plant_yield = data['plant_yield']
    potency = data['potency']
    weed_growth_rate = data['weed_growth_rate']
    weed_resistance = data['weed_resistance']

    species = data['species']

    growing_sprites_folder = data['growing_sprites_folder']
    grown_sprite = data['grown_sprite']
    dead_sprite = data['dead_sprite']
    genes = data['genes']
    mutates_into = data['mutates_into']
    chemical_content = data['chemical_content']

    if "/" not in topline:
        inheritance = True
    else:
        inheritance = False
        inheritance_default = inherent[topline.split('/')[0]]

    if name:
        # print(name,'yooooooooooooooooo')
        class_name = name_replace(name)
        if inheritance:
            inherent[class_name] = {}
            tl_naem = class_name
        class_name = F"class {class_name}():\n"
        full = class_name
        # print(class_name)

        name = F"\t{name}\n"
        full = full + name

    full, namename = add_trait_to_full(full, namename, "namename", None,
            inheritance, inherent, tl_naem, inheritance_default)

    full, description = add_trait_to_full(full, description, "Description", None,
            inheritance, inherent, tl_naem, inheritance_default)

    full, seed_packet_name = add_trait_to_full(full, seed_packet_name, "Seed_packet_name", None,
            inheritance, inherent, tl_naem, inheritance_default)

    full, plantname = add_trait_to_full(full, plantname, "plantname", None,
            inheritance, inherent, tl_naem, inheritance_default)

    full, lifespan = add_trait_to_full(full, lifespan, "lifespan", "25",
            inheritance, inherent, tl_naem, inheritance_default)

    full, endurance = add_trait_to_full(full, endurance, "endurance", "15",
            inheritance, inherent, tl_naem, inheritance_default)

    full, production = add_trait_to_full(full, production, "production", "6",
            inheritance, inherent, tl_naem, inheritance_default)

    full, plant_yield = add_trait_to_full(full, plant_yield, "plant_yield", "3",
            inheritance, inherent, tl_naem, inheritance_default)

    full, potency = add_trait_to_full(full, potency, "potency", "10",
            inheritance, inherent, tl_naem, inheritance_default)

    full, weed_growth_rate = add_trait_to_full(full, weed_growth_rate, "weed_growth_rate", "1",
            inheritance, inherent, tl_naem, inheritance_default)

    full, weed_resistance = add_trait_to_full(full, weed_resistance, "weed_resistance", "5",
            inheritance, inherent, tl_naem, inheritance_default)

    full, growing_sprites_folder = add_trait_to_full(full, growing_sprites_folder,
            "growing_Sprites_folder", None, inheritance, inherent, tl_naem, inheritance_default)

    full, grown_sprite = add_trait_to_full(full, grown_sprite, "Grown_Sprite", None,
            inheritance, inherent, tl_naem, inheritance_default)

    full, dead_sprite = add_trait_to_full(full, dead_sprite, "dead_Sprite", None,
            inheritance, inherent, tl_naem, inheritance_default)

    full, genes = add_trait_to_full(full, genes, "genes", None,
            inheritance, inherent, tl_naem, inheritance_default)

    full, chemical_content = add_trait_to_full(full, chemical_content, "Chemical_content",
            None, inheritance, inherent, tl_naem, inheritance_default)

    if mutates_into:
        mutates_into = F"\t{mutates_into}\n"
        full = full + mutates_into

    if species:
        species = F"\t{species}\n"
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

    clipped_class_name = class_name.replace('():\n','').replace('class ','')
    full = F"{full}\nList_of_plants.append({clipped_class_name})\n"
    text_buffer.append(full)
    print(full)

def read_produce(opened_file, text_buffer, inherent):
    burer = []
    for line in opened_file:
        line = line.replace('\n','')
        line = line.replace('\t','')
        #print(p)
        if '/obj/item/reagent_containers' in line and all(substr not in line for substr in
                ('list(', '=', ',', ')')):
            burer = [line]

        elif not line:

            if burer:
                print("\n\n", burer)
                create_produce(burer, text_buffer, inherent)

                burer = []
        elif burer:
            burer.append(line)

def create_produce(burer, text_buffer, inherent):
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
        if '/obj/item/reagent_containers/' in line and all(substr not in line for substr in
                ('list(', '=', ',', ')')):
            name = line
            name = name.replace("/obj/item/reagent_containers/food/snacks/grown/",'')
            name = name.replace("/",'_')
            name = name.replace("seed = ",'')

            name = F"name = '{name}'"
        elif 'name'  in line:
            pretty_name = line
        elif 'desc'  in line:
            description = line


    if name:
        class_name = name_replace(name)

        class_name = F"class {class_name}():\n"
        full = class_name

    if name:
        name = F"\t{name}\n"
        full = full + name
    if pretty_name:
        pretty_name = pretty_name.replace('name','pretty_name')
        pretty_name = F"\t{pretty_name}\n"
        full = full + pretty_name
    if description:
        description = F"\t{description}\n"
        full = full + description
##    name = ''

    clipped_class_name = class_name.replace('():\n','').replace('class ','')
    full = F"{full}\nList_of_produce.append({clipped_class_name})\n"
    text_buffer.append(full)
    print(full)


print("List_of_plants = []")
for root, dirs, files in os.walk("plants"):

    #for name in files:
    #    if ".dm" in name:
    #
    #        f = open(F"plants/{name}","r+")
    #        #create plants for file
    #        woek_lichgons(f, text_buffer_global, inherent_global)
    #
    #
    #        f.close()
    for filename in files:
        if ".dm" in filename:
            f = open(F"plants/{filename}","r+")
            read_produce(f, text_buffer_global, inherent_global)
            f.close()

FILENAME_PY = 'produce.py'
py_file = open(FILENAME_PY,'w+')
py_file.writelines(text_buffer_global)
py_file.close()
