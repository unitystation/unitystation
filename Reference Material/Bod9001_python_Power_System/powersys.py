#v0.9 [WIP] Still hasn't been cleaned up so still quite messy 20/05/2018 12:21 GMT
import json
import time
import math
import sys
import cProfile
import pyglet #Requires pyglet
from pyglet.window import mouse
#import png
#import numpy as np
from PIL import Image #Requires pillow
import copy
import random
import os
import Stationary_Equipment as Power_sys_module #I should find a automatically of doing this
import Connections
import Power_Functions


def create_3_vertex_list(x, y, x2, y2):
    if x2 > x:
        off_set_x = 3
    elif x2 < x:
        off_set_x = - 3
    else:
        off_set_x = 3

    if y2 > y:
        off_set_y = 3
    elif y2 < y:
        off_set_y = - 3
    else:
        off_set_y = 3
    return x2, y2, x + off_set_x, y - off_set_y,   x - off_set_x, y + off_set_y


def Calculating_Direction(batch,Initial_offset_x,Initial_offset_y ,Box_size,Grid_offset):
    global Window_width
    global Window_height
    for key, values in Power_dictionary.items():
        if 'Upstream' in values:
            Upstreams = values['Upstream']
            for Upstream in Upstreams:
                Upstream_done = list(list(Upstream)[0])  
                Coordinates = list(list(key)[0]) 
                Starting_coordinate_x = (int(round((Box_size/2), 0))) + Initial_offset_x + ((Box_size*Upstream_done[0]) + (Grid_offset*Upstream_done[0]))
                Starting_coordinate_y = (int(round((Box_size/2), 0))) + Initial_offset_y + ((Box_size*Upstream_done[1]) + (Grid_offset*Upstream_done[1]))
    
                Starting_coordinate_x2 = (int(round((Box_size/2), 0))) + Initial_offset_x + ((Box_size*Coordinates[0]) + (Grid_offset*Coordinates[0]))
                Starting_coordinate_y2 = (int(round((Box_size/2), 0))) + Initial_offset_y + ((Box_size*Coordinates[1]) + (Grid_offset*Coordinates[1]))
            
                if not (Starting_coordinate_x > Window_width or Starting_coordinate_x < 0) and not (Starting_coordinate_y > Window_height or Starting_coordinate_y < 0):
                    batch.add(3, pyglet.gl.GL_TRIANGLES, None,('v2i',
                                                               (create_3_vertex_list(Starting_coordinate_x,Starting_coordinate_y,Starting_coordinate_x2,Starting_coordinate_y2))),
                                                                ('c3B',(
                                                                255, 144, 0,
                                                                255, 144, 0, 
                                                                255, 144, 0,
                                                                ))) 

def create_quad_vertex_list(x, y, width, height):
    return x, y, x + width, y, x + width, y + height, x, y + height

def batch_add(batch,R,G,B,Coordinates,Box_size):
    Starting_coordinate_x = Initial_offset_x + ((Box_size*Coordinates[0]) + (Grid_offset*Coordinates[0]))
    Starting_coordinate_y = Initial_offset_y + ((Box_size*Coordinates[1]) + (Grid_offset*Coordinates[1]))
    
    if not (Starting_coordinate_x > Window_width or Starting_coordinate_x < 0) and not (Starting_coordinate_y > Window_height or Starting_coordinate_y < 0):
        batch.add(4, pyglet.gl.GL_QUADS, None,('v2i',
            (create_quad_vertex_list(Starting_coordinate_x,Starting_coordinate_y,Box_size,Box_size))),
            ('c3B',(
             R, G, B,
             R, G, B, 
             R, G, B,
             R, G, B,)))

#@profile
def Drawing_in_window():
    global Cross_hair
    global Window_width
    global Window_height
    global VertexList
    global Cable_Worked_on
    global Junction_check_set
    global Starting_from
    global Started_scanning_from
    global To_overlay_next
    global Working_on_backward_list
    global Parallel_done
    global Parallel_cables_ends
    global Cable_Worked_on_set
    global Box_size
    global Initial_offset_x
    global Initial_offset_y
    global Calculating_Direction
    global Link_check_show
    global Power_draw_appliances_show
    global Junctions_to_work_on_show
    global Junctions_to_work_on_show
    global Grid_offset
    batch = pyglet.graphics.Batch()

    Starting_coordinate_x = ((Box_size*Tile_range_x) + (Grid_offset*Tile_range_x))
    Starting_coordinate_y = ((Box_size*Tile_range_y) + (Grid_offset*Tile_range_y))
    batch.add(4, pyglet.gl.GL_QUADS, None,('v2i',
                                        (create_quad_vertex_list(Initial_offset_x,Initial_offset_y,Starting_coordinate_x,Starting_coordinate_y))),
                                                        ('c3B',(
                                                        74, 0, 176,
                                                        74, 127, 176, 
                                                        74, 127, 176,
                                                        74, 255, 176,
                                                        )))  
    
    for Cable_Worked in Cable_Worked_on_set:
        BLUE = 255
        GREEN = 255
        RED = 255
        Coordinates = list(list(Cable_Worked)[0])
        batch_add(batch,RED,GREEN,BLUE,Coordinates,Box_size)

    for link in links:
        BLUE = 132
        GREEN = 220
        RED = 86
        Coordinates = list(list(link)[0]) 
        batch_add(batch,RED,GREEN,BLUE,Coordinates,Box_size)
            
        
    for key, value in Power_dictionary.items():
        if 'Supplying voltage' in value:
            RED = 255
        else:
            RED = 0
            
        GREEN = 0
        BLUE = 0
        Coordinates = list(list(key)[0]) 
        batch_add(batch,RED,GREEN,BLUE,Coordinates,Box_size)
        
    if Power_draw_appliances_show:   
        for Power_draw in Power_draw_appliances:
            BLUE = 50 
            GREEN = 10
            RED = 220
            Coordinates = Power_draw[0]
            batch_add(batch,RED,GREEN,BLUE,Coordinates,Box_size)
        
    if Calculating_Direction_show:
        Calculating_Direction(batch,Initial_offset_x,Initial_offset_y ,Box_size,Grid_offset)
        
    if Link_check_show:
        for Junction in Junction_check_set:
            BLUE = 222
            GREEN = 220
            RED = 0
            Coordinates = list(Junction)[0]
            batch_add(batch,RED,GREEN,BLUE,Coordinates,Box_size)
            
    if Junctions_to_work_on_show:
        for to_work_on in Junctions_to_work_on_set:
            BLUE = 220
            GREEN = 20
            RED = 160
            Coordinates = list(to_work_on)[0] 
            batch_add(batch,RED,GREEN,BLUE,Coordinates,Box_size)
        
            
        

    RED = 255       
    GREEN = 100
    BLUE = 255
    Coordinates = list(Cross_hair)
    batch_add(batch,RED,GREEN,BLUE,Coordinates,Box_size)
        
    batch.draw()
    #print('Window done')
    #Save_name =  str(time.time())
    #pyglet.image.get_buffer_manager().get_color_buffer().save('screenshot' + Save_name + '.png')

def do_wins():
    #print(Cross_hair)
    window = pyglet.window.Window(1000, 1000,resizable=True)
    @window.event
    def on_draw():
        window.clear()
        Drawing_in_window()
        
    @window.event
    def on_mouse_scroll(x, y, scroll_x, scroll_y):
        global Initial_offset_x
        global Initial_offset_y
        global Box_size
        old_Box_size = Box_size
        if not scroll_x == 0:
            Box_size += int(scroll_x)
        if not scroll_y == 0:
            Box_size += int(scroll_y)
            if Box_size <= 0:
                Box_size = 1
        
        Initial_offset_x = int(Initial_offset_x*(Box_size/old_Box_size))
        Initial_offset_y = int(Initial_offset_y*(Box_size/old_Box_size))
        #print(scroll_x,'scroll_x')
        #print(scroll_y,'scroll_y')

    
        
    @window.event    
    def on_mouse_drag(x, y, dx, dy, buttons, modifiers):
        global Initial_offset_x
        global Initial_offset_y
        Initial_offset_x += dx
        Initial_offset_y += dy

    @window.event
    def on_resize(width, height):
        global Window_width
        global Window_height
        Window_width = width
        Window_height = height
        
    @window.event    
    def on_key_release(symbol, modifiers):
        global Cross_hair
        global Initial_offset_x
        global Initial_offset_y
        global Calculating_Direction_show
        global Link_check_show
        global Power_draw_appliances_show
        global Junctions_to_work_on_show
        if symbol == 65360: #home key
            Initial_offset_x = 0
            Initial_offset_y = 0
        if symbol == 49:
            if Calculating_Direction_show:
                Calculating_Direction_show = False
            else:
                Calculating_Direction_show = True
                
        elif symbol == 50:
            if Link_check_show:
                Link_check_show = False
            else:
                Link_check_show = True

                
        elif symbol == 51:
            if Power_draw_appliances_show:
                Power_draw_appliances_show = False
            else:
                Power_draw_appliances_show = True
                
        elif symbol == 52:
            if Junctions_to_work_on_show:
                Junctions_to_work_on_show = False
            else:
                Junctions_to_work_on_show = True
                
        elif symbol == 65363:
            if modifiers == 17:
                Cross_hair[0] = Cross_hair[0] + 10
            else:
                Cross_hair[0] = Cross_hair[0] + 1
                
            
        elif symbol == 65361:
            if modifiers == 17:
                Cross_hair[0] = Cross_hair[0] - 10
            else: 
                Cross_hair[0] = Cross_hair[0] - 1
               
            
        elif symbol == 65362:
            if modifiers == 17:
                Cross_hair[1] = Cross_hair[1] + 10
            else: 
                Cross_hair[1] = Cross_hair[1] + 1
             
            
        elif symbol == 65364:
            if modifiers == 17:
                Cross_hair[1] = Cross_hair[1] - 10
            else:    
                Cross_hair[1] = Cross_hair[1] - 1
  
        elif symbol == 65293:
            if modifiers == 17:
                print(Cross_hair,Matrix[Cross_hair[0]][Cross_hair[1]]) 
            else:
                print(Cross_hair)
                
        elif symbol == 65473:
            window.close()
        elif symbol == 108:
            if Matrix[Cross_hair[0]][Cross_hair[1]]:
                for Matrix_pop in Matrix[Cross_hair[0]][Cross_hair[1]]:
                    tuple_Cross_hair = tuple([tuple(Cross_hair),Matrix_pop])
                    if tuple_Cross_hair in links:
                        print(links[tuple_Cross_hair])
                        
        elif symbol == 112:
            if Matrix[Cross_hair[0]][Cross_hair[1]]:
                for Matrix_pop in Matrix[Cross_hair[0]][Cross_hair[1]]:
                    tuple_Cross_hair = tuple([tuple(Cross_hair),Matrix_pop])
                    if tuple_Cross_hair in Power_dictionary:
                        print(Power_dictionary[tuple_Cross_hair])

            
        #print(symbol, modifiers)
    #def on_mouse_press(x, y, button, modifiers):
        #if button == mouse.LEFT:
            #print('The left mouse button was pressed.')
     

    if __name__ == "__main__":
        pyglet.app.run()
        return()



class linksX:
    The_other_end = []
    Cable_type = ''
    Cable_locations = [[]]
    Number_of_cables = 0
    

Box_size = 3
Initial_offset_x = 0
Initial_offset_y = 0
Window_width = 0
Window_height = 0
Grid_offset = 0
Cross_hair = [50,50]
Calculating_Direction_show = False
Link_check_show = False
Power_draw_appliances_show = False
Junctions_to_work_on_show = False

 
is_to_Parallel = False 
Overlapping = False           
Dictionary_of_adjacents = {}
Quick_dictionary = {}
In_floor_tile = [] 
Electrical_appliances_locations = []
Electrical_appliances_locations_back_up = []
Cable_Worked_on = []
Junctions_to_work_on_sub_list = []
End_Junctions = []
Cable_in_line = []
Power_draw_appliances = []
Power_supply_appliances = []
Power_supply_appliances_list = []
Battery_Power_supply_engineering = []
Battery_Power_supply_department = []
Up_Flow_Flow_Already_done = []
Junction_origin = []
Starting_from = []
Started_scanning_from = []
To_overlay_next = []
The_current_working_line = []
Working_on_backward_list = []
Working_backwards_done_Update = []
Working_backwards_done = []
Up_Flow_Flow_back = []
Up_Flow_Flow_Follow_back = []
Support_current_detected = []
Mid_junctions = []
Parallel_List = []
Parallel_done = []
Parallel_cables_ends = []
Parallel_conere = []
Parallel_conere_Calling_from = []
Removing_List = []
Adding_List = []
Direction_jump_Next = []
Direction_jump_waiting = []
up_date_come_from = []
Links_to_work_from = []
To_link = [] # [[[23,32], 'do not'],[[23,54],'do so']]
Links_to_delete = []
Pass_on_to_next = []
Direction_jump_Next_wate = []
Up_Flow_Voltage_Remind_chek = []
Junctions_to_work_on_set = set([])
Cable_Worked_on_set = set([])
Junction_check_set = set([])


Electrical_appliances = ['Medium_voltage_cable','Transformer','Radiation_collector','Engineering_batteries','Department_batteries','Low_Voltage_cable','APC','High_voltage_cable']
Stationery_equipment = ['Transformer','Radiation_collector','Engineering_batteries','Department_batteries','APC']

Power_supply_appliances_types = ['Radiation_collector']
Battery_Power_supply_appliances_types = ['Engineering_batteries','Department_batteries']

Power_draw_appliances_types = ['APC']

Electrical_changes_Update = {}
Electrical_changes_up_flow = {}
Electrical_changes = {}
links = {}
Power_dictionary = {}
Persistent_power_system_data = {}
Power_supply_appliances_list = [[],[],[]]

Power_supply_appliances_pryonty = {'Radiation_collector':0,'Engineering_batteries':1,'Department_batteries':2}
#Power_supply_appliances_dictionary = {0:[blae, bale]}










