import Power_Functions

def Department_batteries_Module(Department_batterie,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):
    
    Department_batterie_tuple = tuple([tuple(Department_batterie[0]),Department_batterie[1]])
    Power_dictionary[Department_batterie_tuple]['Use Resistance from modified'] = True 
    Resistance = Power_Functions.Working_out_all_resistance(Department_batterie,Power_dictionary)
    PPSD = Persistent_power_system_data
    #print('Department_batteries', Support_supply_formatting)
    R2 = Resistance
    I2 = 240/Resistance
    V2 = 240
    
    Turn_ratio = 12.5
     
    V1 = (240 * Turn_ratio)
    I1 = (V2/V1)*I2
    R1 = 3000/I1

    Resistance_sub = Power_Functions.Working_out_resistance(Department_batterie,Power_dictionary)
    if Resistance_sub:
        R2_SUB = Resistance_sub
        I2_SUB = 240/Resistance_sub
        V2_SUB = 240
        
         
        V1_SUB = (240 * Turn_ratio)
        I1_SUB = (V2_SUB/V1_SUB)*I2_SUB
        R1_SUB = 3000/I1_SUB
    else:
        R1_SUB = 0
    
    if 'Resistance from modified' in Power_dictionary[Department_batterie_tuple]:
        Power_dictionary[Department_batterie_tuple]['Resistance from modified'][0] = [R1_SUB,0]
    else:
        Power_dictionary[Department_batterie_tuple]['Resistance from modified'] = {0:[R1_SUB,0]}

    if 'Parallel Resistance from cabeis' in Power_dictionary[Department_batterie_tuple]:
        Power_dictionary[Department_batterie_tuple]['Parallel Resistance from cabeis modified'] = {}
        for key, value in Power_dictionary[Department_batterie_tuple]['Parallel Resistance from cabeis'].items():
            R2_SUB = value[0]
            if R2_SUB:
                I2_SUB = 240/R2_SUB 
                V2_SUB = 240

                
                V1_SUB = (V2_SUB*Turn_ratio)
                I1_SUB = (V2_SUB/V1_SUB)*I2_SUB
                R1_SUB = 3000/I1_SUB
            else:
                R1_SUB = 0
            
            Power_dictionary[Department_batterie_tuple]['Parallel Resistance from cabeis modified'][key] = [R1_SUB,value[1]]

            
    Power_dictionary[Department_batterie_tuple]['Resistance'] = Power_Functions.Working_out_resistance_Modified(Department_batterie,Power_dictionary)
    Power_dictionary[Department_batterie_tuple]['Type'] = 'Department_batterie'
    
    if 'Receiving voltage' in Power_dictionary[Department_batterie_tuple]:
        current_supplying_at_Receiving_voltage = Power_Functions.Working_out_current(Department_batterie,Power_dictionary)

        V1 = Power_dictionary[Department_batterie_tuple]['Receiving voltage']
        I1 = current_supplying_at_Receiving_voltage
        R1 = V1/current_supplying_at_Receiving_voltage
        
        R2 = Resistance
        
        V2 = V1/Turn_ratio
        I2 = V2/Resistance
        
        #Power_dictionary[Department_batterie_tuple]['Supplying voltage'] = V2
        #Power_dictionary[Department_batterie_tuple]['Supply current']  = I2

    
    if not Department_batterie_tuple in Persistent_power_system_data:
        Dictionary = {}
        Dictionary['Type'] = 'Department_batterie'
        Dictionary['Cangenerate_resistance'] = True
        Dictionary['Capacity_Percentage'] = 100
        Dictionary['Maximum_current'] = 6
        Dictionary['Supply_voltage'] = 240
        Dictionary['Capacity_max'] = 432000
        #Dictionary['Current_capacity'] = 432000
        #Dictionary['Current_capacity'] = 6000
        Dictionary['Current_capacity'] = 0
        Dictionary['Pulling'] = True
        Dictionary['Charging'] = False
        Dictionary['Charging_multiplier'] = 1.0
        Dictionary['If_voltage_charge'] = 0
        Dictionary['Extra_charge_cut_off'] = 240
        Dictionary['Increased_charge_voltage'] = 264
        Dictionary['Standard_charge_current'] = 0.1
        Dictionary['Charge_steps'] = 0.1
        Dictionary['Max_charging_multiplier'] = 1.2
        PPSD[Department_batterie_tuple] = Dictionary
        
    if is_working_backwards:
        if Department_batterie_tuple in Persistent_power_system_data:
            #Resistance = Power_Functions.Working_out_resistance(Engineering_batteries,Power_dictionary)
            
            if 'Receiving voltage' in Power_dictionary[Department_batterie_tuple]:
                #if 'Up_Flow_voltage' in Power_dictionary[Department_batterie_tuple]:
                    #print('heryyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy', Power_dictionary[Department_batterie_tuple]['Up_Flow_voltage'] ,'Up_Flow_voltage' )
                    #Receiving_voltage = V2 + Power_dictionary[Department_batterie_tuple]['Up_Flow_voltage'] 
                    

                current_supplying_at_Receiving_voltage = I2
                Receiving_voltage = V2
                Power_Functions.Battery_charge(Department_batterie,
                                               Power_dictionary,
                                               Persistent_power_system_data,
                                               is_working_backwards,
                                               Receiving_voltage
                                               )
            elif 'Format_for_sub_syston' in Power_dictionary[Department_batterie_tuple]:
                #print('heryyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy',Power_dictionary[Engineering_batteries_tuple]['Up_Flow_voltage'] ,'Up_Flow_voltage' )
                sub_syston_TOP = Power_dictionary[Department_batterie_tuple]['sub syston TOP'][0]
                sub_syston_TOP_tuple = tuple([tuple(sub_syston_TOP[0]),sub_syston_TOP[1]])
                
                Resistance = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][1]
                current_supplying_at_Receiving_voltage = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2]
                Receiving_voltage = current_supplying_at_Receiving_voltage*Resistance
                
            else:
                current_supplying_at_Receiving_voltage = 0
                Receiving_voltage = 0
                
            if Receiving_voltage < 216:
                #print('v is low')
                #print(Receiving_voltage)
                #if is_working_backwards:
                #PPSD[tuple(Department_batterie)]['Pulling'] = True
                Pulling_Voltage = 240 - Receiving_voltage
                        
                Required_current = 240/Resistance
                
                Receiving_watts = Receiving_voltage * current_supplying_at_Receiving_voltage
                Required_watts = 240 * Required_current
                Pulling_watts = Required_watts - Receiving_watts
                Current_capacity = PPSD[Department_batterie_tuple]['Current_capacity']
                adding_current = Required_current - current_supplying_at_Receiving_voltage 

                Current_charge = Current_capacity - Pulling_watts
                if Current_charge <= 0:
                    if is_working_backwards:
                        PPSD[Department_batterie_tuple]['Current_capacity'] = 0
                        
                    if 'Format_for_sub_syston' in Power_dictionary[Department_batterie_tuple]:
                        if 'current coming from Support' in Power_dictionary[Department_batterie_tuple]:
                            Power_dictionary[Department_batterie_tuple]['current coming from Support'][0] = [adding_current,sub_syston_TOP]
                            Power_dictionary[Department_batterie_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Department_batterie,sub_syston_TOP,Power_dictionary)
                        else:
                            Power_dictionary[Department_batterie_tuple]['current coming from Support'] = {0:[adding_current,sub_syston_TOP]}
                            Power_dictionary[Department_batterie_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Department_batterie,sub_syston_TOP,Power_dictionary)
                            
                        Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] + adding_current
                    else:
                        if 'current coming from' in Power_dictionary[Department_batterie_tuple]:
                            Power_dictionary[Department_batterie_tuple]['current coming from'][0] = [adding_current,0]
                        else:  
                            Power_dictionary[Department_batterie_tuple]['current coming from'] = {0:[adding_current,0]}
                            
                        Power_dictionary[Department_batterie_tuple]['Supply current'] = Power_Functions.Working_out_current(Department_batterie,Power_dictionary)
                        Power_dictionary[Department_batterie_tuple]['Supplying voltage'] = 240
                    #PPSD[tuple(Department_batterie)]['Current_capacity'] = 0
                else:
                    if is_working_backwards:
                        PPSD[Department_batterie_tuple]['Current_capacity'] = Current_charge

                    if 'Format_for_sub_syston' in Power_dictionary[Department_batterie_tuple]:
                        if 'current coming from Support' in Power_dictionary[Engineering_batteries_tuple]:
                            Power_dictionary[Department_batterie_tuple]['current coming from Support'][0] = [adding_current,sub_syston_TOP]
                            Power_dictionary[Department_batterie_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Department_batterie,sub_syston_TOP,Power_dictionary)
                        else:
                            Power_dictionary[Department_batterie_tuple]['current coming from Support'] = {0:[adding_current,sub_syston_TOP]}
                            Power_dictionary[Department_batterie_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Department_batterie,sub_syston_TOP,Power_dictionary)
       
                    else:
                        if 'current coming from' in Power_dictionary[Department_batterie_tuple]:
                            Power_dictionary[Department_batterie_tuple]['current coming from'][0] = [adding_current,0]
                        else:  
                            Power_dictionary[Department_batterie_tuple]['current coming from'] = {0:[adding_current,0]}
                        Power_dictionary[Department_batterie_tuple]['Supply current'] = Power_Functions.Working_out_current(Department_batterie,Power_dictionary)
                        Power_dictionary[Department_batterie_tuple]['Supplying voltage'] = 240
            else:
                if 'Format_for_sub_syston' in Power_dictionary[Department_batterie_tuple]:
                    if 'current coming from Support' in Power_dictionary[Department_batterie_tuple]:
                        Power_dictionary[Department_batterie_tuple]['current coming from Support'][0] = [0,sub_syston_TOP]
                        Power_dictionary[Department_batterie_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Department_batterie,sub_syston_TOP,Power_dictionary)
                    else:
                        Power_dictionary[Department_batterie_tuple]['current coming from Support'] = {0:[0,sub_syston_TOP]}
                        Power_dictionary[Department_batterie_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Department_batterie,sub_syston_TOP,Power_dictionary)
                                                                                                             
                else:    
                    Power_dictionary[Department_batterie_tuple]['Supplying voltage'] = V2
                    Power_dictionary[Department_batterie_tuple]['Supply current'] = I2

 
            if is_working_backwards:
                if 'Receiving voltage' in Power_dictionary[Department_batterie_tuple]:
                    Power_dictionary[Department_batterie_tuple]['sub syston TOP'] = [
                                                                                   Department_batterie,
                                                                                   R2,
                                                                                   Power_dictionary[Department_batterie_tuple]['Supply current']
                                                                                   ]
    if not is_working_backwards:
        Power_Functions.Battery_charge(Department_batterie,
                                               Power_dictionary,
                                               Persistent_power_system_data,
                                               is_working_backwards,
                                               )


    

