#V0.5
import time
import math
import sys
import cProfile
import pyglet



sys.setrecursionlimit(9000)


Air = {}
Look_UP =  {'Oxygen': 0.659, 'Nitrogen': 0.743, 'Plasma': 0.8 }
Check_count_Dictionary = {}
update_list = []
Has_done = {}
Dictionary_of_adjacents = {}
R = 8.3144598
Temporary = {}
Temporary2 = {}
Tile_range = [255,255]

r = range(0,256)
r2 = range(0,256)

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
        Air[tuple_z_p] = Temporary.copy()
        
        
        
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



def Worse_case_update_list():
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            update_list.append((z,p))
    print('done!')

def Making_check_count_Dictionary():
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            Check_count_Dictionary[(z,p)] = 0
    print('Check_count_Dictionary Done!')
Making_check_count_Dictionary()

#Worse_case_update_list()



def old_Visual_check():
    r = range(0,Tile_range[0] + 1)
    r2 = range(0,Tile_range[1] + 1)
    a = []
    for z in r:
        x = []
        for p in r2:
            print((z,p),Air[(z,p)]['Pressure'])


#@numba.jit

#@profile
def new_pressure():
    for Origin_tile in update_list:
        are_different_pressure_set = set(Dictionary_of_adjacents[Origin_tile])
        mix_calculation_dictionary = {}
        JM_calculation_dictionary = {}
        c = False
        JM = 0
        nall = JM
        count = JM
        T = 0
        for tile in are_different_pressure_set:
            if Air[tile]['Obstructed'] == False:
                for key in Air[tile]['Mix']:
                    mix_calculation_dictionary[key] = (Air[tile]['Mix'][key] + mix_calculation_dictionary.get(key, 0))
                    JM_calculation_dictionary[key] = ((Air[tile]['Temperature'] * Air[tile]['Mix'][key]) * Look_UP[key]) + JM_calculation_dictionary.get(key, 0) 


                if not math.isclose(Air[tile]['Pressure'], Air[Origin_tile]['Pressure'], rel_tol=1e-5, abs_tol=0.0):
                    c = True
                count += 1
                


        for key in mix_calculation_dictionary:
            key_mix_worked_out = (mix_calculation_dictionary[key] / count)
            Air[Origin_tile]['Mix'][key] = key_mix_worked_out
            nall += key_mix_worked_out

            JM_calculation_dictionary[key] = (((JM_calculation_dictionary[key] /  Look_UP[key]) / key_mix_worked_out) / len(JM_calculation_dictionary)) ##########
            T += (JM_calculation_dictionary[key] / count )
            
            
        P = ((nall*R*T)/(2)/1000)   
        for tile in are_different_pressure_set:
            if Air[tile]['Obstructed'] == False:
                for key in mix_calculation_dictionary:
                    Air[tile]['Mix'][key] = Air[Origin_tile]['Mix'][key]
                Air[tile]['Temperature'] = T
                Air[tile]['Moles'] = nall
                Air[tile]['Pressure'] = P
            
        if c == False:
            if Check_count_Dictionary[Origin_tile] == 3:
                update_list.remove(Origin_tile)
                Check_count_Dictionary[Origin_tile] = 0
            else:
                Check_count_Dictionary[Origin_tile] += 1

#new_pressure()
#new_pressure.inspect_types()

#@numba.jit
                
#@profile
def Do_the_edge(edge_tiles):

    The_return = {}
    new_edge_tiles = []
    update_set = set(update_list)
    edge_tiles_set = set(edge_tiles)
    #if not all(isinstance(i, int) for i in edge_tiles):
    #try:
    if not all(isinstance(i, int) for i in edge_tiles):
        for tile in edge_tiles_set:
            the_orientation = Dictionary_of_adjacents[tile]
            
            C = 0
            for Tile_orientated in the_orientation:
                Tile_orientated_tuple = tuple(Tile_orientated)
                if Air[Tile_orientated_tuple]['Obstructed'] == False:
                    if not math.isclose(Air[Tile_orientated_tuple]['Pressure'], Air[tile]['Pressure'], rel_tol=1e-4, abs_tol=0.0):
                        C += 1
                        if not Tile_orientated in new_edge_tiles:
                            if not Tile_orientated_tuple in update_set:
                                new_edge_tiles.append(Tile_orientated_tuple)
                                update_list.append(tile)
                

            if C == 0:
                Check_count = Check_count_Dictionary[tile]
                if Check_count == 5:
                    Check_count_Dictionary[tile] = 0
                else:
                    Check_count_Dictionary[tile] += 1
                    new_edge_tiles.append(tile)
    #except(TypeError):     
    else:
        the_orientation = Dictionary_of_adjacents[edge_tiles]
        
        C = 0
        for Tile_orientated in the_orientation:
            Tile_orientated_tuple = tuple(Tile_orientated)
            if Air[Tile_orientated_tuple]['Obstructed'] == False:
                #if not Air[tuple(Tile_orientated)]['Pressure'] == Air[tuple(tuple_edge_tiles)]['Pressure']:
                if not math.isclose(Air[Tile_orientated_tuple]['Pressure'], Air[edge_tiles]['Pressure'], rel_tol=1e-4, abs_tol=0.0):
                    C += 1  
                    if not Tile_orientated in new_edge_tiles:
                        if not Tile_orientated_tuple in update_set:
                            new_edge_tiles.append(Tile_orientated)
                            update_list.append(edge_tiles)

        if C == 0:
            Check_count = Check_count_Dictionary[edge_tiles]
            if Check_count == 5:
                Check_count_Dictionary[edge_tiles] = 0
            else:
                Check_count_Dictionary[edge_tiles] += 1
                new_edge_tiles.append(edge_tiles)
    
    return(new_edge_tiles)

            

