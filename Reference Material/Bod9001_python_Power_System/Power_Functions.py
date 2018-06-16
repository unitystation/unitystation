def Working_out_current(Junction,Power_dictionary,Dont_do_coming_from_self  = False ):
    current_supplying_at_Receiving_voltage = 0
    tuple_Junction = tuple([tuple(Junction[0]),Junction[1]])
    if len(Power_dictionary[tuple_Junction]['current coming from']) > 1:
        for current_in in Power_dictionary[tuple_Junction]['current coming from'].values():
            if Dont_do_coming_from_self:
                if current_in[1]:
                    current_supplying_at_Receiving_voltage += current_in[0]
            else:
                current_supplying_at_Receiving_voltage += current_in[0]
            
        
    else:
        for Current in Power_dictionary[tuple_Junction]['current coming from'].values():
            if Dont_do_coming_from_self:
                if Current[1]:
                    current_supplying_at_Receiving_voltage = Current[0]
                else:
                    current_supplying_at_Receiving_voltage = 0
            else:
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
             

def Battery_calculations(Battery,Power_dictionary,PPSD,is_working_backwards,Current = 0,Voltage = 0,Resistance = 0):
    #print(Battery,'yooooo?s')
    Battery_tuple = tuple([tuple(Battery[0]),Battery[1]])

    if is_working_backwards:
        if Battery_tuple in PPSD:
            
            if 'Receiving voltage' in Power_dictionary[Battery_tuple]:
                current_supplying_at_Receiving_voltage = Current
                Receiving_voltage = Voltage
                Battery_charge(Battery,
                                               Power_dictionary,
                                               PPSD,
                                               is_working_backwards,
                                               Receiving_voltage
                                               )
            elif 'Format_for_sub_syston' in Power_dictionary[Battery_tuple]:
                sub_syston_TOP = Power_dictionary[Battery_tuple]['sub syston TOP'][0]
                sub_syston_TOP_tuple = tuple([tuple(sub_syston_TOP[0]),sub_syston_TOP[1]])
                
                Resistance = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][1]
                current_supplying_at_Receiving_voltage = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2]
                Receiving_voltage = current_supplying_at_Receiving_voltage*Resistance
                
            else:
                current_supplying_at_Receiving_voltage = 0
                Receiving_voltage = 0
            #print(Receiving_voltage,'Minimum_support_voltage', PPSD[Battery_tuple]['Minimum_support_voltage'])
            if Receiving_voltage < PPSD[Battery_tuple]['Minimum_support_voltage']:
                #print('yo adding',PPSD[Battery_tuple]['Standard_supplying_voltage'])
                Pulling_Voltage = PPSD[Battery_tuple]['Standard_supplying_voltage'] - Receiving_voltage
                        
                Required_current = PPSD[Battery_tuple]['Standard_supplying_voltage']/Resistance
                
                Receiving_watts = Receiving_voltage * current_supplying_at_Receiving_voltage
                Required_watts = PPSD[Battery_tuple]['Standard_supplying_voltage'] * Required_current
                Pulling_watts = Required_watts - Receiving_watts
                Current_capacity = PPSD[Battery_tuple]['Current_capacity']
                adding_current = Required_current - current_supplying_at_Receiving_voltage
                
                if Pulling_watts > PPSD[Battery_tuple]['Maximum_watts_support']:
                    Pulling_watts = PPSD[Battery_tuple]['Maximum_watts_support']
                    
                if adding_current > PPSD[Battery_tuple]['Maximum_Current_support']:
                    adding_current = PPSD[Battery_tuple]['Maximum_Current_support']
                #print(Current_capacity,Pulling_watts)
                Current_charge = Current_capacity - Pulling_watts
                #print(Current_charge)
                #print('adding_current',adding_current)
                if Current_charge <= 0:
                    #print('eer')
                    if is_working_backwards:
                        PPSD[Battery_tuple]['Current_capacity'] = 0
                        
                    if 'Format_for_sub_syston' in Power_dictionary[Battery_tuple]:
                        
                        if 'current coming from Support' in Power_dictionary[Battery_tuple]:
                            Power_dictionary[Battery_tuple]['current coming from Support'][0] = [adding_current,sub_syston_TOP]
                            Power_dictionary[Battery_tuple]['Supply current Support'] = Working_out_Support_current(Battery,sub_syston_TOP,Power_dictionary)
                        else:
                            Power_dictionary[Battery_tuple]['current coming from Support'] = {0:[adding_current,sub_syston_TOP]}
                            Power_dictionary[Battery_tuple]['Supply current Support'] = Working_out_Support_current(Battery,sub_syston_TOP,Power_dictionary)
                            
                        Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] + adding_current
                    else:
                        if 'current coming from' in Power_dictionary[Battery_tuple]:
                            Power_dictionary[Battery_tuple]['current coming from'][0] = [adding_current,0]
                        else:  
                            Power_dictionary[Battery_tuple]['current coming from'] = {0:[adding_current,0]}
                            
                        Power_dictionary[Battery_tuple]['Supply current'] = Working_out_current(Battery,Power_dictionary)
                        Power_dictionary[Battery_tuple]['Supplying voltage'] = Power_dictionary[Battery_tuple]['Supply current'] * Resistance
                else:
                    #print('rewer')
                    if is_working_backwards:
                        PPSD[Battery_tuple]['Current_capacity'] = Current_charge

                    if 'Format_for_sub_syston' in Power_dictionary[Battery_tuple]:
                        if 'current coming from Support' in Power_dictionary[Battery_tuple]:
                            Power_dictionary[Battery_tuple]['current coming from Support'][0] = [adding_current,sub_syston_TOP]
                            Power_dictionary[Battery_tuple]['Supply current Support'] = Working_out_Support_current(Battery,sub_syston_TOP,Power_dictionary)
                        else:
                            Power_dictionary[Battery_tuple]['current coming from Support'] = {0:[adding_current,sub_syston_TOP]}
                            Power_dictionary[Battery_tuple]['Supply current Support'] = Working_out_Support_current(Battery,sub_syston_TOP,Power_dictionary)
       
                    else:
                        #print('yay',adding_current )
                        if 'current coming from' in Power_dictionary[Battery_tuple]:
                            Power_dictionary[Battery_tuple]['current coming from'][0] = [adding_current,0]
                        else:  
                            Power_dictionary[Battery_tuple]['current coming from'] = {0:[adding_current,0]}
                        #print()
                        Power_dictionary[Battery_tuple]['Supply current'] = Working_out_current(Battery,Power_dictionary)
                        Power_dictionary[Battery_tuple]['Supplying voltage'] = Power_dictionary[Battery_tuple]['Supply current'] * Resistance
                        #print(Power_dictionary[Battery_tuple]['current coming from'])
            else:
                if 'Format_for_sub_syston' in Power_dictionary[Battery_tuple]:
                    if 'current coming from Support' in Power_dictionary[Battery_tuple]:
                        Power_dictionary[Battery_tuple]['current coming from Support'][0] = [0,sub_syston_TOP]
                        Power_dictionary[Battery_tuple]['Supply current Support'] = Working_out_Support_current(Battery,sub_syston_TOP,Power_dictionary)
                    else:
                        Power_dictionary[Battery_tuple]['current coming from Support'] = {0:[0,sub_syston_TOP]}
                        Power_dictionary[Battery_tuple]['Supply current Support'] = Working_out_Support_current(Battery,sub_syston_TOP,Power_dictionary)
                                                                                                             
                else:    
                    Power_dictionary[Battery_tuple]['Supplying voltage'] = Voltage
                    Power_dictionary[Battery_tuple]['Supply current'] = Current
                    
            if is_working_backwards:
                if 'Receiving voltage' in Power_dictionary[Battery_tuple]:
                    Power_dictionary[Battery_tuple]['sub syston TOP'] = [
                                                                                   Battery,
                                                                                   Resistance,
                                                                                   Power_dictionary[Battery_tuple]['Supply current']
                                                                                   ]
                elif not 'Format_for_sub_syston' in Power_dictionary[Battery_tuple]:
                    Power_dictionary[Battery_tuple]['sub syston TOP'] = [
                                                                                   Battery,
                                                                                   Resistance,
                                                                                   Power_dictionary[Battery_tuple]['Supply current']
                                                                                   ]
                    
                
                
    if not is_working_backwards:
        Battery_charge(Battery,
                                               Power_dictionary,
                                               PPSD,
                                               is_working_backwards,
                                               )