def Engineering_batteries_Module(Engineering_batteries,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):
    Engineering_batteries_tuple = tuple([tuple(Engineering_batteries[0]),Engineering_batteries[1]])
    Power_dictionary[Engineering_batteries_tuple]['Type'] = 'Engineering_Battery'
    
    PPSD = Persistent_power_system_data
    #print('Engineering_Battery',Engineering_batteries)
    #print(Power_dictionary[Engineering_batteries_tuple])
    
    
    if not Engineering_batteries_tuple in PPSD:
        Dictionary = {}
        Dictionary['Type'] = 'Engineering_Battery'
        Dictionary['Cangenerate_resistance'] = True
        Dictionary['Capacity_Percentage'] = 100
        Dictionary['Maximum_current'] = 1
        Dictionary['Supply_voltage'] = 3000
        Dictionary['Capacity_max'] = 1800000
        Dictionary['Current_capacity'] = 1800000
        #Dictionary['Current_capacity'] = 10000
        Dictionary['Current_capacity'] = 0
        Dictionary['Pulling'] = True
        Dictionary['Charging'] = False
        Dictionary['Charging_multiplier'] = 1.0
        Dictionary['If_voltage_charge'] = 0
        Dictionary['Extra_charge_cut_off'] = 3000
        Dictionary['Increased_charge_voltage'] = 3100
        Dictionary['Standard_charge_current'] = 0.1
        Dictionary['Charge_steps'] = 0.1
        Dictionary['Max_charging_multiplier'] = 1.5
        PPSD[Engineering_batteries_tuple] = Dictionary
        
    if is_working_backwards:
        if Engineering_batteries_tuple in Persistent_power_system_data:
            
            
            
            if 'Receiving voltage' in Power_dictionary[Engineering_batteries_tuple]:
                Resistance = Power_Functions.Working_out_all_resistance(Engineering_batteries,Power_dictionary)
                if 'Up_Flow_voltage' in Power_dictionary[Engineering_batteries_tuple]:
                    #print('revaive ing ', Power_dictionary[Engineering_batteries_tuple]['Receiving voltage'] )
                    Receiving_voltage = Power_dictionary[Engineering_batteries_tuple]['Receiving voltage'] + Power_dictionary[Engineering_batteries_tuple]['Up_Flow_voltage']
                    current_supplying_at_Receiving_voltage = (Receiving_voltage/Resistance)
                    
                else:    
                    current_supplying_at_Receiving_voltage = Power_Functions.Working_out_current(Engineering_batteries,Power_dictionary)
                    Receiving_voltage = Power_dictionary[Engineering_batteries_tuple]['Receiving voltage']
                    Power_Functions.Battery_charge(Engineering_batteries,
                                                   Power_dictionary,
                                                   Persistent_power_system_data,
                                                   is_working_backwards,
                                                   Receiving_voltage
                                                   )
            elif 'Format_for_sub_syston' in Power_dictionary[Engineering_batteries_tuple]:
                #print('heryyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy',Power_dictionary[Engineering_batteries_tuple]['Up_Flow_voltage'] ,'Up_Flow_voltage' )
                sub_syston_TOP = Power_dictionary[Engineering_batteries_tuple]['sub syston TOP'][0]
                sub_syston_TOP_tuple = tuple([tuple(sub_syston_TOP[0]),sub_syston_TOP[1]])
                
                Resistance = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][1]
                current_supplying_at_Receiving_voltage = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2]
                Receiving_voltage = current_supplying_at_Receiving_voltage*Resistance
                
            else:
                current_supplying_at_Receiving_voltage = 0
                Receiving_voltage = 0
            #print(Receiving_voltage,Engineering_batteries)    
            if Receiving_voltage < 2700:
                #PPSD[tuple(Engineering_batteries)]['Pulling'] = True
                Pulling_Voltage = 3000 - Receiving_voltage
                #print(Power_dictionary[Engineering_batteries_tuple]) 
                Required_current = 3000/Resistance
                
                Receiving_watts = Receiving_voltage * current_supplying_at_Receiving_voltage
                Required_watts = 3000 * Required_current
                Pulling_watts = Required_watts - Receiving_watts
                Current_capacity = PPSD[Engineering_batteries_tuple]['Current_capacity']
                Current_charge = Current_capacity - Pulling_watts
                adding_current = Required_current - current_supplying_at_Receiving_voltage 
                #print(Current_charge,'here')
                if Current_charge <= 0:
                    #print('ER?')
                    
                    if is_working_backwards:
                        #print(Current_charge)
                        PPSD[Engineering_batteries_tuple]['Current_capacity'] = 0
                    if 'Format_for_sub_syston' in Power_dictionary[Engineering_batteries_tuple]:
                        if 'current coming from Support' in Power_dictionary[Engineering_batteries_tuple]:
                            Power_dictionary[Engineering_batteries_tuple]['current coming from Support'][0] = [adding_current,sub_syston_TOP]

                        else:
                            Power_dictionary[Engineering_batteries_tuple]['current coming from Support'] = {0:[adding_current,sub_syston_TOP]}
                        Power_dictionary[Engineering_batteries_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Engineering_batteries,sub_syston_TOP,Power_dictionary)

                            
                        Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] + adding_current
                    else:
                        Power_dictionary[Engineering_batteries_tuple]['Supplying voltage'] = 3000
                        #Power_dictionary[Engineering_batteries_tuple]['Supply current']  = Required_current
                        if 'current coming from' in Power_dictionary[Engineering_batteries_tuple]:
                            Power_dictionary[Engineering_batteries_tuple]['current coming from'][0] = [adding_current,0]
                        else:  
                            Power_dictionary[Engineering_batteries_tuple]['current coming from'] = {0:[adding_current,0]}
                        Power_dictionary[Engineering_batteries_tuple]['Supply current'] = Power_Functions.Working_out_current(Engineering_batteries,Power_dictionary)
                else:
                    if is_working_backwards:
                        PPSD[Engineering_batteries_tuple]['Current_capacity'] = Current_charge
                    #print('AP')
                    if 'Format_for_sub_syston' in Power_dictionary[Engineering_batteries_tuple]:
                        if 'current coming from Support' in Power_dictionary[Engineering_batteries_tuple]:
                            Power_dictionary[Engineering_batteries_tuple]['current coming from Support'][0] = [adding_current,sub_syston_TOP]
                            
                        else:
                            Power_dictionary[Engineering_batteries_tuple]['current coming from Support'] = {0:[adding_current,sub_syston_TOP]}
                        Power_dictionary[Engineering_batteries_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Engineering_batteries,sub_syston_TOP,Power_dictionary)
                        
                    else:
                        Power_dictionary[Engineering_batteries_tuple]['Supplying voltage'] = 3000
                        if 'current coming from' in Power_dictionary[Engineering_batteries_tuple]:
                            Power_dictionary[Engineering_batteries_tuple]['current coming from'][0] = [adding_current,0]
                        else:  
                            Power_dictionary[Engineering_batteries_tuple]['current coming from'] = {0:[adding_current,0]}
                        Power_dictionary[Engineering_batteries_tuple]['Supply current'] = Power_Functions.Working_out_current(Engineering_batteries,Power_dictionary)
            else:
                #Power_dictionary[Engineering_batteries_tuple]['Supplying voltage'] = Receiving_voltage
                if 'Format_for_sub_syston' in Power_dictionary[Engineering_batteries_tuple]:
                    if 'current coming from Support' in Power_dictionary[Engineering_batteries_tuple]:
                        Power_dictionary[Engineering_batteries_tuple]['current coming from Support'][0] = [0,sub_syston_TOP]
                       
                    else:
                        Power_dictionary[Engineering_batteries_tuple]['current coming from Support'] = {0:[0,sub_syston_TOP]}
                    Power_dictionary[Engineering_batteries_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Engineering_batteries,sub_syston_TOP,Power_dictionary)
                #else:
                    
                    #Power_dictionary[Engineering_batteries_tuple]['Supply current'] = Power_Functions.Working_out_current(Engineering_batteries,Power_dictionary)
                
                    
      
            
     
            if 'Receiving voltage' in Power_dictionary[Engineering_batteries_tuple]:
                if is_working_backwards:
                    Power_dictionary[Engineering_batteries_tuple]['sub syston TOP'] = [
                                                                                   Engineering_batteries,
                                                                                   Resistance,
                                                                                   Power_dictionary[Engineering_batteries_tuple]['Supply current']
                                                                                   ]
    #print(Power_dictionary[Engineering_batteries_tuple])
    if not is_working_backwards:
        Power_Functions.Battery_charge(Engineering_batteries,
                                               Power_dictionary,
                                               Persistent_power_system_data,
                                               is_working_backwards,
                                               )

        
                    
                            

