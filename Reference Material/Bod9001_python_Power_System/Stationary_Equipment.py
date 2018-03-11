def Department_batteries(Department_batterie,Power_dictionary):
    
    #print('Department_batteries', Department_batterie)
    if len(Power_dictionary[tuple(Department_batterie)]['Resistance coming from']) > 1:
        for Resistance in Power_dictionary[tuple(Transformer)]['Resistance from cabeis'].values():
            ResistanceX_all += 1/Resistance[0]
        Resistance = 1/ResistanceX_all
    else:
        for rs in Power_dictionary[tuple(Department_batterie)]['Resistance from cabeis'].values():
            Resistance = rs[0]
    R2 = Resistance
    I2 = 240/Resistance
    V2 = 240

     
    V1 = 3000
    I1 = (V2/V1)*I2
    R1 = 3000/I1
    
    Turn_ratio = 0.08 #* by

    Power_dictionary[tuple(Department_batterie)]['Resistance'] = R1
    Power_dictionary[tuple(Department_batterie)]['Type'] = 'Department_batterie'
    if 'Supply current' in Power_dictionary[tuple(Department_batterie)]:
        if len(Power_dictionary[tuple(Department_batterie)]['current coming from']) > 1:
            for current_in in Power_dictionary[tuple(Department_batterie)]['current coming from'].values():
                current_supplying_in_Receiving_voltage += current_in[0]
            
        
        else:
            for Current in Power_dictionary[tuple(Department_batterie)]['current coming from'].values():
                current_supplying_in_Receiving_voltage = Current[0]
            
        
        V1 = Power_dictionary[tuple(Department_batterie)]['Receiving voltage']
        I1 = current_supplying_in_Receiving_voltage
        R1 = V1/current_supplying_in_Receiving_voltage
        
        R2 = Resistance
        V2 = V1 * Turn_ratio
        I2 = V2/Resistance
        
        Power_dictionary[tuple(Department_batterie)]['Supplying voltage'] = V2
        Power_dictionary[tuple(Department_batterie)]['Supply current']  = I2  

def Engineering_batteries(Engineering_batteries,Power_dictionary):
    #print('Engineering_batteries')
    Power_dictionary[tuple(Engineering_batteries)]['Type'] = 'Engineering_Battery'
    #Resistance = Power_dictionary[tuple(Engineering_batteries)]['Resistance']
    #The_required_current = 240/Resistance
    #Expected_voltage = 3000
    #Resistance_at_3000 = Expected_voltage/The_required_current
    #Power_dictionary[tuple(Engineering_batteries)]['Resistance'] = Resistance_at_3000
    #if 'Upstream' in Power_dictionary[tuple(Engineering_batteries)]:
        #Power_dictionary[tuple(Engineering_batteries)]['Supplying voltage'] = 240
        #print('Need to add')
def Transformer(Transformer,Power_dictionary):
    #print('Transformer')
    ResistanceX_all = 0
    if len(Power_dictionary[tuple(Transformer)]['Resistance coming from']) > 1:
        for Resistance in Power_dictionary[tuple(Transformer)]['Resistance from cabeis'].values():
            ResistanceX_all += 1/Resistance[0]
        Resistance = 1/ResistanceX_all
    else:
        for rs in Power_dictionary[tuple(Transformer)]['Resistance from cabeis'].values():
            Resistance = rs[0]
            
    R2 = Resistance
    I2 = 3000/Resistance
    V2 = 3000

     
    V1 = 270000
    I1 = (V2/V1)*I2
    R1 = 270000/I1
    
    Turn_ratio = 3/275 #* by

    Power_dictionary[tuple(Transformer)]['Resistance'] = R1
    Power_dictionary[tuple(Transformer)]['Type'] = 'Transformer'
    if 'Supply current' in Power_dictionary[tuple(Transformer)]:
        current_supplying_at_Receiving_voltage = 0
        if len(Power_dictionary[tuple(Transformer)]['current coming from']) > 1:
            for current_in in Power_dictionary[tuple(Transformer)]['current coming from'].values():
                current_supplying_at_Receiving_voltage += current_in[0]
            
        
        else:
            for Current in Power_dictionary[tuple(Transformer)]['current coming from'].values():
                current_supplying_at_Receiving_voltage = Current[0]
        
        V1 = Power_dictionary[tuple(Transformer)]['Receiving voltage']
        I1 = current_supplying_at_Receiving_voltage
        R1 = V1/current_supplying_at_Receiving_voltage
        
        R2 = Resistance
        V2 = V1 * Turn_ratio
        I2 = V2/Resistance
        
        Power_dictionary[tuple(Transformer)]['Supplying voltage'] = V2
        Power_dictionary[tuple(Transformer)]['Supply current']  = I2
        
            

def Radiation_collector(Radiation_collector,Power_dictionary):
    #print('Radiation_collector')
    Power_dictionary[tuple(Radiation_collector)]['Supply current'] = 0.01
    Power_dictionary[tuple(Radiation_collector)]['Supplying voltage'] = 275000
    Power_dictionary[tuple(Radiation_collector)]['Receiving voltage'] = 275000

    
module_dictionary = {}  
module_dictionary['Department_batteries'] = Department_batteries
module_dictionary['Engineering_batteries'] = Engineering_batteries
module_dictionary['Transformer'] = Transformer
module_dictionary['Radiation_collector'] = Radiation_collector
