#V0.9
#needs BoxStation.json From unitystation\UnityProject\Assets\Resources\Metadata BoxStation.7z
import json
import time
import math
import sys
import cProfile
import pyglet





sys.setrecursionlimit(9000)



Look_UP =  {'Oxygen': 0.659, 'Nitrogen': 0.743, 'Plasma': 0.8,'Carbon Dioxide':0.655}
Molar_Masses = {'Oxygen':31.9988,'Nitrogen': 28.0134 ,'Plasma': 40,  'Carbon Dioxide':44.01} 

edge_tiles = []


Plasma_fuel_list = []
Plasma_fuel_set = set(Plasma_fuel_list)
update_list = []
update_set = set(update_list)
Space_list = []
Space_set = set(Space_list)

Has_done = {}
check_Reaction_Dictionary = {}
Air = {}
is_space = {}
Dictionary_of_adjacents = {}
Check_count_Dictionary = {}
Check_count_Dictionary_Moving = {}

Temporary = {}
Temporary2 = {}
is_space_2 = {}

R = 8.3144598





Tile_range = [360,210]

r = range(0,Tile_range[0]+1)
r2 = range(0,Tile_range[1]+1)



a = []
for z in r:
    
    x = []
    for p in r2:
        tuple_z_p = tuple((z,p))
        Temporary['Temperature'] = 293.15
        Temporary['Pressure'] = 101.325

        
        Temporary2['Oxygen'] = 16.628484400890768491815384755837
        Temporary2['Nitrogen']= 66.513937603563073967261539023347
        Temporary['Mix'] = Temporary2.copy()
        Temporary['Moles'] = 83.142422004453842459076923779184
        Temporary['Fire'] = 0         
        Temporary['Obstructed'] = False
        Temporary['Space'] = True 
        Air[tuple_z_p] = Temporary.copy()
        

is_space['Temperature'] = 2.7
is_space['Pressure'] = 0.000000316

is_space_2['Oxygen'] = 0.000000000000281

is_space['Mix'] = is_space_2.copy() 
is_space['Moles'] = 0.000000000000281
is_space['Fire'] = 0 
is_space['Obstructed'] = False
is_space['Space'] = True


