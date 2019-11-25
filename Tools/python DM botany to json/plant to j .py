
import json

import plans



#import pdb; pdb.set_trace()  breakpoint()

  
Plants = []


for Design in plans.List_of_plants:
    Dictionary = {'name':Design.name}

    if hasattr(Design,'plantname'):
        Dictionary['plantname'] = Design.plantname
        
    if hasattr(Design,'Description'):
        Dictionary['Description'] = Design.Description

    if hasattr(Design,'icon_state'):
        Dictionary['seed_packet'] = Design.icon_state
        
    if hasattr(Design,'lifespan'):
        Dictionary['lifespan'] = Design.lifespan

    if hasattr(Design,'endurance'):
        Dictionary['endurance'] = Design.endurance

    if hasattr(Design,'production'):
        Dictionary['production'] = Design.production

    if hasattr(Design,'plant_yield'):
        Dictionary['plant_yield'] = Design.plant_yield

    if hasattr(Design,'potency'):
        Dictionary['potency'] = Design.potency

    if hasattr(Design,'weed_growth_rate'):
        Dictionary['weed_growth_rate'] = Design.weed_growth_rate

    if hasattr(Design,'weed_resistance'):
        Dictionary['weed_resistance'] = Design.weed_resistance

    if hasattr(Design,'growing_icon'):
        Dictionary['growing_icon'] = Design.growing_icon

    if hasattr(Design,'dead_Sprite'):
        Dictionary['dead_Sprite'] = Design.dead_Sprite

    if hasattr(Design,'dead_Sprite'):
        Dictionary['dead_Sprite'] = Design.dead_Sprite

    if hasattr(Design,'genes'):
        Dictionary['genes'] = Design.genes

    if hasattr(Design,'mutates_into'):
        Dictionary['mutates_into'] = Design.mutates_into

    if hasattr(Design,'reagents_add'):
        Dictionary['reagents_add'] = Design.reagents_add


    if hasattr(Design,'species'):
        Dictionary['species'] = Design.species
        
    Plants.append(Dictionary)
    #print(Dictionary)
    
Filename = "plants"+'.json'
print(Filename)
with open(Filename,'w') as json_data:
    json.dump(Plants, json_data, indent=4)
    json_data.close()
    #the_json = json.load(json_data)
