You need only 2 files, "OSCsender.cs" (in the main folder) and "OSCHandler.cs" (in ...\OSC_hmd-3daudio\UnityOSC\src). You have to put both scripts in the main camera object. In OSCHandler
 the only thing you may need to change is the port number, which is somewhere in the code (Lui can help you with that). OSCsender is where we select what we want to send and all the 
calculations required are made, the only thing you need to change is the name of the source object, which in our case was "droneplayingsound". 