#@profile
def Atmospherics():
    #print(Air[(50,50)]['Obstructed'])
    #Air[48,48]['Obstructed'] = True
    Air[49,48]['Obstructed'] = True
    Air[50,48]['Obstructed'] = True
    Air[51,48]['Obstructed'] = True
    #Air[52,48]['Obstructed'] = True
    
    #Air[48,52]['Obstructed'] = True
    Air[49,52]['Obstructed'] = True
    #Air[50,52]['Obstructed'] = True
    Air[51,52]['Obstructed'] = True
    #Air[52,52]['Obstructed'] = True
    
    Air[52,49]['Obstructed'] = True
    Air[52,50]['Obstructed'] = True
    Air[52,51]['Obstructed'] = True
    
    Air[48,49]['Obstructed'] = True
    Air[48,50]['Obstructed'] = True
    Air[48,51]['Obstructed'] = True



    
    start_time = time.time()
    count =  0
    #Air[(50,50)]['Temperature'] = 5000000
    #Air[(50,50)]['Pressure'] = 500
    
    #Air[(50,50)]['Temperature'] = 300
    Air[(50,50)]['Moles'] = 80
    #Air[(50,50)]['Pressure'] = 1
    #print(Air[(50,50)]['Pressure'])
    #Air[(50,50)]['Temperature'] = 50000
    #mixe = {'Oxygen': 16, 'Nitrogen': 63.2, 'Plasma': 0.8}
    #Air[(50,50)]['Mix'] = mixe
    edge_tiles = Do_the_edge((50,50))
    while count < 0:
        if count < 1000:
            #Air[(50,50)]['Pressure'] = 1
            #Air[(50,50)]['Moles'] = 1
            mixe = {'Oxygen': 0.1, 'Nitrogen': 0.1, 'Plasma': 0.1}
            Air[(50,50)]['Mix'] = mixe   
            #Air[(50,50)]['Moles'] = 80
            #Air[(50,50)]['Pressure'] = 0.1
            
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

        
        edge_tiles = Do_the_edge(edge_tiles)
     
        if not edge_tiles:
            edge_tiles = Dictionary_of_adjacents[(50,50)]    
        count += 1        
        print(count)

    print("--- %s seconds ---" % (time.time() - start_time))
    #print(update_list,'update_list ')
    #print(edge_tiles,'edge_tiles')


print (Air[(50,50)]['Pressure'],Air[(51,50)]['Pressure'],Air[(50,49)]['Pressure'],'Just looking')
print (Air[(50,50)]['Mix'],Air[(51,50)]['Mix'],Air[(50,49)]['Mix'],'Just Mixing')
print (Air[(50,50)]['Temperature'],Air[(51,50)]['Temperature'],Air[(50,49)]['Temperature'],'Just tempting')
print (Air[(50,50)]['Moles'],Air[(51,50)]['Moles'],Air[(50,49)]['Moles'],'Just Counting')





#cProfile.run('Atmospherics()')


Atmospherics()






#Pressure_checke((5,5))
print (Air[(50,50)]['Pressure'],Air[(51,50)]['Pressure'],Air[(50,49)]['Pressure'],'Just looking')
print (Air[(50,50)]['Mix'],Air[(51,50)]['Mix'],Air[(50,49)]['Mix'],'Just Mixing')
print (Air[(50,50)]['Temperature'],Air[(51,50)]['Temperature'],Air[(50,49)]['Temperature'],'Just tempting')
print (Air[(50,50)]['Moles'],Air[(51,50)]['Moles'],Air[(50,49)]['Moles'],'Just Counting')


print (Air[(60,60)]['Temperature'])
print (Air[(60,60)]['Mix'])




def create_quad_vertex_list(x, y, width, height):
    return x, y, x + width, y, x + width, y + height, x, y + height


def Drawing_boxes():
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
            if Air[tuple((z,p))]['Obstructed'] == True:
                #print('wall!')
                int_wall = 255
            else:
                int_wall = 0
                
            
            int_Temperature = (int(round((Air[tuple((z,p))]['Temperature'] / 2), 0))) 
            int_Pressure = (int(round(2 * Air[tuple((z,p))]['Moles'] , 0)))
            vertex_list = batch.add(4, pyglet.gl.GL_QUADS, None,
                                                                                               ('v2i', (create_quad_vertex_list(e+a,x+t,5,5))),
                                                                                               ('c3B', (
                                                                                               int_Pressure, int_Temperature, int_wall,
                                                                                               int_Pressure, int_Temperature, int_wall,
                                                                                               int_Pressure, int_Temperature, int_wall,
                                                                                               int_Pressure, int_Temperature, int_wall)))

            x += 5
            t += 0
        e += 5
        a += 0
    batch.draw()

print(len(update_list),'the length of update list ')

def do_wins():      

    window = pyglet.window.Window(1920, 1080)
    @window.event
    def on_draw():
        window.clear()
        Drawing_boxes()

    if __name__ == "__main__":
        pyglet.app.run()

do_wins()







