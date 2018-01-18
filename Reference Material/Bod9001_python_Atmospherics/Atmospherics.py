#V1
#needs BoxStation.json From unitystation\UnityProject\Assets\Resources\Metadata BoxStation.7z
#from numba import jit
import json
import time
import math
import sys
import cProfile
import pyglet
# Importing stuff 




sys.setrecursionlimit(9000)

# Stopping stuff from breaking From recurring too much


Heat_capacity_of_gases =  {'Oxygen': 0.659, 'Nitrogen': 0.743, 'Plasma': 0.8,'Carbon Dioxide':0.655}
Molar_Masses = {'Oxygen':31.9988,'Nitrogen': 28.0134 ,'Plasma': 40,  'Carbon Dioxide':44.01} 

# The the variables for the gases Heat_capacity_of_gases and Molar_Masses

edge_tiles = []
edge_tiles_set = set(edge_tiles) 

Plasma_fuel_list = []
Plasma_fuel_set = set(Plasma_fuel_list)
update_list = []
update_set = set(update_list)
Space_list = []
Space_set = set(Space_list)

odd_list = []
odd_set = set(odd_list)

Even_list = []
Even_set = set(odd_list)


Has_done = {}
check_Reaction_Dictionary = {}
Air = {}
is_space_Mix = {}
Space_pressure = {}
Mixes = {}
is_space = {}
Dictionary_of_adjacents = {}
Check_count_Dictionary = {}
Check_count_Dictionary_Moving = {}

Temporary = {}
Temporary2 = {}
is_space_2 = {}

#Making all the lists Dictionaries and sets n stuff 

R = 8.3144598 #The gas constant 





Tile_range = [360,210] #The size of the map



####seting all Tiles to Whatever you want \/

r = range(0,Tile_range[0]+1)
r2 = range(0,Tile_range[1]+1)



a = []
for z in r:
    
    x = []
    for p in r2:
        tuple_z_p = tuple((z,p))
        Temporary['Temperature'] = 293.15
        Temporary['Pressure'] = 101.325
        #Temporary['Temperature'] = 3000

        
        Temporary2['Oxygen'] = 16.628484400890768491815384755837
        Temporary2['Nitrogen']= 66.513937603563073967261539023347
        
        
        Temporary['Moles'] = 83.142422004453842459076923779184
        Temporary['Fire'] = 0         
        Temporary['Obstructed'] = False
        Temporary['Space'] = True 
        Air[tuple_z_p] = Temporary.copy()
        Mixes[tuple_z_p] = Temporary2.copy()
        

#######
#\/ Defining space tiles

is_space['Temperature'] = 2.7
is_space['Pressure'] = 0.000000316

is_space_Mix['Oxygen'] = 0.000000000000281

is_space['Moles'] = 0.000000000000281
is_space['Fire'] = 0 
is_space['Obstructed'] = False
is_space['Space'] = True


#Importing the map \/

with open('BoxStation.json') as json_data:
    the_json = json.load(json_data)
    #print(the_json["Walls1"]["tilePositions"])
    insex = 0
    insey = 0

    #seting all the walls and floors to the map in the Simulation


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

    for hr in the_json["UnderFloor0"]["tilePositions"]:
        inse = []
        insex = hr['x']
        insey = hr['y']
        Air[(insex,insey)]['Space'] = False

    #for hr in the_json["Doors Closed11"]["tilePositions"]:
    #    inse = []
    #    insex = hr['x']
    #    insey = hr['y']
    #    Air[(insex,insey)]['Obstructed'] = True
        
    #for hr in the_json["Doors Closed10"]["tilePositions"]:
    #    inse = []
    #    insex = hr['x']
    #    insey = hr['y']
    #    Air[(insex,insey)]['Obstructed'] = True
        
    #for hr in the_json["Doors Closed9"]["tilePositions"]:
    #    inse = []
    #    insex = hr['x']
    #    insey = hr['y']
    #    Air[(insex,insey)]['Obstructed'] = True
        
    #for hr in the_json["Doors Open0"]["tilePositions"]:
    #    inse = []
    #    insex = hr['x']
    #    insey = hr['y']
    #    Air[(insex,insey)]['Obstructed'] = True




        
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