def Transformer_Module(Transformer,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):
    Transformer_tuple = tuple([tuple(Transformer[0]),Transformer[1]])
    Power_dictionary[Transformer_tuple]['Type'] = 'Transformer'
    Power_dictionary[Transformer_tuple]['Use Resistance from modified'] = True 
    print('Transformer', Support_supply_formatting)
    Resistance = Power_Functions.Working_out_all_resistance(Transformer,Power_dictionary)
           
    R2 = Resistance
    #print('R2',Resistance) 
    I2 = 3000/Resistance
    #print('I2',I2)
    V2 = 3000
    #print('V2',V2)
    
    Turn_ratio = 250

    
    V1 = (V2*Turn_ratio)
    #print('V1',V1)
    I1 = (V2/V1)*I2
    #print('I1',I1)
    #R1 = Turn_ratio * R2
    #270000
    R1 = 270000/I1
    #print('R1',R1)

    
    Resistance_sub = Power_Functions.Working_out_resistance(Transformer,Power_dictionary)
    if Resistance_sub:
        R2_SUB = Resistance_sub
        I2_SUB = 240/Resistance_sub
        V2_SUB = 240
        
         
        V1_SUB = (240 * Turn_ratio)
        I1_SUB = (V2_SUB/V1_SUB)*I2_SUB
        R1_SUB = 3000/I1_SUB
    else:
        R1_SUB = 0
    
    if 'Resistance from modified' in Power_dictionary[Transformer_tuple]:
        Power_dictionary[Transformer_tuple]['Resistance from modified'][0] = [R1_SUB,0]
    else:
        Power_dictionary[Transformer_tuple]['Resistance from modified'] = {0:[R1_SUB,0]}

    if 'Parallel Resistance from cabeis' in Power_dictionary[Transformer_tuple]:
        Power_dictionary[Transformer_tuple]['Parallel Resistance from cabeis modified'] = {}
        for key, value in Power_dictionary[Transformer_tuple]['Parallel Resistance from cabeis'].items():
            R2_SUB = value[0]
            if R2_SUB:
                I2_SUB = 3000/Resistance 
                V2_SUB = 3000

                
                V1_SUB = (V2_SUB*Turn_ratio)
                I1_SUB = (V2_SUB/V1_SUB)*I2_SUB
                R1_SUB = 270000/I1_SUB
            else:
                R1_SUB = 0
            
            Power_dictionary[Transformer_tuple]['Parallel Resistance from cabeis modified'][key] = [R1_SUB,value[1]]
            

    Power_dictionary[Transformer_tuple]['Resistance'] = Power_Functions.Working_out_resistance_Modified(Transformer,Power_dictionary)
    
    if 'Receiving voltage' in Power_dictionary[Transformer_tuple]:
        current_supplying_at_Receiving_voltage = Power_Functions.Working_out_current(Transformer,Power_dictionary)
        #print(current_supplying_at_Receiving_voltage)
        V1 = Power_dictionary[Transformer_tuple]['Receiving voltage']
        #print('V1 op',V1 )
        I1 = current_supplying_at_Receiving_voltage
        print('I1 op',I1 )
        R1 = V1/current_supplying_at_Receiving_voltage
        #print('R1 op',R1 )
        

        R2 = Resistance
        #print('R2 op',R2 )
        V2 = V1/Turn_ratio
        if V2 > 3300:
            V2 = 3300
        #if V2 > 3000:
        #    V2 = 3000
        #print('V2 op',V2 )

        I2 = (V2/V1)*I1
            
        I2 = V2/Resistance
        #print('I2 op',I2 )
        
        Power_dictionary[Transformer_tuple]['Supplying voltage'] = V2
        Power_dictionary[Transformer_tuple]['Supply current']  = I2
        
    if is_working_backwards:
        if not 'Format_for_sub_syston' in Power_dictionary[Transformer_tuple]:
            if Support_supply_formatting:
                Power_dictionary[Transformer_tuple]['sub syston TOP'] = [
                                                                                   Transformer,
                                                                                   Resistance,
                                                                                   Power_dictionary[Transformer_tuple]['Supply current']
                                                                                   ]
            

