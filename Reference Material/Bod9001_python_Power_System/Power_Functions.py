def Working_out_current(Junction,Power_dictionary):
    current_supplying_at_Receiving_voltage = 0
    tuple_Junction = tuple([tuple(Junction[0]),Junction[1]])
    if len(Power_dictionary[tuple_Junction]['current coming from']) > 1:
        for current_in in Power_dictionary[tuple_Junction]['current coming from'].values():
            current_supplying_at_Receiving_voltage += current_in[0]
            
        
    else:
        for Current in Power_dictionary[tuple_Junction]['current coming from'].values():
            current_supplying_at_Receiving_voltage = Current[0]
    return(current_supplying_at_Receiving_voltage)

def Working_out_Support_current(Junction,Destination,Power_dictionary):
    current_supplying_at_Receiving_voltage = 0
    tuple_Junction = tuple([tuple(Junction[0]),Junction[1]])
    Checked_support = []
    for Unchecked_support in Power_dictionary[tuple_Junction]['current coming from Support'].values():
        if Unchecked_support[1] == Destination:
            Checked_support.append(Unchecked_support)
    if len(Checked_support) > 1:
        for current_in in Checked_support:
            current_supplying_at_Receiving_voltage += current_in[0] 
    else:
        for Current in Checked_support:
            current_supplying_at_Receiving_voltage = Current[0]
            
    return(current_supplying_at_Receiving_voltage)

def Working_out_resistance(Junction,Power_dictionary):
    ResistanceX_all = 0
    Resistance = 0
    tuple_Junction = tuple([tuple(Junction[0]),Junction[1]])
    if tuple_Junction in Power_dictionary:
        if len(Power_dictionary[tuple_Junction]['Resistance from cabeis']) > 1:
            #print('more then one', Power_dictionary[tuple(Junction)]['being redsed from'] )
            for Resistance in Power_dictionary[tuple_Junction]['Resistance from cabeis'].values():
                if Resistance[0]:
                    ResistanceX_all += 1/Resistance[0]
            if ResistanceX_all:
                Resistance = 1/ResistanceX_all
            else:
                Resistance = 0
            
        else:
            for value in Power_dictionary[tuple_Junction]['Resistance from cabeis'].values():
                Resistance = value[0]
    return(Resistance)

def Working_out_all_resistance(Junction,Power_dictionary):
    ResistanceX_all = 0
    Resistance = 0
    tuple_Junction = tuple([tuple(Junction[0]),Junction[1]])
    if tuple_Junction in Power_dictionary:
        if len(Power_dictionary[tuple_Junction]['Resistance from cabeis']) > 1:
            #print('more then one', Power_dictionary[tuple(Junction)]['being redsed from'] )
            for Resistance_pop in Power_dictionary[tuple_Junction]['Resistance from cabeis'].values():
                if Resistance_pop[0]:
                    ResistanceX_all += 1/Resistance_pop[0]     
            if ResistanceX_all:
                Resistance = ResistanceX_all
            else:
                Resistance = 0

        else:
            for value in Power_dictionary[tuple_Junction]['Resistance from cabeis'].values():
                if value[0]:
                    Resistance = 1/value[0]
    #print('hr 1')
    if 'Parallel Resistance from cabeis' in Power_dictionary[tuple_Junction]:
        #print('hr 2')
        if len(Power_dictionary[tuple_Junction]['Parallel Resistance from cabeis']) > 1:
            #print('hr 3.0')
            ResistanceX_all = 0
            for Resistance_pop in Power_dictionary[tuple_Junction]['Parallel Resistance from cabeis'].values():
                if Resistance_pop[0]:
                    ResistanceX_all += 1/Resistance_pop[0]
                    
            if ResistanceX_all:
                Resistance_sub = ResistanceX_all
            else:
                Resistance_sub = 0
            Resistance += Resistance_sub
                        
        else:
            #print('hr 3.1')
            for value in Power_dictionary[tuple_Junction]['Parallel Resistance from cabeis'].values():
                if value[0]:
                    Resistance += 1/value[0]
                #print(value[0])
    if Resistance:
        True_resistance = 1/Resistance
                    
    return(True_resistance)


 
def Working_out_resistance_Modified(Junction,Power_dictionary):
    ResistanceX_all = 0
    Resistance = 0
    tuple_Junction = tuple([tuple(Junction[0]),Junction[1]])
    if tuple_Junction in Power_dictionary:
        if len(Power_dictionary[tuple_Junction]['Resistance from modified']) > 1:
            #print('more then one', Power_dictionary[tuple(Junction)]['being redsed from'] )
            for Resistance in Power_dictionary[tuple_Junction]['Resistance from modified'].values():
                if Resistance[0]:
                    ResistanceX_all += 1/Resistance[0]
            if ResistanceX_all:
                Resistance = 1/ResistanceX_all
            else:
                Resistance = 0
            
        else:
            for value in Power_dictionary[tuple_Junction]['Resistance from modified'].values():
                Resistance = value[0]
    return(Resistance)



