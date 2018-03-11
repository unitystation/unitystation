#v0.5 [WIP] Still hasn't been cleaned up so still quite messy 11/03/2018 01:00 GMT
import json
import time
import math
import sys
import cProfile
import pyglet #Requires pyglet
#import png
#import numpy as np
from PIL import Image #Requires pillow
import copy
import random
import os
import Stationary_Equipment as Power_sys_module #I should find a automatically of doing this






In_floor_tile = []            
Dictionary_of_adjacents = {}
Quick_dictionary = {}
Electrical_appliances_locations = []
Cable_Worked_on = []
Junctions_to_work_on = []
Going_into_machine = [] #end into aplche
Coming_from_machine = [] #from aplslych
End_Junctions = []
Cable_in_line = []
Power_draw_appliances = []
Power_sply_appliances = []
Junction_origin = []
Junction_check_list = []
Starting_from = []
Started_scanning_from = []
To_overlay_next = []
The_current_working_line = []
Working_on_backward_list = []
Mid_junctions = []
Electrical_changes = {}
links = {}
link_to = {}
link_from = {}
Power_dictionary = {}
On_junction = False



High_voltage_cable_Colour    = (26,255,26,255)        
Medium_Voltage_Cable_Colour  = (86,176,190,255)
Transformer_Colour           = (255,79,255,255)
Radiation_collector_Colour   = (255,240,26,255)
Medium_voltage_cable_Colour  = (86,176,190,255)
Engineering_batteries_Colour = (153,176,3,255)
Department_batteries_Colour  = (197,153,204,255)
Low_voltage_cable_Colour     = (26,69,255,255)
APC_Colour                   = (255,193,69,255)
Link_up_cable                = (9,166,69,255)
Link_down_cable              = (234,43,175,255)



im = Image.open("Power.png")
px = im.load()
Tile_range_x = im.size[0]
Tile_range_y = im.size[1]

Matrix = [[0 for x in range(Tile_range_x)] for y in range(Tile_range_y)]

def create_quad_vertex_list(x, y, width, height):
    return x, y, x + width, y, x + width, y + height, x, y + height


def Drawing_boxes(Cross_hair):
    #print(Cross_hair)
    global Cable_Worked_on
    global Junction_check_list
    global Starting_from
    global Started_scanning_from
    global To_overlay_next
    global Working_on_backward_list
    #print(Started_scanning_from,'Started_scanning_from')
    batch = pyglet.graphics.Batch()
    r = range(0,Tile_range_x)
    r2 = range(0,Tile_range_y)
    e = 0
    a = 0
    for z in r:
        #print (z,'in 1')
        t = -670
        x = 0
        for p in r2:
            #if Air[(z,p)]['Space'] == False:
            if 1 == 1:
                if "Medium_Voltage_Cable" in Matrix[z][p]:
                    RED = 255
                else:
                    RED = 0
                
                if "High_voltage_cable" in Matrix[z][p]:
                    GREEN = 255
                else:
                    GREEN = 0
                if "Low_voltage_cable" in Matrix[z][p]:
                    BLUE = 255
                else:
                    BLUE = 0

                
                if [z,p] in Cable_Worked_on:
                    BLUE = 255
                    GREEN = 255
                    RED = 255

                    
                if [z,p] in Junctions_to_work_on:
                    BLUE = 220
                    GREEN = 20
                    RED = 160


                if tuple([z,p]) in Power_dictionary:
                    #print(Power_dictionary[tuple([z,p])])
                    BLUE = 127
                    GREEN = 127
                    RED = 127
                    Even_int = 127
                    

                if [z,p] in Junction_check_list:
                    BLUE = 222
                    GREEN = 220
                    RED = 0

                    
                if (z,p) == Starting_from:
                    BLUE = 132
                    GREEN = 220
                    RED = 86
                    
              

                    
                if [z,p] in To_overlay_next:
                #if 3 > 5:
                    BLUE = 0
                    GREEN = 128
                    RED = 255

                if [z,p] in Working_on_backward_list:
                    BLUE = 130
                    GREEN = 0
                    RED = 75

                    
                if [z,p] == Started_scanning_from:
                    BLUE = 0
                    GREEN = 220
                    RED = 255
                    
                if tuple([z,p]) in Power_dictionary:
                    try:
                        
                        RED = (int(round((Power_dictionary[tuple([z,p])]['Supplying voltage']*Power_dictionary[tuple([z,p])]['Supply current'])/2, 0)))

                    except KeyError:
                        RED = 0
                    GREEN = 0
                    BLUE = 0

                if (z,p) == Cross_hair:  
                    BLUE = 100
                    GREEN = 255
                    RED = 100
                    
                vertex_list = batch.add(4, pyglet.gl.GL_QUADS, None,
                                                        ('v2i', (create_quad_vertex_list(e+a,x+t,10,10))),
                                                        ('c3B', (
                                                        RED, GREEN, BLUE,
                                                        RED, GREEN, BLUE, 
                                                        RED, GREEN, BLUE,
                                                        RED, GREEN, BLUE)))   #pyglet  Magic
            x += 10
            t += 0
        e += 10
        a += 0
    batch.draw()
    #Save_name =  str(time.time())
    #pyglet.image.get_buffer_manager().get_color_buffer().save('screenshot' + Save_name + '.png')