High_voltage_cable_Colour            = (26,255,26,255)        
Transformer_Colour                   = (255,79,255,255)
Radiation_collector_Colour           = (255,240,26,255)
Medium_Voltage_Cable_Colour          = (86,176,190,255)
Engineering_batteries_Colour         = (153,176,3,255)
Department_batteries_Colour          = (197,153,204,255)
Low_voltage_cable_Colour             = (26,69,255,255)
APC_Colour                           = (255,193,69,255)
Link_cable_High_voltage_Colour       = (9,166,69,255)
Link_cable_Medium_voltage_Colour     = (86,131,176,255)
Link_cable_Low_voltage_Colour        = (26,179,255,255)
Medium_and_Low_voltage_Cable_Colour  = (182,224,52,255)

Colour_dictionary = {
    High_voltage_cable_Colour:['High_voltage_cable'],
    Medium_Voltage_Cable_Colour:['Medium_voltage_cable'],
    Transformer_Colour:['Transformer'],
    Radiation_collector_Colour:['Radiation_collector'],
    Engineering_batteries_Colour:['Engineering_batteries'],
    Department_batteries_Colour:['Department_batteries'],
    Low_voltage_cable_Colour:['Low_voltage_cable'],
    APC_Colour:['APC'],
    Link_cable_High_voltage_Colour:['Link_cable_High_voltage'],
    Link_cable_Medium_voltage_Colour:['Link_cable_Medium_voltage'],
    Link_cable_Low_voltage_Colour:['Link_cable_Low_voltage'],
    Medium_and_Low_voltage_Cable_Colour:['Low_voltage_cable','Medium_voltage_cable'],
}
#im = Image.open("Power tes.png")

#
im = Image.open("Ultimate power V3.png")
#im = Image.open("power.png")
px = im.load()
print(im.size)
Tile_range_x = im.size[0]
Tile_range_y = Tile_range_x
#Tile_range_y = im.size[1]

Matrix = [[0 for x in range(0,Tile_range_x )] for y in range(0,Tile_range_y)]

#
 
            


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


def Initialising_Matrix():
    for x in range(0,Tile_range_x):
        #print(x)
        for y in range(0,Tile_range_y):

            Matrix[x][y] = In_floor_tile.copy()
   
            try:
                #print(px[x,y])
                if px[x,y] in Colour_dictionary:
                    for cabel in Colour_dictionary[px[x,y]]:
                        if cabel in Power_draw_appliances_types:
                        #print('adding')
                            Power_draw_appliances.append([[x,y],cabel])

                        if cabel in Power_supply_appliances_pryonty:
                            Power_supply_appliances_list[Power_supply_appliances_pryonty[cabel]].append([[x,y],cabel])
                            
                        if cabel in Stationery_equipment:
                            #print([[x,y],cabel])
                            Electrical_appliances_locations.append([[x,y],cabel])


                        
                    Matrix[x][y].extend(Colour_dictionary[px[x,y]])
                    #print(Matrix[x][y])
            except IndexError:
                pass
            
    Electrical_appliances_locations_back_up = Electrical_appliances_locations.copy()
    #print(Electrical_appliances_locations)
    print('Initialising_Matrix() done')
Initialising_Matrix()


def Initialising_the_List_of_electricals():
    global Electrical_appliances_locations
    for x in range(0,Tile_range_x):
        for y in range(0,Tile_range_y):
            #print('hey1')
            if Matrix[x][y]:
                #print('hey2')
                for Appliance in Matrix[x][y]:
                    #print('hey3')
                    if Appliance in Stationery_equipment:
                        #print('hey4')
                        Electrical_appliances_locations.append([[x,y],Appliance])
                        #print('adding')

#Initialising_the_List_of_electricals()

def Circuit_search():
    global Cable_Worked_on
    global Started_scanning_from
    global Parallel_List
    global Junctions_to_work_on_set
    global Junctions_to_work_on_sub_list
    global Electrical_appliances_locations
    global Cross_hair
    working = True
    #print('hey',Electrical_appliances_locations)
    
    Starting_tile = random.sample(Electrical_appliances_locations,1)[0]
    Cross_hair = Starting_tile[0]
    #Starting_tile = Electrical_appliances_locations[0]
    Started_scanning_from = Starting_tile
    Electrical_appliances_locations.remove(Starting_tile)

    is_one = False

    Junction_search(Starting_tile)
    
    Looping_Function()

    if Electrical_appliances_locations:
        print('it fucked itself',Electrical_appliances_locations[0]) 
        Cross_hair = Electrical_appliances_locations[0][0]
        do_wins()
        
        Circuit_search()

def Looping_Function(Show = False):
    global Junctions_to_work_on_sub_list
    #print(Junctions_to_work_on_sub_list)
    Working_List = Junctions_to_work_on_sub_list.copy()
    Junctions_to_work_on_sub_list[:] = []
    if Show:
        print(Working_List)
        o_wins()
    for Junction in Working_List:

        #do_wins()
        Junction_search(Junction)
    if Junctions_to_work_on_sub_list:
        Looping_Function(Show)
    #else:
        #do_wins((56, 125))
    
#@profile        
def Cable_search(Starting_tile, is_first = False):
    global Cable_in_lien
    global End_Junctions
    global Junction_origin
    global Parallel_List
    global Junctions_to_work_on_set
    global Junctions_to_work_on_sub_list
    global is_to_Parallel
    global Cable_Worked_on_set
    global Cable_Worked_on
    global To_link
    global Electrical_appliances_locations
    
    tuple_Starting_tile = tuple([tuple(Starting_tile[0]),Starting_tile[1]])   
    #print('doing', Starting_tile)
    Starting_tile_coordinates = Starting_tile[0].copy()
    Adjacent_Tiles_That_are_connectable = []
    Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_tile_coordinates)].copy()
    Starting_tile_Data = Starting_tile[1]
    Adjacent_count = 0

    for Adjacent_tile in Adjacent_tiles:
        Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
        if Adjacent_tile_Data:
            for Adjacent_tile_sub_Data in Adjacent_tile_Data:
                if Starting_tile_Data in Connections.Connectable_dictionary:
                    Connections.pass_to_link = []
                    #print(Adjacent_tile_Data,'Adjacent_tile_Data',Starting_tile_Data,'Starting_tile_Data')
                    if Connections.Connectable_dictionary[Starting_tile_Data](Adjacent_tile_sub_Data):
                        if Connections.pass_to_link:
                            #print(Connections.pass_to_link,'Connections.pass_to_link')
                            
                            if Connections.pass_to_link[1]:
                                #print(Adjacent_tile_Data)
                                The_to_link_tile = [Adjacent_tile,Adjacent_tile_sub_Data] #####################################
                            else:
                                #print(Starting_tile_Data)
                                The_to_link_tile = Starting_tile #####################################
                                
                            add_yes = True
                            for to_t in To_link:
                                if The_to_link_tile == to_t[0]:
                                    add_yes = False
                            if add_yes:    
                                To_link.append([The_to_link_tile,Connections.pass_to_link[0]])
                                
                        Adjacent_count += 1
                        Adjacent_Tiles_That_are_connectable.append([Adjacent_tile,Adjacent_tile_sub_Data])
                        
                        
                        #print('Connections.pass_to_link in Cable_search',Connections.pass_to_link)

                    
    #print(Adjacent_count,'Adjacent_count')
    if Adjacent_count == 1:
        if Starting_tile_Data in Stationery_equipment:
            if Starting_tile in Electrical_appliances_locations:
                Electrical_appliances_locations.remove(Starting_tile)
                
        if is_first:
            if not Adjacent_Tiles_That_are_connectable in End_Junctions:
                End_Junctions.append(Adjacent_Tiles_That_are_connectable[0])
                #print('adding 0',Adjacent_Tiles_That_are_connectable[0])
                
        if not Starting_tile in End_Junctions:
            End_Junctions.append(Starting_tile)
            #print('adding 0.5',Starting_tile)
            if not tuple_Starting_tile in Junctions_to_work_on_set:
            #print('addding 4 ',Starting_tile)
                Junctions_to_work_on_sub_list.append(Starting_tile.copy())
                Junctions_to_work_on_set.add(tuple_Starting_tile)     
    
    if Adjacent_count == 2:
        Cable_in_line.append(Starting_tile)
        Cable_Worked_on.append(Starting_tile)
        Compound = tuple([tuple(Starting_tile[0]),Starting_tile[1]])

        Cable_Worked_on_set.add(Compound)
        
        for Adjacent_Tile_with_Connected in Adjacent_Tiles_That_are_connectable:
            Adjacent_Tile_with_Connected_Data = Adjacent_Tile_with_Connected[1]
            
            if Starting_tile_Data in Stationery_equipment:
                if Starting_tile in Electrical_appliances_locations:
                        Electrical_appliances_locations.remove(Starting_tile)
                
                if not tuple_Starting_tile in Junctions_to_work_on_set:
                   #print('addding 2 ',Starting_tile)
                   Junctions_to_work_on_set.add(tuple_Starting_tile)
                   Junctions_to_work_on_sub_list.append(Starting_tile.copy())
                
                if Adjacent_Tile_with_Connected_Data in Stationery_equipment:
                    if Adjacent_Tile_with_Connected in Electrical_appliances_locations:
                        Electrical_appliances_locations.remove(Adjacent_Tile_with_Connected)
                    if not Adjacent_Tile_with_Connected in End_Junctions:
                        End_Junctions.append(Adjacent_Tile_with_Connected)
                    Junction_origin = Starting_tile.copy()
                    
                else:
                    sub_Adjacents = Dictionary_of_adjacents[tuple(Adjacent_Tile_with_Connected[0])].copy()
                    Adjacent_sub_count = 0
                    for sub_Adjacent in sub_Adjacents:
                        Adjacent_sub_tile_Data = Matrix[sub_Adjacent[0]][sub_Adjacent[1]]
                        if Adjacent_sub_tile_Data:
                            for Adjacent_sub_tile_Data_sub in Adjacent_sub_tile_Data:
                                if Adjacent_sub_tile_Data_sub in Connections.Connectable_dictionary:
                                    if Connections.Connectable_dictionary[Adjacent_Tile_with_Connected_Data](Adjacent_sub_tile_Data_sub):
                                        Adjacent_sub_count += 1

                            
                    if Adjacent_sub_count > 2:
                        if not Adjacent_Tile_with_Connected in End_Junctions:
                            End_Junctions.append(Adjacent_Tile_with_Connected)
                            #print('adding 1',Starting_tile)
                        Junction_origin = Starting_tile.copy()

            Compound = tuple([tuple(Adjacent_Tile_with_Connected[0]),Adjacent_Tile_with_Connected[1]])
            
            if Compound not in Cable_Worked_on_set: 
                
                if Adjacent_Tile_with_Connected[1] not in Stationery_equipment:
                    Cable_search(Adjacent_Tile_with_Connected)
                           
                else:         
                    if Adjacent_Tile_with_Connected in Electrical_appliances_locations:
                        #print(Electrical_appliances_locations)
                        Electrical_appliances_locations.remove(Adjacent_Tile_with_Connected)
                
                    if not Compound in Junctions_to_work_on_set:

                        Junctions_to_work_on_set.add(Compound)
                        Junctions_to_work_on_sub_list.append(Adjacent_Tile_with_Connected.copy())
                    if not Adjacent_Tile_with_Connected in End_Junctions:
                        End_Junctions.append(Adjacent_Tile_with_Connected)
                        #print('adding 2',Adjacent_Tile_with_Connected)
            
            elif Compound in Junctions_to_work_on_set:
                #print('adding 3',Adjacent_Tile_with_Connected)
                End_Junctions.append(Adjacent_Tile_with_Connected) 
                               
                
    elif Adjacent_count > 2:
        
        if len(Cable_in_line) == 0:
            Cable_in_line.append(Starting_tile)
            Cable_Worked_on.append(Starting_tile)  ##############
            Compound = tuple([tuple(Starting_tile[0]),Starting_tile[1]])   
            Cable_Worked_on_set.add(Compound)
            
            for Adjacent_Tile_with_Connected in Adjacent_Tiles_That_are_connectable:
                Adjacent_Tile_with_Connected_Data = Adjacent_Tile_with_Connected[1]
                
                if Adjacent_Tile_with_Connected_Data in Stationery_equipment:
                    if Adjacent_Tile_with_Connected in Electrical_appliances_locations:
                        Electrical_appliances_locations.remove(Adjacent_Tile_with_Connected)
                        
                    if not Adjacent_Tile_with_Connected in End_Junctions:
                        End_Junctions.append(Adjacent_Tile_with_Connected.copy())
                        #print('adding 4',Adjacent_Tile_with_Connected)
                    Junction_origin = Starting_tile.copy()                        

                else:
                    ########################
                    sub_Adjacents = Dictionary_of_adjacents[tuple(Adjacent_Tile_with_Connected[0])].copy()
                    Adjacent_sub_count = 0
                    for sub_Adjacent in sub_Adjacents:
                        Adjacent_sub_tile_Data = Matrix[sub_Adjacent[0]][sub_Adjacent[1]]
                        if Adjacent_sub_tile_Data:
                            for Adjacent_sub_tile_Data_sub in Adjacent_sub_tile_Data:
                                if Adjacent_sub_tile_Data_sub in Connections.Connectable_dictionary:
                                    if Connections.Connectable_dictionary[Adjacent_Tile_with_Connected_Data](Adjacent_sub_tile_Data_sub):
                                        Adjacent_sub_count += 1
                                    #if Adjacent_sub_tile_Data[0] in Stationery_equipment:
                                        #if not sub_Adjacent in Junctions_to_work_on:
                                            #Junctions_to_work_on.append(sub_Adjacent.copy())
                   
                                   
                    if Adjacent_sub_count > 2:
                        if not Starting_tile in Parallel_List:  ##########
                            Parallel_List.append(Starting_tile)
                            is_to_Parallel =  True
                        
                        if not Adjacent_Tile_with_Connected in End_Junctions:
                            #print('adding 5',Adjacent_Tile_with_Connected.copy())
                            End_Junctions.append(Adjacent_Tile_with_Connected.copy())
                        Junction_origin = Starting_tile
                        
            

        else:
            if not Starting_tile in End_Junctions:
                End_Junctions.append(Starting_tile)
                #print('adding 6',Starting_tile)
                
            for Adjacent_Tile_with_Connected in Adjacent_Tiles_That_are_connectable:
                sub_Adjacents = Dictionary_of_adjacents[tuple(Adjacent_Tile_with_Connected[0])].copy()
                Adjacent_sub_count = 0
                Adjacent_Tile_with_Connected_Data = Adjacent_Tile_with_Connected[1]
                for sub_Adjacent in sub_Adjacents:
                    Adjacent_sub_tile_Data = Matrix[sub_Adjacent[0]][sub_Adjacent[1]]
                    if Adjacent_sub_tile_Data:
                        for Adjacent_sub_tile_Data_sub in Adjacent_sub_tile_Data:
                            if Adjacent_sub_tile_Data_sub in Connections.Connectable_dictionary:
                                if Connections.Connectable_dictionary[Adjacent_Tile_with_Connected_Data](Adjacent_sub_tile_Data_sub):
                                    Adjacent_sub_count += 1
                                    
                if Adjacent_sub_count > 2:
                    if not Starting_tile in Parallel_List:
                        Parallel_List.append(Starting_tile)  ##########

        if not tuple_Starting_tile in Junctions_to_work_on_set:
            #print('addding 4 ',Starting_tile)
            Junctions_to_work_on_sub_list.append(Starting_tile.copy())
            Junctions_to_work_on_set.add(tuple_Starting_tile)     
        
