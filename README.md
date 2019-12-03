 PtP-chat
 ---------
person-to-person chat using TCP protocol 
subject to a network connection, initialization runs when program start:

- connection requests are sent to all nodes on the network using a specific port
- creating the number of threads corresponding to the number of nodes
- in each separate thread a TCP connection is established 
- the message sent by the user is a shared resource and all threads use it
- in this way, implementes a semblance of client-server architecture without a server and TCP security
