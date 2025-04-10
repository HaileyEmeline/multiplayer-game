January 24
2:30pm - 4:45pm (2.25 hours)

Once my initial plan was finished, I began gathering the materials I need to set up multiplayer servers. I created the Unity project and created a Photon account, where I planned initially on hosting the servers. I then added the Photon extensions to the Unity project and VS code. I finished the day by attempting to program a lobby, but ultimately I struggled quite a bit with this.

January 28
5:00pm - 6:30pm (1.5 hours)

Still unsure of where to really start and slightly overwhelmed by the number of options I had, I researched Photon server methods as well as GameLift server methods. I found and collected documentation on Photon servers, and debugged my code from the 24th.

January 29
1:00pm - 4:00pm (3 hours)
6:00pm - 7:00pm (1 hour)

My initial plan utilized Microsoft Playfab for the matchmaking infrastructure. I began today by researching Playfab, and while it feels doable, I discovered that for the battle royale archetype (which is the type of game I am creating), it can only host up to 32 players at a time. Unsure of how to connect players to the photon servers without Playfab, I began looking into AWS (Amazon Web servers) as an alternative. I had initially not featured these as a consideration due to pricing and lack of documentation with Unity, but I decided to give myself a week to try them out and, if I do not make progress, I can revert to Playfab. 

I began my AWS journey by making an account. I immediately jumped in to trying to set up a GameLift fleet to manage and run the servers, but I did not have a working Unity build yet. I needed to upload a Unity project to be connected to the server. In retrospect at the end of the project, this is where I made my first mistake, as I should have just uploaded a barebones project with nothing but the code to connect to the server and built the game itself from there. But I did not realize this was a possibility, so I began my research on Netcode to build the actual in-game connections.

I researched the differences between Netcode for Entities and Netcode for GameObjects. Netcode for Entities (NFE) uses prefabs and entities for everything within the game, while Netcode for GameObjects (NGO) uses the typical game objects. I only had experience programming with game objects, but using entities allows for more players connected and faster connections, so I dove in with NFE. This is another choice that, in retrospect, I regret; NFE is a very new system with almost no tutorials or documentation, and it made my programming infinitely harder.

I then started a new project to begin fresh without the Photon connection code I already had. 

January 31
2:00pm - 4:00pm (2 hours)

I installed NFE into this new project and began learning how to set up connections. I added an RPC, which are remote procedure calls, and are basically short messages sent between the server and client to get them to interact. Servers can send RPCs to individual clients or all clients, and any client can send them to the server. After some work, the client can now send messages to the server!

With a bit more work, I set it up so that the game creates a local server world (this is just a local world within Unity, not an actual server). The players are now able to join the server - there is no gameplay yet, but multiple clients (as long as they are all from within the Unity project) can connect to the server. I tested this by connecting once through the editor and once through a build. These do not actually show up within the game, they are just two little messages confirming they were able to join.

February 3
9:00pm - 11:00pm (2 hours)

When I had tested the server joining a few days before, I had to create a build, which is a lengthy process. Therefore, I installed Multiplayer Play Mode, a Unity add-on, to run multiple windows within Unity simulating builds; therefore connecting multiple “clients” all from within the Unity editor. This saves quite a bit of time each time I try to test the game, and allows me to simulate lag, client connections and disconnects, and more. 

I then worked on setting up the InGame connection with Unity for clients with the server. This is a stronger connection than RPCs give us, as instead of simple messages back and forth, this basically ties the client in and sticks them with the server. I set this up by having the client send an RPC requesting InGame status to the server, which the server then receives and authenticates, adding the player. 

I ended the day by learning about Netcode ghosts. These sync snapshot data across the server and the clients, including spawning, deleting, and transforming.

January 5-6
11:00pm - 1:30am (2.5 hours)

I created a prefab for the player objects used in game. Prefabs are basically used to have one “object” spawn multiple times within the game, as well as spawning late and not needing to be created at start. I made the player a prefab of a capsule shape, and then spawned players in at random positions within the game server.

A note to myself: *** MAKE SURE playmode is in client/server and all windows are in client only! You cannot to a server if the server does not exist in the first place. This caused my half an hour of pulling my hair out for a simple simple fix. *** 

February 7
7:00pm - 10:00pm (3 hours)

After quite a bit of testing and issues, I was able to get movement working with the players I have set up! I also set up client side prediction - this means that, when a client tries to move, instead of waiting for the server to adjust the player movement, the client “predicts” where in the game world they will end up. If the client is incorrect, it will adjust the player position back to where the server has it. This reduces the appearance of lag client side, and uses the Playmode ghost tools.