#@profile            
def Parallel_cables_Starting(Parallel_List):
    #print('Starting Parallel_List',Parallel_List)
    global Parallel_done
    global Parallel_cables_ends
    global Cable_Worked_on
    global Overlapping
    global Parallel_conere
    global Junctions_to_work_on_sub_list
    global Parallel_conere_Calling_from
    #print('Parallel_cables_Starting!!!',Parallel_List )
    #do_wins(tuple(Parallel_List[0]))
    Overlapping = False
    First = True

    #print(Parallel_List,'Parallel_List')
    do_next_parallel = Parallel_cables(Parallel_List,First)
    

                        
    if Overlapping:
        The_return = True
        if do_next_parallel:

            Parallel_cables(do_next_parallel)
            Formatting_for_Parallel_cables(Parallel_List)
    else:
        #print('hapings?')
        Parallel_done[:] = []
        Parallel_List[:] = []
        Parallel_cables_ends[:] = []
        Parallel_conere_Calling_from[:] = []
        
        The_return = False
        
    return(The_return)

#@profile
def Parallel_cables(Parallel_List,First = False):
    #print(Parallel_List)
    global Parallel_done
    global Parallel_cables_ends
    global Cable_Worked_on_set
    global Overlapping
    global Junctions_to_work_on_sub_list
    global To_link
    global Cross_hair
    do_next_parallel = []
    Parallel_Tiles_That_are_connectable = []
    
    
    for Parallel in Parallel_List:
        #Cross_hair = Parallel[0]
        #do_wins()
        Parallel_Adjacent_count = 0
        Parallel_Tiles_For_next_list = []
        #print(Parallel)
        Adjacent_Parallel_tiles = Dictionary_of_adjacents[tuple(Parallel[0])].copy()
        Parallel_tile_Data = Parallel[1]
        if Parallel_tile_Data in Stationery_equipment:
            if not Parallel in Parallel_cables_ends:
                Parallel_cables_ends.append(Parallel)
                
        for Adjacent_tile in Adjacent_Parallel_tiles:
            Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
            if Adjacent_tile_Data:
                for Adjacent_tile_Data_sub in Adjacent_tile_Data:
                    Compound = tuple([tuple(Adjacent_tile),Adjacent_tile_Data_sub])
                    
                    if Adjacent_tile_Data_sub in Connections.Connectable_dictionary:
                        Connections.pass_to_link = []
                        if Connections.Connectable_dictionary[Parallel_tile_Data](Adjacent_tile_Data_sub):
                            Compound_non_tuple = [Adjacent_tile,Adjacent_tile_Data_sub]
                                
                                #print('Connections.pass_to_link in Parallel_cables',Connections.pass_to_link)
                            if Connections.pass_to_link:
                                    #print(Connections.pass_to_link,'Connections.pass_to_link')
                                if Connections.pass_to_link[1]:
                                        #print(Adjacent_tile_Data)
                                    The_to_link_tile = [Adjacent_tile,Adjacent_tile_Data_sub] ###########################
                                else:
                                        #print(Parallel_tile_Data) 
                                    The_to_link_tile = Parallel ###########################

                                        
                                add_yes = True
                                for to_t in To_link:
                                    if The_to_link_tile == to_t[0]:
                                        add_yes = False
                                if add_yes:    
                                    To_link.append([The_to_link_tile,Connections.pass_to_link[0]])
                                        
                            Parallel_Adjacent_count += 1
                            if not tuple(Adjacent_tile) in Cable_Worked_on_set: 
                                if not Compound_non_tuple in Parallel_conere:
                                    if not Compound_non_tuple in Parallel_done: 
                                        if not Adjacent_tile_Data_sub in Stationery_equipment:
                                            if not Compound_non_tuple in Parallel_Tiles_For_next_list: 
                                                Parallel_Tiles_That_are_connectable.append(Compound_non_tuple)
                                                Parallel_Tiles_For_next_list.append(Compound_non_tuple)
                                                Overlapping = True
                                        else:
                                            if not Compound in Junctions_to_work_on_set:
                                                Parallel_cables_ends.append(Compound_non_tuple) 
                                else:
                                    
                                    Parallel_conere.remove(Compound_non_tuple) 
                                    Parallel_done.append(Compound_non_tuple) 
                                    Parallel_Tiles_For_next_list.append(Compound_non_tuple) 
                                    if not Adjacent_tile_Data_sub in Stationery_equipment:
                                        if not Compound_non_tuple in Parallel_Tiles_For_next_list: 
                                            Parallel_Tiles_That_are_connectable.append(Compound_non_tuple) ######### 
                                            Parallel_Tiles_For_next_list.append(Compound_non_tuple) #########
                                    else:
                                        Parallel_cables_ends.append(Compound_non_tuple) #########
                                    
        #print(Parallel_Adjacent_count)
        
        if Parallel_Adjacent_count > 2:
            Parallel_done.append(Parallel)
            #print('Success')
            #print('hey1',do_next_parallel)
            #do_next_parallel = do_next_parallel + Parallel_Tiles_For_next_list
            #print('hey2',do_next_parallel)
            for Tiles_For in Parallel_Tiles_For_next_list:
                if not Tiles_For in do_next_parallel:
                    if Quick_look(Tiles_For):
                        do_next_parallel.append(Tiles_For)
                    else:
                        if not Parallel in Parallel_conere_Calling_from:
                            #print('hey')
                            Parallel_conere_Calling_from.append(Parallel)
                        tuple_adding = tuple([tuple(Tiles_For[0]),Tiles_For[1]])
                        #if not tuple_adding in Cable_Worked_on_set:
                            #if not Tiles_For in Parallel_conere:
                        Parallel_conere.append(Tiles_For) 
                        #do_next_parallel.append(Tiles_For)
                
        
        else: 
            if not Parallel in Parallel_conere:
                Parallel_conere.append(Parallel)

        
  
    if First:      
        return(do_next_parallel)
    else:
        if do_next_parallel:
            #print(do_next_parallel)
            Parallel_cables(do_next_parallel)
#@profile
def Quick_look(Tiles_For):
    Parallel_Adjacent_count = 0
    Parallel_Tiles_For_next_list = []
    #print(Tiles_For)
    Adjacent_Parallel_tiles = Dictionary_of_adjacents[tuple(Tiles_For[0])].copy()
    Parallel_tile_Data = Tiles_For[1]
    for Adjacent_tile in Adjacent_Parallel_tiles:
        Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
        if Adjacent_tile_Data:
            for Adjacent_tile_Data_sub in Adjacent_tile_Data:
                if not tuple([tuple(Adjacent_tile),Adjacent_tile_Data_sub]) in Cable_Worked_on_set:
                    if Adjacent_tile_Data_sub in Connections.Connectable_dictionary:
                        if Connections.Connectable_dictionary[Parallel_tile_Data](Adjacent_tile_Data_sub):
                            Parallel_Adjacent_count += 1
                        
    if Parallel_Adjacent_count >2:
        The_return = True
    else:
        The_return = False
            
    return(The_return)
    
    
#@profile
def Formatting_for_Parallel_cables(Parallel_List):
    global Parallel_done
    global Parallel_cables_ends
    global Junctions_to_work_on_set
    global Cable_Worked_on
    global Overlapping
    global Junctions_to_work_on_sub_list
    global Parallel_conere_Calling_from
    global Cross_hair
    #print(Junctions_to_work_on,'Beginning In programme')
    Add_list = [] 
    for conere in Parallel_conere:
        if not conere in Parallel_cables_ends:
            
            Adjacent_Parallel_tiles = Dictionary_of_adjacents[tuple(conere[0])].copy()
            Parallel_tile_Data = conere[1]
            for Adjacent_tile in Adjacent_Parallel_tiles:
                Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
                if Adjacent_tile_Data:
                    for Adjacent_tile_Data_sub in Adjacent_tile_Data:
                        if not tuple([tuple(Adjacent_tile),Adjacent_tile_Data_sub]) in Cable_Worked_on_set:
                            if not [Adjacent_tile,Adjacent_tile_Data_sub] in Parallel_done:
                                
                                if Adjacent_tile_Data_sub in Connections.Connectable_dictionary:
                                    if Connections.Connectable_dictionary[Parallel_tile_Data](Adjacent_tile_Data_sub):
                                        Add_list.append(conere) ###############
    Add_list_2 = []
    for adding in Add_list:
        Adjacent_Parallel_tiles = Dictionary_of_adjacents[tuple(adding[0])].copy()
        Parallel_tile_Data = adding[1]
        for Adjacent_tile in Adjacent_Parallel_tiles:
            Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
            if Adjacent_tile_Data:
                for Adjacent_tile_Data_sub in Adjacent_tile_Data:
                    if not adding in Parallel_List:
                        if [Adjacent_tile,Adjacent_tile_Data_sub] in Parallel_conere_Calling_from:
                            Add_list_2.append([Adjacent_tile,Adjacent_tile_Data_sub])
                            if adding in Parallel_done:
                                Parallel_done.remove(adding)
                         
    Parallel_cables_ends_all = Add_list_2 + Parallel_List + Parallel_cables_ends
    #print(Parallel_List,'<-Parallel_List',Parallel_cables_ends_all,'<-Parallel_cables_ends_all')
        
    for Starting in Parallel_List:
        
        
        for ends in Parallel_cables_ends_all:
            if ends in Electrical_appliances_locations:
                Electrical_appliances_locations.remove(ends)
            if Starting == ends:
                pass
                #print('oh fuck')
            else:
                
                if To_link:
                    for To_l in To_link:
                        if Starting == To_l[0]:
                            for to_link_String in To_l[1]:
                                Dictionary1[to_link_String] = True
                        elif ends == To_l[0]:
                            for to_link_String in To_l[1]:
                                Dictionary2[to_link_String] = True
                           
                Dictionary1 = {'The other end':ends}
                Dictionary1['Cable locations'] = Parallel_done.copy()
                Dictionary1['Number of cables'] = len(Parallel_done)

                Compound = tuple([tuple(Starting[0]),Starting[1]])
                if Compound in links:
                    do_not = False
                    for links_pop in links[Compound]:
                        The_other_end = links_pop['The other end']
                        if The_other_end == ends:
                            do_not = True
                    if not do_not:
                        links[Compound].append(Dictionary1.copy())
                else:
                    links[Compound] = []
                    links[Compound].append(Dictionary1.copy())
                    
                Dictionary2 = {'The other end':Starting}
                Dictionary2['Cable locations'] = Parallel_done.copy()
                Dictionary2['Number of cables'] = len(Parallel_done)
                                 
                Compound = tuple([tuple(ends[0]),ends[1]])
                                 
                if Compound in links:
                    do_not = False
                    for links_pop in links[Compound]:
                        The_other_end = links_pop['The other end']
                        if The_other_end == Starting:
                            do_not = True
                    if not do_not:
                        links[Compound].append(Dictionary2.copy())
                else:
                    links[Compound] = []
                    links[Compound].append(Dictionary2.copy())  
    Junctions_to_work_on_sub_list = Junctions_to_work_on_sub_list + Parallel_cables_ends + Parallel_cables_ends_all
    for Parallel_cables_to_add_to_Junctions in (Parallel_cables_ends + Parallel_cables_ends_all):
        Compound = tuple([tuple(Parallel_cables_to_add_to_Junctions[0]),Parallel_cables_to_add_to_Junctions[1]])
        Junctions_to_work_on_set.add(Compound)
    #print(Junctions_to_work_on)

            
    for ends in Parallel_cables_ends:
        Compound = tuple([tuple(ends[0]),ends[1]])
        if not Compound in Junctions_to_work_on_set:
            Junctions_to_work_on_sub_list.append(ends)
            Junctions_to_work_on_set.add(Compound)
            
    for Cable in (Parallel_done):
        #Cross_hair = Cable[0]
        #do_wins()
        
        Compound = tuple([tuple(Cable[0]),Cable[1]])
        Cable_Worked_on_set.add(Compound)
    #do_wins(tuple(Parallel_cables_ends[0]))
    Parallel_done[:] = []
    Parallel_List[:] = []
    Parallel_conere[:] = []
    Parallel_cables_ends[:] = []
    Parallel_conere_Calling_from[:] = []
    To_link[:] = []
    #print(Junctions_to_work_on,'In programme')
    
