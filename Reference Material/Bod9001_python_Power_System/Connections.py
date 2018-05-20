pass_to_link = []

def Link_cable_Low_voltage(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'Low_voltage_cable'}
    Stationery_connectables = {'APC'}
    if Adjacent_tile_Data in connectable:
        return True
    
    elif Adjacent_tile_Data == 'Department_batteries':
        pass_to_link = [['go_to'],True]
        return True
    
    elif Adjacent_tile_Data in Stationery_connectables:
        return True
    else:
        return False
    

def Link_cable_Medium_voltage(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'Medium_voltage_cable'}
    Stationery_connectables = {'Transformer'}
    if Adjacent_tile_Data in connectable:
        return True
    
    elif Adjacent_tile_Data == 'Department_batteries':
        pass_to_link = [['not_go_to'],True]
        return True
    
    elif Adjacent_tile_Data == 'Engineering_batteries':
        pass_to_link = [['go_to'],True]
        return True
    
    elif Adjacent_tile_Data in Stationery_connectables:
        return True
    else:
        return False
    
    
def Link_cable_High_voltage(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'High_voltage_cable'}
    #Stationery_connectables = {'Transformer'}
    
    if Adjacent_tile_Data in connectable:
        return True
    
    elif Adjacent_tile_Data == 'Radiation_collector':
        pass_to_link = [['go_to'],True]
        return True
    
    elif Adjacent_tile_Data == 'Transformer':
        #pass_to_link = [['not_go_to'],True]
        return True
    
    #elif Adjacent_tile_Data in Stationery_connectables:
        #return True
    else:
        return False
    

def Low_voltage_cable(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'Link_cable_Low_voltage', 'Low_voltage_cable'}
    if Adjacent_tile_Data in connectable:
        return True
    else:
        return False

    
def Medium_voltage_cable(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'Medium_voltage_cable', 'Link_cable_Medium_voltage'}
    if Adjacent_tile_Data in connectable:
        return True
    else:
        return False

    
def High_voltage_cable(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'High_voltage_cable', 'Link_cable_High_voltage'}
    #Stationery_connectables = {'Department_batteries','APC'}
    if Adjacent_tile_Data in connectable:
        return True
    else:
        return False
    

def Transformer(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'Link_cable_Medium_voltage'}
    #Stationery_connectables = ['Transformer']
    if Adjacent_tile_Data == 'Link_cable_High_voltage':
        #pass_to_link = [['not_go_to'],False]
        return True
                   
    elif Adjacent_tile_Data in connectable:
        return True
    #elif Adjacent_tile_Data in Stationery_connectables:
        #return True
    else:
        return False

    
def Radiation_collector(Adjacent_tile_Data):
    global pass_to_link
    #print('hey ^^')
    connectable = {'Link_cable_High_voltage'}
    #Stationery_connectables = ['Radiation_collector']
    #if Adjacent_tile_Data in connectable:
        #return True
    if Adjacent_tile_Data == 'Link_cable_High_voltage':
        pass_to_link = [['go_to'],False]
        return True
    #elif Adjacent_tile_Data in Stationery_connectables:
        #return True
    else:
        return False

    
def Engineering_batteries(Adjacent_tile_Data):
    global pass_to_link
    #connectable = {'Link_cable_Medium_voltage'}
    #Stationery_connectables = ['Engineering_batteries']
    #if Adjacent_tile_Data in connectable:
        #return True

    if Adjacent_tile_Data == 'Link_cable_Medium_voltage':
        pass_to_link = [['go_to'],False]
        return True
    #elif Adjacent_tile_Data in Stationery_connectables:
        #return True
    else:
        return False

    
def Department_batteries(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'Link_cable_Low_voltage', 'Link_cable_Medium_voltage'}
    #Stationery_connectables = ['Department_batteries']
    if Adjacent_tile_Data == 'Link_cable_Low_voltage':
        pass_to_link = [['go_to'],False]
        return True
    elif Adjacent_tile_Data == 'Link_cable_Medium_voltage':
        pass_to_link = [['not_go_to'],False]
        return True
    #elif Adjacent_tile_Data in Stationery_connectables:
        #return True
    else:
        return False

    
def APC(Adjacent_tile_Data):
    global pass_to_link
    connectable = {'Link_cable_Low_voltage'}
    #Stationery_connectables = ['APC']
    if Adjacent_tile_Data in connectable:
        return True
    #elif Adjacent_tile_Data in Stationery_connectables:
        #return True
    else:
        return False


    
Connectable_dictionary = {}  
Connectable_dictionary['Link_cable_Low_voltage'] = Link_cable_Low_voltage
Connectable_dictionary['Link_cable_Medium_voltage'] = Link_cable_Medium_voltage
Connectable_dictionary['Link_cable_High_voltage'] = Link_cable_High_voltage
Connectable_dictionary['Low_voltage_cable'] = Low_voltage_cable
Connectable_dictionary['Medium_voltage_cable'] = Medium_voltage_cable
Connectable_dictionary['High_voltage_cable'] = High_voltage_cable

Connectable_dictionary['Transformer'] = Transformer
Connectable_dictionary['Radiation_collector'] = Radiation_collector
Connectable_dictionary['Engineering_batteries'] = Engineering_batteries
Connectable_dictionary['Department_batteries'] = Department_batteries
Connectable_dictionary['APC'] = APC