February 9
10:00 - 11:30 (1.5 hours)

I set up a “dedicated server” instead of a client hosted server within Unity. This is not actually a server that can be used outside of Unity, but means that a client does not have to be the one hosting the server, which helps with input lag. This happens because, when a client hosts the server, it shows both the predicted movement from the client and the actual movement from the server, making it look buggy. The issue is removed with a dedicated server.

February 13-14
10:30pm - 1:30am (3 hours)

I created a way to change values and variables for each player, and send that information to the server. Initially this did not work, as the value changed but immediately reset. This is because the server is authoritative. I need to update the value within the server as well, not just in the local client copy, which was surprisingly difficult to figure out. 

I then had a new problem - the same value updates for both clients connected - they do not inherently update to the same value, but both are updated when only one should be at a time. To fix this, I had to send an RPC from each client to the server, requesting that value to be changed for that one specific client.
RPCs are used for one-off instances, such as spawning a player, effects, or something else. Ghosts are meant for more constant data, such as player movement. Ghosts are per-entity data and use client-side information, and are unreliable in theory, while RPCs are reliable.
When a client joins late, they will not receive any RPCs sent up to that point, but they will receive the updated form of any ghost data.

After I got these issues sorted out, the player was able to shoot! They can only shoot to the right, but being able to spawn objects from the players was a huge step forwards. I forgot to add the bullet prefab to the entity manager which was frustrating until I figured it out.

February 16
1:30am - 2:30am (1 hour)

I added version control to my game and added a github repository for my game. There is a very specific way to do this for Unity projects, using Github desktop and through the terminal.

February 17
2:00pm - 5:30pm (3.5 hours)
8:00pm - 9:00pm (1 hour)

I began working on physics for my game, so that the player can fall and eventually jump. Physics do not operate the same as they do in simple single player, because of the server prediction, and because the player is an entity instead of a game object. There is surprisingly little documentation for it in NFE.

I had to install the Unity Physics package to get this to work, which refused to install in the Unity editor. I had to clear the cache of my project, which broke the project temporarily until I re-installed all of the assets. Once it was installed, I wrote a script to give colliders to entities, as I believed that Unity only had built-in colliders for objects. This was functional, but meant that for everything I add or modify, I have to manually program in the values for their colliders.

The player now has gravity acting upon it! I initially used 3D colliders despite it being a 2D project, because NFE does not support 2D and was made for 3D. The players cannot jump, but can move side to side and fall. Currently, upon hitting a wall, they rotate into the third dimension, which I need to fix.

I then spent some time later this evening researching Unity Multiplay and Unity Matchmaker as alternatives to Playfab/AWS Gamelift. I believe that, as they are made through Unity, they may be easier to implement.

February 20
8:30pm - 10:30pm (2 hours)

I began today by researching Unity Physics with ECS (entity control system, what netcode for entities is built around) to better optimize physics. I changed some values so that the player cannot clip through the ground if they hit another player, but still have the rotation issue.

I then turned the ground into a prefab, which helps deal with my manually programming the colliders in; now, if I want to change the size of the ground, I simply need to add more entities from the prefab, and these will come with the same built-in collider. Lastly, I was able to fix the rotation, by locking the quaternion rotation of the player entities.

A note from myself: I now have a much greater understanding of how ECS works (entities) - I think I understand their physics, collisions, etc.

February 21
12:00pm - 4:30pm (4.5 hours)

I added a file called PlayerAspect, which was used to hold (at least some) values about the player, which could be read by other files. This currently does not work, but I have the infrastructure set up to get it working in the future. 

I finally was able to implement jumping! I had been using 3D rigidbodies and box colliders, which are the typical colliders and body mechanics in Unity, but for entities you can use physics bodies and physics colliders. This was part of Unity Physics which I had previously installed, and works with prefabs and entities! To use these, I had to rewrite large portions of my existing movement system to be entirely based in PhysicsVelocity. Now, jumping is functional most of the time. Sometimes, for some reason, the jump button is registered but the player does not actually jump.

February 22
10:00am - 11:00am (1 hour)

After some research, I modified the jump code to use Unity’s built-in keybind functions instead of the manual GetKeyDown programming. This fixed the jumping!

February 24
5:00pm - 6:30pm (1.5 hours)
10:30pm - 12:00am (1.5 hours)