with open('BoxStation.json') as json_data:
    the_json = json.load(json_data)
    #print(the_json["Walls1"]["tilePositions"])
    insex = 0
    insey = 0

    for hr in the_json["Walls1"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Obstructed'] = True

    for hr in the_json["Walls0"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Obstructed'] = True
        
        
    for hr in the_json["Walls-1"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Obstructed'] = True
        #print((insex,insey), Air[(insex,insey)]['Obstructed'])
    for hr in the_json["Floors150"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False

    for hr in the_json["Floors100"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False
    
    for hr in the_json["Floors50"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False 

    for hr in the_json["Floors20"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False
        
    for hr in the_json["Floors1"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False
        

    for hr in the_json["Floors0"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False

    for hr in the_json["UnderFloor1"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False

    #for hr in the_json["UnderFloor0"]["tilePositions"]:
    #    inse = []
    #    insex = hr['x']
    #    insey = hr['y']
    #    Air[(insex,insey)]['Space'] = False


        
##~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


#Air[54,66]['Obstructed'] = True
#Air[9,70]['Obstructed'] = True 
#Air[2,57]['Obstructed'] = True 
#Air[3,57]['Obstructed'] = True 
#Air[4,57]['Obstructed'] = True 
#Air[14+8,24]['Obstructed'] = True 
#Air[14+8,28]['Obstructed'] = True 
#Air[20,12]['Obstructed'] = True 
#Air[25,7]['Obstructed'] = True
#Air[25+6,7]['Obstructed'] = True 
#Air[25+6+4,7]['Obstructed'] = True 
#Air[25+6+4+5+2,7]['Obstructed'] = True
#Air[25+6+4+5+2,3]['Obstructed'] = True
#Air[60,13+6]['Obstructed'] = True 
#Air[60,13+7]['Obstructed'] = True
#Air[60-4,13+7+4]['Obstructed'] = True
#Air[60+2,13+7+4+12]['Obstructed'] = True
#Air[60-24,13+7+4+12+30+6]['Obstructed'] = True



##~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~        
#print(Air[(50,50)]['Obstructed'],'hey?')



def Orientation(tile):
    T = []
    p = [[0,0],[1,0],[0,1],[-1,0],[0,-1]]
    for Z in p:
        a = list(tile)
        b = a[0]+Z[0]
        c = []
        c.append(b)
        s = a[1]+Z[1]
        c.append(s)
        if (not (c[0] > Tile_range[0] or c[0] < 0 or  c[1] > Tile_range[1] or c[1] < 0)):
            T.append(tuple(c))

    return(T)


def Making_Dictionary_of_adjacents():
    
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            The_adjacent = Orientation((z,p))
            Dictionary_of_adjacents[(z,p)] = The_adjacent
    print('Making_Dictionary_of_adjacents Done!')
    
Making_Dictionary_of_adjacents()

#print(Dictionary_of_adjacents[(50,50)],'Dictionary_of_adjacents')

def Worse_case_update_set():
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            if Air[(z,p)]['Obstructed'] == False: 
                update_set.add((z,p))
    print('done!', len(r),len(r2))

Worse_case_update_set()

def Worse_case_edge_tiles():
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            if Air[(z,p)]['Obstructed'] == False: 
                edge_tiles.append((z,p))
    print('Worse_case_edge_tiles done!', len(r),len(r2))

#Worse_case_edge_tiles()
#print(len(edge_tiles))

def Making_check_count_Dictionary_and_Check_count_Dictionary_Moving():
    
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            Check_count_Dictionary[(z,p)] = 0
            Check_count_Dictionary_Moving[(z,p)] = 0
    print('Check_count_Dictionary and Check count Dictionary Moving Done!')
Making_check_count_Dictionary_and_Check_count_Dictionary_Moving()



def Making_space_set():
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            if Air[(z,p)]['Space'] == True:
                if Air[(z,p)]['Obstructed'] == False:
                    Space_set.add((z,p))
                else:
                    Air[(z,p)]['Space'] = False

    print('Making_space_set Done!')

Making_space_set()



def old_Visual_check():
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        x = []
        for p in r2:
            print((z,p),Air[(z,p)]['Pressure'])
            print((z,p),Air[(z,p)]['Moles'])




def Setting_space():
    for Space_tile in Space_set:
            Air[Space_tile] = is_space.copy()
            
Setting_space()

#@numba.jit

#@profile
def new_pressure():
    update_list = []
    for Origin_tile in update_set:
        are_different_pressure = Dictionary_of_adjacents[Origin_tile]
        mix_calculation_dictionary = {}
        JM_calculation_dictionary = {}
        Tile_working_on = []
        c = False
        nall = 0
        count = 0
        T = 0
        for tile in are_different_pressure:
            if Air[tile]['Obstructed'] == False:
                for key, value in Air[tile]['Mix'].items():
                    mix_calculation_dictionary[key] = (value + mix_calculation_dictionary.get(key, 0))
                    JM_calculation_dictionary[key] = ((Air[tile]['Temperature'] * value) * Look_UP[key]) + JM_calculation_dictionary.get(key, 0)
                if not math.isclose(Air[tile]['Pressure'], Air[Origin_tile]['Pressure'], rel_tol=1e-5, abs_tol=0.0):
                    c = True
                count += 1
                Tile_working_on.append(tile)

        for key in mix_calculation_dictionary:
            key_mix_worked_out = (mix_calculation_dictionary[key] / count)
            nall += key_mix_worked_out
            
            JM_calculation_dictionary[key] = (((JM_calculation_dictionary[key] /  Look_UP[key]) / key_mix_worked_out) / len(JM_calculation_dictionary)) 
            T += (JM_calculation_dictionary[key] / count)
            Air[Origin_tile]['Mix'][key] = key_mix_worked_out
              
        P = ((nall*R*T)/(2)/1000)   
        for tile in Tile_working_on:
            if Air[tile]['Space'] == False:
                for key in mix_calculation_dictionary:
        
                    if key == 'Plasma':
                        if Air[Origin_tile]['Mix']['Plasma'] > 1:
                            Plasma_fuel_set.add(tile)
                            
                    Air[tile]['Mix'][key] = Air[Origin_tile]['Mix'][key]
                Air[tile]['Temperature'] = T
                Air[tile]['Moles'] = nall
                Air[tile]['Pressure'] = P
            
        if c == False:
            if Check_count_Dictionary_Moving[Origin_tile] == 1:
                update_list.append(Origin_tile)
                global edge_tiles
                edge_tiles.append(Origin_tile)
                Check_count_Dictionary_Moving[Origin_tile] = 0
            else:
                Check_count_Dictionary_Moving[Origin_tile] += 1

    #hkjbn
    for Tile_remove in update_list:
        update_set.remove(Tile_remove)

#new_pressure()
#new_pressure.inspect_types()


def Air_Reactions():
    #print(Plasma_fuel_set,'Plasma_fuel_set')
    Plasma_fuel_set_copy = Plasma_fuel_set.copy()
    for Tile in Plasma_fuel_set_copy:
        if Air[Tile]['Temperature'] > 200:
            #print('Temperature')
            if Air[Tile]['Mix']['Oxygen'] > 1:
                #print('Oxygen')
                #print(Air[Tile]['Mix'],Tile )
                old_tem = Air[Tile]['Temperature']
                Carbon_Dioxide_Amount = Air[Tile]['Mix']['Oxygen'] + Air[Tile]['Mix']['Plasma']
                JM = ((Air[Tile]['Temperature'] * Carbon_Dioxide_Amount) * Look_UP['Carbon Dioxide'])
                J = Molar_Masses['Carbon Dioxide'] * JM
                J =+ -1000
                JM = J / Molar_Masses['Carbon Dioxide']
                Air[Tile]['Temperature'] = ((JM /  Look_UP['Carbon Dioxide']) / Carbon_Dioxide_Amount)
                #print(old_tem,'old',Air[Tile]['Temperature'] ,'new')
                del Air[Tile]['Mix']['Oxygen']
                del Air[Tile]['Mix']['Plasma']
                Air[Tile]['Mix']['Carbon Dioxide'] = Carbon_Dioxide_Amount
                Plasma_fuel_set.remove(Tile)



#@numba.jit
                
#@profile
def Do_the_edge():
    global edge_tiles
    #print(edge_tiles)
    if not edge_tiles:
        return
    
    List_to_go_into_update_set = []
    new_edge_tiles = []
    edge_tiles_set = set(edge_tiles)
    try:
        for tile in edge_tiles_set:
            the_orientation = Dictionary_of_adjacents[tile].copy()
            del the_orientation[0]
            C = 0
            Count_for_update_set = 0
            for Tile_orientated in the_orientation:
                Tile_orientated_tuple = tuple(Tile_orientated)
                if Air[Tile_orientated]['Obstructed'] == False:
                    if not math.isclose(Air[Tile_orientated_tuple]['Pressure'], Air[tile]['Pressure'], rel_tol=1e-5, abs_tol=0.0):
                        C += 1

                        if not Tile_orientated_tuple in update_set:
                            
                            if not Tile_orientated in new_edge_tiles:
                                
                                update_set.add(tile)
                                new_edge_tiles.append(Tile_orientated_tuple)
                        else:
                            Count_for_update_set += 1                
            if C == 0:
                if Check_count_Dictionary[tile] == 0:
                    Decay = True
                    for Tile_orientated in the_orientation:
                        if Tile_orientated in update_set:
                            Decay = False

                    if Decay == False:
                        if Air[tile]['Obstructed'] == False:
                            new_edge_tiles.append(tile)
                    Check_count_Dictionary[tile] = 0
                else:
                    Check_count_Dictionary[tile] += 1
                    new_edge_tiles.append(tile)
            else:
                if Count_for_update_set > 1:
                    update_set.add(tile)

                    
    except(TypeError):
        the_orientation = Dictionary_of_adjacents[edge_tiles].copy()
        C = 0
        Count_for_update_set = 0
        for Tile_orientated in the_orientation:
            Tile_orientated_tuple = tuple(Tile_orientated)
            if Air[Tile_orientated_tuple]['Obstructed'] == False:
                #if not Air[tuple(Tile_orientated)]['Pressure'] == Air[tuple(tuple_edge_tiles)]['Pressure']:
                if not math.isclose(Air[Tile_orientated_tuple]['Pressure'], Air[edge_tiles]['Pressure'], rel_tol=1e-5, abs_tol=0.0):
                    C += 1 
                    if not Tile_orientated in new_edge_tiles:
                        
                        if not Tile_orientated_tuple in update_set:
                             
                            new_edge_tiles.append(Tile_orientated)
                            update_set.add(edge_tiles)

                        else:
                            Count_for_update_set += 1

        if C == 0:
            if Check_count_Dictionary[edge_tiles] == 0:
                Decay = True
                for Tile_orientated in the_orientation:
                    if Tile_orientated in update_set:
                        Decay = False

                if Decay == False:
                    if Air[tile]['Obstructed'] == False:
                        new_edge_tiles.append(edge_tiles)
                Check_count_Dictionary[edge_tiles] = 0
            else:
                Check_count_Dictionary[edge_tiles] += 1
                new_edge_tiles.append(edge_tiles)
        else:
            if Count_for_update_set > 1:
                update_set.add(edge_tiles)


    edge_tiles = new_edge_tiles.copy()

            

#@profile
def Atmospherics():
    
    
    global edge_tiles
    count =  0
    #edge_tiles = []
    start_time = time.time()
    #Air[(48,48)]['Temperature'] = 240000
    #Air[(48,48)]['Pressure'] = 500
    
    #Air[(180,105)]['Temperature'] = 5000000
    #Air[(180,105)]['Pressure'] = 500
    
    #Air[(50,50)]['Temperature'] = 300
    #Air[(50,50)]['Moles'] = 80
    #Air[(50,50)]['Pressure'] = 1
    #print(Air[(50,50)]['Pressure'])
    #Air[(50,50)]['Temperature'] = 1000
    #mixe = {'Oxygen': 16.628484400890, 'Nitrogen': 33, 'Plasma': 33}
    #Air[(50,50)]['Mix'] = mixe
    
    #edge_tiles1 = Do_the_edge((100,100))
    
    edge_tiles = Dictionary_of_adjacents[(180,105)]
    
    #edge_tiles = edge_tiles1 + edge_tiles2
    while count < 100:
        start_Tick_time = time.time()
        #if count == 120:
            
            #Air[(50,50)]['Pressure'] = 1.322*(10**-14)
            #Air[(50,50)]['Moles'] = 1
            #mixe = {'Oxygen': 1, 'Nitrogen': 1, 'Plasma': 1}
            #Air[(50,50)]['Mix'] = mixe   
            #Air[(50,50)]['Moles'] = 80
            
            #Air[(100,100)]['Pressure'] = 0.1
            
            #Air[(48,48)]['Pressure'] = 0.1
            
            #Air[(50,50)]['Temperature'] = 3.15
            
            #Air[(51,50)]['Temperature'] = 0
            #Air[(51,50)]['Moles'] = 0.1
            #Air[(51,50)]['Pressure'] = 0.1
            
            #Air[(50,51)]['Temperature'] = 0
            #Air[(50,51)]['Moles'] = 0.1
            #Air[(50,51)]['Pressure'] = 0.1
            
            #Air[(49,50)]['Temperature'] = 0
            #Air[(49,50)]['Moles'] = 0.1
            #Air[(49,50)]['Pressure'] = 0.1

            #Air[(50,49)]['Temperature'] = 0
            #Air[(50,49)]['Moles'] = 0.1
            #Air[(50,49)]['Pressure'] = 0.1

        new_pressure()
        


        Air_Reactions()

        



        
        Do_the_edge()
     
        #if not edge_tiles:
        #    edge_tiles = Dictionary_of_adjacents[(50,50)]
            
            #edge_tiles = Dictionary_of_adjacents[(100,100)]  
        count += 1        
        print(count)
        Tick_time = (time.time() - start_Tick_time)
        #print(Tick_time)
        if not Tick_time > 0.2:
            time.sleep(0.2 - Tick_time)
        #else:
        #    print('Atmospheric system - fuck!!!')

    print("--- %s seconds ---" % (time.time() - start_time))
    #print(update_list,'update_list ')
    #print(edge_tiles,'edge_tiles')

#print (Air[(60,60)]['Temperature'],'Temperature')

#print (Air[(50,50)]['Pressure'],Air[(51,50)]['Pressure'],Air[(50,49)]['Pressure'],'Just looking')
print (Air[(50,50)]['Mix'],Air[(51,50)]['Mix'],Air[(50,49)]['Mix'],'Just Mixing')
#print (Air[(50,50)]['Temperature'],Air[(51,50)]['Temperature'],Air[(50,49)]['Temperature'],'Just tempting')
print (Air[(50,50)]['Moles'],Air[(51,50)]['Moles'],Air[(50,49)]['Moles'],'Just Counting')





#cProfile.run('Atmospherics()')


Atmospherics()






#Pressure_checke((5,5))
#print (Air[(50,50)]['Pressure'],Air[(51,50)]['Pressure'],Air[(50,49)]['Pressure'],'Just looking')
print (Air[(50,50)]['Mix'],Air[(51,50)]['Mix'],Air[(50,49)]['Mix'],'Just Mixing')
#print (Air[(50,50)]['Temperature'],Air[(51,50)]['Temperature'],Air[(50,49)]['Temperature'],'Just tempting')
print (Air[(50,50)]['Moles'],Air[(51,50)]['Moles'],Air[(50,49)]['Moles'],'Just Counting')


#print (Air[(60,60)]['Temperature'],'Temperature')
print (Air[(60,60)]['Mix'],'Mix')
#print (Air[(60,60)]['Pressure'],'Pressure')
print (Air[(60,60)]['Moles'],'Just Counting')


def create_quad_vertex_list(x, y, width, height):
    return x, y, x + width, y, x + width, y + height, x, y + height


def Drawing_boxes():
    global edge_tiles
    batch = pyglet.graphics.Batch()
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    e = 0
    a = 100
    for z in r:
        #print (z,'in 1')
        t = 100
        x = 0
        for p in r2:
            #print(Air[tuple((z,p))]['Obstructed'])
            if Air[(z,p)]['Space'] == False:

                
                if Air[tuple((z,p))]['Obstructed'] == True:
                    #print('wall!')
                    int_wall = 255
                else:
                    int_wall = 0
                if (z,p) in edge_tiles:
                    int_edge_tiles = 255
                else:
                    int_edge_tiles = 0
                    
                if (z,p) in update_set:
                    #print('hey?')
                    int_update_set = 255
                else:
                    int_update_set = 0
                #int_wall = 0


                    
                int_Temperature = 0
                #int_Temperature = (int(round((Air[tuple((z,p))]['Temperature'] / 2), 0))) 
                int_Moles = (int(round(2 * Air[tuple((z,p))]['Moles'], 0)))
                int_Pressure = (int(round(2 * Air[tuple((z,p))]['Pressure'], 0)))
                vertex_list = batch.add(4, pyglet.gl.GL_QUADS, None,
                                                                                                   ('v2i', (create_quad_vertex_list(e+a,x+t,4,4))),
                                                                                                   ('c3B', (
                                                                                                   int_Pressure, int_Temperature, int_wall,
                                                                                                   int_Pressure, int_Temperature, int_wall,
                                                                                                   int_Pressure, int_Temperature, int_wall,
                                                                                                   int_Pressure, int_Temperature, int_wall)))

            x += 4
            t += 0
        e += 4
        a += 0
    batch.draw()


print(len(update_set),'the length of update set')

print(len(edge_tiles),'the length of edge tiles')

#old_Visual_check()

def do_wins():      

    window = pyglet.window.Window(1920, 1080)
    @window.event
    def on_draw():
        window.clear()
        Drawing_boxes()

    if __name__ == "__main__":
        pyglet.app.run()

do_wins()