def do_wins(Cross_hair):      
    #print(Cross_hair)
    window = pyglet.window.Window(1250, 1250)
    @window.event
    def on_draw():
        #print(Cross_hair)
        window.clear()
        Drawing_boxes(Cross_hair)

    if __name__ == "__main__":
        pyglet.app.run()
        return()




def Orientation(tile): ### Making the adjacent tile to the tile
    T = []
    p = [[1,0],[0,1],[-1,0],[0,-1]]  # set what they are here
    for Z in p:
        a = list(tile)
        b = a[0]+Z[0]
        c = []
        c.append(b)
        s = a[1]+Z[1]
        c.append(s)
        if (not (c[0] > Tile_range_x or c[0] < 0 or  c[1] > Tile_range_y or c[1] < 0)): #Making sure they're not out of bounds
            T.append(c)
    return(T)


def Making_Dictionary_of_adjacents():  # just puts adjacent tiles into a dictionary for quick reference
    
    r = range(0,Tile_range_x)
    r2 = range(0,Tile_range_y)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            The_adjacent = Orientation((z,p))
            Dictionary_of_adjacents[(z,p)] = The_adjacent
    print('Making_Dictionary_of_adjacents Done!')
    
Making_Dictionary_of_adjacents()



for x in range(0,im.size[0]):
    for y in range(0,im.size[1]):
        Matrix[x][y] = In_floor_tile.copy()
        if px[x,y] == Medium_Voltage_Cable_Colour:
            Matrix[x][y].append('Medium_Voltage_Cable')
        if px[x,y] == Transformer_Colour:
            Matrix[x][y].append('Transformer')
        if px[x,y] == Radiation_collector_Colour:
            Matrix[x][y].append('Radiation_collector')
            Power_sply_appliances.append([x,y])
        if px[x,y] == Engineering_batteries_Colour:
            Matrix[x][y].append('Engineering_batteries')
        if px[x,y] == Department_batteries_Colour:
            Matrix[x][y].append('Department_batteries')
        if px[x,y] == Low_voltage_cable_Colour:
            Matrix[x][y].append('Low_voltage_cable')
        if px[x,y] == APC_Colour:
            Power_draw_appliances.append([x,y])
            Matrix[x][y].append('APC')
        if px[x,y] == High_voltage_cable_Colour:
            Matrix[x][y].append('High_voltage_cable')
        if px[x,y] == Link_up_cable:
            Matrix[x][y].append('Link_up_cable')
        if px[x,y] == Link_down_cable:
            Matrix[x][y].append('Link_down_cable')
        links[tuple([x,y])] = []
        link_to[tuple([x,y])] = []
        link_from[tuple([x,y])] = []


Electrical_appliances = ['Medium_Voltage_Cable','Transformer','Radiation_collector','Engineering_batteries','Department_batteries','Low_voltage_cable','APC','High_voltage_cable','Link_up_cable','Link_down_cable']
Cables = ['High_voltage_cable','Low_voltage_cable','Medium_Voltage_Cable']
Stationery_equipment = ['Transformer','Radiation_collector','Engineering_batteries','Department_batteries','APC']
Cable_links = ['Link_up_cable','Link_down_cable']

def Initialising_the_List_of_electricals():
    for x in range(0,im.size[0]):
        for y in range(0,im.size[1]):
            for Appliance in Matrix[x][y]:
                if Appliance in Stationery_equipment:
                    Electrical_appliances_locations.append([x,y])

Initialising_the_List_of_electricals()

def Circuit_search():
    global Cable_Worked_on
    global Started_scanning_from
    working = True
    On_junction = False

    Starting_tile = random.sample(Electrical_appliances_locations,1)[0]
    Started_scanning_from = Starting_tile
    #print(Starting_tile) ##################################
    #print(Matrix[Starting_tile[0]][Starting_tile[1]])  ##################################

    c = 0
    Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_tile)].copy()
    String_of_Cable_type = Matrix[Starting_tile[0]][Starting_tile[1]][0]
    if 33 > 1:
        is_one = False
        #Cable_search(Starting_tile)
        #to_format()
        #print(Ends, 'Ends')
        

        
        #Cable_Worked_on = []
        Junction_search(Starting_tile) 
        #print('links',links,enrte,'link_to',link_to,enrte,'link_from',link_from)
        while working:
            
            for Junction in Junctions_to_work_on:
                #print(Junctions_to_work_on,'Junctions_to_work_on')
                #print('man')
                Junction_search(Junction)
                #Junctions_to_work_on.remove(Junction)
            working = False
     

        