#/\That's for map outpost.json to Close off all the external doors
#this is where You can set custom  walls or Tiles 

##~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~        



#####Stuff for the preview 
def create_quad_vertex_list(x, y, width, height):
    return x, y, x + width, y, x + width, y + height, x, y + height


def Drawing_boxes():
    edge_tiles_set = set(edge_tiles)

    global Air
    global Mixes
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

            #if Air[(z,p)]['Space'] == False:
            if 1 == 1:

                
                if Air[tuple((z,p))]['Obstructed'] == True:
                    #print('wall!')
                    int_wall = 255
                else:
                    int_wall = 0
                if (z,p) in edge_tiles_set:
                    int_edge_tiles = 255
                else:
                    int_edge_tiles = 0
                    
                if (z,p) in update_set:
                    #print('hey?')
                    int_update_set = 255
                else:
                    int_update_set = 0
                #int_wall = 0
                #print(odd_set )
                #print(Even_set)
                #if (z,p) in Even_set:
                #    int_Moles = 255
                #else:
                #    int_Moles = 0

                #if (z,p) in odd_set:
                #    int_Temperature = 255
                #else:
                #    int_Temperature = 0
                    


                


                #int_Moles = 255 
                #int_Temperature = 255
                int_Temperature = (int(round((Air[tuple((z,p))]['Temperature'] / 8), 0))) 
                int_Moles = (int(round(Air[tuple((z,p))]['Moles'], 0)))
                #int_Moles = (int(round(4 * Mixes[tuple((z,p))]['Oxygen'], 0))) 
                int_Pressure = (int(round(2 * Air[tuple((z,p))]['Pressure'], 0))) #Visual marker 
                if (z,p) == (20,121):
                    
                    int_update_set = 255
                    int_edge_tiles = 255
                    int_wall = 255
                    int_wall = 255
                    int_Temperature = 255
                    int_Moles = 255
                RED = int_edge_tiles    ####set what Colours you want to be what it's to do with RGB so 0 ro 255 (It can stack overflow It will just go back to 0 As long as the numbers not too high ) 
                GREEN = int_update_set
                BLUE = int_wall
                    
                vertex_list = batch.add(4, pyglet.gl.GL_QUADS, None,
                                                                                                   ('v2i', (create_quad_vertex_list(e+a,x+t,5,5))),
                                                                                                   ('c3B', (
                                                                                                   RED, GREEN, BLUE,
                                                                                                   RED, GREEN, BLUE, 
                                                                                                   RED, GREEN, BLUE,
                                                                                                   RED, GREEN, BLUE)))   #pyglet  Magic

            x += 5
            t += 0
        e += 5
        a += 0
    batch.draw()

#pyglet  Magic
def do_wins():      

    window = pyglet.window.Window(1920, 1080)
    @window.event
    def on_draw():
        window.clear()
        Drawing_boxes()

    if __name__ == "__main__":
        pyglet.app.run()
        return() 


#####



def Orientation(tile): ### Making the adjacent tile to the tile
    T = []
    p = [[0,0],[1,0],[0,1],[-1,0],[0,-1]]  # set what they are here
    for Z in p:
        a = list(tile)
        b = a[0]+Z[0]
        c = []
        c.append(b)
        s = a[1]+Z[1]
        c.append(s)
        if (not (c[0] > Tile_range[0] or c[0] < 0 or  c[1] > Tile_range[1] or c[1] < 0)): #Making sure they're not out of bounds
            T.append(tuple(c))

    return(T)


def Making_Dictionary_of_adjacents():  # just puts adjacent tiles into a dictionary for quick reference
    
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



def Worse_case_update_set(): #Makes every tile active so they can be updated (They will remove themselves if they are not needed)
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


def Worse_case_edge_tiles(): #this  Has frozen all the times I've done it so don't do it
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

