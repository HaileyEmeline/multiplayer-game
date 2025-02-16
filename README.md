# multiplayer-game

24 January:
2:30pm
Created a Photon account
Created a new Unity project
Added Photon servers to Unity project and VS Code.
https://www.youtube.com/watch?v=SrfTnwBSIKI&list=PL_l4pPo5eSc2nHazxm_VjIrURNdWcWQpd
Began programming hosting a lobby - struggling
4:45pm

28 January:
5:00pm
Researched Photon cloud servers matchmaking methods
Researched GameLift matchmaking methods
Found documentation on Photon servers
Debugged programming
6:30pm

29 January:
1:00pm
Discovered that PlayFab, for battle royale archetypes, can only host 32 players at a time
With photon servers on their way to functionality, decided to try GameLift for the matchmaking - will give it a week of effort and, if progress cannot be made, will revert to Playfab.
Made an AWS account
Attempted to set up a GameLift fleet (host of instances) - did not have a build yet.
Began learning Netcode (a Mirror alternative)
4:00 pm

Again 29 January:
6:00pm
Learned about differences between Netcode for objects and for entities
Began programming Netcode for entities in VS code to create a server.
7:00pm

31 January:
2:00 pm
Installed Netcode for Entities into project
Added an RPC - this is how the server and client interact
Servers can send RPCs to individual clients or all clients, and any client can send them to the server.
The client can now send messages to the server!! 
Players can now join the server. There is only one server (there is no server management) and there is no gameplay yet, but multiple clients can connect to the server!!
3:30pm

3 February:
9:00pm
Installed Multiplayer Play Mode (a Unity addon) to assist with lag reduction and speed up server-client interactions
Used Play Mode to be able to run multiple windows through Unity (multiple clients) without having to rebuild the project each time (saves 2-3 minutes and storage per run)
Learned about InGame tool with Netcode for entities - this basically allows for stronger connections than RPCs. The client sends an RPC requesting InGame status to the server, which the server then receives and authenticates, adding the player.
Added the code for the client to send RPC requesting InGame.
Learned about Netcode ghosts - these sync snapshot data across the server and clients, including spawning, deleting, and transforming.
10:30pm

5/6 February:
11:00pm
Created a prefab for the player objects used in the game
Spawned in players at random positions within the game server
*** MAKE SURE playmode is in client/server and all windows are in client only! You cannot to a server if the server does not exist in the first place. This caused my half an hour of pulling my hair out for a simple simple fix.
1:30am

7 February:
7:00pm
Got movement working! Client side prediction helps remove the appearance of lag. Uses the Playmode ghost tools
10:00pm

9 February: 
10:00pm
Set up a dedicated server instead of a client hosted server. This helps with input lag, which is present in the base host server, likely because it shows both the immediate movement and server predicted movement for each player? This issue is removed with a dedicated server, which is what should be used in the final version.
11:30pm

13 February:
10:30pm
Created a way to change values/variables for each player into the server
Currently does not work - the value changes but immediately resets. This is because the server is authoritative. I need to update it in server, not just in the local client copy. This took surprisingly long for me to figure out.
New problem - this updates it for both clients connected - they do not inherently update to the same value, but each one is updated. I believe to fix this, I need to send an RPC from each client to the server, requesting it to be changed for that specific client.
RPCs are used for one-off instances, such as spawning a player, VFX, or something else. Ghosts are meant for more constant data, like player movement. Ghosts are per-entity data and use client side information, and are unreliable, while RPCs are reliable.
When a client joins late, they will not receive any RPCs sent up to that point, but they will receive the updated form of any ghost data.
The players can now shoot! Only in one direction, but this is still progress. I forgot to add the bullet prefab to the entity manager and was frustrated for a bit,
Moving forwards, my biggest goal is to work on implementing server infrastructure, but I also intend to hopefully get gravity/collisions and mouse aiming working. 
1:30am

15 February: 
1:00am
Adding version control to my game