def Cable_search(Starting_tile, is_fiers = False):
    global On_junction
    global Cable_in_line
    global Going_into_machine
    global End_Junctions
    global Coming_from_machine
    global Junction_origin
    On_junction = False
    
    
    #print('hey',Starting_tile, Matrix[Starting_tile[0]][Starting_tile[1]][0])
    #print(Cable_Worked_on)
    
    Adjacent_Tiles_with_same = []
    Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_tile)].copy()
    Starting_tile_Data = Matrix[Starting_tile[0]][Starting_tile[1]]
    Adjacent_count = 0
    #if Starting_tile_Data in Stationery_equipment:
        #End_Junctions.append(Starting_tile)
    
    for Adjacent_tile in Adjacent_tiles:
        #print(Matrix[Adjacent_tile[0]][Adjacent_tile[1]])
        Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
        if Adjacent_tile_Data:
           
            if (Adjacent_tile_Data[0] == Matrix[Starting_tile[0]][Starting_tile[1]][0]):
                
                Adjacent_count += 1
                Adjacent_Tiles_with_same.append(Adjacent_tile)
                
            elif Adjacent_tile_Data[0] in Cables:
                Adjacent_count += 1
                Adjacent_Tiles_with_same.append(Adjacent_tile)
                
            elif Adjacent_tile_Data[0] in Cable_links:
                Adjacent_count += 1
                Adjacent_Tiles_with_same.append(Adjacent_tile)
            
            elif Adjacent_tile_Data[0] in Stationery_equipment:
                
                #print('33',Adjacent_tile_Data[0], Adjacent_tile)##################################
                #if is_fiers:
                    #Adjacent_Tiles_with_same.append(Adjacent_tile)
                Adjacent_count += 1
                Adjacent_Tiles_with_same.append(Adjacent_tile)
                
            
                
                    
                
                #if Starting_tile_Data[0] in Cable_links:
                    #Adjacent_count += 1
                    
                    #if not Adjacent_tile in Junctions_to_work_on:
                        #Junctions_to_work_on.append(Adjacent_tile)
                
                        
                        #Cable_Worked_on.append(Adjacent_tile)
                        #Adjacent_count += 1
                        #Adjacent_Tiles_with_same.append(Adjacent_tile)
                        #if not Adjacent_tile in Junctions_to_work_on:
                            #Junctions_to_work_on.append(Adjacent_tile)
                        #if not Starting_tile in Junctions_to_work_on:
                            #Junctions_to_work_on.append(Starting_tile)
                #Cable_in_line.append(Adjacent_tile)
                
                #End_Junctions.append(Adjacent_tile)
                
                
    #print(Adjacent_count)
    if Adjacent_count == 1:
        Starting_tile_Data = Matrix[Starting_tile[0]][Starting_tile[1]]
        if is_fiers:
            if Starting_tile_Data[0] in Cable_links and not len(Cable_in_line):
                #print(Adjacent_Tiles_with_same,'help!!!') ###############################################
                Adjacent_count += 1
                
        #Junctions_to_work_on.append(Starting_tile)
        #print('1',Starting_tile)
        print('add End_Junctions = at end of cbael' )
        End_Junctions.append(Starting_tile)
    if Adjacent_count == 2:
        #print('2',Starting_tile)
        #print('pop?')
        #rint(Adjacent_Tiles_with_same,'Adjacent_Tiles_with_same')
        Cable_in_line.append(Adjacent_tile)
        Cable_Worked_on.append(Starting_tile)
        for Adjacent_Tile_with_same in Adjacent_Tiles_with_same:
            
            if Matrix[Starting_tile[0]][Starting_tile[1]][0] in Stationery_equipment:
                if not Starting_tile in Junctions_to_work_on:
                   Junctions_to_work_on.append(Starting_tile)
                   
                if Adjacent_Tile_with_same in Stationery_equipment:
                    End_Junctions.append(Adjacent_Tile_with_same)
                    Junction_origin = Starting_tile.copy()
                else:
                    sub_Adjacents = Dictionary_of_adjacents[tuple(Adjacent_Tile_with_same)].copy()
                    Adjacent_sub_count = 0
                    for sub_Adjacent in sub_Adjacents:
                        Adjacent_sub_tile_Data = Matrix[sub_Adjacent[0]][sub_Adjacent[1]]
                        if Adjacent_sub_tile_Data:
                            if Adjacent_sub_tile_Data[0] in Cables:
                                Adjacent_sub_count += 1
                                
                            elif Adjacent_sub_tile_Data[0] in Cable_links:
                                Adjacent_sub_count += 1
                            
                            elif Adjacent_sub_tile_Data[0] in Stationery_equipment:
                                Adjacent_sub_count += 1
                                #Junctions_to_work_on.append(sub_Adjacent)
                                #Adjacent_Tile_with_same_Data = Matrix[Adjacent_Tile_with_same[0]][Adjacent_Tile_with_same[1]][0]  
                                #if Adjacent_Tile_with_same_Data in Cable_links:
                                    #print('heyrrrrr?')
                                    #End_Junctions.append(Adjacent_Tile_with_same)
                                    #Junction_origin = Starting_tile
                                
                    if Adjacent_sub_count > 2:
                        #if not Adjacent_Tile_with_same in Junctions_to_work_on:
                            #Junctions_to_work_on.append(Adjacent_Tile_with_same)
                        End_Junctions.append(Adjacent_Tile_with_same)
                        Junction_origin = Starting_tile.copy()


                        


                        
            if Adjacent_Tile_with_same not in Cable_Worked_on:
                if Matrix[Adjacent_Tile_with_same[0]][Adjacent_Tile_with_same[1]][0] not in Stationery_equipment:
                    #print('runging nexke cabel',Adjacent_Tile_with_same,Adjacent_Tiles_with_same)
                    #print('add Junction cables by  Stationery_equipment')

                    Cable_search(Adjacent_Tile_with_same)
                    
                    
                else:
                    #print('add Junction cables by  Stationery_equipment')
                    if not Adjacent_Tile_with_same in Junctions_to_work_on:
                        if Matrix[Starting_tile[0]][Starting_tile[1]][0] in Cable_links:
                            Junctions_to_work_on.append(Adjacent_Tile_with_same)

                            
                    if Matrix[Starting_tile[0]][Starting_tile[1]][0] == 'Link_up_cable':
                        #print('add Coming_from_machine = Link_up_cable')
                        Going_into_machine.append(Adjacent_Tile_with_same)

                    elif Matrix[Starting_tile[0]][Starting_tile[1]][0] == 'Link_down_cable':
                        #print('add Coming_from_machine = Link_down_cable' )
                        Coming_from_machine.append(Adjacent_Tile_with_same)
            
            elif Adjacent_Tile_with_same in Junctions_to_work_on:
                #print('y432eszsd')
            
                if Matrix[Starting_tile[0]][Starting_tile[1]][0] == 'Link_up_cable':
                    #print('add Coming_from_machine = Link_up_cable')
                    Going_into_machine.append(Adjacent_Tile_with_same)

                elif Matrix[Starting_tile[0]][Starting_tile[1]][0] == 'Link_down_cable':
                    #print('add Coming_from_machine = Link_down_cable' )
                    Coming_from_machine.append(Adjacent_Tile_with_same)
                #if not len(Cable_in_line):
                else:
                    End_Junctions.append(Adjacent_Tile_with_same) 
                    
                
                        
                    
                
    elif Adjacent_count > 2:
        #print('pop')
        if len(Cable_in_line) == 0:
            Cable_Worked_on.append(Starting_tile)
            #print('doing this')
            for Adjacent_Tile_with_same in Adjacent_Tiles_with_same:
                Adjacent_Tile_with_same_Data = Matrix[Adjacent_Tile_with_same[0]][Adjacent_Tile_with_same[1]]
                if Adjacent_Tile_with_same_Data[0] in Stationery_equipment:
                    End_Junctions.append(Adjacent_Tile_with_same.copy())
                    Junction_origin = Starting_tile.copy()
                        

                else:
                    sub_Adjacents = Dictionary_of_adjacents[tuple(Adjacent_Tile_with_same)].copy()
                    Adjacent_sub_count = 0
                    for sub_Adjacent in sub_Adjacents:
                        Adjacent_sub_tile_Data = Matrix[sub_Adjacent[0]][sub_Adjacent[1]]
                        if Adjacent_sub_tile_Data:
                            if Adjacent_sub_tile_Data[0] in Cables:
                                Adjacent_sub_count += 1
                                
                            elif Adjacent_sub_tile_Data[0] in Cable_links:
                                Adjacent_sub_count += 1
                            
                            elif Adjacent_sub_tile_Data[0] in Stationery_equipment:
                                Adjacent_sub_count += 1
                                #Junctions_to_work_on.append(sub_Adjacent)

                                
                                #Adjacent_Tile_with_same_Data = Matrix[Adjacent_Tile_with_same[0]][Adjacent_Tile_with_same[1]][0]  
                                #if Adjacent_Tile_with_same_Data in Cable_links:
                                    #print('heyrrrrr?')
                                    #End_Junctions.append(Adjacent_Tile_with_same)
                                    #Junction_origin = Starting_tile
                                
                    if Adjacent_sub_count > 2:
                        #if not Adjacent_Tile_with_same in Junctions_to_work_on:
                            #Junctions_to_work_on.append(Adjacent_Tile_with_same.copy())
                        End_Junctions.append(Adjacent_Tile_with_same)
                        Junction_origin = Starting_tile.copy()

                    
                #print('WOW o3o')
                #On_junction = True
                #print('add End_Junctions cables with 0 there cales > 2' )
                #End_Junctions.append(Adjacent_Tile_with_same)
        else:
            End_Junctions.append(Starting_tile)   
        #print('add Junction cables')
        #print(Cable_in_line)
        if not Starting_tile in Junctions_to_work_on:
            #print(Starting_tile,'Starting_tile in Adjacent_count > 2:')
            Junctions_to_work_on.append(Starting_tile.copy())
  
                    
        
    
        