#Worse_case_edge_tiles() #this  Has frozen all the times I've done it so don't do it


def Pitch_Patch(bo = 'good'): # Making my lag system patchwork
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    for z in r:
        #print (z,'in 1')
        x = []
        E = 0
        for p in r2:
            if bo == 'bad': # Ignores this this is a test
                if p % 5 == 0:
                    if z % 5 == 0:
                        odd_set.add((z,p))
                else:
                    if (p+4) % 5 == 0:
                        #odd_set.add((z,p))
                        if (z+2) % 5 == 0:
                            odd_set.add((z,p))
                    else:
                        if (p+3) % 5 == 0:
                            if (z+4) % 5 == 0:
                                odd_set.add((z,p))
                        else:
                            if (p+2) % 5 == 0:
                                if (z+1) % 5 == 0:
                                    odd_set.add((z,p))
                            else:
                                if (p+1) % 5 == 0:
                                    if (z+3) % 5 == 0:
                                        odd_set.add((z,p))
                                
                            
           


                
                 
          

            else: # yeah, it only does half  At a time that's how it saves time
                
                if z % 2 == 0:
                    if p % 2 == 0:
                        odd_set.add((z,p))
                    else:
                        Even_set.add((z,p))
                else:
                    if p % 2 == 0:
                        Even_set.add((z,p))

                    else:
                        
                        odd_set.add((z,p))


Pitch_Patch()
#print(odd_set )
#print(Even_set)




            
            

def Making_check_count_Dictionary_and_Check_count_Dictionary_Moving(): #Just stuff for remembering decay 
    
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



def Making_space_set():  #Setting all tiles to space that have ['Space'] == True and Are not obstructed
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



def old_Visual_check():  # if You want to print out the entire map  with Currently Pressure and Mixes of gases  But you can add more
    global Air
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        x = []
        for p in r2:
            if Air[(z,p)]['Space'] == False:
                if Air[(z,p)]['Obstructed'] == False:
                    print((z,p),Air[(z,p)]['Pressure'])
                    print((z,p),Mixes[(z,p)])




def Setting_space(): #idk why this is split over 2 functions but Sets them to space, (I was intending to run this while the atmospherics But I found a better way) 
    for Space_tile in Space_set:
            Air[Space_tile] = is_space.copy()
            Mixes[Space_tile] = is_space_Mix.copy()
            
Setting_space()


#yay Actual atmospherics

