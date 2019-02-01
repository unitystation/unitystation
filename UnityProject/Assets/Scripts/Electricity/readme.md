### The Electrical System A brief introduction
**How does it work?**
so, Is based off V/IR and ohm's law.
It simulates each supply separately then adds them all together to get the final component 

**What controls everything?**
Electrical synchronisation is the main manager but 
Each supply so E.G an SMES manages its own connections, and Current calculations

note that Resistance is handled by the resistance source itself, the supply adds it self to a set on the resistance device and then It is updated Via InitialPowerUpdateResistance

**What is PowerUpdateStructureChange, PowerUpdateStructureChangeReact, InitialPowerUpdateResistance, PowerUpdateResistanceChange, PowerUpdateCurrentChange and PowerNetworkUpdate?**

PowerUpdateStructureChange 
is a signal to all the supplies to flush out everything. Connections, voltage Since the structure of the network has changed Making the previous simulation void

PowerUpdateStructureChangeReact 
Signals to supplies regenerate connections

InitialPowerUpdateResistance 
is Used to force resistance sources to set up the data structures for resistance values

PowerUpdateResistanceChange
Is used when a resistance source has changed resistance and needs to update the circuit values
 
PowerUpdateCurrentChange 
Is a signal to the supplies to finally calculate There voltages and currents for the cables 

note The calculation is done every time a  CurrentChange Has gone over the cable since it is more efficient than calling every cable on PowerNetworkUpdate

PowerNetworkUpdate
This Update cycle is where you can Check the voltage/Current/resistance (Since all calculations are finished) and Make your machine react appropriately for the next tick 

**what is ElectricityInput,  ElectricityOutput, ResistanceInput, ResistancyOutput,DirectionInput, and DirectionOutput**

DirectionInput and DirectionOutput
is Used per part of the network to jump from one node to the other, to determine the direction of flow 
DirectionOutput doesn't immediately execute, It is executed once The cable who called it has sent out  messages to all the other connections, It is designed to do this so it  mitigates some of the strange current paths through the network

ResistanceInput and ResistancyOutput
Following along the path designated by DirectionInput and DirectionOutput it makes its way to the supply getting modified and combined as it goes through different machinery

ElectricityInput, ElectricityOutput
Then from the supply the current is pushed to each part of the network down the same Lanes as was designated by DirectionInput and DirectionOutput, Getting split and modified been going for machinery


**what is InLineDevice, PoweredDevice, PowerSupply, WireConnect and DeadEndConnection**

InLineDevice  Allows a modification of ElectricityInput,  ElectricityOutput, ResistanceInput, ResistancyOutput, This could be used so let's say you want something that reduces the resistance, increases the current like a transformer,  or something like a resistor since the entire network is technically parallel this it would make it in series with the resistance.
It calls back to the controlling device for what to do

PoweredDevice
Is Your simple resistance load like an APC, it will ensure DirectionInput and DirectionOutput will terminate at the device 

PowerSupply 
is modified so it can handle setting up  DirectionInput and DirectionOutput and  ElectricityInput, ElectricityOutput Management, It's a supply it has somewhat more control than the others

WireConnect 
Is used on cables and connectors, It has modifications so it can make a cableline making calculations skip long sections or cable where values will be the same so therefore saving processing time

**what Data does each part of the electrical network contain?**
**IntrinsicElectronicData**
Contains 

Categorytype 
What type of machine or cable, APC, SMES, ect.

CanConnectTo 
What devices is it able to connect to

ConnectionReaction 
What resistances are given to which connection so Maybe you want to display 5000 ohms to a low voltage cable while a low voltage machine connector you want to display 1000 ohms this is how you would do it

ControllingDevice
Is a IDeviceControl of the machine controlling that section of the network

ControllingUpdate
Is a way of accessing the updating cycles of the machine controlling that section of the network will be null if controlling device doesn't have updating cycles


**ElectronicData**
Contains 

ResistanceToConnectedDevices
Stores what supplies are connected to this section through what local connection, 
note: This will only be on things like APCs and things that have resistances

connections
Simply a list of all the neighbours

CurrentGoingTo (needed for calculating current)
According to each supply (int)
Which connections are receiving current and how much

CurrentComingFrom
According to each supply (int)
Which connections current is coming from and how much

ResistanceGoingTo
According to each supply (int)
Which connections are receiving resistance and how much

ResistanceComingFrom
According to each supply (int)
Which connections resistance is coming from and how much

Downstream and Upstream
According to each supply (int)
is Used for the direction of flow, calculated from the supply outwards 

FirstPresent
Used for calculations a voltage and Current 
(Since current can only flow 2 ways, you have to work out what's plus and minus so this is based on which supply is first present)

ActualCurrentChargeInWire
is the Store of CurrentInWire and ActualVoltage and EstimatedResistance but in a class

SourceVoltages
According to each supply (int)
What's the voltage on the line