def Junction_search(Starting_Junction):
    #if not Starting_Junction:
        #return
    #print(Starting_Junction,'Starting_Junction')
    Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_Junction)].copy()
    for Adjacent_tile in Adjacent_tiles:

        Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
        if Adjacent_tile_Data:
            Starting_Junction_Data = Matrix[Starting_Junction[0]][Starting_Junction[1]]
            #print(Adjacent_tile_Data[0],'1')
            if not Adjacent_tile_Data[0] in Stationery_equipment:
                #print(Adjacent_tile_Data[0],'bayyyy?')
                if Starting_Junction_Data:
                    if Starting_Junction_Data[0] in Stationery_equipment:  
                        if Adjacent_tile_Data[0] in Cable_links:
                            if not Adjacent_tile in Cable_Worked_on:
                                if not Adjacent_tile in Junctions_to_work_on:
                                    #print(Adjacent_tile,'Starting_tile') ############################
                                    Cable_search(Adjacent_tile,True)
                                    to_format()
                    elif Starting_Junction_Data[0] == Adjacent_tile_Data[0]:
                        if not Adjacent_tile in Cable_Worked_on:
                            if not Adjacent_tile in Junctions_to_work_on:
                                #print(Adjacent_tile,'Starting_tile') ####################################
                                Cable_search(Adjacent_tile,True)
                                to_format()
                                
                    elif Adjacent_tile_Data[0] in Cables:
                        if not Adjacent_tile in Junctions_to_work_on:
                                #print(Adjacent_tile,'Starting_tile') #####################################
                                Cable_search(Adjacent_tile,True)
                                to_format()
                                
                    elif Adjacent_tile_Data[0] in Cable_links:
                        if not Adjacent_tile in Junctions_to_work_on:
                                #print(Adjacent_tile,'Starting_tile') ############################
                                Cable_search(Adjacent_tile,True)
                                to_format()
            else:
                if not Adjacent_tile in Junctions_to_work_on:
                    Cable_search(Adjacent_tile,True)
                    to_format()
                    #Junctions_to_work_on.append(Adjacent_tile)

                
             
            