#@profile        
def Junction_search(Starting_Junction):
    #do_wins(tuple(Starting_Junction))
    global Junctions_to_work_on_set
    global Junctions_to_work_on_sub_list
    global Parallel_List
    global is_to_Parallel
    global Cable_Worked_on
    global Cable_Worked_on_set
    global To_link
    #print('Working at',Starting_Junction)
    #do_wins(tuple(Starting_Junction))
    #print('pop2',Starting_Junction)
    
    Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_Junction[0])].copy()
    for Adjacent_tile in Adjacent_tiles:
        #print('pop3')
        Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
        #print(Adjacent_tile)
        #print(Adjacent_tile_Data)
        if Adjacent_tile_Data:
            for Adjacent_tile_sub in Adjacent_tile_Data:
                #print('pop4') ### 
                Starting_Junction_Data = Starting_Junction[1]
                Compound = [tuple(Adjacent_tile),Adjacent_tile_sub]
                #print(Compound)
                if not tuple(Compound) in Cable_Worked_on_set: 
                    #print('pop5') ###
                    
                    if not tuple(Compound) in Junctions_to_work_on_set: 
                        #print('pop6') ###Cable_search
                        
                        
                        if Adjacent_tile_sub in Connections.Connectable_dictionary:
                            Connections.pass_to_link = []
                            if Connections.Connectable_dictionary[Starting_Junction_Data](Adjacent_tile_sub):
                                #print('Connectionspass_to_link in Junction_search',Connections.pass_to_link)
                                #print('pop8')
                                if Connections.pass_to_link: ##########################################################

                                    
                                    if Connections.pass_to_link[1]:
                                        #print(Adjacent_tile_Data)
                                        The_to_link_tile = [Adjacent_tile,Adjacent_tile_sub] ##########################################################
                                    else:
                                        The_to_link_tile = Starting_Junction
                                        #print(Starting_Junction_Data)

                                    add_yes = True
                                    for to_t in To_link:
                                        if The_to_link_tile == to_t[0]:
                                            add_yes = False
                                    if add_yes:    
                                        To_link.append([The_to_link_tile,Connections.pass_to_link[0]])
                                    
                                Cable_search([Adjacent_tile,Adjacent_tile_sub],True)

                                if not is_to_Parallel:
                                    to_format()
                                else:
                                    #print('what???')
                                    Junction_origin[:] = []
                                    Cable_in_line[:] = []
                                    End_Junctions[:] = []
                                    On_junction = False
                                    
                                if Parallel_List:
                                    if Parallel_List[0]:
                                        Parallel_cables_Starting(Parallel_List)
                                        is_to_Parallel = False 


                        

#@profile            
def to_format():
    global Cable_in_line
    global End_Junctions
    global Junction_origin
    global To_link
    is_one = False
    is_two = False
    Dictionary1 = {}
    Dictionary2 = {}

    if Junction_origin:
        for Junction_1 in End_Junctions:
            if not Junction_1 == Junction_origin:      
                if To_link:
                    for To_l in To_link:
                        if Junction_origin == To_l[0]:
                            for to_link_String in To_l[1]:
                                Dictionary1[to_link_String] = True
                        elif Junction_1 == To_l[0]:
                            for to_link_String in To_l[1]:
                                Dictionary2[to_link_String] = True
                
                Dictionary1['Cable type'] = 'Error'
                Dictionary1['The other end'] = Junction_1

                Compound = tuple([tuple(Junction_origin[0]),Junction_origin[1]])
                if Compound in links:
                    links[Compound].append(Dictionary1.copy())
                else:
                    links[Compound] = []
                    links[Compound].append(Dictionary1.copy())
                
                Dictionary2['Cable type'] = 'Error'
                Dictionary2['The other end'] = Junction_origin.copy()

                Compound2 = tuple([tuple(Junction_1[0]),Junction_1[1]])
                if Compound2 in links:
                    links[Compound2].append(Dictionary2.copy())
                else:
                    links[Compound2] = []
                    links[Compound2].append(Dictionary2.copy())
        
    else:
        if len(End_Junctions) == 2:
            try:
                
                String_of_Cable_type = Cable_in_line[0][1]
                if not String_of_Cable_type:
                    String_of_Cable_type = 'Error'
            except IndexError:
                String_of_Cable_type = 'Error'

            if To_link:
                for To_l in To_link:
                    if End_Junctions[0] == To_l[0]:
                        for to_link_String in To_l[1]:
                            Dictionary1[to_link_String] = True
                    elif End_Junctions[1] == To_l[0]:
                        for to_link_String in To_l[1]:
                            Dictionary2[to_link_String] = True
            
            Dictionary1['The other end'] = End_Junctions[1]
            Dictionary1['Cable type'] = String_of_Cable_type
            Dictionary1['Cable locations'] = Cable_in_line.copy()
            Dictionary1['Number of cables'] = len(Cable_in_line)
            Compound = tuple([tuple(End_Junctions[0][0]),End_Junctions[0][1]])
            Add_or_not = True
            if Compound in links:
                for links_pop in links[Compound]:
                    if links_pop['The other end'] == End_Junctions[1]:
                        Add_or_not = False
                if Add_or_not:
                    links[Compound].append(Dictionary1.copy())  #on Cable Junctions
            else:
                links[Compound] = []
                links[Compound].append(Dictionary1.copy())

            Dictionary2['The other end'] = End_Junctions[0]
            Dictionary2['Cable type'] = String_of_Cable_type
            Dictionary2['Cable locations'] = Cable_in_line.copy()
            Dictionary2['Number of cables'] = len(Cable_in_line)

            Compound = tuple([tuple(End_Junctions[1][0]),End_Junctions[1][1]])
            if Compound in links:
                if Add_or_not:
                    
                    links[Compound].append(Dictionary2.copy()) #on Cable Junctions
            else:
                links[Compound] = []
                links[Compound].append(Dictionary2.copy())

        else:
            print(End_Junctions,'End_Junctions fuuuuuuuuuuuuuuuuuuuuuuuuuuu')
    To_link[:] = []   
    Junction_origin[:] = []
    Cable_in_line[:] = []
    End_Junctions[:] = []
    On_junction = False

def check_Dictionary_link(): 
    
    r = range(0,Tile_range_x)
    r2 = range(0,Tile_range_y)
    a = []
    for z in r:
        x = []
        for p in r2:
            if tuple((z,p)) in links:
                for links_pop in links[tuple((z,p))]:
                    #print(links_pop)
                    if len(links_pop['The other end']) == 0:
                        print('what noOOoOO!',tuple((z,p)), links_pop)

                    if tuple(links_pop['The other end']) == tuple((z,p)):
                        print('Its going to itself noOOoOO!',tuple((z,p)), links_pop)
                    
            
    print('chai_diatrly Done!')

def Link_check(Link):
    global Junction_check_set
    global Started_scanning_from
    Started_scanning_from = Link
    if (Link) in links:
        for to in links[(Link)]:
            if to['The other end']:
                Junction_check_set.add(tuple(to['The other end']))
            

def Direction_calculation():
    global Direction_jump_Next
    global Direction_jump_waiting
    global Power_dictionary
    global Power_supply_appliances_list
    is_frits = True

    for Power_supplys in Power_supply_appliances_list:
        #print(Power_supplys)
        if Power_supplys:
            random_Appliance = random.sample(Power_supplys,1)[0]
            Direction_jump_Next = [random_Appliance]
            Direction_Jumping(Direction_jump_Next)
            Other_Power_supply_appliances = Power_supplys.copy()
            Other_Power_supply_appliances.remove(random_Appliance)
            for Other_Power_supply in Other_Power_supply_appliances:
                tuple_Other_Power_supply = tuple([tuple(Other_Power_supply[0]),Other_Power_supply[1]])
                #print(tuple_Other_Power_supply)
                if tuple_Other_Power_supply in Power_dictionary:
                    pass
                else:
                    Direction_jump_Next = [Other_Power_supply]
                    Direction_Jumping(Direction_jump_Next)

        



def Direction_format(Coming_from,Going_to):
    global Power_dictionary
    tuple_Coming_from = tuple([tuple(Coming_from[0]),Coming_from[1]])
    tuple_Going_to = tuple([tuple(Going_to[0]),Going_to[1]])
        
    if tuple_Coming_from in Power_dictionary:
        if 'Downstream' in Power_dictionary[tuple_Coming_from]:
            Power_dictionary[tuple_Coming_from]['Downstream'].add(tuple_Going_to)
        else:
            Power_dictionary[tuple_Coming_from]['Downstream'] = {tuple_Going_to}
    else:
        Power_dictionary[tuple_Coming_from] = {'Downstream':{tuple_Going_to}}
        
    if tuple_Going_to in Power_dictionary:
        if 'Upstream' in Power_dictionary[tuple_Going_to]:
            Power_dictionary[tuple_Going_to]['Upstream'].add(tuple_Coming_from)
        else:
            Power_dictionary[tuple_Going_to]['Upstream'] = {tuple_Coming_from}
    else:
        Power_dictionary[tuple_Going_to] = {'Upstream':{tuple_Coming_from}}
        

def Direction_Jumping(Directions_jumping, do = False):
    global Direction_jump_Next_wate 
    #print(Directions_jumping,'Directions_jumping')
    for Jumping_from in Directions_jumping:
        #print('Directions_jumping',Directions_jumping)
        Tuple_jumping_from = tuple([tuple(Jumping_from[0]),Jumping_from[1]])
        #print(Jumping_from)
        if 3 > 1: #'################
            qick_store = [] 
            for links_pop in links[Tuple_jumping_from]:
                    The_other_end = links_pop['The other end']
                    Not_to_cancel = True
                    if Tuple_jumping_from in Power_dictionary:
                        Tuple_The_other_end = tuple([tuple(The_other_end[0]),The_other_end[1]])
                        if 'Upstream' in Power_dictionary[Tuple_jumping_from]:
                            
                            if Tuple_The_other_end in  Power_dictionary[Tuple_jumping_from]['Upstream']:

                                #print('help')
                                Not_to_cancel = False
                        if 'Downstream' in Power_dictionary[Tuple_jumping_from]:
                            if Tuple_The_other_end in  Power_dictionary[Tuple_jumping_from]['Downstream']:
                                Direction_format(Jumping_from,The_other_end)
                                Not_to_cancel = False
                    if Not_to_cancel:
                        qick_store.append(The_other_end)
                        
        if len(qick_store) > 1:
            #print('stiltt')
            if not do:
                #print('wat8ing')
                Direction_jump_Next_wate.append(Jumping_from)
            else:
                #print('doing')
                for qick_store_pop in qick_store:
                    Direction_format(Jumping_from,qick_store_pop)
                    Direction_jump_Next.append(qick_store_pop)
                
        else:
            #print('then fine')
            for qick_store_pop in qick_store:
                Direction_format(Jumping_from,qick_store_pop)
                Direction_jump_Next.append(qick_store_pop)
                            
                
    if Direction_jump_Next:
        #print('hey', Direction_jump_Next)
        Jumping_next = Direction_jump_Next.copy()
        Directions_jumping[:] = []
        Direction_jump_Next[:] = []
        Direction_Jumping(Jumping_next)
        
    elif Direction_jump_Next_wate:
        #print('POP')
        Jumping_next = Direction_jump_Next_wate.copy()
        Direction_jump_Next_wate[:] = []
        Directions_jumping[:] = []
        Direction_jump_Next[:] = []
        for next_one in Jumping_next:
            Direction_Jumping([next_one],True)
        
        
            
        
    

    
            
    
    


def Circuit_initialization():
    global The_current_working_line
    global To_overlay_next
    global To_overlay_next
    work_on_nexd = []
    work_on_nexd_set = set([])
    #print(Power_draw_appliances)
    for Appliance in Power_draw_appliances:
        if Appliance[1] in Power_sys_module.module_dictionary:
            Power_sys_module.module_dictionary[Appliance[1]](Appliance,Power_dictionary,Persistent_power_system_data) ####################################################

        if not Appliance in The_current_working_line:
            The_current_working_line.append(Appliance)
        if not Appliance in work_on_nexd:
            work_on_nexd.append(Appliance)
                        
    #print(work_on_nexd,'heeyeyyeyey')
    if len(work_on_nexd) > 0:
        #print('working on!')
        f = True
        Circuit_jup(work_on_nexd,f)
    else:
        print('hey noo?')
    
    First = True
    for Power_supplys in Power_supply_appliances_list:
        work_on_nexd = []
        if Power_supplys:
            if First:
                First = False
            else:
                
                for Power_supply in Power_supplys:
                    Power_supply_tuple = tuple([tuple(Power_supply[0]),Power_supply[1]])
                    if not 'Resistance from cabeis' in Power_dictionary[Power_supply_tuple]:
                        if Power_supply_tuple in Persistent_power_system_data:
                            if 'Cangenerate_resistance' in Persistent_power_system_data[Power_supply_tuple]:
                                if 'Current_capacity' in Persistent_power_system_data[Power_supply_tuple]:
                                    if Persistent_power_system_data[Power_supply_tuple]['Current_capacity'] < Persistent_power_system_data[Power_supply_tuple]['Capacity_max']:
                                        if Power_supply[1] in Power_sys_module.module_dictionary:
                                            Power_sys_module.module_dictionary[Appliance[1]](Appliance,Power_dictionary,Persistent_power_system_data)
                                            work_on_nexd.append(Appliance)
                        else:
                            if Power_supply[1] in Power_sys_module.module_dictionary:
                                Power_sys_module.module_dictionary[Appliance[1]](Appliance,Power_dictionary,Persistent_power_system_data)
                                work_on_nexd.append(Appliance)
                            
        if work_on_nexd:
            f = True
            Circuit_jup(work_on_nexd,f)
            
    
                                

                               
                        
                    
    
        

