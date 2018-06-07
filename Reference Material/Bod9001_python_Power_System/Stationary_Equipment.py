import Power_Functions

def Department_batteries_Module(Department_batterie,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):
    
    Department_batterie_tuple = tuple([tuple(Department_batterie[0]),Department_batterie[1]])
    print('Department_batteries_Module',Department_batterie )
    if not Department_batterie_tuple in Persistent_power_system_data:
        Dictionary = {}
        Dictionary['Type'] = 'Department_batterie'
        Dictionary['Cangenerate_resistance'] = True
        Dictionary['Capacity_Percentage'] = 100
        Dictionary['Maximum_current'] = 6
        Dictionary['Supply_voltage'] = 240
        Dictionary['Capacity_max'] = 432000
        #Dictionary['Current_capacity'] = 432000
        Dictionary['Current_capacity'] = 6000
        #Dictionary['Current_capacity'] = 0
        Dictionary['Pulling'] = True
        Dictionary['Charging'] = False
        Dictionary['Charging_multiplier'] = 1.0
        Dictionary['If_voltage_charge'] = 0
        Dictionary['Extra_charge_cut_off'] = 240
        Dictionary['Increased_charge_voltage'] = 264
        Dictionary['Standard_charge_current'] = 0.1
        Dictionary['Charge_steps'] = 0.1
        Dictionary['Max_charging_multiplier'] = 1.2
        Dictionary['Minimum_support_voltage'] = 216
        Dictionary['Standard_supplying_voltage'] = 240
        Dictionary['Maximum_watts_support'] = 480
        Dictionary['Maximum_Current_support'] = 2
        
        Dictionary['Turn_ratio'] = 12.5
        Dictionary['Expected_output'] = 240
        Dictionary['Expected_input'] = 3000
        Dictionary['Voltage_limiting'] = 0
        Dictionary['Voltage_limited_to'] = 0
        Persistent_power_system_data[Department_batterie_tuple] = Dictionary
        
    current = 0
    Voltage = 0

    if 'sub syston TOP' in Power_dictionary[Department_batterie_tuple]:
        current = Power_Functions.Working_out_current(Department_batterie,Power_dictionary,True)
        Voltage = Power_dictionary[Department_batterie_tuple]['Receiving voltage']
    
    Resistance = Power_Functions.Working_out_all_resistance(Department_batterie,Power_dictionary)  
    Returned_list = Power_Functions.Transformer_Calculations(Department_batterie,
                                             Power_dictionary,
                                             Persistent_power_system_data,
                                             Resistance,
                                             current,
                                             Voltage,
                                             is_working_backwards,
                                             Support_supply_formatting,)
    #print(Returned_list)
    Power_Functions.Battery_calculations(Department_batterie
                                         ,Power_dictionary,
                                         Persistent_power_system_data,
                                         is_working_backwards,
                                         Returned_list[1],
                                         Returned_list[0],
                                         Returned_list[2])
 


    

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
        Dictionary['Current_capacity'] = 10000
        #Dictionary['Current_capacity'] = 0
        Dictionary['Pulling'] = True
        Dictionary['Charging'] = False
        Dictionary['Charging_multiplier'] = 1.0
        Dictionary['If_voltage_charge'] = 0
        Dictionary['Extra_charge_cut_off'] = 3000
        Dictionary['Increased_charge_voltage'] = 3100
        Dictionary['Standard_charge_current'] = 0.1
        Dictionary['Charge_steps'] = 0.1
        Dictionary['Max_charging_multiplier'] = 1.5
        Dictionary['Minimum_support_voltage'] = 2700
        Dictionary['Standard_supplying_voltage'] = 3000
        Dictionary['Maximum_watts_support'] = 9000
        Dictionary['Maximum_Current_support'] = 3
        PPSD[Engineering_batteries_tuple] = Dictionary

    voltage = 0
    current = 0

    if 'Receiving voltage' in Power_dictionary[Engineering_batteries_tuple]:
        voltage = Power_dictionary[Engineering_batteries_tuple]['Supplying voltage']
        current = Power_Functions.Working_out_current(Engineering_batteries,Power_dictionary,True)
        
    if 'Format_for_sub_syston' in Power_dictionary[Engineering_batteries_tuple]:
        sub_syston_TOP = Power_dictionary[Engineering_batteries_tuple]['sub syston TOP'][0]
        sub_syston_TOP_tuple = tuple([tuple(sub_syston_TOP[0]),sub_syston_TOP[1]])
        Resistance = Power_dictionary[sub_syston_TOP_tuple]['sub syston TOP'][1]
    else:    
        Resistance = Power_Functions.Working_out_all_resistance(Engineering_batteries,Power_dictionary)
    Power_Functions.Battery_calculations(Engineering_batteries
                                         ,Power_dictionary,
                                         Persistent_power_system_data,
                                         is_working_backwards,
                                         current,
                                         voltage,
                                         Resistance)


        
                    
                            

def Transformer_Module(Transformer,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):
    Transformer_tuple = tuple([tuple(Transformer[0]),Transformer[1]])
    Power_dictionary[Transformer_tuple]['Type'] = 'Transformer'
    if not Transformer_tuple in Persistent_power_system_data:
        Dictionary = {}
        Dictionary['Type'] = 'Transformer'
        Dictionary['Turn_ratio'] = 250
        Dictionary['Expected_output'] = 3000
        Dictionary['Expected_input'] = 270000
        Dictionary['Voltage_limiting'] = 3300
        Dictionary['Voltage_limited_to'] = 3300
        Persistent_power_system_data[Transformer_tuple] = Dictionary
    current = 0
    Voltage = 0

    if 'Receiving voltage' in Power_dictionary[Transformer_tuple]:
        current = Power_Functions.Working_out_current(Transformer,Power_dictionary,True)
        Voltage = Power_dictionary[Transformer_tuple]['Receiving voltage']

    Resistance = Power_Functions.Working_out_all_resistance(Transformer,Power_dictionary)

    
    Returned_list = Power_Functions.Transformer_Calculations(Transformer,
                                             Power_dictionary,
                                             Persistent_power_system_data,
                                             Resistance,
                                             current,
                                             Voltage,
                                             is_working_backwards,
                                             Support_supply_formatting,
        )

        
    
            

def Radiation_collector_Module(Radiation_collector,Power_dictionary,Persistent_power_system_data,is_working_backwards = False,Support_supply_formatting = False):
    
    Radiation_collector_tuple = tuple([tuple(Radiation_collector[0]),Radiation_collector[1]])
    #Supply_current = 10
    #Supply_current = 2.16666666666666666666
    Supply_current = 0.5
    #Supply_current = 0.0000001
    #Supply_current = 0.166666666666666666666666666666666
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







    