def to_format():
    #do_wins((164, 112))

    global On_junction
    global Cable_in_line
    global Going_into_machine
    global End_Junctions
    global Junction_origin
    global Coming_from_machine
    is_one = False
    is_two = False
    Dictionary1 = {}
    Dictionary2 = {}
    #print('formying')
    On_junction = False

    is_42_155 = False
    #if [42, 156] in (Going_into_machine + Coming_from_machine + End_Junctions):
        #print('what??')
        
    if Junction_origin:
        #print(Going_into_machine,'<Going_into_machine',Coming_from_machine,'<Coming_from_machine',End_Junctions,'<End_Junctions',Junction_origin,'<Junction_origin') ##############################
        for Junction_1 in End_Junctions:
            
            
            #Dictionary1['Cable type'] = String_of_Cable_type
            #Dictionary1['Cable locations'] = Cable_in_line.copy()
            #Dictionary1['Number of cables'] = len(Cable_in_line)
            #link_from[tuple(Going_into_machine[0])] = []
            Dictionary1['The other end'] = Junction_1
            
            links[tuple(Junction_origin)].append(Dictionary1.copy())
            Dictionary2['The other end'] = Junction_origin.copy()
            links[tuple(Junction_1)].append(Dictionary2.copy())
        
            
            
            
    #print(len(Going_into_machine) + len(Coming_from_machine) + len(End_Junctions))
    #print(Going_into_machine ,'Going_into_machine' , Coming_from_machine,'Coming_from_machine' , End_Junctions,'End_Junctions')
    #Going_into_machine = [] #end into aplche to
    #Coming_from_machine = [] #from aplslych | from
    
        
    else:
        if (len(Going_into_machine) + len(Coming_from_machine) + len(End_Junctions)) == 2:
            #print(Cable_in_line)
            #if [56, 165] in (Going_into_machine or Coming_from_machine or End_Junctions):
                #print(Going_into_machine,'Going_into_machine',Coming_from_machine,'Coming_from_machine',End_Junctions,'End_Junctions','heyyyyyyyyyy 42, 154')
                #is_42_155 = True
            #print(Going_into_machine,'Going_into_machine',Coming_from_machine,'Coming_from_machine',End_Junctions,'End_Junctions',)
            try:
                String_of_Cable_type = (Matrix[Cable_in_line[0][0]][Cable_in_line[0][1]])
            except IndexError:
                String_of_Cable_type = 'Error'
            
            #print(String_of_Cable_type,'String_of_Cable_type')
            if Going_into_machine:
                if Coming_from_machine:
                    Dictionary1['The other end'] = Coming_from_machine[0]

                else:
                    Dictionary1['The other end'] = End_Junctions[0]

                        
                Dictionary1['Cable type'] = String_of_Cable_type
                #Dictionary1['Cable locations'] = Cable_in_line.copy()
                #Dictionary1['Number of cables'] = len(Cable_in_line)
                #link_from[tuple(Going_into_machine[0])] = []
                link_from[tuple(Going_into_machine[0])].append(Dictionary1.copy()) #From junction to machine
                links[tuple(Going_into_machine[0])].append(Dictionary1.copy())
                
                if is_42_155:
                    print(links[tuple(Going_into_machine[0])], tuple(Going_into_machine[0]))
                
            else:
                if Coming_from_machine:

                    Dictionary1['The other end'] = Coming_from_machine[0]
                else:
                    Dictionary1['The other end'] = End_Junctions[1]

                Dictionary1['Cable type'] = String_of_Cable_type
                #Dictionary1['Cable locations'] = Cable_in_line.copy()
                #Dictionary1['Number of cables'] = len(Cable_in_line)
                links[tuple(End_Junctions[0])].append(Dictionary1.copy())  #on Cable Junctions
                is_one = True
                
                if is_42_155:
                    print(links[tuple(End_Junctions[0])], End_Junctions[0] )

            if Coming_from_machine:
                if Going_into_machine:
                    Dictionary2['The other end'] = Going_into_machine[0]

                else:
                    #print(Going_into_machine,'Going_into_machine',Coming_from_machine,'Coming_from_machine',End_Junctions,'End_Junctions')
                    if is_one:
                        Dictionary2['The other end'] = End_Junctions[0]
                    else:
                        Dictionary2['The other end'] = End_Junctions[1]

                Dictionary2['Cable type'] = String_of_Cable_type
                #Dictionary2['Cable locations'] = Cable_in_line.copy()
                #Dictionary2['Number of cables'] = len(Cable_in_line)
                #link_to[tuple(Coming_from_machine[0])] = []
                link_to[tuple(Coming_from_machine[0])].append(Dictionary2.copy()) #From machine to junction
                links[tuple(Coming_from_machine[0])].append(Dictionary2.copy())
                
                if is_42_155:
                    print(links[tuple(Coming_from_machine[0])], tuple(Coming_from_machine[0]) )
            else:
                if Going_into_machine:

                    Dictionary2['The other end'] = Going_into_machine[0]
                else:
                    Dictionary2['The other end'] = End_Junctions[0]

                Dictionary2['Cable type'] = String_of_Cable_type
                #Dictionary2['Cable locations'] = Cable_in_line.copy()
                #Dictionary2['Number of cables'] = len(Cable_in_line)
                if is_one:
                    #links[tuple(End_Junctions[1])] = []
                    links[tuple(End_Junctions[1])].append(Dictionary2.copy()) #on Cable Junctions
                    
                    if is_42_155:
                        print(links[tuple(End_Junctions[1])] ,tuple(End_Junctions[1]),'1')
                else:    
                    #links[tuple(End_Junctions[0])] = []
                    links[tuple(End_Junctions[0])].append(Dictionary2.copy()) #on Cable Junctions
                    
                    if is_42_155:
                        print(links[tuple(End_Junctions[0])] ,tuple(End_Junctions[0]),'0')
                if is_42_155:
                    print('yeah')
        else:
            print(Going_into_machine,'Going_into_machine',Coming_from_machine,'Coming_from_machine',End_Junctions,'End_Junctions')
    Junction_origin[:] = []
    Cable_in_line[:] = []
    Going_into_machine[:] = []
    End_Junctions[:] = []
    Coming_from_machine[:] = []
    On_junction = False