def Circuit_jup(work_on_nexd_form_top,First = False):
    global The_current_working_line
    global To_overlay_next
    global Electrical_changes
    global Mid_junctions
    global Cross_hair
    #print('Circuit_jup', work_on_nexd_form_top)
    work_on_nexd = []
    work_on_nexd_set = set([])
    for Working_on in work_on_nexd_form_top:
        Working_on_tuple = tuple([tuple(Working_on[0]),Working_on[1]])
        #print(Working_on,'<-Working_on',Matrix[Working_on[0]][Working_on[1]],'<-Matrix')
        Working_on_Data = Working_on[1]
        #print([Working_on_tuple]) 
        if not First:
            if Working_on_Data in Power_sys_module.module_dictionary:
                #print('loop2?')
                Electrical_changes[Working_on_tuple] = [Working_on_Data,Working_on]
        
        if not Working_on_tuple in Electrical_changes:
            #print('loop?')
            if 'Upstream' in Power_dictionary[Working_on_tuple]:
                if len(Power_dictionary[Working_on_tuple]['Upstream']) > 1:
                    #print('Multiple upstream',Working_on_tuple,Power_dictionary[Working_on_tuple]['Upstream'])
                    #Cross_hair = Working_on[0]
                    #do_wins()
                    Circuit_jump_landing_Split_cable(Working_on_tuple,Power_dictionary[Working_on_tuple]['Upstream'])
                    for tuple_The_other_end in Power_dictionary[Working_on_tuple]['Upstream']:
                        First_The_other_end = list(tuple_The_other_end)
                        The_other_end = [list(First_The_other_end[0]),First_The_other_end[1]]
                        if The_other_end in The_current_working_line:
                            if not The_other_end in To_overlay_next:
                                #print('overlay?')
                                To_overlay_next.append(The_other_end)
                        else:
                            if not The_other_end in work_on_nexd:
                                work_on_nexd.append(The_other_end)
                        
                else:   
                    for tuple_The_other_end in Power_dictionary[Working_on_tuple]['Upstream']:
                        First_The_other_end = list(tuple_The_other_end)
                        The_other_end = [list(First_The_other_end[0]),First_The_other_end[1]]
                        #print(Working_on,'From',The_other_end,'to')
                        Circuit_jump_landing(Working_on,The_other_end)
                        if The_other_end in The_current_working_line:
                            if not The_other_end in To_overlay_next:
                                #print('overlay?')
                                To_overlay_next.append(The_other_end)
                        else:
                            if not The_other_end in work_on_nexd:
                                work_on_nexd.append(The_other_end)
                    
                
                
    if len(work_on_nexd) > 0:
        First = False
        Circuit_jup(work_on_nexd)
    else:
        #do_wins((186, 147))
        work_on_nexd = To_overlay_next.copy()
        To_overlay_next[:] = []
        
        if len(work_on_nexd) > 0:
            The_current_working_line[:] = []
            First = False
            Circuit_jup(work_on_nexd,First)
        else:
            if len(Electrical_changes) > 0:
                for key, the_list in Electrical_changes.items():
                    #print('hey1',the_list[1])
                    #do_wins()
                    Power_sys_module.module_dictionary[the_list[0]](the_list[1],Power_dictionary,Persistent_power_system_data)
                    work_on_nexd.append(the_list[1])
                    
                if len(work_on_nexd) > 0:
                    The_current_working_line[:] = []
                    Electrical_changes = {}
                    #print('doing')
                    First = True
                    Circuit_jup(work_on_nexd,First)
                    


                    
def Circuit_jump_landing_Split_cable(Working_on,quck_do):
    
    #print('heyyyyyyyyyyy', Working_on)
    Working_on_tuple = tuple([tuple(Working_on[0]),Working_on[1]])                             
    #Current  = 1000/Power_dictionary[Working_on_tuple]['Resistance']
    
    #Current_split = Current/len(quck_do)

    #Resistance_split = 1000/Current_split
    Resistance_split  = Power_Functions.Working_out_resistance(Working_on,Power_dictionary)


    for The_other_end in quck_do:
        The_other_end_tuple = tuple([tuple(The_other_end[0]),The_other_end[1]])

        if The_other_end_tuple in Power_dictionary:
            #if not Working_on in Power_dictionary[tuple(The_other_end)]['Downstream']:
                #print(Working_on,'Working_on', The_other_end,'The_other_end')
            if not 'Use Resistance from modified' in Power_dictionary[Working_on_tuple]:
                if 'Parallel Resistance from cabeis' in Power_dictionary[The_other_end_tuple]:
                    if 'Parallel Resistance from cabeis' in Power_dictionary[Working_on_tuple]:
                        Power_dictionary[The_other_end_tuple]['Parallel Resistance from cabeis'].update(Power_dictionary[Working_on_tuple]['Parallel Resistance from cabeis'].copy())
                   
                    Power_dictionary[The_other_end_tuple]['Parallel Resistance from cabeis'][Working_on_tuple] = [Resistance_split,Working_on]
                else:
                    if 'Parallel Resistance from cabeis' in Power_dictionary[Working_on_tuple]:
                        Power_dictionary[The_other_end_tuple]['Parallel Resistance from cabeis'] = Power_dictionary[Working_on_tuple]['Parallel Resistance from cabeis'].copy()
                       
                        Power_dictionary[The_other_end_tuple]['Parallel Resistance from cabeis'][Working_on_tuple] = [Resistance_split,Working_on]
                    else:
                   
                        Power_dictionary[The_other_end_tuple]['Parallel Resistance from cabeis'] = {Working_on_tuple:[Resistance_split,Working_on]}
            #print('doing it here',Working_on)
            if 'Resistance from cabeis' in Power_dictionary[The_other_end_tuple]:
                Power_dictionary[The_other_end_tuple]['Resistance from cabeis'][Working_on_tuple] = [0,Working_on]
            else:
                Power_dictionary[The_other_end_tuple]['Resistance from cabeis'] = {Working_on_tuple:[0,Working_on]}

            #else:
                #print('mmmmmmmmmmooooooooooo')
        #print(Working_on,'Working_on', The_other_end,'The_other_end')
        if not The_other_end in  The_current_working_line: 
            The_current_working_line.append(The_other_end)
        #print(The_other_end)
        #print(Working_on_tuple)
        #if The_other_end_tuple in Power_dictionary[Working_on_tuple]['Downstream']:
            #print('WHTr?')
            
        Power_dictionary[The_other_end_tuple]['Resistance'] = Power_Functions.Working_out_resistance(The_other_end,Power_dictionary)


                
                
def Circuit_jump_landing(Working_on,The_other_end):
    Working_on_tuple = tuple([tuple(Working_on[0]),Working_on[1]])
    The_other_end_tuple = tuple([tuple(The_other_end[0]),The_other_end[1]])                            
    #print(Working_on,Power_dictionary[tuple(The_other_end)])
    if The_other_end_tuple in Power_dictionary:
        if 'Use Resistance from modified' in Power_dictionary[Working_on_tuple]:
            Resistance = Power_Functions.Working_out_resistance_Modified(Working_on,Power_dictionary)
        else:

            Resistance = Power_Functions.Working_out_resistance(Working_on,Power_dictionary)

            #print(Working_on,'Working_on', The_other_end,'The_other_end')
        if 'Resistance from cabeis' in Power_dictionary[The_other_end_tuple]:

            Power_dictionary[The_other_end_tuple]['Resistance from cabeis'][Working_on_tuple] = [Resistance,Working_on]
        else:
            #print(Working_on_tuple,Working_on)
            Power_dictionary[The_other_end_tuple]['Resistance from cabeis'] = {Working_on_tuple:[Resistance,Working_on]}

        if not 'Use Resistance from modified' in Power_dictionary[Working_on_tuple]:
            if 'Parallel Resistance from cabeis' in Power_dictionary[Working_on_tuple]:  
                if 'Parallel Resistance from cabeis' in Power_dictionary[The_other_end_tuple]:
                    Power_dictionary[The_other_end_tuple]['Parallel Resistance from cabeis'].update(Power_dictionary[Working_on_tuple]['Parallel Resistance from cabeis'])
                else:
                    Power_dictionary[The_other_end_tuple]['Parallel Resistance from cabeis'] = Power_dictionary[Working_on_tuple]['Parallel Resistance from cabeis']
        
    if not The_other_end in  The_current_working_line: 
        The_current_working_line.append(The_other_end)

    #print(Power_dictionary[Working_on_tuple])
    #if The_other_end_tuple in Power_dictionary[Working_on_tuple]['Downstream']:
        #print('Its linking to its self somewhere "oh shiiiiiiiit"')

    Power_dictionary[The_other_end_tuple]['Resistance'] = Power_Functions.Working_out_resistance(The_other_end,Power_dictionary)


def work_backwords():
    global Power_dictionary
    global Power_supply_appliances_list
    global Electrical_changes
    global Up_Flow_Flow_back
    global Up_Flow_Flow_Follow_back
    global Up_Flow_Voltage_Remind_chek
    Is_constant_supply = True
    for Power_supplys in Power_supply_appliances_list:
        if Power_supplys:
            if Is_constant_supply: ###########################
                    
                Working_on_list = []
                back_flow_Working_on_list = []
                for Power_supply in Power_supplys:
                    Power_supply_tuple = tuple([tuple(Power_supply[0]),Power_supply[1]])
                    if Power_supply_tuple in Power_dictionary:
                        if 'Downstream' in Power_dictionary[Power_supply_tuple]:
                             if 'Resistance from cabeis' in Power_dictionary[Power_supply_tuple]:
                                 if Is_constant_supply:
                                    Working_on_list.append(Power_supply)
                                 else:
                                     if not 'Receiving voltage' in Power_dictionary[Power_supply_tuple]:
                                         
                                             if Persistent_power_system_data[Power_supply_tuple]['Current_capacity'] > 0:
                                                 if not 'Format_for_sub_syston' in Power_dictionary[Power_supply_tuple]:
                                                     Working_on_list.append(Power_supply)

                
                Jumping_backwards(Working_on_list)
                if Up_Flow_Voltage_Remind_chek:
                    #print('hey soing Up_Flow_Voltage_Remind_chek')
                    Up_Flow_Voltage_Remind_chek_loop()
                    #Jumping_backwards(Up_Flow_Voltage_Remind_chek,False,True)
                Is_constant_supply = False
                #do_wins() 
            
                    
  


def Up_Flow_Voltage_Remind_chek_loop():
    global Up_Flow_Voltage_Remind_chek
    #print('poping?')
    Working_on_list = []
    for Remind_chek in Up_Flow_Voltage_Remind_chek:
        if not Remind_chek in Working_on_list:
            Working_on_list.append(Remind_chek)
    #Working_on_list = Up_Flow_Voltage_Remind_chek.copy()
    Up_Flow_Voltage_Remind_chek[:] = []
    Jumping_backwards(Working_on_list,False,True)
    if Up_Flow_Voltage_Remind_chek:
        #do_wins()
        Up_Flow_Voltage_Remind_chek_loop() 
                
        #
            

            

        
def Jumping_backwards(Working_on_list, First = False,jutes_do = False):
    global Electrical_changes
    global Pass_on_to_next
    global Up_Flow_Voltage_Remind_chek
    #print('Working_on_list',Working_on_list)
    Pass_on_to_next[:] = []
    Pass_on_to_next_Checked = []
    for Working_on in Working_on_list:
        For_format = []
        do_next = []
        Have_same_parallel_Resistance = []
        
        Working_on_tuple = tuple([tuple(Working_on[0]),Working_on[1]])
        Working_on_Data = Working_on[1]
        if not First:
            #print(Working_on_Data)
            if Working_on_Data in Power_sys_module.module_dictionary:
                Electrical_changes[Working_on_tuple] = [Working_on_Data,Working_on]
        if not Working_on_tuple in Electrical_changes:
            if 'Resistance from cabeis' in Power_dictionary[Working_on_tuple]:
                for the_othere_end_tuple, the_othere_end_all in Power_dictionary[Working_on_tuple]['Resistance from cabeis'].items():
                    if not do_next == Working_on:
                        #if not 'Receiving voltage' in Power_dictionary[the_othere_end_tuple]:
                        The_other_end = the_othere_end_all[1]
                        do_next.append(The_other_end)
                        
        if len(do_next) > 1:
            #print(do_next,'Splitting' )
            Have_same_parallel_Resistance = []
            if 'Parallel Resistance from cabeis' in Power_dictionary[Working_on_tuple]:
                for next_done in do_next:
                    next_done_tuple = tuple([tuple(next_done[0]),next_done[1]])
                    if 'Parallel Resistance from cabeis' in Power_dictionary[next_done_tuple]:
                        if Power_dictionary[Working_on_tuple]['Parallel Resistance from cabeis'] == Power_dictionary[next_done_tuple]['Parallel Resistance from cabeis']:
                            #print('hey 1',Power_dictionary[Working_on_tuple]['Parallel Resistance from cabeis'])
                            #print('''

                            #print('hey 2',Power_dictionary[next_done_tuple]['Parallel Resistance from cabeis'])
                            Have_same_parallel_Resistance.append(next_done)
                    else:
                        Pass_on_to_next.append(next_done)
                        For_format.append(next_done)
                        
            #print(Have_same_parallel_Resistance,'Have_same_parallel_Resistance')
            #print(do_next,'do_next')
            if Have_same_parallel_Resistance:
                if len(Have_same_parallel_Resistance) > 1:
                    #print('hjeyyyyyyyyyyyyy',Have_same_parallel_Resistance)
                    Top_resistance = [0,'Error']
                    for same_parallel_Resistance in Have_same_parallel_Resistance:
                        Resistance = Power_Functions.Working_out_all_resistance(same_parallel_Resistance,Power_dictionary)
                        if Resistance > Top_resistance[0]:
                            Top_resistance = [Resistance,same_parallel_Resistance]
                    if Top_resistance[0]:
                        #print('Chosen one ', Top_resistance[1])
                        Pass_on_to_next.append(Top_resistance[1])
                        For_format.append(Top_resistance[1])
                    else:
                        print('help!!!')
                else:
                   Pass_on_to_next.extend(do_next)
                   For_format.extend(do_next)
            else:
                #print('aaddng do_next ',do_next)
                Pass_on_to_next.extend(do_next)
                For_format.extend(do_next)

                #print('Whats added', Pass_on_to_next)

                    
                    

        else: 
            if do_next:
                if do_next[0]:
                    Pass_on_to_next.append(do_next[0])
                    For_format.append(do_next[0])
                    
        for links_pop in links[Working_on_tuple]:
            links_pop_other_end = links_pop['The other end']
            links_pop_other_end_tuple = tuple([tuple(links_pop_other_end[0]),links_pop_other_end[1]])
            if 'Resistance from cabeis' in Power_dictionary[Working_on_tuple]:
                #print(links_pop_other_end_tuple, Power_dictionary[Working_on_tuple]['Resistance from cabeis'])
                if not links_pop_other_end_tuple in Power_dictionary[Working_on_tuple]['Resistance from cabeis']:
                    if 'Upstream' in Power_dictionary[Working_on_tuple]:
                        if not links_pop_other_end_tuple in Power_dictionary[Working_on_tuple]['Upstream']:
                            if not links_pop_other_end_tuple in Power_dictionary[Working_on_tuple]['current coming from']:
                                if not 'Format_for_sub_syston' in Power_dictionary[links_pop_other_end_tuple]:
                                    #print(Pass_on_to_next,'1')
                                    #print('wow', links_pop_other_end_tuple,'Update_downstream_travel_to_end')
                                    Information_pass_down_format_any_direct(Working_on,[links_pop_other_end])
                                    Update_downstream_travel_to_end(links_pop_other_end)
                                    #print(Pass_on_to_next,'2')
 
 
        
        if 'Upstream' in Power_dictionary[Working_on_tuple]:
            for other_end_tuple in Power_dictionary[Working_on_tuple]['Upstream']:
                First_The_other_end = list(other_end_tuple)
                The_other_end = [list(First_The_other_end[0]),First_The_other_end[1]]
                if 'current coming from' in Power_dictionary[Working_on_tuple]: 
                    if not other_end_tuple in Power_dictionary[Working_on_tuple]['current coming from']:
                        if not 'current coming from Support' in Power_dictionary[other_end_tuple]:
                            #pass
                            #print('Update_Upstream_travel_to_end',other_end_tuple )

                            if not jutes_do:
                                #print('daddasd')
                                Up_Flow_Voltage_Remind_chek.append(Working_on)
                            else:
                                Information_pass_down_format_any_direct(Working_on,[The_other_end])
                                Update_Upstream_travel_to_end(The_other_end)
                                #if Up_Flow_Flow_Follow_back:
                                    #Pass_on_to_next.extend(Up_Flow_Flow_Follow_back.copy())
                                #Up_Flow_Flow_Follow_back[:] = []

                    

            
        if Pass_on_to_next:
            
            for Pass_on_Check in Pass_on_to_next:
                if not Pass_on_Check in Pass_on_to_next_Checked:
                    Pass_on_to_next_Checked.append(Pass_on_Check)
        
        
        if For_format:
            #print('For_format', For_format, '1')
            Landing_backward(Working_on,For_format)
    #print(Pass_on_to_next_Checked,'nic')
    if Pass_on_to_next_Checked:
        
        on_to_next = Pass_on_to_next_Checked.copy()
        Jumping_backwards(on_to_next)
    else:
        if Electrical_changes:
            work_on_nexd = []
            is_Working_backwards = True
            is_setting_as_top = True
            for key, the_list in Electrical_changes.items():
                Power_sys_module.module_dictionary[the_list[0]](the_list[1],Power_dictionary,Persistent_power_system_data,is_Working_backwards,is_setting_as_top)
                work_on_nexd.append(the_list[1])
            Electrical_changes = {}
            First = True
            Jumping_backwards(work_on_nexd,First)