I began working on updated shooting, which will use the position of the mouse to aim towards where the client clicks, instead of only pointing forwards. This is much more difficult than it typically would be, as we do not have easy ways to store the position of the mouse or player in relationship to each other. This is largely because the player should ideally be centered, with the camera following the player, instead of being locked to a certain position. Then, I would lock the camera onto the player before aiming, setting the position to (0,0) for the player, and finding the position of the mouse in relation.

I spent much of my time today trying a different method, which was storing the player location with the fixed camera and calculating the difference between the two. I eventually realized this should not be my focus - I need the camera to follow the player.

I then began researching how to fix the camera to the player. I was very demoralized by the end of this process, as I did not know how to store the position of the local player to keep the camera trained to it.

February 25
6:00pm - 8:30pm (2.5 hours)

While trying to test camera settings, I discovered that the jump was not localized to one player. This means, when one player jumps, every connected player will also jump. I updated the function to now only work with the local ghost - the issue had been that the jump code had been in a file directed by a Simulate tag, which means what happens within the file happens everywhere in the server. The way I have inputs set up is that I take the input in a file that is client specific, and then have the code that actually sets what happens once an input is received into a file that is server-wide, because it will be the same for each client (when they are the one to create the input). By moving the jump input code in the client file, I was able to fix this issue.

I then installed Cinemachine to try and help with managing the camera. When I first installed it, I believed that I had broken my project - it turns out that Cinemachine is a 3D camera being used in a 2D project, and that it defaulted to the incorrect z axis, meaning that all of my entities were “behind” the 2D plane. I then fixed this, but was unable still to sync the camera to the player.

February 26
2:00pm - 4:00pm (2 hours)

I updated my PlayerAspect script to hold the actual player position in the world, as well as the physics components used for the player. Using this, I moved the camera to always go to the local player position. I also had to lock the camera to -1 on the z axis. Once I had done all of this, the camera began following the local player!

February 27
12:00am - 4:30am (4.5 hours)

I began working on getting bullets to shoot towards the mouse pointer. At some point in my past week or so of programming, I had broken them, and they did not shoot at all. I was able to fix them pretty quickly (I did not document what was the issue with them). After some attempts, the bullets began flying towards the mouse position, but they were very broken. They were updated on server positions instead of client positions, meaning they would begin flying towards the mouse position before veering off into the direction that they are from all connected clients combined.

I spent two hours trying to put the shooting onto the client and, despite the code seemingly being identical to the local client code I had for my camera, it refused to work. I was very demoralized after this and went to bed very stressed.

March 3
12:00am - 1:30am (1.5 hours)
6:30pm - 8:30pm (2 hours)

I wrote a script which calculates the position of the mouse pointer in relation to the position of the player entity. This works.

I then began trying to implement the code into the client and server management. Somehow, the code that actually moves and destroys bullets was broken once again in this process, but I was able to fix it once again. The client now shows the correct aiming direction calculated through the mouse in relation to the player! However, the server does not receive these floats yet.

March 4
12:00am - 4:30am (4.5 hours)

I began working on carrying the messages from the clients to the server. I accidentally removed the client independence, meaning that each one impacts the other. There is a very fine line between having client-specific inputs and having them managed by the server.

I can now add the position to a IComponentData, which is how variables are passed from the server to the clients. However, the variables still do not properly get passed for some reason. While working on this, I once again broke and fixed the shooting.

After scrolling through documentation for a while, I stumbled across a random video from years ago that was on an entirely different topic but had the solution to my issue! I had to change the IComponentData to an IInputComponentData, because the position of the mouse is counted as an input. I had not even previously known that IInputComponentData was a thing. The server now reads the correct angle, but shoots in the wrong direction despite that - this was because of a failsafe I had previously set up dictating that if the length of a float was zero, I would set the angle to (1, 0, 0). I do not know why this was being tripped, the length of the float was not zero, but removing it fixed the shooting! It finally worked! 

March 17
2:30pm - 4:30pm (2 hours)

Spring break! I began by reviewing my code, as it had been a minute. I researched lobby and server infrastructure to figure out which to work on next; now that I have finished the game elements I wanted to add to show I understood netcode for entities, I am lost as to where to go next. I created a Unity Dashboard and linked it to my project - this is where I will be creating both lobbies and multiplayer server infrastructure.