def check_Dictionary_link(): 
    
    r = range(0,Tile_range_x)
    r2 = range(0,Tile_range_y)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:

            for links_pop in links[tuple((z,p))]:
                if len(links_pop['The other end']) == 0:
                    print('what noOOoOO!',tuple((z,p)), links_pop)

                if tuple(links_pop['The other end']) == tuple((z,p)):
                    print('Its going to itself noOOoOO!',tuple((z,p)), links_pop)
                    
            
    print('chai_diatrly Done!')


def Link_Sort():
    def getKey1(item):
        return item[0]               
    def getKey2(item):
        return item[1]
    for link in links:
        if len(links[link]) > 1:
            The_other_ends = []
            for Dictionary_link in links[link]:
                
                ####Temporary!!!
                the_link = Dictionary_link['The other end'].copy() 
                #print(link,tuple(the_link))
                if tuple(the_link) == link:
                    pass
                    #print('WTF!!!!!! WHAT? nooo Dictionary_link[The other en].copy() == link:',link)
                else:
                    if the_link in The_other_ends:
                        pass
                        #print('WTF!!!!!! WHAT? nooo  Dictionary_link[The other end].copy() in The_other_ends',link)
                    else:
                        The_other_ends.append(the_link)

            
            
            The_other_ends = sorted(The_other_ends, key=getKey2)
            The_other_ends = sorted(The_other_ends, key=getKey1)
            del links[link]
            links[link] = []
            for The_other_end in The_other_ends:
                Dictionary1 = {}
                Dictionary1['The other end'] = The_other_end
                links[link].append(Dictionary1)
    print('Link_Sort Done!')

def Link_check(Link):
    global Junction_check_list
    global Starting_from
    Starting_from = (Link) 
    if (Link) in links:
        for to in links[(Link)]:
            if to['The other end']:
                Junction_check_list.append(to['The other end'])
            
            

def Circuit_initialization():
    global The_current_working_line
    global To_overlay_next
    global To_overlay_next
    work_on_nexd = []
    work_on_nexd_set = set([])
    for Appliance in Power_draw_appliances:
        ##
        Appliance_tuple = tuple([Appliance[0],Appliance[1]])
        Temporary_dictionary = {}
        Temporary_dictionary['Type'] = (Matrix[Appliance[0]][Appliance[1]][0])
        Temporary_dictionary['Resistance'] = 240
        Temporary_dictionary2 = {}
        Temporary_dictionary2[Appliance_tuple] = [240,Appliance] 
        Temporary_dictionary['Resistance from cabeis'] = Temporary_dictionary2.copy()
        Temporary_dictionary['Resistance coming from'] = Appliance
        Power_dictionary[Appliance_tuple] = Temporary_dictionary.copy()
        
        ##
        if not Appliance in The_current_working_line:
            The_current_working_line.append(Appliance)
        if not Appliance_tuple in work_on_nexd_set:
            work_on_nexd.append(Appliance)
        work_on_nexd_set.add(Appliance_tuple)
                        
    print(work_on_nexd,'heeyeyyeyey')              
    if len(work_on_nexd) > 0:
        #print('working on!')
        f = True
        Circuit_jup(work_on_nexd,f)       