def Update_Upstream_travel_to_end(up_voltage, First = False):
    global Working_on_backward_list
    global Electrical_changes_up_flow
    global Working_backwards_done
    global Cross_hair
    global Up_Flow_Flow_back
    global Support_current_detected
    tuple_Working_up_voltage = tuple([tuple(up_voltage[0]),up_voltage[1]])
    Working_up_voltage_Data = up_voltage[1]
    #print('Update_Upstream_travel_to_end',up_voltage)
    if not First:
        if Working_up_voltage_Data in Power_sys_module.module_dictionary:
            Electrical_changes_up_flow[tuple_Working_up_voltage] = [Working_up_voltage_Data,up_voltage]
    do_noese = []            
    if not tuple_Working_up_voltage in Electrical_changes_up_flow:
        for links_pop in links[tuple_Working_up_voltage]:
            links_pop_other_end = links_pop['The other end']
            links_pop_other_end_tuple = tuple([tuple(links_pop_other_end[0]),links_pop_other_end[1]])
            if not links_pop_other_end in Working_backwards_done:
                if not links_pop_other_end_tuple in Power_dictionary[tuple_Working_up_voltage]['Resistance from cabeis']:
                    if not 'Receiving voltage' in Power_dictionary[links_pop_other_end_tuple]:
                        if not 'not_go_to' in links_pop:
                            if 'Downstream' in Power_dictionary[tuple_Working_up_voltage]:
                                if not links_pop_other_end_tuple in Power_dictionary[tuple_Working_up_voltage]['Downstream']:
                                    do_noese.append(links_pop_other_end)
                            else:
                                do_noese.append(links_pop_other_end)
    #print('pop0.4') 
    if do_noese:
        Information_pass_down_format_any_direct(up_voltage,do_noese)
        for doing_now in do_noese:
            Update_Upstream_travel_to_end(doing_now)
    else:
        #print('pop1')                  
        if Electrical_changes_up_flow:
            work_on_nexd = []
            is_Working_backwards = True
            for key, the_list in Electrical_changes_up_flow.items():
                
                Power_sys_module.module_dictionary[the_list[0]](the_list[1],Power_dictionary,Persistent_power_system_data,is_Working_backwards)
                work_on_nexd.append(the_list[1])
                if 'Supply current Support' in Power_dictionary[key]:
                    Support_current_detected.append(the_list[1])
            Electrical_changes_up_flow = {}
            is_Working_backwards = True
            for key, the_list in Electrical_changes_up_flow.items():
                print('hey')
                #do_wins()
                Power_sys_module.module_dictionary[the_list[0]](the_list[1],Power_dictionary,Persistent_power_system_data,is_Working_backwards)
                work_on_nexd.append(the_list[1])
                if 'Supply current Support' in Power_dictionary[key]:
                    Support_current_detected.append(the_list[1])
            Electrical_changes_up_flow = {}
            
            for do_nexd in work_on_nexd:
                First = True
                Update_Upstream_travel_to_end(do_nexd,First)
        else:
            #print('pop2') 
            if 'Downstream' in Power_dictionary[tuple_Working_up_voltage]:
                if 'Supply current Support' in Power_dictionary[tuple_Working_up_voltage]:
                    #print('Update_Upstream_Get_back_to_mainline(tuple_Working_up_voltage)', tuple_Working_up_voltage)
                    #Landing_backward_Support(up_voltage,[The_other_end])
                    Update_Upstream_Get_back_to_mainline(tuple_Working_up_voltage)
                    #Source_update(up_voltage)################################################################################
                else:
                    if Support_current_detected:
                        Support_Jump = Support_current_detected.copy()
                        Support_current_detected[:] = []
                        for current_detected in Support_Jump:
                            tuple_current_detected = tuple([tuple(current_detected[0]),current_detected[1]])
                            if 'Supply current Support' in Power_dictionary[tuple_current_detected]: 
                                Update_Upstream_Get_back_to_mainline(tuple_current_detected)

def Update_Upstream_Get_back_to_mainline(tuple_Working_on):
    #print('Update_downstream_Get_back_to_mainline',tuple_Working_on)
    if 'Downstream' in Power_dictionary[tuple_Working_on]:
        for other_end_tuple in Power_dictionary[tuple_Working_on]['Downstream']:
            if 'current coming from' in Power_dictionary[other_end_tuple]:
                First_The_other_end = list(other_end_tuple)
                The_other_end = [list(First_The_other_end[0]),First_The_other_end[1]]
                
                First_tuple_Working_on = list(tuple_Working_on)
                Working_on = [list(First_tuple_Working_on[0]),First_tuple_Working_on[1]]

                Formatting_support_landing(Working_on,[The_other_end])
                Travel_to_source_and_update(The_other_end)
                
            else:
                First_The_other_end = list(other_end_tuple)
                The_other_end = [list(First_The_other_end[0]),First_The_other_end[1]]
                
                First_tuple_Working_on = list(tuple_Working_on)
                Working_on = [list(First_tuple_Working_on[0]),First_tuple_Working_on[1]]
                
                Formatting_support_landing(Working_on,[The_other_end])
                Update_Upstream_Get_back_to_mainline(other_end_tuple)

def Update_downstream_travel_to_end(up_voltage, First = False):
    global Working_on_backward_list
    global Electrical_changes_up_flow
    global Working_backwards_done
    global Cross_hair
    global Up_Flow_Flow_back
    global Support_current_detected
    tuple_Working_up_voltage = tuple([tuple(up_voltage[0]),up_voltage[1]])
    Working_up_voltage_Data = up_voltage[1]
    #print('Update_downstream_travel_to_end',up_voltage )


    if not First:
        if Working_up_voltage_Data in Power_sys_module.module_dictionary:
            Electrical_changes_up_flow[tuple_Working_up_voltage] = [Working_up_voltage_Data,up_voltage]
    do_noese = []            
    if not tuple_Working_up_voltage in Electrical_changes_up_flow:
        for links_pop in links[tuple_Working_up_voltage]:
            links_pop_other_end = links_pop['The other end']
            links_pop_other_end_tuple = tuple([tuple(links_pop_other_end[0]),links_pop_other_end[1]])
            if not links_pop_other_end in Working_backwards_done:
                if not 'Format_for_sub_syston' in Power_dictionary[links_pop_other_end_tuple]: 
                    if not 'not_go_to' in links_pop:
                        if 'Upstream' in Power_dictionary[tuple_Working_up_voltage]:
                            #print(links_pop_other_end_tuple, Power_dictionary[tuple_Working_up_voltage]['Upstream'])
                            #print(Power_dictionary[tuple_Working_up_voltage]['Downstream'])
                            #do_wins()
                            if not links_pop_other_end_tuple in Power_dictionary[tuple_Working_up_voltage]['Upstream']:
                                do_noese.append(links_pop_other_end)



    if do_noese:
        Information_pass_down_format_any_direct(up_voltage,do_noese)
        for doing_now in do_noese:
            Update_downstream_travel_to_end(doing_now)
    else:
                            
        if Electrical_changes_up_flow:
            work_on_nexd = []
            is_Working_backwards = True
            for key, the_list in Electrical_changes_up_flow.items():
                print('hey')
                #do_wins()
                Power_sys_module.module_dictionary[the_list[0]](the_list[1],Power_dictionary,Persistent_power_system_data,is_Working_backwards)
                work_on_nexd.append(the_list[1])
                if 'Supply current Support' in Power_dictionary[key]:
                    Support_current_detected.append(the_list[1])

            Electrical_changes_up_flow = {}
            
            
            for do_nexd in work_on_nexd:
                First = True
                Update_downstream_travel_to_end(do_nexd,First)
        else:
            if 'Supply current Support' in Power_dictionary[tuple_Working_up_voltage]:
                #print('Update_downstream_Get_back_to_mainline', tuple_Working_up_voltage)
                Update_downstream_Get_back_to_mainline(tuple_Working_up_voltage)
            else:
                if Support_current_detected:
                    Support_Jump = Support_current_detected.copy()
                    Support_current_detected[:] = []
                    for current_detected in Support_Jump:
                        tuple_current_detected = tuple([tuple(current_detected[0]),current_detected[1]])
                        if 'Supply current Support' in Power_dictionary[tuple_current_detected]: 
                            Update_downstream_Get_back_to_mainline(tuple_current_detected)


def Information_pass_down_format_any_direct(Power_sply_appliance,The_other_ends):
    Power_sply_appliance_tuple = tuple([tuple(Power_sply_appliance[0]),Power_sply_appliance[1]])
    #Cross_hair = The_other_ends[0]
    for The_other in The_other_ends:
        The_other_tuple = tuple([tuple(The_other[0]),The_other[1]])
        if 'sub syston TOP' in Power_dictionary[Power_sply_appliance_tuple]:
            Power_dictionary[The_other_tuple]['sub syston TOP'] = Power_dictionary[Power_sply_appliance_tuple]['sub syston TOP']
            Power_dictionary[The_other_tuple]['Format_for_sub_syston'] = True 
        else: 
            print('help!! Information_pass_down_format_any_direct',Power_sply_appliance )
            

def Update_downstream_Get_back_to_mainline(tuple_Working_on):
    #print('Update_downstream_Get_back_to_mainline',tuple_Working_on)
    if 'Upstream' in Power_dictionary[tuple_Working_on]:
        for other_end_tuple in Power_dictionary[tuple_Working_on]['Upstream']:
            if 'current coming from' in Power_dictionary[other_end_tuple]:
                First_The_other_end = list(other_end_tuple)
                The_other_end = [list(First_The_other_end[0]),First_The_other_end[1]]
                
                First_tuple_Working_on = list(tuple_Working_on)
                Working_on = [list(First_tuple_Working_on[0]),First_tuple_Working_on[1]]

                Formatting_support_landing(Working_on,[The_other_end])
                Travel_to_source_and_update(The_other_end)
                
            else:
                First_The_other_end = list(other_end_tuple)
                The_other_end = [list(First_The_other_end[0]),First_The_other_end[1]]
                
                First_tuple_Working_on = list(tuple_Working_on)
                Working_on = [list(First_tuple_Working_on[0]),First_tuple_Working_on[1]]
                
                Formatting_support_landing(Working_on,[The_other_end])
                Update_downstream_Get_back_to_mainline(other_end_tuple)

                