I then wrote the code to create a simple lobby with up to four players and a distinct lobby ID. I also wrote the code to ping the lobby every 20 seconds, because without that, the lobby would disappear and be unable to be searched by those not already in it. I began writing the code to join a lobby with a lobby code/lobby ID as well. 

The lobby does not work, because I need a create lobby button, a join lobby text box that lets you put in the code, and a play button to let both players begin the match.

March 18
1:00pm - 4:30pm (3.5 hours)

I began working on the start button, which creates a server if a player is the first player online, and joins one if it already exists. This somewhat works; each player can create servers, and the join server is functional - the only issue is that the player does not know whether or not the server is already started, because they cannot ping the server to check before they are in the server. Therefore, each player tries to start their own server, which causes issues.

I might separate this into two buttons, a start button and a join button. I will then try to move forwards with either lobbies or matchmaking. I was very stressed about being able to finish at this time.

March 19
12:00pm - 4:00pm (4 hours)

I created a separate startup scene which takes the players into the main game scene. This will eventually be where the lobby exists before matchmaking takes place. I temporarily disabled the play button. 

I then paid for Unity Game Server Hosting (UGS - used to be Multiplay). I then began researching how to set up Multiplay, which required me to make a build for my game. This was impossible because of experimental bits of code that only work in the Unity editor but not in builds. These bits of code were hidden throughout my files; each time a build failed, it only alerted me of one at a time. Therefore, this was a long and tedious process.

March 22
2:00pm - 6:30pm (4.5 hours)

I altered some settings and was finally able to get the build working! I then researched how to get UGS working in my project - since I already have a server joining system, this is difficult. It would have been easier to set this up first and then built the game around it, but I did not know this. 

I created a build configuration in UGS and a fleet, which is how servers are managed and run. This was basically setting up the specific settings for servers. I had to do research to set this up optimally. Next, I set up Unity Matchmaker with the logic needed to place players into the correct servers when trying to join a game. This is not currently working within my project, but once I figure out how to integrate it within Unity itself, it’ll have the logic already determined.

To finish the day off, I updated my game to run on Linux servers instead of Windows, which is cheaper (and the better option) for UGS.

March 23
1:00pm - 5:00pm (4 hours)

I continued my research on how to hook Unity up to external servers by downloading the only existing sample project that uses Netcode for Entities and UGS. I dug through the code for a while, trying to understand how it worked. Both files that looked helpful had been removed in previous versions, as a more complicated system had been put in place. 

I installed the SDK for UGS into my Unity project and finally found some documentation for it. I tried to write my own version of the code using the same project, but ran into some problems. The code sets up the client initially through matchmaking in the sample project, before using the matchmaking service to either join or create a server depending on which is needed. This was too many steps at once for me to tackle, so I researched proton servers instead. Despite UGS being made for Unity, it is harder to set up, and I believe if I am running out of time I might try to switch to Photon.

March 24
12:00pm - 6:30pm

I wrote two separate functions for joining the client to the Multiplay server instead of the local server, neither of which worked. Every bit of documentation for UGS is only functional in Netcode for GameObjects, which features entirely different functions. Therefore, linking the server is going to be very difficult.

I began feeling very discouraged after hours of scrolling through forums and finding nothing. Eventually, I set up a system for sending the connection data from multiplay to the game itself, before I realized I was currently handling it all within one file. I wrote a server script that is separate from the GameBootstrap script, which has been joining the server. Initially I tried using the same functions as Netcode for GameObjects, as I was trying to do this within the startup scene which I hoped was independent from the other scene, but this did not work. I also tried sending the port information with an IComponentData, but this threw an error.

A note I had written: 
Ideas moving forward:
Try making the server command functions first, with the server creation and find server query. Then try to join one through client side
Try using matchmaker client side and using that to create servers?
Research more on deployments
I am incredibly frustrated and stressed that I will not be able to finish this project prior to the end of the semester.

March 26
2:00pm - 3:00pm (1 hour)
7:30pm - 10:00pm (2.5 hours)

I figured out what each line in the Netcode for GameObjects documentation did for server creation, and began trying to convert them into Netcode for Entities.

When I returned to the project later in the day, I wrote a script that works with entities and tries to find open servers. To test this, I had to re-build the project and upload this new build to UGS. This takes quite a while; I decided not to test anything until I am fairly confident in my solution.

I learned about test allocations, which is manually making a machine (which hosts servers) through UGS instead of using Matchmaker to start them. I believe this is a good first step, and if I have time afterwards, I can integrate Matchmaker. I now have variables that find a server, gets the IP address and port, and saves them to variables that I now need to pass through GameBootstrap somehow. I feel much more confident in my amount of knowledge; even if I do not finish, I feel like I now understand the process and can make leaps of progress.