#@profile
def The_calculations(Origin_tile):


    
    global Check_count_Dictionary_Moving
    are_different_pressure = Dictionary_of_adjacents[Origin_tile] #Gets the adjacents
    mix_calculation_dictionary = {}
    JM_calculation_dictionary = {}
    update_list = False
    Tile_working_on = []
    key_to_del = []
    c = False
    is_space = False
    nall = 0
    count = 0
    T = 0
    #Makes some variables
    
    for tile in are_different_pressure:
        if Air[tile]['Space']: 
                is_space = True # you don't need to process space Tiles do you?
                break
        
        if not Air[tile]['Obstructed']:  # you don't need to process wall Tiles do you?
            for key, value in Mixes[tile].items(): # Gets the name of the gas and the quantity of gas in moles 
                mix_calculation_dictionary[key] = (value + mix_calculation_dictionary.get(key, 0)) #adds up all the Gas of a certain type in Adjacent tiles and itself 
                JM_calculation_dictionary[key] = ((Air[tile]['Temperature'] * value) * Heat_capacity_of_gases[key]) + JM_calculation_dictionary.get(key, 0) # Works out how much energy  (not in J You have to Times by the Molar_Masses to get the J)
                if value < 0.000000000000001: # Something to stop it turning into 0 and causing a crash because of dividing by 0
                    key_to_del.append(key)
            if not math.isclose(Air[tile]['Pressure'], Air[Origin_tile]['Pressure'], rel_tol=1e-2, abs_tol=0.0): # Checks if it needs to decay By pressure difference
                c = True
            count += 1 #How many tiles it is getting from
            Tile_working_on.append(tile) # what tiles it is getting from

    if not is_space: # you don't need to process space Tiles do you?
        for key in mix_calculation_dictionary:
            key_mix_worked_out = (mix_calculation_dictionary[key] / count) #Dividing by the tiles used
            nall += key_mix_worked_out # How many moles of gas altogether 
                
            JM_calculation_dictionary[key] = (((JM_calculation_dictionary[key] /  Heat_capacity_of_gases[key]) / key_mix_worked_out) / len(JM_calculation_dictionary)) #Turning it back into K
            Mixes[Origin_tile][key] = key_mix_worked_out # for set the Tiles
            T += (JM_calculation_dictionary[key] / count) #setign the Temperature in k


            if key == 'Plasma': #Taking of the Checking if The current gases plasma
                #if key_mix_worked_out['Plasma'] > 0: # Checking how much there is
                global Plasma_fuel_set
                Plasma_fuel_set.add(tile) # adding to the Tiles that need to be checked for reactions

        P = ((nall*R*T)/(2)/1000) # Working out the pressure in kpa

        for key in key_to_del: # Something to stop it turning into 0 and causing a crash because of dividing by 0
            try:
                del Mixes[Origin_tile][key]
            except(KeyError):
                pass

        for tile in Tile_working_on: # Setting tiles time
            if not Air[tile]['Space']: # the other way I found of doing it 

                Mixes[tile]= Mixes[Origin_tile].copy()    #   Applying gas mixture to it
                Air[tile]['Temperature'] = T #   Applying Temperature in K to it
                Air[tile]['Moles'] = nall #   Applying Number of molesto it 
                Air[tile]['Pressure'] = P #   Applying Pressure to it 
    else: # If space Tile   Making it like space 
        for tile in are_different_pressure:
            if not Air[tile]['Obstructed']:
                Mixes[tile] = is_space_Mix.copy()
                Air[tile]['Temperature'] = 2.7
                Air[tile]['Moles'] = 0.000000000000281
                Air[tile]['Pressure'] = 0.000000316

    if not c: #Checking if decay is true 
        if Check_count_Dictionary_Moving[Origin_tile] >= 3: #A bit of leniency
            update_list = True # and Needs to be removed
            Check_count_Dictionary_Moving[Origin_tile] = 0
        else:
            Check_count_Dictionary_Moving[Origin_tile] += 1 #A bit of leniency
    return(update_list)

    


def new_pressure(lag,odd_even):
    global edge_tiles
    global update_set
    #mixe = {'Oxygen': 16.628484400890768491815384755837, 'Plasma': 66.513937603563073967261539023347}
    #Mixes[(20,121)] = mixe.copy()
    #Air[(20,121)]['Temperature'] = 3000.00
    # this is a good Place to place constants for >>>Testing!!! <<<<<<< 
    update_liste = []

    for Origin_tile in update_set:
        update_list = False
        if lag:
            if odd_even: #if lag alternate between the tiles that you're doing and not doung  
                if Origin_tile in Even_set:
                    update_list = The_calculations(Origin_tile)
                    if update_list:
                        update_liste.append(Origin_tile) # Adding it to the list of tiles that need to be removed
                
            else: #If you're doing half you have to do the other half
                if Origin_tile in odd_set:
                    update_list = The_calculations(Origin_tile)
                    if update_list:
                        update_liste.append(Origin_tile) # Adding it to the list of tiles that need to be removed
        else:
            update_list = The_calculations(Origin_tile)
            if update_list:
                update_liste.append(Origin_tile) # Adding it to the list of tiles that need to be removed

    
        
    if odd_even: #If you're doing half you have to do the other half
        odd_even = False
    else:
        odd_even = True

    #print(len(update_liste))
    #print(Check_count_Dictionary_Moving[20,121])
    

    

    for Tile_remove in update_liste: #The removing process
    
        edge_tiles.append(Tile_remove) # adds it to the edge tiles  
        #print(Tile_remove)

        update_set.remove(Tile_remove) #  removings it 
        
        #except KeyError:
            #pass
        
    return(odd_even) # What to do next odd or even

    
        