def Formatting_support_landing(Jumping_from,Landing_ons):
    global Cross_hair
    Jumping_from_tuple = tuple([tuple(Jumping_from[0]),Jumping_from[1]])
    Simply_Times_by = 0
                                                                        
    for the_othere_end in Landing_ons:
        if the_othere_end:
            the_othere_end_tuple = tuple([tuple(the_othere_end[0]),the_othere_end[1]])
            
            Destination = Power_dictionary[Jumping_from_tuple]['sub syston TOP'][0]
            Supply_current = Power_Functions.Working_out_Support_current(Jumping_from,Destination,Power_dictionary)      
            if 'current coming from Support' in Power_dictionary[the_othere_end_tuple]:
                Power_dictionary[the_othere_end_tuple]['current coming from Support'][Jumping_from_tuple] = [Supply_current,Destination]
                Power_dictionary[the_othere_end_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(the_othere_end,Destination,Power_dictionary)
                    
            else:
                Power_dictionary[the_othere_end_tuple]['current coming from Support'] = {Jumping_from_tuple:[Supply_current,Destination]}
                Power_dictionary[the_othere_end_tuple]['Supply current Support'] = Power_dictionary[the_othere_end_tuple]['current coming from Support'][Jumping_from_tuple][0]

def Travel_to_source_and_update(Working_on):
    global Pass_on_to_next
    #print('Travel_to_source_and_update',Working_on)
    Working_on_tuple = tuple([tuple(Working_on[0]),Working_on[1]])
    if 'current coming from' in Power_dictionary[Working_on_tuple]:
        if len(Power_dictionary[Working_on_tuple]['current coming from']) > 1:
            print('Maybe problem?')
        #print(Power_dictionary[Working_on_tuple]['current coming from'])
        for The_other_end_tuple, The_other_end_and_current in Power_dictionary[Working_on_tuple]['current coming from'].items():
            if The_other_end_and_current[1]:
                Formatting_support_landing(Working_on,[The_other_end_and_current[1]])
                if Power_dictionary[The_other_end_tuple]['sub syston TOP'][0] ==  The_other_end_and_current[1]:
                    Supply_back_into_line(The_other_end_and_current[1])
                    Pass_on_to_next.append(The_other_end_and_current[1])
                else:
                    Travel_to_source_and_update(The_other_end_and_current[1])
    else:
        print('help!,Travel_to_source_and_update ',Working_on)

def Supply_back_into_line(Working_on):
    global Power_dictionary
    #print('Supply_back_into_line', Working_on)
    Working_on_tuple = tuple([tuple(Working_on[0]),Working_on[1]])
    Current = Power_Functions.Working_out_Support_current(Working_on,Working_on,Power_dictionary)

    if 'current coming from' in Power_dictionary[Working_on_tuple]:
        print('hey??')
        #print(Working_on_tuple)
        Power_dictionary[Working_on_tuple]['current coming from']['From support'] = [Current,0]
        #print(Power_dictionary[Working_on_tuple]['current coming from']['From support'])
        Power_dictionary[Working_on_tuple]['Supply current'] = Power_Functions.Working_out_current(Working_on,Power_dictionary)
        #print(Power_dictionary[Working_on_tuple]['current coming from'])            
    else:
        print('help!! Back_into_line ',Working_on )


def Landing_backward(Jumping_from,Landing_ons):
    global Cross_hair
    Jumping_from_tuple = tuple([tuple(Jumping_from[0]),Jumping_from[1]])
    Simply_Times_by = 0
    #print('Landing_backward, Formatting from', Jumping_from, 'to',Landing_ons  )
    if len(Landing_ons) > 1:
        Resistanceto = Power_Functions.Working_out_all_resistance(Jumping_from,Power_dictionary)
        #do_wins()
        #print(Jumping_from_tuple,Power_dictionary[Jumping_from_tuple] )
        
        voltage = Power_dictionary[Jumping_from_tuple]['Supplying voltage']

        Needed_current = voltage/Resistanceto

        Simply_Times_by = Power_dictionary[Jumping_from_tuple]['Supply current']/Needed_current                 

                                                                        
    for the_othere_end in Landing_ons:
        if the_othere_end:
            the_othere_end_tuple = tuple([tuple(the_othere_end[0]),the_othere_end[1]])
            if Simply_Times_by:
                if 'Use Resistance from modified' in Power_dictionary[the_othere_end_tuple]:
                    Simply_Times_by_resistance = Power_Functions.Working_out_resistance_Modified(the_othere_end,Power_dictionary)
                else:
                    Simply_Times_by_resistance = Power_Functions.Working_out_all_resistance(the_othere_end,Power_dictionary)
                #print(Simply_Times_by_resistance)   
                Supply_current = Simply_Times_by * (Power_dictionary[Jumping_from_tuple]['Supplying voltage']/ Simply_Times_by_resistance)
            else:
                
                Supply_current = Power_dictionary[Jumping_from_tuple]['Supply current']
            if 'sub syston TOP' in Power_dictionary[Jumping_from_tuple]:
                Power_dictionary[the_othere_end_tuple]['sub syston TOP'] = Power_dictionary[Jumping_from_tuple]['sub syston TOP']
                        
            if 'current coming from' in Power_dictionary[the_othere_end_tuple]:
                Power_dictionary[the_othere_end_tuple]['current coming from'][Jumping_from_tuple] = [Supply_current,Jumping_from]
                Power_dictionary[the_othere_end_tuple]['Supply current'] = Power_Functions.Working_out_current(the_othere_end,Power_dictionary)
                    
            else:
                Power_dictionary[the_othere_end_tuple]['current coming from'] = {Jumping_from_tuple:[Supply_current,Jumping_from]}
                Power_dictionary[the_othere_end_tuple]['Supply current'] = Power_dictionary[the_othere_end_tuple]['current coming from'][Jumping_from_tuple][0]
                
            Supply_current = Power_Functions.Working_out_current(the_othere_end,Power_dictionary)
            
            if 'Use Resistance from modified' in Power_dictionary[the_othere_end_tuple]:
                Resistance = Power_Functions.Working_out_resistance_Modified(the_othere_end,Power_dictionary)
                
            else:
                Resistance = Power_Functions.Working_out_all_resistance(the_othere_end,Power_dictionary)
                
            
            Power_dictionary[the_othere_end_tuple]['Receiving voltage'] = Supply_current * Resistance
            Power_dictionary[the_othere_end_tuple]['Supplying voltage'] = Power_dictionary[the_othere_end_tuple]['Receiving voltage']

def Update_electrical():
    global Removing_List 
    global Adding_List 
    global links
    global Power_dictionary
    global Junctions_to_work_on_sub_list
    global Started_scanning_from
    global Working_on_backward_list
    global To_overlay_next
    global Cable_Worked_on
    global Junctions_to_work_on_set
    Tiles_remove = []
    Tiles_add = []
    
    
    for Removing in Removing_List:
        try:
            Matrix[Removing[0][0]][Removing[0][1]].remove(Removing[1])
            Tiles_remove.append(Removing)
        except ValueError:
            print('hey this is not here!!!',Removing)
        
    
        
   # if (74, 113) in links:
       ## print('2links',links[(74, 113)],'links')
        
    Tiles_remove_update(Tiles_remove)
    Removeing_Electrical(Tiles_remove)

    
    #if (74, 113) in links:
       # print('3links',links[(74, 113)],'links')
    for Adding in Adding_List:
        Matrix[Adding[0][0]][Adding[0][1]].append(Adding[1])
        Tiles_add.append(Adding)
        
    #if (74, 113) in links:
        #print('1links',links[(74, 113)])
        
    Tiles_add_update(Tiles_add)
    Adding_Electrical(Tiles_add)


    
    Removing_List[:] = []
    Adding_List[:] = []
    #Cable_Worked_on[:] = []
    #Power_dictionary = {}
    #do_wins((50,50))   
    #Circuit_initialization()
    #work_backwords()
    #do_wins((50,50))

def Tiles_add_update(Tiles):
    for Tile in Tiles:
        Tile_Data = Tile[1] 
        if Tile_Data in Power_supply_appliances_pryonty:
            Power_supply_appliances_list[Power_supply_appliances_pryonty[Tile_Data]].append(Tile)

def Tiles_remove_update(Tiles):
    for Tile in Tiles:
        Tile_Data = Tile[1] 
        if Tile_Data in Power_supply_appliances_pryonty:
            Power_supply_appliances_list[Power_supply_appliances_pryonty[Tile_Data]].remove(Tile)
            
        elif Tile in Power_draw_appliances:
            Power_draw_appliances.remove(Tile)





                                   
def Cable_search_for_up_date(Starting_tile,Has_passed_a_link = False,is_first = False):

    global Junction_origin
    global Parallel_List
    global Junctions_to_work_on_set
    global Junctions_to_work_on_sub_list
    global is_to_Parallel
    global Cable_Worked_on_set
    global Cable_Worked_on
    global To_link
    global up_date_come_from
    global Links_to_work_from
    global Links_to_delete
    tuple_Starting_tile = tuple([tuple(Starting_tile[0]),Starting_tile[1]])
    if tuple_Starting_tile in Cable_Worked_on_set:
        Cable_Worked_on_set.remove(tuple_Starting_tile)
    up_date_come_from.append(Starting_tile)
    
    #print('what?', Starting_tile)
    Starting_tile_coordinates = Starting_tile[0].copy()
    Adjacent_Tiles_That_are_connectable = []
    Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_tile_coordinates)].copy()
    Starting_tile_Data = Starting_tile[1]
    Adjacent_count = 0
    

    for Adjacent_tile in Adjacent_tiles:
        Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
        if Adjacent_tile_Data:
            for Adjacent_tile_sub_Data in Adjacent_tile_Data:
                if not [Adjacent_tile,Adjacent_tile_sub_Data] in up_date_come_from:
                    
                    if Starting_tile_Data in Connections.Connectable_dictionary:
                        Connections.pass_to_link = []
                        if Connections.Connectable_dictionary[Starting_tile_Data](Adjacent_tile_sub_Data):
                            Adjacent_count += 1
                            Adjacent_Tiles_That_are_connectable.append([Adjacent_tile,Adjacent_tile_sub_Data])
                            
    for Adjacent_Tiles_That_are in Adjacent_Tiles_That_are_connectable:
        #if not Adjacent_Tiles_That_are in up_date_come_from: 
        
        tuple_Adjacent_Tiles_That_are = tuple([tuple(Adjacent_Tiles_That_are[0]),Adjacent_Tiles_That_are[1]])
        if tuple_Adjacent_Tiles_That_are in links:
            if Has_passed_a_link:
                Links_to_work_from.append(Adjacent_Tiles_That_are)
            else:
                if tuple_Adjacent_Tiles_That_are in Cable_Worked_on_set:
                    Cable_Worked_on_set.remove(tuple_Adjacent_Tiles_That_are)
                #print('Deleting this',Adjacent_Tiles_That_are)
                Links_to_delete.append(Adjacent_Tiles_That_are)
                if tuple_Adjacent_Tiles_That_are in Junctions_to_work_on_set:
                    Junctions_to_work_on_set.remove(tuple_Adjacent_Tiles_That_are)
                    
                if not is_first:
                    Has_passed_a_link_for_sub = True
                else:
                    Has_passed_a_link_for_sub = False
                    
                Cable_search_for_up_date(Adjacent_Tiles_That_are,Has_passed_a_link_for_sub)
        else:
            up_date_come_from.append(Adjacent_Tiles_That_are)
            if tuple_Adjacent_Tiles_That_are in Cable_Worked_on_set:
                Cable_Worked_on_set.remove(tuple_Adjacent_Tiles_That_are)
            Cable_search_for_up_date(Adjacent_Tiles_That_are,Has_passed_a_link)

                        
def Removeing_Electrical(Tiles_for_Removeing):
    global Power_dictionary
    global up_date_come_from
    global Junctions_to_work_on_sub_list
    global Links_to_delete
    global Cable_Worked_on_set
    Already_done = []
    #print('Removeing_Electrical', Starting_tile)
    Tiles_for_Removeing_That_are_adjacent = []
    
    for Tile in Tiles_for_Removeing:
        
        tuple_Starting_tile = tuple([tuple(Tile[0]),Tile[1]])
        if tuple_Starting_tile in Cable_Worked_on_set:
            Cable_Worked_on_set.remove(tuple_Starting_tile)
        if tuple_Starting_tile in links:
            Links_to_delete.append(Tile)
            Junctions_to_work_on_set.remove(tuple_Starting_tile)
        Starting_tile_Data = Tile[1]
        
        Starting_tile_coordinates = Tile[0].copy()
        Adjacent_Tiles_That_are_connectable = []
        Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_tile_coordinates)].copy()
        for Adjacent_tile in Adjacent_tiles:
            Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
            if Adjacent_tile_Data:
                for Adjacent_tile_sub_Data in Adjacent_tile_Data:
                    if Starting_tile_Data in Connections.Connectable_dictionary:
                        Connections.pass_to_link = []
                        if Connections.Connectable_dictionary[Starting_tile_Data](Adjacent_tile_sub_Data):
                            Tiles_for_Removeing_That_are_adjacent.append(Tile)
            
                
    
    for Tile in Tiles_for_Removeing_That_are_adjacent:
        #print('Tile',Tile)
    #for Tile in Tiles_for_Removeing:
        if not Tile in Already_done:
            
            Cable_search_for_up_date(Tile,False,True)
            #print('Cable_search_for_Adding_Electrical(Tile)',Tile)
            #print(Links_to_delete,'Links_to_delete')
            #print(Links_to_work_from, 'Links_to_work_from')
            #do_wins()
            
            for Links_to_work in Links_to_work_from:
                for Links_to in Links_to_delete:
                    tuple_Links_to = tuple([tuple(Links_to[0]),Links_to[1]])
                    tuple_Links_to_work = tuple([tuple(Links_to_work[0]),Links_to_work[1]])
                    
                    for links_pop in links[tuple_Links_to_work]:
                        The_other_end = links_pop['The other end']
                        if The_other_end in Links_to_delete:
                            links[tuple_Links_to_work].remove(links_pop)
                
                        

            for Links_to in Links_to_delete:
                tuple_Links_to = tuple([tuple(Links_to[0]),Links_to[1]])
                if tuple_Links_to in links:
                        #print('Deleting', tuple_Links_to)
                    del links[tuple_Links_to]


                
            #print('YO')
           # do_wins()
            for Links_to_work in Links_to_work_from:
                #print(Links_to_work)
                #do_wins()
                Junction_search(Links_to_work)
                #print(Junctions_to_work_on_sub_list,'Junctions_to_work_on_sub_list')
                
            if Junctions_to_work_on_sub_list:
                #print(Junctions_to_work_on_sub_list,'Junctions_to_work_on_sub_list')
                Looping_Function()
            #do_wins()
            #Already_done.extend(up_date_come_from)
            Junctions_to_work_on_sub_list[:] = []
            Links_to_delete[:] = []    
            Links_to_work_from[:] = [] 
            up_date_come_from[:] = []
    #do_wins()
    Already_done[:] = []