def Transformer_Calculations(Transformer,Power_dictionary,PPSD,Resistance,current,voltage,is_working_backwards,Support_supply_formatting):
    Transformer_tuple = tuple([tuple(Transformer[0]),Transformer[1]])
    Power_dictionary[Transformer_tuple]['Use Resistance from modified'] = True 

    
    R2 = Resistance
    I2 = PPSD[Transformer_tuple]['Expected_output']/Resistance
    V2 = PPSD[Transformer_tuple]['Expected_output']
    
    Turn_ratio = PPSD[Transformer_tuple]['Turn_ratio']

    
    V1 = (V2*Turn_ratio)
    I1 = (V2/V1)*I2
    R1 = PPSD[Transformer_tuple]['Expected_input']/I1
    
    Resistance_sub = Working_out_resistance(Transformer,Power_dictionary)
    if Resistance_sub:
        R2_SUB = Resistance_sub
        I2_SUB = PPSD[Transformer_tuple]['Expected_output']/Resistance_sub
        V2_SUB = PPSD[Transformer_tuple]['Expected_output']
        
         
        V1_SUB = (V2_SUB * Turn_ratio)
        I1_SUB = (V2_SUB/V1_SUB)*I2_SUB
        R1_SUB = PPSD[Transformer_tuple]['Expected_input']/I1_SUB
    else:
        R1_SUB = 0
    
    if 'Resistance from modified' in Power_dictionary[Transformer_tuple]:
        Power_dictionary[Transformer_tuple]['Resistance from modified'][0] = [R1_SUB,0]
    else:
        Power_dictionary[Transformer_tuple]['Resistance from modified'] = {0:[R1_SUB,0]}
    #print(R1_SUB,'R1_SUB')

    if 'Parallel Resistance from cabeis' in Power_dictionary[Transformer_tuple]:
        Power_dictionary[Transformer_tuple]['Parallel Resistance from cabeis modified'] = {}
        for key, value in Power_dictionary[Transformer_tuple]['Parallel Resistance from cabeis'].items():
            R2_SUB = value[0]
            if R2_SUB:
                I2_SUB = PPSD[Transformer_tuple]['Expected_output']/Resistance 
                V2_SUB = PPSD[Transformer_tuple]['Expected_output']

                
                V1_SUB = (V2_SUB*Turn_ratio)
                I1_SUB = (V2_SUB/V1_SUB)*I2_SUB
                R1_SUB = PPSD[Transformer_tuple]['Expected_input']/I1_SUB
            else:
                R1_SUB = 0
            
            Power_dictionary[Transformer_tuple]['Parallel Resistance from cabeis modified'][key] = [R1_SUB,value[1]]
            

    Power_dictionary[Transformer_tuple]['Resistance'] = Working_out_resistance_Modified(Transformer,Power_dictionary)
    
    if voltage:
        V1 = voltage
        I1 = current
        R1 = Working_out_resistance_Modified(Transformer,Power_dictionary)
        

        R2 = Resistance
        V2 = V1/Turn_ratio
        if PPSD[Transformer_tuple]['Voltage_limiting']:
            if V2 > PPSD[Transformer_tuple]['Voltage_limiting']:
                V2 = PPSD[Transformer_tuple]['Voltage_limited_to']

        I2 = (V2/V1)*I1
        I2 = V2/Resistance
        
        Power_dictionary[Transformer_tuple]['Supplying voltage'] = V2
        Power_dictionary[Transformer_tuple]['Supply current']  = I2
        
    if is_working_backwards:
        if not 'Format_for_sub_syston' in Power_dictionary[Transformer_tuple]:
            if Support_supply_formatting:
                if not 'Supply current' in Power_dictionary[Transformer_tuple]:
                    Supply_current = 0
                else:
                    Supply_current = Power_dictionary[Transformer_tuple]['Supply current']
                Power_dictionary[Transformer_tuple]['sub syston TOP'] = [
                                                                                   Transformer,
                                                                                   Resistance,
                                                                                   Supply_current
                                                                                   ]
    return([V2,I2,R2]) 


#(Battery,Power_dictionary,PPSD,is_working_backwards)
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
            if 'Resistance from modified' in PD[Tuple_Battery]:
                PD[Tuple_Battery]['Resistance from modified']['Battery'] = [(PPSD[Tuple_Battery]['Supply_voltage']/Charging_current),0]
            else:    
                PD[Tuple_Battery]['Resistance from modified'] = {0:[PPSD[Tuple_Battery]['Supply_voltage']/Charging_current,0]}
                
            PD[Tuple_Battery]['Resistance'] = Working_out_resistance_Modified(Battery,PD) 
            
    else:
        PPSD[Tuple_Battery]['Charging'] = False
        