def Circuit_jup(work_on_nexd_form_top,First = False,Just_pick = False):
    global The_current_working_line
    global To_overlay_next
    global Electrical_changes
    global Mid_junctions
    #print('Circuit_jup', work_on_nexd_form_top)
    work_on_nexd = []
    work_on_nexd_set = set([])
    for Working_on in work_on_nexd_form_top:
         
        Working_on_tuple = tuple([Working_on[0],Working_on[1]])
        Working_on_Data = Matrix[Working_on[0]][Working_on[1]][0]
        if not First:
            if Working_on_Data in Power_sys_module.module_dictionary:
                Electrical_changes[Working_on_tuple] = [Working_on_Data,Working_on]
            
        if not Working_on_tuple in Electrical_changes:
            if 'Upstream' in Power_dictionary[Working_on_tuple]:
                for The_other_end in Power_dictionary[Working_on_tuple]['Upstream']:

                    Circuit_jump_landing(Working_on,The_other_end)
                    if The_other_end in The_current_working_line:
                        if not The_other_end in To_overlay_next:
                            To_overlay_next.append(The_other_end)
                    else:
                        if not The_other_end in work_on_nexd:
                            work_on_nexd.append(The_other_end)
            else:
                cl = 0
                quck_do = []
                for linked_to_Appliance in links[Working_on_tuple]:

                    The_other_end = linked_to_Appliance['The other end']
                    if not The_other_end in Power_dictionary[Working_on_tuple]['Resistance coming from']:
                        cl += 1
                        if not The_other_end in quck_do:  
                            quck_do.append(The_other_end)
                        #Circuit_jump_landing(Working_on,The_other_end)
                        
                if not cl > 1:
                    for done in quck_do:
                        #print(cl)
                        #print(quck_do)
                        Circuit_jump_landing(Working_on,done)
                        if done in The_current_working_line:
                            if not done in To_overlay_next:
                                To_overlay_next.append(done)
                        else:
                            if not done in work_on_nexd:
                                work_on_nexd.append(done)
                elif Just_pick:
                    for done in quck_do:
                        #print(cl)
                        #print(quck_do)
                        Circuit_jump_landing(Working_on,done)
                        if done in The_current_working_line:
                            if not done in To_overlay_next:
                                To_overlay_next.append(done)
                        else:
                            if not done in work_on_nexd:
                                work_on_nexd.append(done)
                    
                else:
                    if not Working_on in Mid_junctions:
                        #print('adding',Working_on )
                        Mid_junctions.append(Working_on)
                    
                
                
    if len(work_on_nexd) > 0:
        First = False
        Circuit_jup(work_on_nexd,)
    else:
        #do_wins((164, 112))
        work_on_nexd = To_overlay_next.copy()
        To_overlay_next[:] = []
        
        if len(work_on_nexd) > 0:
            The_current_working_line[:] = []
            First = False
            Circuit_jup(work_on_nexd,First)
        else:
            if len(Electrical_changes) > 0:
                
                for key, the_list in Electrical_changes.items():
                    #print('hey1')
                    Power_sys_module.module_dictionary[the_list[0]](the_list[1],Power_dictionary)
                    work_on_nexd.append(the_list[1])
                if len(work_on_nexd) > 0:
                    The_current_working_line[:] = []
                    Electrical_changes = {}
                    #print('doing')
                    First = True
                    Circuit_jup(work_on_nexd,First,Just_pick)
                    

            else:
                if len(Mid_junctions) > 0:
                    First = False
                    #print(Mid_junctions,'p1')
                    #print(work_on_nexd_form_top,'p2')
                    if The_current_working_line: 
                        Try_to_do_it = False
                    else:
                        Try_to_do_it = True
                    work_on_nexd = Mid_junctions.copy()
                    Mid_junctions[:] = []
                    The_current_working_line[:] = []
                    #print(work_on_nexd,'work_on_nexd')
                    Circuit_jup(work_on_nexd,First,Try_to_do_it)

                
                
def Circuit_jump_landing(Working_on,The_other_end):
    
    Working_on_tuple = tuple([Working_on[0],Working_on[1]])

    if tuple(The_other_end) in Power_dictionary:
        if not Working_on in Power_dictionary[tuple(The_other_end)]['Resistance coming from']:
            Power_dictionary[tuple(The_other_end)]['Resistance coming from'].append(Working_on)
            Power_dictionary[tuple(The_other_end)]['Resistance from cabeis'][Working_on_tuple] = [(Power_dictionary[Working_on_tuple]['Resistance']),Working_on]
    else:
        Temporary_dictionary2 = {}
        Temporary_dictionary3 = {}  
        Temporary_dictionary3[Working_on_tuple] = [(Power_dictionary[Working_on_tuple]['Resistance']),Working_on]
        Temporary_dictionary2['Resistance from cabeis'] = Temporary_dictionary3
        Temporary_dictionary2['Resistance coming from'] = []
        Temporary_dictionary2['Resistance coming from'].append(Working_on)
        Power_dictionary[tuple(The_other_end)] = Temporary_dictionary2.copy()        
    if not The_other_end in  The_current_working_line: 
        The_current_working_line.append(The_other_end)
        
    if 'Upstream' in Power_dictionary[Working_on_tuple]:
        if not The_other_end in Power_dictionary[Working_on_tuple]['Upstream']:
            Power_dictionary[Working_on_tuple]['Upstream'].append(The_other_end)
    else:
        Power_dictionary[Working_on_tuple]['Upstream'] = []
        Power_dictionary[Working_on_tuple]['Upstream'].append(The_other_end)
    
    ####################################################
    ResistanceX_all = 0
    Resistance = 0
    if tuple(The_other_end) in Power_dictionary:
        if len(Power_dictionary[tuple(The_other_end)]['Resistance coming from']) > 1:
            #print('more then one', Power_dictionary[tuple(Junction)]['being redsed from'] )
            for Resistance in Power_dictionary[tuple(The_other_end)]['Resistance from cabeis'].values():
                ResistanceX_all += 1/Resistance[0]
            Resistance = 1/ResistanceX_all
            Power_dictionary[tuple(The_other_end)]['Resistance'] = Resistance
        else:
            for value in Power_dictionary[tuple(The_other_end)]['Resistance from cabeis'].values():
                Power_dictionary[tuple(The_other_end)]['Resistance'] = value[0]
    ###############################################################################