def Adding_Electrical(Tiles_for_adding):
    global Power_dictionary
    global up_date_come_from
    global Junctions_to_work_on_sub_list
    global Links_to_delete
    Already_done = []
    Tiles_for_adding_That_are_adjacent = []
    #print(Tiles_for_adding)
    #if 2 < 0:
    for Tile in Tiles_for_adding:
        tuple_Starting_tile = tuple([tuple(Tile[0]),Tile[1]])

        Starting_tile_Data = Tile[1]
        #print('what?', Starting_tile)
        Starting_tile_coordinates = Tile[0].copy()
        Adjacent_Tiles_That_are_connectable = []
        Adjacent_tiles = Dictionary_of_adjacents[tuple(Starting_tile_coordinates)].copy()
        for Adjacent_tile in Adjacent_tiles:
            #print('1')
            Adjacent_tile_Data = Matrix[Adjacent_tile[0]][Adjacent_tile[1]]
            if Adjacent_tile_Data:
                #print('2')
                for Adjacent_tile_sub_Data in Adjacent_tile_Data:
                    #print('3')
                    if Starting_tile_Data in Connections.Connectable_dictionary:
                        #print('4')
                        Connections.pass_to_link = []
                        if Connections.Connectable_dictionary[Starting_tile_Data](Adjacent_tile_sub_Data):
                            #print('5')
                            tuple_Adjacent_tile = tuple([tuple(Adjacent_tile),Adjacent_tile_sub_Data])
                            if tuple_Adjacent_tile in Cable_Worked_on_set:
                                #print('6')
                                Tiles_for_adding_That_are_adjacent.append(Tile)
                            elif tuple_Adjacent_tile in links:
                                Tiles_for_adding_That_are_adjacent.append(Tile)
                                
    for Tile in Tiles_for_adding_That_are_adjacent:
    #for Tile in Tiles_for_adding:
        #print('Tile',Tile)
        if not Tile in Already_done:
            Cable_search_for_up_date(Tile)
            #print('Cable_search_for_Adding_Electrical(Tile)',Tile)
            #print(Links_to_delete,'Links_to_delete')
            #print(Links_to_work_from, 'Links_to_work_from')
            #do_wins()
            
            for Links_to_work in Links_to_work_from:
                for Links_to in Links_to_delete:
                    tuple_Links_to = tuple([tuple(Links_to[0]),Links_to[1]])
                    tuple_Links_to_work = tuple([tuple(Links_to_work[0]),Links_to_work[1]])
                    
                    for links_pop in links[tuple_Links_to_work]:
                        The_other_end = links_pop['The other end']
                        if The_other_end in Links_to_delete:
                            links[tuple_Links_to_work].remove(links_pop)
   
                        

            for Links_to in Links_to_delete:
                tuple_Links_to = tuple([tuple(Links_to[0]),Links_to[1]])
                if tuple_Links_to in links:
                    del links[tuple_Links_to]

            #print('YO')
            #do_wins()
            for Links_to_work in Links_to_work_from:
                #print('hey!!!',Links_to_work)
                #do_wins()
                Junction_search(Links_to_work)

            #print(Junctions_to_work_on_sub_list,'Junctions_to_work_on_sub_list')    
            if Junctions_to_work_on_sub_list:
                Looping_Function()
                
            Already_done.extend(up_date_come_from)
                
            #do_wins()
            
            Junctions_to_work_on_sub_list[:] = []
            Links_to_delete[:] = []    
            Links_to_work_from[:] = [] 
            up_date_come_from[:] = []
    #do_wins()
    Already_done[:] = []

def Tick_Update():
    global Removing_List 
    global Adding_List 
    global links
    global Power_dictionary
    global Junctions_to_work_on_sub_list
    global Started_scanning_from
    global Working_on_backward_list
    global To_overlay_next
    global Cable_Worked_on
    global Junctions_to_work_on_set
    global Matrix_update_stor
    
    #adding is if Connection is made to Machine or junction
    #Taking away
    count = 0
    RT = True
    while count < 50:
        #print(count)
        start_Main_time = time.time()
        Show_After_update = False
        Power_dictionary = {}
        if count < 1:

            Adding_List.extend([
                
                    #[[167, 262],'Low_voltage_cable'],
                    #[[167, 263],'Low_voltage_cable'],
                    #[[167, 264],'Low_voltage_cable'],
                    #[[167, 265],'Low_voltage_cable'],
                    #[[167, 266],'Low_voltage_cable'],
                    #[[167, 267],'Low_voltage_cable'],
                    #[[167, 268],'Low_voltage_cable'],
                    #[[167, 269],'Low_voltage_cable'],
                    #[[167, 270],'Low_voltage_cable'],
                    #[[167, 271],'Low_voltage_cable'],
                    #[[167, 272],'Low_voltage_cable'],
                    #[[167, 273],'Low_voltage_cable'],
                    #[[167, 274],'Low_voltage_cable'],
                    #[[167, 275],'Low_voltage_cable'],
                    #[[167, 276],'Low_voltage_cable'],
                    #[[167, 277],'Low_voltage_cable'],
                    #[[167, 278],'Low_voltage_cable'],
                    #[[167, 279],'Low_voltage_cable'],
                    #[[167, 280],'Low_voltage_cable'],
                    #[[167, 281],'Low_voltage_cable'],
                    #[[167, 282],'Low_voltage_cable'],
                    #[[167, 283],'Low_voltage_cable'],
                    #[[167, 284],'Low_voltage_cable'],
                    #[[168, 276],'Low_voltage_cable'],
                    #[[168, 275],'Low_voltage_cable'],
                    #[[168, 274],'Low_voltage_cable'],
                    #[[168, 273],'Low_voltage_cable'],
                    #[[169, 273],'Low_voltage_cable'],
                    #[[169, 274],'Low_voltage_cable'],
                    #[[169, 275],'Low_voltage_cable'],
                    #[[169, 276],'Low_voltage_cable'],
                    #[[170, 276],'Low_voltage_cable'],
                    #[[170, 275],'Low_voltage_cable'],
                    #[[170, 274],'Low_voltage_cable'],
                    #[[170, 273],'Low_voltage_cable'],
            
                    #[[70,114],'Medium_voltage_cable'],

                    #[[71,114],'Medium_voltage_cable'],
                    #[[72,114],'Medium_voltage_cable'],
                    [[70,115],'Medium_voltage_cable'],
                    [[71,115],'Medium_voltage_cable'],
                    [[72,115],'Medium_voltage_cable'],
                    [[70,116],'Medium_voltage_cable'],
                    [[71,116],'Medium_voltage_cable'],
                    [[72,116],'Medium_voltage_cable'],
                    [[70,117],'Medium_voltage_cable'],
                    [[71,117],'Medium_voltage_cable'],
                    [[72,117],'Medium_voltage_cable'],
            ])
            
                               
            Removing_List.append([[76, 113],'Medium_voltage_cable'])
            
            start_Update_electrical_time = time.time()
            Update_electrical()
            Show_After_update = True
            print("--- %s Update_electrical seconds Total time ---" % (time.time() - start_Update_electrical_time))
            print('c2')
            do_wins()


            #print(links[(70, 113)])
        elif count > 25:
            if RT:
                RT = False
                #do_wins((50,50))
                
                Adding_List.append([[76, 113],'Medium_voltage_cable'])
                
                Removing_List.extend([
                    
                    #[[167, 262],'Low_voltage_cable'],
                    #[[167, 263],'Low_voltage_cable'],
                    #[[167, 264],'Low_voltage_cable'],
                    #[[167, 265],'Low_voltage_cable'],
                    #[[167, 266],'Low_voltage_cable'],
                    #[[167, 267],'Low_voltage_cable'],
                    #[[167, 268],'Low_voltage_cable'],
                    #[[167, 269],'Low_voltage_cable'],
                    #[[167, 270],'Low_voltage_cable'],
                    #[[167, 271],'Low_voltage_cable'],
                    #[[167, 272],'Low_voltage_cable'],
                    #[[167, 273],'Low_voltage_cable'],
                    #[[167, 274],'Low_voltage_cable'],
                    #[[167, 275],'Low_voltage_cable'],
                    #[[167, 276],'Low_voltage_cable'],
                    #[[167, 277],'Low_voltage_cable'],
                    #[[167, 278],'Low_voltage_cable'],
                    #[[167, 279],'Low_voltage_cable'],
                    #[[167, 280],'Low_voltage_cable'],
                    #[[167, 281],'Low_voltage_cable'],
                    #[[167, 282],'Low_voltage_cable'],
                    #[[167, 283],'Low_voltage_cable'],
                    #[[167, 284],'Low_voltage_cable'],
                    #[[168, 276],'Low_voltage_cable'],
                    #[[168, 275],'Low_voltage_cable'],
                    #[[168, 274],'Low_voltage_cable'],
                    #[[168, 273],'Low_voltage_cable'],
                    #[[169, 273],'Low_voltage_cable'],
                    #[[169, 274],'Low_voltage_cable'],
                    #[[169, 275],'Low_voltage_cable'],
                    #[[169, 276],'Low_voltage_cable'],
                    #[[170, 276],'Low_voltage_cable'],
                    #[[170, 275],'Low_voltage_cable'],
                    #[[170, 274],'Low_voltage_cable'],
                    #[[170, 273],'Low_voltage_cable'],
            
                    #[[70,114],'Medium_voltage_cable'],

                    #[[71,114],'Medium_voltage_cable'],
                    #[[72,114],'Medium_voltage_cable'],
                    [[70,115],'Medium_voltage_cable'],
                    [[71,115],'Medium_voltage_cable'],
                    [[72,115],'Medium_voltage_cable'],
                    [[70,116],'Medium_voltage_cable'],
                    [[71,116],'Medium_voltage_cable'],
                    [[72,116],'Medium_voltage_cable'],
                    [[70,117],'Medium_voltage_cable'],
                    [[71,117],'Medium_voltage_cable'],
                    [[72,117],'Medium_voltage_cable'],
                    ])
                                
                start_Update_electrical_time = time.time()
                Update_electrical()
                Show_After_update = True
                print("--- %s Update_electrical seconds Total time ---" % (time.time() - start_Update_electrical_time))
                do_wins()        
        Direction_calculation()
        if Show_After_update:
            do_wins()
        Circuit_initialization()
        work_backwords()
        if Show_After_update:
            do_wins()
        count += 1
        print("--- %s seconds Total time ---" % (time.time() - start_Main_time))
        

    Cross_hair = [50,50]
    do_wins()


def links_print():
    for key, value  in links.items():
        sr = ''
        for dich in value:
            add = ''
            if 'The other end' in dich:
                add = add +  ' The other end ' + str(dich['The other end'])
                
            if 'Cable type' in dich:
                add = add +  ' Cable type ' + str(dich['Cable type'])
                
            if 'Number of cables' in dich:
                add = add +  ' Number of cables ' + str(dich['Number of cables'])

            if 'not_go_to' in dich:
                add = add +  ' not_go_to ' + str(dich['not_go_to'])
                
            if 'go_to' in dich:
                add = add +  ' go_to ' + str(dich['go_to'])
            sr =  sr + ('' + add)
        print(key,sr)
        
def Power_dictionary_ptint():
    r = range(0,Tile_range_x)
    r2 = range(0,Tile_range_y)
    a = []
    for z in r:
        #print (z,'in 1')
        x = []
        for p in r2:
            if Matrix[z][p]:
                for tye in Matrix[z][p]:
                    if tuple([(z,p),tye]) in Power_dictionary:
                        
                        sr = ''
                            
                        add = ''
                        #if 'Upstream' in dich:
                            #add = add +  ' Upstream ' + str(dich['Upstream']+ '],')
                                        
                        #if 'Downstream' in dich:
                            #add = add +  ' Downstream ' + str(dich['Downstream']+ '],')
                                        
                        if 'Type' in Power_dictionary[tuple([(z,p),tye])]:
                            add = add +  ' [Type : ' + str(Power_dictionary[tuple([(z,p),tye])]['Type']) + '],'
                        #print(add)
                        if 'Resistance' in Power_dictionary[tuple([(z,p),tye])]:
                            add = add +  ' [Resistance : ' + str(Power_dictionary[tuple([(z,p),tye])]['Resistance']) + '],'

                        if 'Receiving voltage' in Power_dictionary[tuple([(z,p),tye])]:
                            add = add +  ' [Receiving voltage : ' + str(Power_dictionary[tuple([(z,p),tye])]['Receiving voltage']) + '],'
                                        
                        if 'Supplying voltage' in Power_dictionary[tuple([(z,p),tye])]:
                            add = add +  ' [Supplying voltage : ' + str(Power_dictionary[tuple([(z,p),tye])]['Supplying voltage']) + '],'
                                    
                        if 'Supply current' in Power_dictionary[tuple([(z,p),tye])]:
                            add = add +  ' [Supply current : ' + str(Power_dictionary[tuple([(z,p),tye])]['Supply current']) + '],'
                            
                        if 'Up_Flow_voltage' in Power_dictionary[tuple([(z,p),tye])]:
                            add = add +  ' [Up_Flow_voltage : ' + str(Power_dictionary[tuple([(z,p),tye])]['Up_Flow_voltage']) + '],'

                        sr =  sr + ('' + add)
                        print(tuple([(z,p),tye]),sr)
                        #print(tuple((z,p)), Power_dictionary[tuple([(z,p),tye])])


#start_Main_time = time.time()
#do_wins((50,50))
start_time = time.time()   
Circuit_search()
print("--- %s seconds Circuit_search time ---" % (time.time() - start_time))
 
do_wins()
start_time = time.time()
Direction_calculation()
print("--- %s seconds Direction_calculation time  ---" % (time.time() - start_time))
do_wins()
start_time = time.time()
Circuit_initialization()
print("--- %s seconds Circuit_initialization time  ---" % (time.time() - start_time))
do_wins()
start_time = time.time()
work_backwords()
print("--- %s seconds work_backwords time ---" % (time.time() - start_time))

  
#links_print()
#Power_dictionary_ptint()
            
print(im.format, im.size, im.mode)
Cross_hair = [73, 57]
do_wins()