def Working_out_all_resistance_Modified(Junction,Power_dictionary):
    ResistanceX_all = 0
    Resistance = 0
    tuple_Junction = tuple([tuple(Junction[0]),Junction[1]])
    if tuple_Junction in Power_dictionary:
        if len(Power_dictionary[tuple_Junction]['Resistance from modified']) > 1:
            #print('more then one', Power_dictionary[tuple(Junction)]['being redsed from'] )
            for Resistance_pop in Power_dictionary[tuple_Junction]['Resistance from modified'].values():
                if Resistance_pop[0]:
                    ResistanceX_all += 1/Resistance_pop[0]     
            if ResistanceX_all:
                Resistance = ResistanceX_all
            else:
                Resistance = 0

        else:
            for value in Power_dictionary[tuple_Junction]['Resistance from modified'].values():
                if value[0]:
                    Resistance = 1/value[0]
    #print('hr 1')
    if 'Parallel Resistance from cabeis modified' in Power_dictionary[tuple_Junction]:
        #print('hr 2')
        if len(Power_dictionary[tuple_Junction]['Parallel Resistance from cabeis modified']) > 1:
            #print('hr 3.0')
            ResistanceX_all = 0
            for Resistance_pop in Power_dictionary[tuple_Junction]['Parallel Resistance from cabeis modified'].values():
                if Resistance_pop[0]:
                    ResistanceX_all += 1/Resistance_pop[0]
                    
            if ResistanceX_all:
                Resistance_sub = ResistanceX_all
            else:
                Resistance_sub = 0
            Resistance += Resistance_sub
                        
        else:
            #print('hr 3.1')
            for value in Power_dictionary[tuple_Junction]['Parallel Resistance from cabeis modified'].values():
                if value[0]:
                    Resistance += 1/value[0]#
                #print(value[0])
    if Resistance:
        True_resistance = 1/Resistance
    
                    
    return(True_resistance)
             

                    

#(Department_batterie,Power_dictionary,Persistent_power_system_data,is_working_backwards)
def Battery_charge(Battery,PD,PPSD,IWB,voltage = 0):
    #print('hey1')
    Tuple_Battery = tuple([tuple(Battery[0]),Battery[1]])

    if IWB:
        #print('pop1')
        Receiving_voltage = voltage
        if Receiving_voltage >= PPSD[Tuple_Battery]['Increased_charge_voltage']:
            #print(PPSD[Tuple_Battery]['If_voltage_charge'])
            PPSD[Tuple_Battery]['Pulling'] = False
            if PPSD[Tuple_Battery]['Charging']:
                #print('pop3')
                PPSD[Tuple_Battery]['Current_capacity'] = PPSD[Tuple_Battery]['If_voltage_charge']
            if not PPSD[Tuple_Battery]['Charging_multiplier'] >= PPSD[Tuple_Battery]['Max_charging_multiplier']:
                #print('adding')
                Charging_multiplier = PPSD[Tuple_Battery]['Charging_multiplier']
                PPSD[Tuple_Battery]['Charging_multiplier'] = Charging_multiplier + PPSD[Tuple_Battery]['Charge_steps']
                
        elif Receiving_voltage >= PPSD[Tuple_Battery]['Extra_charge_cut_off']:
            #print('pop2')
            PPSD[Tuple_Battery]['Pulling'] = False
            if PPSD[Tuple_Battery]['Charging']:
                #print('pop3')
                PPSD[Tuple_Battery]['Current_capacity'] = PPSD[Tuple_Battery]['If_voltage_charge']
        else:
            PPSD[Tuple_Battery]['Pulling'] = True     
    else:
        #print('heytyy?')
        if PPSD[Tuple_Battery]['Pulling'] == False:
            
            Charge_calculations(Battery,PD,PPSD,IWB)
        else:
            if not 0.0 >= PPSD[Tuple_Battery]['Charging_multiplier']:
                Charging_multiplier = PPSD[Tuple_Battery]['Charging_multiplier']
                PPSD[Tuple_Battery]['Charging_multiplier'] = Charging_multiplier - PPSD[Tuple_Battery]['Charge_steps']
                Charge_calculations(Battery,PD,PPSD,IWB)
               
def Charge_calculations(Battery,PD,PPSD,IWB):
    Tuple_Battery = tuple([tuple(Battery[0]),Battery[1]])
    if not PPSD[Tuple_Battery]['Charging_multiplier'] == 0:
        PPSD[Tuple_Battery]['Charging'] = True
        Current_capacity = PPSD[Tuple_Battery]['Current_capacity']
        Capacity_max = PPSD[Tuple_Battery]['Capacity_max']
        if Current_capacity < Capacity_max:
            #print(PPSD[Tuple_Battery]['Charging_multiplier'])
            Charging_current = PPSD[Tuple_Battery]['Standard_charge_current'] * PPSD[Tuple_Battery]['Charging_multiplier']
            Charging_watts = (PPSD[Tuple_Battery]['Supply_voltage'] * Charging_current) 
            New_current_capacity = Current_capacity + Charging_watts
            #print(New_current_capacity)
            if New_current_capacity >= Capacity_max:
                PPSD[Tuple_Battery]['If_voltage_charge'] = Capacity_max
            else:
                PPSD[Tuple_Battery]['If_voltage_charge'] = New_current_capacity           
            #if 'Resistance from modified' in PD[Tuple_Battery]:
                #PD[Tuple_Battery]['Resistance from modified']['Battery'] = [(PPSD[Tuple_Battery]['Supply_voltage']/Charging_current),0]
            #else:    
                #PD[Tuple_Battery]['Resistance from modified'] = {0:[PPSD[Tuple_Battery]['Supply_voltage']/Charging_current,0]}
                
            #PD[Tuple_Battery]['Resistance'] = Working_out_resistance_Modified(Battery,PD) 
            
    else:
        PPSD[Tuple_Battery]['Charging'] = False
        