def work_backwords():
    global Power_sply_appliances
    global Electrical_changes
    Working_on_list = Power_sply_appliances
    Electrical_changes = {}
    First = True
    Jumping_backwards(Working_on_list,First)

        
def Jumping_backwards(Working_on_list_top, First = False):
    global Working_on_backward_list
    global Electrical_changes
    Working_on_backward_list = []
    #print(Working_on_list_top,'Working_on_list_top Jumping_backwards')
    for Working_on in Working_on_list_top:
        Landing_backward(Working_on)
        Working_on_tuple = tuple((Working_on[0],Working_on[1]))
        Working_on_Data = Matrix[Working_on[0]][Working_on[1]][0]
        
        if not First:
            #print('hey on this?')
            if Working_on_Data in Power_sys_module.module_dictionary:
                Electrical_changes[Working_on_tuple] = [Working_on_Data,Working_on]
                
        if not Working_on_tuple in Electrical_changes:        
            for the_othere_end, Resistance  in Power_dictionary[Working_on_tuple]['Resistance from cabeis'].items():
                #print('hey')First = False
                if not Resistance[1] == Working_on:
                    if not Resistance[1] in Working_on_backward_list:
                        Working_on_backward_list.append(Resistance[1])
    
    if len(Working_on_backward_list) > 0:
        #do_wins((164, 112))
        #print(Working_on_list,'pop')
        Jumping_backwards(Working_on_backward_list)
    else:
        if len(Electrical_changes) > 0:
            work_on_nexd = []
            for key, the_list in Electrical_changes.items():
                #print('hey1')
                Power_sys_module.module_dictionary[the_list[0]](the_list[1],Power_dictionary)
                work_on_nexd.append(the_list[1])
            #print(work_on_nexd,'opo')
            if len(work_on_nexd) > 0:
                #print(work_on_nexd,'opo3')
                The_current_working_line[:] = []
                Electrical_changes = {}
                #print('doing')
                First = True
                Jumping_backwards(work_on_nexd,First)
        
             
def Landing_backward(Power_sply_appliance):
    #print(Power_sply_appliance)
    Power_sply_appliance_tuple = tuple((Power_sply_appliance[0],Power_sply_appliance[1]))
    Simply_Times_by = 0
    if len(Power_dictionary[Power_sply_appliance_tuple]['Resistance coming from']) > 1:
        all_resisted = 0                                                                 
        for The_resisted in Power_dictionary[Power_sply_appliance_tuple]['Resistance from cabeis'].values():
            all_resisted += The_resisted[0]
        Simply_Times_by = Power_dictionary[Power_sply_appliance_tuple]['Supply current']/all_resisted                 

                                                                             
    for the_othere_end_tuple, the_othere_end in Power_dictionary[Power_sply_appliance_tuple]['Resistance from cabeis'].items():
        Power_dictionary[the_othere_end_tuple]['Receiving voltage'] = Power_dictionary[Power_sply_appliance_tuple]['Supplying voltage']
        Power_dictionary[the_othere_end_tuple]['Supplying voltage'] = Power_dictionary[the_othere_end_tuple]['Receiving voltage']
        
        if Simply_Times_by:
            Supply_current = Simply_Times_by * Power_dictionary[Power_sply_appliance_tuple]['Resistance']
        else:
            Supply_current = Power_dictionary[Power_sply_appliance_tuple]['Supply current']
                    
        if 'current coming from' in Power_dictionary[the_othere_end_tuple]:
            if not Power_sply_appliance_tuple == the_othere_end_tuple:
                Power_dictionary[the_othere_end_tuple]['current coming from'][Power_sply_appliance_tuple] = [Supply_current,Power_sply_appliance]
            
            if len(Power_dictionary[the_othere_end_tuple]['current coming from']) > 1:
                Total_Current = 0
                for Currents in Power_dictionary[the_othere_end_tuple]['current coming from'].values():
                    Total_Current += Currents[0]
                Power_dictionary[the_othere_end_tuple]['Supply current'] = Total_Current
            else:
                for Current in Power_dictionary[the_othere_end_tuple]['current coming from'].values():
                    Power_dictionary[the_othere_end_tuple]['Supply current'] = Current[0]
                    
        else:
            temporarydictionary1 = {}
            temporarydictionary1[Power_sply_appliance_tuple] = [Supply_current,Power_sply_appliance]
            Power_dictionary[the_othere_end_tuple]['current coming from'] = temporarydictionary1.copy()
            Power_dictionary[the_othere_end_tuple]['Supply current'] = Power_dictionary[the_othere_end_tuple]['current coming from'][Power_sply_appliance_tuple][0]


    
Circuit_search()

check_Dictionary_link()
Link_Sort()

#do_wins((64, 112))
Circuit_initialization()

#do_wins((77, 129))

work_backwords()
#

print(Power_dictionary[(43, 155)])

#print(links[(56, 165)],'pop')
#print(links[(87, 110)])

#Link_check((42, 155))

r = range(0,Tile_range_x)
r2 = range(0,Tile_range_y)
a = []
for z in r:
    #print (z,'in 1')
    x = []
    for p in r2:
        if tuple((z,p)) in Power_dictionary:
            pass
            #print(tuple((z,p)), Power_dictionary[tuple((z,p))])
        #if len(links[tuple((z,p))]) > 0:
            #pass
            #print((z,p),links[tuple((z,p))])
            



print(im.format, im.size, im.mode)
do_wins((43, 155))