March 28

I forgot to log my time today and do not remember exactly what I did; I just wrote down the date, and Unity shows that I worked on the project today. I think it is likely I created and ran a test allocation with my build to test it.

March 31
3:00pm - 4:30 pm (1.5 hours)

I went back through and documented my server code so that I could quickly access and remember anything I needed to within the server linking process. Then I created a new built to test this, which required my changing the build settings once again. 

April 2
12:30pm - 5:00pm (4.5 hours)

I updated my build in Unity cloud, syncing my cloud and game code. Then I created a new test allocation and started a dedicated server. The game would not run, but I fixed this issue by waiting for EntitiesReferences to be generated (which happens after connection) before updating the files which use it in the shoot system.

Upon fixing this, I created yet another build and test allocation. There are no longer any errors upon launch, but nothing happens in the project, which is very demoralizing. I have the server up and running, and the code to join a local server; I just need to understand how to put them together. After researching my code a bit more, I believe that the auto initialization I use in Game Bootstrap is not correct. I have a different way of connecting with the Play Button I had made; I think I need to adapt this to work with Multiplay.

April 3
10:00pm - 12:00am (2 hours)

I went through each of my files, mostly my play button file (UINetcodeForEntities), my GameBootstrap file, and my Server file. In each, I wrote down what the core components do, and what I think could be useful for connecting the servers.

I then researched methods of transportation, to send data between files, so that I could send the port and IP to the GameBootstrap file. Following this, I went through two tutorials on using NGO with UGS, and mixed their code with mine. I wrote 185 lines of notes to myself, contrasting my code with theirs, figuring out how to implement the NGO files in NFE, and more. 

This was far and away the most beneficial bout of work I have done since beginning to work on server connection. I believe that I now understand the structure that I need to follow, some of the lines of code I will need, and more. I believe I will use just one file for both my connecting to the Multiplay server and starting the Unity game. 

April 5
3:30pm - 6:30pm (3 hours)

I completely rewrote my networking code using the notes I had taken. It is now all within one file, and is the closest to correct I have been yet I believe. I built the project once and created a new build in Unity Cloud, as well as a new test allocation. Upon testing the game, clients still do not connect, but the CPU usage of the dedicated server in Unity Cloud increased briefly, making me think that the servers are now connected and it is just the clients that cannot join. This is a huge step forward, and while I am very lost as to how the clients will join, I believe I am close to a solution. That being said, I am no longer enjoying working on this one problem; I have spent so many hours on one issue that it is getting to be a drag.

April 7
8:30am - 11:00am (2.5 hours)
6:30pm - 9:00pm (2.5 hours)

I created this entire document, using the much worse documentation I had previously kept and making it much neater and tidier. For connecting clients, I have also decided that I will attempt to use Matchmaker, skipping the step of connecting them manually and instead utilizing a tool meant for placing clients into servers. I might try some other tactics tonight instead, but I am very lost as to where to go without this step.

I then went back and did some further attempts at connecting the server and client. I discovered that I was wrong about the server connecting, but found which line is seemingly causing the issue. This is very frustrating because the only part from documentation/tutorials that is universal is two lines, and despite having the lines the same as everywhere, they throw an error. However, while they do not work on the local Unity client side, they do work on the UGS server engine logs, which makes even less sense. I am demoralized but at least know where in my code the (first) problem lies.

April 8-9
11:30pm-2:30am (3 hours)

I attempted multiple fixes for this issue. The problem is that I initialize a service in Unity but, despite waiting for it to be completed, when I try to call it nothing happens. I have tried waiting between when it is initialized and called, checking to see if that line of code has run (it has), moving the initialization to earlier in the function, and more. I even posted my issue on a forum, because it makes so little sense to me, and I need whatever help I can get. I am very discouraged with my project; at this point, finishing is obviously a priority for me, but just fixing this is going to be one of the larger computer science achievements in my life, if I can do so.

April 9
+15

I began today by restructuring my server connection code to divide into the server and client immediately, and then only run the connection code on the server. Naturally this did not work; this was my final idea for what to do. This is the first computer science error I have come across where I genuinely believe something is wrong with the packages or something outside of my control; I have everything correct. I then tried using older versions of UGS in Unity, including Multiplay, which has been deprecated for a year. None of these work either. 