#new_pressure()
#new_pressure.inspect_types()


def Air_Reactions(): #All the reactions yay
    global Air
    global Mixes
    global Plasma_fuel_set
    #print('Plasma o3o/')
    #print(len(Plasma_fuel_set))
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
def Do_the_edge(): #my edge tile system
    #print('hey?')
    global edge_tiles
    #print(edge_tiles)
    if not edge_tiles: #Checking if empty
        return
    
    List_to_go_into_update_set = []
    new_edge_tiles = []
    edge_tiles_set = set(edge_tiles) #sets are Faster in python

    for tile in edge_tiles_set: 
        if Air[tile]['Space'] == False: #You don't need to scan space if it's always the same
            the_orientation = Dictionary_of_adjacents[tile].copy()
            del the_orientation[0] #It doesn't need itself does it
            C = 0
            Count_for_update_set = 0
            for Tile_orientated in the_orientation:
                

                if Air[Tile_orientated]['Obstructed'] == False: # You don't need to check in walls
                    
                    if not math.isclose(Air[Tile_orientated]['Pressure'], Air[tile]['Pressure'], rel_tol=1e-5, abs_tol=0.0): #Checking if the pressures different
                        C += 1
                        #new_edge_tiles.append(Tile_orientated)

                        if not Tile_orientated in update_set: #Don't need to scan over something that's already working on
                            
                            if not Tile_orientated in new_edge_tiles: # No need to add self multiple times

                                update_set.add(tile)
                                new_edge_tiles.append(Tile_orientated)
                            else:
                                update_set.add(tile)
                        else:
                            
                            Count_for_update_set += 1

                    
            if C == 0:
                if Check_count_Dictionary[tile] >= 0: # yeah it's 0
                    Decay = True
                    for Tile_orientated in the_orientation:  
                        if Tile_orientated in update_set:# If it's next to an update Tile It doesn't need to decay
                            Decay = False

                    if Decay == False:
                        if Air[tile]['Obstructed'] == False:
                            new_edge_tiles.append(tile)
                    Check_count_Dictionary[tile] = 0
                else:
                    Check_count_Dictionary[tile] += 1
                    new_edge_tiles.append(tile)
            else:
                if Count_for_update_set > 1: # for some Strange edge case scenarios with walls and being surrounded by update tiles as well 
                    update_set.add(tile) 
                    
    edge_tiles = new_edge_tiles.copy()

                    
    

            

#@profile
def Atmospherics(): # the Thing that runs
    global Air
    
    global edge_tiles
    count =  0
    start_time = time.time()

    edge_tiles = Dictionary_of_adjacents[(180,105)]
    

    lag_tik = 0
    odd_even = False
    lag = False
    #do_wins()
    while count < 100:
        start_Tick_time = time.time() #Starts the tick time

        odd_even = new_pressure(lag,odd_even) # Does the calculations 
        
        Air_Reactions() # air reactions
        #odd_even = False

        Do_the_edge()
       
                
        print(count)
        Tick_time = (time.time() - start_Tick_time) # Work out how long it took
        print(Tick_time)
        count += 1
        if not Tick_time > 0.2: #if not Longer than target  
            #time.sleep(0.2 - Tick_time) # for the Pause
            if Tick_time < 0.1: # if It's half the time of what would it take for the lagy version to do it in Target
                lag = False #Then it's good to go to that
                
        
            
        else: # if Longer than target 

            lag = True #Pretty self explanatory
            
        #do_wins()

    print("--- %s seconds ---" % (time.time() - start_time)) #Total time





#cProfile.run('Atmospherics()')


Atmospherics()







print(len(update_set),'the length of update set') #How many tiles

print(len(edge_tiles),'the length of edge tiles')#How many tiles

#old_Visual_check()  
print(Air[(20,121)]['Pressure']) #Checking individual tiles
print(Air[(20,121)]['Temperature']) #Checking individual tiles
print(Mixes[(20,121)]) #Checking individual tiles 


do_wins() #The graphics display thing