def Radiation_collector_Module(Radiation_collector,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):
    
    Radiation_collector_tuple = tuple([tuple(Radiation_collector[0]),Radiation_collector[1]])
    #Supply_current = 10
    #Supply_current = 0.0019
    #Supply_current = 0.0000001
    Supply_current = 0.166666666666666666666666666666666
    Power_dictionary[Radiation_collector_tuple]['Type'] = 'Radiation_collector'
    if not 'Format_for_sub_syston' in Power_dictionary[Radiation_collector_tuple]:
        #print('Radiation_collector')
        
        Resistance = Power_Functions.Working_out_all_resistance(Radiation_collector,Power_dictionary)
        

        
        Power_dictionary[Radiation_collector_tuple]['Supplying voltage'] = Resistance*Supply_current
        Power_dictionary[Radiation_collector_tuple]['Receiving voltage'] = Resistance*Supply_current
        if 'current coming from' in Power_dictionary[Radiation_collector_tuple]:
            Power_dictionary[Radiation_collector_tuple]['current coming from'][0] = [Supply_current,0]
            Power_dictionary[Radiation_collector_tuple]['Supply current'] = Power_Functions.Working_out_current(Radiation_collector,Power_dictionary)
        else:
            Power_dictionary[Radiation_collector_tuple]['Supply current'] = Supply_current
            Power_dictionary[Radiation_collector_tuple]['current coming from'] = {0:[Supply_current,0]}
    else:
        sub_syston_TOP = Power_dictionary[Radiation_collector_tuple]['sub syston TOP'][0]
        sub_syston_TOP_tuple = tuple([tuple(sub_syston_TOP[0]),sub_syston_TOP[1]])
            
        Resistance = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][1]
        current_supplying_at_Receiving_voltage = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2]
        Receiving_voltage = current_supplying_at_Receiving_voltage*Resistance

        if 'current coming from Support' in Power_dictionary[Radiation_collector_tuple]:
            Power_dictionary[Radiation_collector_tuple]['current coming from Support'][0] = [Supply_current,sub_syston_TOP]
            Power_dictionary[Radiation_collector_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Radiation_collector,sub_syston_TOP,Power_dictionary)
        else:
            Power_dictionary[Radiation_collector_tuple]['current coming from Support'] = {0:[Supply_current,sub_syston_TOP]}
            Power_dictionary[Radiation_collector_tuple]['Supply current Support'] = Power_Functions.Working_out_Support_current(Radiation_collector,sub_syston_TOP,Power_dictionary)

        

        #Power_dictionary[Radiation_collector_tuple]['Supply current Support']  = Supply_current
        Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][2] + Supply_current

        
        
    if is_working_backwards:
        if not 'Format_for_sub_syston' in Power_dictionary[Radiation_collector_tuple]:
            Power_dictionary[Radiation_collector_tuple]['sub syston TOP'] = [
                                                                               Radiation_collector,
                                                                               Resistance,
                                                                               Power_dictionary[Radiation_collector_tuple]['Supply current']
                                                                               ]


def APC_Module(APC,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):   
        APC_tuple = tuple([tuple(APC[0]),APC[1]])
        #print(Appliance)
        Power_dictionary[APC_tuple]['Type'] = APC[1]
        Power_dictionary[APC_tuple]['Resistance'] = 240

        Power_dictionary[APC_tuple]['Resistance from cabeis'] = {APC_tuple:[240,0]}
        #Power_dictionary[APC_tuple]['Downstream'] = [0]
    
module_dictionary = {}  
module_dictionary['Department_batteries'] = Department_batteries_Module
module_dictionary['Engineering_batteries'] = Engineering_batteries_Module
module_dictionary['Transformer'] = Transformer_Module
module_dictionary['Radiation_collector'] = Radiation_collector_Module
module_dictionary['APC'] = APC_Module







    
