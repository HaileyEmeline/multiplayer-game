## Final Project Writeup
##### Hailey Schoppe - Multiplayer Independent Study

### Code Walkthrough

##### Server Connections

I am using the Netcode for Entities (NCE) package within Unity to handle server connections and interactions between the clients and servers. Adding the package to the project provides the tools to create a connection, as well as a server world and client world within the project. The __server world__ hosts everything that is server-authoritative, such as enemies or world settings, while the __client world__ is what the players interact with.

To make the connection between the client and server worlds, I initially used NCE's build in rapid initialization. This uses Unity itself as a local server, by overloading the initialization code to include a server port (7979 is the local Unity 'server'). This code is found in GameBootstrap.cs, and looks as such:

```
public override bool Initialize(string defaultWorldName) {
  AutoConnectPort = 7979;
  return base.Initialize(defaultWorldName);
}
```

While the client is now connected to the server, nothing can really be done between the two, outside of sending simple values from one to the other using RPCs. To synchronize data, to be able to spawn objects and store player position, we need the NCE __InGame__ system, which is how NCE synchronizes data. When a new client connects, they mark the connection to the server as InGame, and then send an RPC (InGameRequestRPC) to the server. The server then receives the RPC, and marks the connection to the client as InGame. 

The client code:
1. Loops through each existing entity with a NetworkID (each client) in the foreach loop
2. Adds the NetworkStreamInGame component to each
3. Creates an RPC to send to the server
4. Sends the RPC to the server
5. Cleans up the entities, using the Entity Command Buffer

```
//Creates an entity command buffer
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        //Loops through clients not yet with InGame status
        foreach ((RefRO<NetworkId> networkId,
                 Entity entity) 
                 in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess()) {
                    
                    //Adds InGame status to client
                    entityCommandBuffer.AddComponent<NetworkStreamInGame>(entity);
                    UnityEngine.Debug.Log("Setting Client as InGame");

                    //Creates an RPC
                    Entity rpcEntity = entityCommandBuffer.CreateEntity();

                    //Sends RPC to server, requesting to go In Game.
                    //Sends the server the player ID for backfill
                    entityCommandBuffer.AddComponent(rpcEntity, new GoInGameRequestRPC {
                        AuthPlayerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId
                    }); 

                    entityCommandBuffer.AddComponent(rpcEntity, new SendRpcCommandRequest {
                        TargetConnection = entity
                    }); 

        }
        
        entityCommandBuffer.Playback(state.EntityManager);
```

The RPC is defined as such, with the PlayerId being used for backfill later on with Matchmaking:

```
public struct GoInGameRequestRPC : IRpcCommand {
    public FixedString64Bytes AuthPlayerId;
}
```

We still need the server to listen to the RPC and add the InGame status from the server side. The process is similar on the server side: We loop through any incoming RPCs, and then add the NetworkStreamInGame component to their entities, before destroying the entities.

```
foreach ((RefRO<ReceiveRpcCommandRequest> ReceiveRpcCommandRequest, RefRO<GoInGameRequestRPC> goInGameRequest,
                  Entity entity) 
                  in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequestRPC>>().WithAll<GoInGameRequestRPC>().WithEntityAccess()) {

    var sourceConnection = ReceiveRpcCommandRequest.ValueRO.SourceConnection;
                      
    //Add the Ingame component to the network connection
    entityCommandBuffer.AddComponent<NetworkStreamInGame>(sourceConnection);
                      
    UnityEngine.Debug.Log("Client Connected to Server");
    entityCommandBuffer.DestroyEntity(entity);
  }

  entityCommandBuffer.Playback(state.EntityManager);
```

With this system, clients can now connect to the local servers. They do not have any appearance or form, but are listed as entities within the server world. 

#### Player Entities

To give the player an actual form, we need to use the ghost feature of NCE. Netcode Ghosts synchronize all snapshot data between the client and server, including spawning, destroying, and transforming (moving, rotating, etc) the client. Therefore, we will make each player a ghost. To begin this process, we create a player entity in the Unity editor and bake it, which is converting it from a GameObject to entities. We then add a Player and Ghost to the player entity, which we convert into a prefab in the Unity editor.

```
public class PlayerAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerAuthoring> {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player());

            AddComponent(entity, new PlayerInputStateGhost());
        }
    }
}
```

Since we set the player to a prefab, which we must do to use the ghost package, we also must convert the prefab into an entitiy instead of a GameObject. This code does this for both the player and bullet prefabs; I will return to the bullet shortly.

```
public class EntitiesReferencesAuthoring : MonoBehaviour
{

    //For the player prefab
    public GameObject playerPrefabGameObject;

    //For the bullet prefab
    public GameObject bulletPrefabGameObject;

    public class Baker : Baker<EntitiesReferencesAuthoring> {
        public override void Bake(EntitiesReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences {
                playerPrefabEntity = GetEntity(authoring.playerPrefabGameObject, TransformUsageFlags.Dynamic),
                bulletPrefabEntity = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct EntitiesReferences : IComponentData {
    public Entity playerPrefabEntity;
    public Entity bulletPrefabEntity;
}
```

Now, to spawn the actual player object with the prefab entity, we need to return to the GoInGameServerSystem script that we used to listen for RPCs and add InGame to the client from the server side. We get the entities references created above:
```
EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
```
And then create the player entitiy, instantiating it with the player prefab entity within EntitiesReferences, and lastly setting the client to a random starting position:
```
Entity playerEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerPrefabEntity);
                      
//Sets random location for player upon spawn
entityCommandBuffer.SetComponent(playerEntity, LocalTransform.FromPosition(new Unity.Mathematics.float3(UnityEngine.Random.Range(-8, +8), 0, 0)));
```

We now need to add the ghost owner itself to the player entity. To do so, we need to find the NetworkId for the client. Since we sent the RPC requesting InGame status, we can find the source connection of the RPC, and that is the NetworkId. Assigning the ghost owner to the player is very important: it allows us to use prediction, synchronizes the server and client, and having the client as the ghost owner allows for the prediction to only occur locally.

Prediction works as such: When the player submits an input for the client, such as moving, the server will not move the client until it has received and processed the move request. This could take a fraction of a second, meaning that it would look slow for the client itself. Prediction therefore allows us to immediately move the client entity in the same way the server will before the server does; then, once the server does, it corrects if there is any discrepancy. 

We add the ghost owner: 
```
NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ReceiveRpcCommandRequest.ValueRO.SourceConnection);

entityCommandBuffer.AddComponent(playerEntity, new GhostOwner {
  NetworkId = networkId.Value,
});
```

#### Player Movement

To make the player move, we once again need to "author" the movement system, which is where we bake it and add the needed components to it. For the movement, we are also adding an IInputComponentData, which allows us to pass input-related information between files easily. In this file, we store three things: an __inputVector__, an InputEvent for jumping, and an InputEvent for shooting.

We now have another script, NetcodePlayerInputSystem, which runs only in the ghost input system group, which is client-side. We require this code to have an InGame connection prior to running with the line ``` state.RequireForUpdate<NetworkStreamInGame>() ``` within the OnCreate of the file.

In the update function, we iterate through all player inputs. To keep them local to the client who sent them, it is important we use the ```.WithAll<GhostOwnerIsLocal>() ```. We now check for the actual keys being pressed, and if the left or right keys are, we modify the x value of the input vector by 1:

```
        foreach ((
            RefRW<NetcodePlayerInput> netcodePlayerInput,
            RefRW<MyValue> myValue,
            RefRW<PhysicsVelocity> physicsVelocity,
            RefRW<PhysicsMass> physicsMass)
            in SystemAPI.Query<RefRW<NetcodePlayerInput>, RefRW<MyValue>, RefRW<PhysicsVelocity>, RefRW<PhysicsMass>>().WithAll<GhostOwnerIsLocal>()) {

            //Simple left/right movement code:
            float2 inputVector = new float2();

            if (Input.GetKey(KeyCode.A)) {
                inputVector.x = -1f;
            }

            if (Input.GetKey(KeyCode.D)) {
                inputVector.x = +1f;
            }
```

We now need a system that actually moves the entity upon receiving these inputs and the input vector being changed. This is done in NetcodePlayerMovementSystem, which is also where we implement prediction. To move the player, we need to use another aspect of Netcode for Entities - Unity Physics. Entities cannot have rigidbodies or colliders in Unity 2D - we could work around this by supplying 3D colliders to the 2D entities, but the more optimal solution is to use the colliders provided in Unity Physics. However, this also means that our movement will be controlled through Unity Physics, and not the traditional Unity movement. The code for this is actually quite straightfoward, we simply update the velocity by multiplying the vector by an arbitrary movespeed value:

```
float moveSpeed = 10f;
float jumpImpulse = 10f;

physicsVelocity.ValueRW.Linear.x = netcodePlayerInput.ValueRO.inputVector.x * moveSpeed; 
physicsVelocity.ValueRW.Linear.z = netcodePlayerInput.ValueRO.inputVector.y * moveSpeed;  
```

To add player jumping, there are a few viable methods. RPCs are meant for one-off events, such as inputs; but InputEvents are built around inputs, and deal with differences in frame rates between the client and server. Using InputEvents is quite straightforward; we already initialized it within the bake, so now we simply have to set it. I do so here:

```
if (Input.GetAxisRaw("Jump") > 0) {
  netcodePlayerInput.ValueRW.jump.Set();
} else {
  netcodePlayerInput.ValueRW.jump = default;
}
```

And then, to move the player:
```
if (netcodePlayerInput.ValueRO.jump.IsSet && Mathf.Abs(physicsVelocity.ValueRW.Linear.y) < 0.01f)
{ 

  physicsVelocity.ValueRW.Linear.y = 0; 

  //Modifies the physics y value of the player by the jump height
  physicsVelocity.ValueRW.Linear.y = netcodePlayerInput.ValueRO.inputVector.y + jumpImpulse;

  Debug.Log("Jump! ");
} else
{
  netcodePlayerInput.ValueRW.jump = default;
}
```

The second half of the if statement determines that the player is not already jumping; the first half checks if jump has been set.

#### Shooting

Shooting is initially a very similar system to jumping: we have it set as an InputEvent, where it can either be Set or put to be Default. The differences are that we have to create a bullet entity every time the shoot is called, and calculate the position of the mouse relative to the player to aim.

The first of these is fairly easy. In ShootSystem.cs, we detect if shoot is Set in the same way we did in jump. Then, instead of impacting the player physics, we spawn in a bullet entity. Remember that the bullet entity has been authored at the same time as the player entity. Next, we set the bullet to spawn at the position of the client. Lastly, we also need to set up prediction for the bullets, by providing a ghost owner. 

```
Entity bulletEntity = entityCommandBuffer.Instantiate(entitiesReferences.bulletPrefabEntity);

//Spawn it onto player
entityCommandBuffer.SetComponent(bulletEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));
entityCommandBuffer.SetComponent(bulletEntity, new GhostOwner { NetworkId = ghostOwner.ValueRO.NetworkId });
```

Figuring out the direction to aim is a bit more complicated. Currently, we have the camera set to static, with the players moving unattached to it. However, as multiple clients join, we need each to have the camera assigned to the player entity, so that if they are in different areas of the game world, they can see themselves. To do this, we first need to store which client is local, and then what their position is.

To do this, I made CharacterAspect.cs, which is an IAspect file. We attach this file to the player, and it stores all of the pertinent information about the player, including position, NetworkId, and more. Using this, we can very easily find the local client, and set the camera to it's position:

```
foreach (var character in SystemAPI.Query<CharacterAspect>().WithAll<GhostOwnerIsLocal>()) {
  var playerPosition = character.Transform.ValueRO.Position;

  //Move chamera to local client
  camera.transform.position = new UnityEngine.Vector3(playerPosition.x, playerPosition.y, offset.z); 
}
```

The offset.z in the z position is important, as this is a 2D game in a 3D environment. The entire game exists at a z of -1, and is flat, meaning that at any other z you will not see the game at all. This makes sure the camera is properly aligned to see it.

Now the aim direction must be calculated. In PlayerMouseState.cs, I get the mouse position every time the mouse is clicked with this line: ```UnityEngine.Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);```. This calculates the position of the mouse from the camera, which is set to (0,0,0) constantly, as the player is always centered. To ensure that I have the correct angle, I subtract the player position from the mouse position and normalize it. ```UnityEngine.Vector3 aimDirection = (mouseWorldPosition - playerPosition).normalized;``` The normalize function ensures that it points in the same direction, but that the magnitude is 1, meaning that forces can be multiplied by it and extended at exactly the intended force, and in the right direction.

This is exactly what we do with the angle. In BulletSystem.cs, we first ensure that we are only running on the local client, again with CharacterAspect. Then, we multiply the aim direction by movespeed to move the bullet: ```localTransform.ValueRW.Position += aimDirection * moveSpeed * SystemAPI.Time.DeltaTime;```. The last step is making sure we destroy the bullet; we do not want memory leaks or too many entities at once, so we run the bullet on a timer, and destroy the entity once the timer is up:

```
if (state.World.IsServer()) {
  bullet.ValueRW.timer -= SystemAPI.Time.DeltaTime;
  if (bullet.ValueRW.timer <= 0f) {
    entityCommandBuffer.DestroyEntity(entity);
  }
}
```

The bullet now shoots in the proper direction from the player, with prediction, for about five seconds! Furthermore, the player now follows the client for each individual client. This concluded what I hoped to achieve within the servers; we now move to server infrastructure and management.

#### Joining Dedicated Servers

Switching from a Unity hosted server to a dedicated server hosted through an external provider requires an overhaul of the join code. Because we are working with Unity Multiplayer services, with Multiplay server hosting and Matchmaker for joining said servers. Therefore, in a new script that handles the Multiplay server creation, we need to initialize these services.

Using a dedicated server requires code to be carefully split between the server and client side. Unity will not build code that is not specifically designated for the server, so we must put a ```#if UNITY_SERVER``` around these blocks of code. Furthermore, we check where to actually run the server code with the line ```if (Application.platform == RuntimePlatform.LinuxServer)```. Therefore, on the server itself, we initialize the code with the line ```await UnityServices.InitializeAsync()```. 

The await usage tells Unity not to continue running while handling a large function. The start function it runs in therefore needs to be async, as it allows Unity to run other functions while it processes the await functions inside of it.

Now that we are connected, we need to get the IP and port of the server. Multiplayer Services has a built in feature for this called ServerConfig, which stores these parameters, along with a few others. Once we do that, we need to start a query to search for servers, which we plug a handful of parameters into. These parameters are all arbitrary for this project except the first, which states the minimum number of players permitted (unless the JSON code for the Unity servers has a lower number - I have it set to 2 in Unity Multiplayer, while it is 10 in my code).

```
ServerConfig serverConfig = MultiplayService.Instance.ServerConfig;
IServerQueryHandler serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(10, "MyServer", "MyGameType", "0", "TestMap");
```

We now check to see if a server has been found by checking to ensure that the server IP exists: ```if (serverConfig.AllocationId != string.Empty)```. If so, we need to manually start the server world now. With the previous auto connection code, this happened automatically, but to connect we now need to comment that out (the function now simply returns false). Initially, I attempted to pass the server IP and port into this function, but this did not work.

```
//Creates a server world
serverWorld = ClientServerBootstrap.CreateServerWorld("serverWorld");

//Destroys the base local world
foreach (World world in World.All) {
  if (world.Flags == WorldFlags.Game) {
    world.Dispose();
    break;
  }
}

if (World.DefaultGameObjectInjectionWorld == null) {
  World.DefaultGameObjectInjectionWorld = serverWorld;
}

//Loads current scene in server
await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); 
```

The final line of code pasted above also loads the samplescene, which is the scene in which the game exists on. This file runs inside of a startup scene, to ensure that the server connections were handled prior to the gameplay loop.

We now have a server world within Unity and a dedicated server running in Unity Multiplayer, but the two are not connected. In Netcode for GameObjects, there is a simple connection function, but with NCE this must be done manually. (Netcode for GameObjects also handles the world creation). To connect the two with NCE, we need to add a network stream driver to the server world, and then use that to listen to any network endpoint that has the IP address of the server. Once this happens, the two are connected. The final step is to set the server to be ready for players to join.
- Network Stream Drivers are structs that are used to listen for connection data
- The NetworkEndpoint is a reference to the endpoint, typically the IP and port, of a server. We check to make sure it is the right one by making sure it has the same IP as the server.

```
                //Set connection data and start the server in Unity
RefRW<NetworkStreamDriver> networkStreamDriver = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
                networkStreamDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(serverConfig.Port));
Debug.Log("Server connected!");

await MultiplayService.Instance.ReadyServerForPlayersAsync();
```

We now have dedicated servers connected to Unity! Clients cannot join them yet, however - passing the IP and port to the client for joining is very difficult, and the most logistical way to send it to them is by having them connect automatically through Unity Matchmaker.

#### Joining Matches with Matchmaker

Like Unity Multiplay, Matchmaker needs to be set up outside of Unity first, within Unity Cloud. It also needs to be initialized in Unity - this time in MatchmakingManager.cs - but this time it must be done on the client. We also need to get PlayerIds, which is how Matchmaker stores information about the players joining a server. We do this by generating them randomly; instead of making players login and setting up an account system, we make them all anonymous.

```
if (Application.platform != RuntimePlatform.LinuxServer) {

  //Initialize Unity Services - for Multiplayer hosting 
  await UnityServices.InitializeAsync();

  //Gets the random player ID 
  await AuthenticationService.Instance.SignInAnonymouslyAsync();
  string playerId = AuthenticationService.Instance.PlayerId;

}
```

Unity Matchmaker works through tickets, which exist on queues and have lists of players on the queue. The queue I am using is called 'test', which we store in the ticket options, as this is the second component needed to create a ticket response. The list of players needs to be composed of the PlayerIds. We add a list instead of just one player when players are joining because, if they are joining from a lobby, multiple players will be on the ticket at once. 

Once these two things are composed, we create a ticket using the list of players and the ticket options (just the queue name in our case; in other cases, it could consist of other variables such as player skill levels, what game mode they are trying to join, etc). We store the Id of the ticket.

```
CreateTicketOptions createTicketOptions = new CreateTicketOptions("test");

//Should only be one player currently; list is for multiple clients joining after Lobby is set up
List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player> { new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId)};
Debug.Log($"Player ID in Matchmaker Manager: {AuthenticationService.Instance.PlayerId}");

//Creates a ticket with the players joining and the ticket options
CreateTicketResponse createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
currentTicket = createTicketResponse.Id;
Debug.Log("Ticket Created!");
```

Now that we have a ticket created, we need to wait and see if Matchmaker accepts it. In a forever loop, we wait one second, and then start by creating a TicketStatusResponse, ```TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);```, which simply tells us the status of the ticket. 
- If it returns Timeout, the ticket did not find a server within the 40 seconds it is given. It will not try again in my current system.
- If it returns Failed, no servers exist currently, or some other bug exists. I have it print the resulting status message to provide more information.
- If it returns InProgress, I simply have it print that it is in progress, so we know it is connecting.
- If it returns Found, we have found a server, and need to connect the client.

The code for this is very similar to that for connecting the server in the first place. We create a client world, and delete any existing world for the client; we load the game scene out of the startup scene for the client; and we use the server port and IP to connect the client. Here, the Ip and Port are passed in from the server that was found by the ticket. We create a network endpoint with the ip and port, then the NetworkStreamDriver, and use the Driver to connect the client world to the endpoint. In total, the code looks like this:

```
if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Found) {

  //Creates the client world in Unity
  World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
  entityManager = clientWorld.EntityManager;

  //Destroys the base local world
  foreach (World world in World.All) {
    if (world.Flags == WorldFlags.Game) {
      world.Dispose();
      break;
    }
  }

  //Sets client code to apply to client world
  if (World.DefaultGameObjectInjectionWorld == null) {
    World.DefaultGameObjectInjectionWorld = clientWorld;
  }

  //Loads the game scene
  await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

  //Join server
  int? port = multiplayAssignment.Port;

  string ip = multiplayAssignment.Ip;

  Debug.Log("Port: " + port);
  Debug.Log("Ip:" + multiplayAssignment.Ip);

  //Creates an endpoint; place to connect to, using the IP and Port
  NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.Parse(ip, ushort.Parse(port.ToString()));
  RefRW<NetworkStreamDriver> networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();

  //Sets the client world to be into the server
  networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

  return;

}
```

Lastly, we need a way to deallocate servers. Once servers are no longer used, costs still incur upon them being empty, and they take up space, so it is important to close them. This is just done by using ```Application.Quit```, but it is important it is not done prematurely. Every time the server updates, we call this function if there are 0 players connected. Then, within the function, we wait 60 seconds before deallocating. If there are still zero players connected, the server deallocates; otherwise, it will not.

We store the number of players connected in a HashSet of strings in ServerPlayerTracker.cs, where we store the PlayerIds (generated by the anonymous sign-in through Matchmaker). We use a Hashset because it prevents multiples from joining, keeping the count accurate. The code to add the PlayerId exists in the GoInGameServerSystem.cs script, which is passed the PlayerId through the GoInGameRequestRPC. 

The RPC, created in GoInGameClientSystem:
```
entityCommandBuffer.AddComponent(rpcEntity, new GoInGameRequestRPC {
  AuthPlayerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId
});
```
And where it is added to the list, in GoInGameServerSystem:
```
ServerPlayerTracker.ConnectedClientIds.Add(goInGameRequest.ValueRO.AuthPlayerId.ToString());
```

This is the end of the fully functional code I have in my demo, but I also programmed a backfill system. As it is not something I want in my final project (as this genre of game does not allow players to join once a game is started), and is not fully functional, it is disabled, but the code works as follows:

#### Backfill (Unfinished)

Backfill is the ability for players to join servers once they are already started, either because they are not full or because a player left. In Matchmaker, this is done with a separate type of ticket, called BackfillTickets. When we sign in the player anonymously at the top of MatchmakingManager, we now also create a backfill ticket. But this is done on the server - so we must add an else to the if statement that ensures we run on client:

```
 else {

  //Initializes Multiplay Services on the server side
  while (UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing) {
    await Task.Yield();
  }

  //Create a backfill ticket ID
  IMatchmakerService matchmakerService = MatchmakerService.Instance;
  PayloadAllocation payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();
  string backfillTicketId = payloadAllocation.BackfillTicketId;
```

PayloadAllocation is not an existing function, but is one needed for this process, and needs to be manually programmed in exactly as is done here:

```
[System.Serializable]
public class PayloadAllocation {
  public MatchProperties matchProperties;
  public string GeneratorName;
  public string QueueName;
  public string PoolName;
  public string EnvironmentId;
  public string BackfillTicketId;
  public string MatchId;
  public string PoolId;
}
```

The way that we tell the servers that there are still some empty spaces, and so more players can join, is by approving the backfill ticket. In the update method, we check to ensure that the backfill ticket Id exists and that the number of collected clients is fewer than the server limit: if both are true, we run ```BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);``` to approve the ticket. We get the count of clients from the Hashset detailed above.

The backfill ticket must stay updated with the correct list of current clients on the server, so we update it whenever a player connects or disconnects by using the built-in Unity functions ```OnPlayerConnected()``` and ```OnPlayerDisconnected```. The update code starts by reforming the list of players, by populating a list with the Network Ids set in the Hashset. We then use the MatchProperties class to redefine the properties for the match, using the list of players we created and the backfill ticket Id. Finally, we update the backfill ticket with the Id stored in the payload allocation, replacing it with a new backfill ticket with the new backfill Id, and the properties defined in MatchProperties. 

In total, the code for updating the backfill ticket looks like this:
```
        //Creates a list of players
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player>();

        //Adds all players connected to server to list
        foreach (var playerId in ServerPlayerTracker.ConnectedClientIds) {
            players.Add(new Unity.Services.Matchmaker.Models.Player(playerId.ToString()));
        }

        //States the settings of this server
        MatchProperties matchProperties = new MatchProperties(null, players, null, backfillTicketId);

        //Update the backfill ticket with the list of players and match settings
        await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId, 
            new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(matchProperties)));
```

### Reflection

Overall, I am very satisfied with what I learned throughout this Independent Study. My goal for this study was to come away from it with a basis to build a full multiplayer game off of, as well as the knowledge to continue this endeavor, and I believe I gained both of those things. I am far more comfortable with the Unity editor as a whole, I feel like I have a decent understanding of Netcode for Entities and the DOTS system it is built off of, and I know how to build off of the framework I have set up in terms of server infrastructure. 

My main goals for this project going in included:

1. Understand intermediary levels of C# programming

I feel far more comfortable and confident in C# and Unity than I had previously. On top of simply expanding my repertoire of what I know how to do in C#, I feel much more comfotable with optimization, using various packages and SDKs, and the sending/sharing of data. While much of the coding for this project was localized inside the NCE system and Unity Multiplayer, I feel like I could now pick up C# and create any intermediate games or programs within it with the knowledge I have gained.

2. Have an expanded programming knowledge on projects that require networking

I am vastly more comfortable with networking than I had been previously, and believe that I understand the basic order of operations it requires. I know how Matchmaker interacts with Multiplay, I know how the server and client need to be separated and how to pass information between them, and I know exactly how I would go about the next steps of adding to my project. While not fully implemented, I do have the code for lobbies written out, and believe I could easily add it.

3. Create a project where players are automatically assigned to servers up to a certain size
4. Create a project in which two or more individuals are able to connect to the same servers and be identified as having connected together

The most difficult part of this was connecting clients to servers; changing the size of them afterwards is quite straightforward. I currently have each server set to a minimum and maximum of two players for demonstration, but changing this number is relatively straightforward through the JSON coding in Unity Multiplayer. I could set minimum to one maximum to ten, or set the minimum to ten, but have it decrease by one every minute that the minimum is not met, eventually hitting one. 

5. Develop the skills necessary to research and discover solutions for code independently

Debugging became a major part of this project as it went on. While there was a decent amount of documentation throughout the early stages of connecting the client to the local server, there became less and less as I progressed the difficulty of what I programmed. The first task where I was really without any resources was introducing gravity and jumping. Then camera positioning, aim direction, server connecting, matchmaking, and backfill each had very little, if any, documentation.

Debugging is also somewhat different within this project, as the divide between what takes place on the client and server means that some debug messages will only go to the client, and some only to the server. If you successfully connect to the server, you can check the server logs to see what messages it picks up, but when this itself is the problem, you have to find creative ways of checking what does and does not work within the client. In a handful of cases I had a hundred lines of correct code and one line incorrect, and before I was able to understand how debugging worked in this environment, I would manually rewrite almost all of the code to figure out what was incorrect. By the end of the project, however, I had a much better understanding about how to locate issues.

6. Have a solid understanding of multiple different server providers and the benefits and drawbacks of each

This is what I focused on in the initial paper I wrote for this independent study, and while I detailed a handful of options, I ended up changing each of them. By the end of the project, I have a good understanding of the decisions I made and the different platforms I engaged with, but I believe that I could not have accurately chosen everything without first trying a few things. I will write more on this later.

7. Master the Unity game engine in the process of game development

Unity is a very intense and layered game engine, but I believe I came away with a better understanding of it than I had going in. I know many more packages that it offers, have a good understanding of their multiplayer services, and increased my exposure with different scenes, worlds, and UI. I would not say that I am a master at using the game engine, but I am much more comfortable with it than I had been, and I certainly know my way around.

#### Changes Along the Way

Within the short paper I wrote after two weeks, where I outlined the different possibilities for server hosting platforms, I eventually chose to use Microsoft Playfab, and briefly mentioned that I would be making the game itself, and connecting to Playfab, using Mirror.

Neither of these platforms remained through the project. Upon failing to start a handful of times, confused as to what to begin with, I found a video talking about Netcode for Entities. An alternative to Mirror, and Netcode for GameObjects (which is what I had compared Mirror to initially), NCE offers a much faster connection, for many more players. Netcode for GameObjects is built for up to a dozen players, and does not mitigate lag or have adequate prediction, meaning it is unfit for the genre of game I was hoping to create. Mirror was better than this, but still worse than Netcode for GameObjects, which appeared ready to become an industry standard. I quickly switched to NCE.

This ended up being a mistake, in my opinion. Netcode for Entities has only been publicly available for under two years, has very little documentation through Unity itself, and very few tutorials or outside resources have become available. Even without the lack of documentation, NCE is a much harder software than at least NGO: GameObjects has a built-in function called NetworkManager which deals with building worlds, connecting to servers, storing connected players, etc, while Netcode for Entities has none of this. I had to build each of these functions manually, and, while adding to what I learned, this caused a lot of undue stress and overall restricted what I could finish for my final project.

Playfab also changed after I discovered it could only host up to 32 players within the Battle Royale genre, and I wanted to host up to 50. I briefly tried Amazon Gamelift, but the lack of information on where to start and intense documentation was too much to jump into. Eventually, I chose Unity Multiplayer, which I had written off initially: but upon further research, it featured the hosting for games such as Apex Legends, which is one of the largest Battle Royale games. Therefore, I figured this would work.

Lastly, I switched the development notebook from being submitted weekly to being submitted at the end of the semester. I figured that, with the meetings every other week, this was redundant.

#### Grades

I remember in Advanced Programming, you had us write what grade you believe we deserve on the open-ended projects, and I decided to do this here as well.

1. A short paper outlining the benefits and drawbacks of each server provider: I completed this to the best of my ability, and while my plans eventually changed, I had all the information I could prior to actually beginning to code. 10/10
2. A development notebook showcasing the design process: I kept notes throughout the project on my updates, but I believe that I could have kept more detailed notes, including screenshots of code and better documentation of what sources I was using that day. I believe I mostly explained the final thought process in this documentation. 19/20
3. Consistent communication with instructor every 1-2 weeks: 20/20
4. Video showcasing final project: /5
5. Final project: I accomplished everything I set out to wihtin my final project, and while I did not reach my stretch goals, I believe I showed both a deep understanding of the subject matter and completed a project that demonstrates this understanding. The final project is fully functional and well-documented, and includes in-server interactions (shooting, gravity, collisions) and server infrastructure (matchmaking, etc). 43/45

#### Final Thoughts

Overall, this project has been one of the highlights of my time as a computer science major, but has also been the most stressful topic I have broached. I often found myself stuck for weeks with no real resources to utilize, and having to try solution after solution until one worked. Looking back, I wish I had chosen Netcode for GameObjects, as I believe I could have created at least a lobby on top of what I have, and probably more. Furthermore, I would have had many more resources, both in terms of people to go to, source projects to analyze, and documentation to read. 

With that being said, I am very proud of what I did accomplish. The lack of documentation and resources meant that good portions of the code were made entirely by my connecting tidbits of what tutorials did exist, and this led to a much greater understanding of what I was working on. I believe that, with another month, I could have produced a much stronger prototype with lobby, graphics, and more. As it is, however, I believe I met my expectations for the project going in, and exceeded my expectations throughout many points of the semester as I realized how difficult what I had signed up for actually was.

#### Sources Cited (IEEE)

[1]“Struct NetworkStreamDriver
 | Netcode for Entities | 1.1.0-pre.3,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.netcode@1.1/api/Unity.netCode.networkStreamDriver.html (accessed May 04, 2025).
 
[2]Unity Technologies, “How to control Netcode child entities,” Unity Discussions, Apr. 27, 2024. https://discussions.unity.com/t/how-to-control-netcode-child-entities/945744 (accessed May 04, 2025).

[3]Code Monkey, “- YouTube,” www.youtube.com, Dec. 09, 2024. https://www.youtube.com/watch?v=HpUmpw_N8BA

[4]Code Monkey, “Run your Dedicated Servers like a PRO! (Unity Game Server Hosting Multiplayer Tutorial),” YouTube, Mar. 15, 2023. https://www.youtube.com/watch?v=IvCVFywNXMc (accessed May 04, 2025).

[5]Code Monkey, “Making a MULTIPLAYER Game? Join your Players with LOBBY!,” YouTube, Nov. 29, 2022. https://www.youtube.com/watch?v=-KDlEBfCBiU (accessed May 04, 2025).

[6]Memphis Game Developers, “Netcode for Entities: Unity Physics,” YouTube, Jul. 22, 2023. https://www.youtube.com/watch?v=BsZHB_JxxNo (accessed May 04, 2025).

[7]Developers Hub, “Netcode for Entities For Beginners (DOTS) #05 Input,” YouTube, Dec. 31, 2023. https://www.youtube.com/watch?v=1DsdC38PYvg (accessed May 04, 2025).

[8]“Struct NetworkId
   | Netcode for Entities | 1.0.17,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.netcode@1.0/api/Unity.netCode.networkId.html (accessed May 04, 2025).
   
[9]“Class GhostFieldAttribute
   | Netcode for Entities | 1.0.17,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.netcode@1.0/api/Unity.netCode.GhostFieldAttribute.html (accessed May 04, 2025).

[10]“Connecting server and clients | Netcode for Entities | 1.3.6,” Unity3d.com, 2024. https://docs.unity3d.com/Packages/com.unity.netcode@1.3/manual/network-connection.html (accessed May 04, 2025).

[11]“Time synchronization | Netcode for Entities | 1.3.6,” Unity3d.com, 2024. https://docs.unity3d.com/Packages/com.unity.netcode@1.3/manual/time-synchronization.html (accessed May 04, 2025).

[12]“Entities list | Netcode for Entities | 1.0.17,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.netcode@1.0/manual/entities-list.html (accessed May 04, 2025).

[13]Unity-Technologies, “com.unity.services.samples.matchplay/Assets/Scripts/Matchplay/Server/Netcode at master · Unity-Technologies/com.unity.services.samples.matchplay,” GitHub, 2022. https://github.com/Unity-Technologies/com.unity.services.samples.matchplay/tree/master/Assets/Scripts/Matchplay/Server/Netcode (accessed May 04, 2025).

[14]Unity Technologies, “(Question) NetCode How to Identify Local Player,” Unity Discussions, Mar. 29, 2020. https://discussions.unity.com/t/question-netcode-how-to-identify-local-player/782893/2 (accessed May 04, 2025).

[15]Unity Technologies, “[netcode] How to get connected clients on client side?,” Unity Discussions, Jan. 28, 2023. https://discussions.unity.com/t/netcode-how-to-get-connected-clients-on-client-side/251253/4 (accessed May 04, 2025).

[16]“Build a session with Netcode for Entities,” Unity.com, 2025. https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/build-with-netcode-for-entities (accessed May 04, 2025).

[17]Unity Technologies, “Can’t connect Netcode for Entities to Lobby and Relay,” Unity Discussions, Jul. 22, 2022. https://discussions.unity.com/t/cant-connect-netcode-for-entities-to-lobby-and-relay/885191/5 (accessed May 04, 2025).

[18]Unity Technologies, “NetworkManager.Singletion.StartServer vs NetworkManager.Singletion.Starthost in Netcode,” Unity Discussions, Nov. 30, 2023. https://discussions.unity.com/t/networkmanager-singletion-startserver-vs-networkmanager-singletion-starthost-in-netcode/934634 (accessed May 04, 2025).

[19]Unity-Technologies, “megacity-metro/Assets/Scripts/Gameplay/Server/Netcode/ServerInGame.cs at master · Unity-Technologies/megacity-metro,” GitHub, 2024. https://github.com/Unity-Technologies/megacity-metro/blob/master/Assets/Scripts/Gameplay/Server/Netcode/ServerInGame.cs (accessed May 04, 2025).

[20]“Prediction | Netcode for Entities | 1.4.0,” Unity3d.com, 2024. https://docs.unity3d.com/Packages/com.unity.netcode@1.4/manual/prediction.html (accessed May 04, 2025).

[21]“Command stream | Netcode for Entities | 1.0.17,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.netcode@1.0/manual/command-stream.html (accessed May 04, 2025).

[22]InterfaceBE, “Reddit - The heart of the internet,” Reddit.com, 2022. https://www.reddit.com/r/Unity3D/comments/slhkzk/best_practices_for_netcode_player_input_in/ (accessed May 04, 2025).

[23]“Namespace Unity.netCode
   | Netcode for Entities | 1.0.17,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.netcode@1.0/api/Unity.netCode.html (accessed May 04, 2025).
   
[24]Unity Technologies, “Spawn bullet from Camera [Netcode],” Unity Discussions, Dec. 10, 2022. https://discussions.unity.com/t/spawn-bullet-from-camera-netcode/255649 (accessed May 04, 2025).

[25]Unity Technologies, “How to attach a camera to a ghost/prefab when using Unity NetCode?,” Unity Discussions, Jun. 09, 2020. https://discussions.unity.com/t/how-to-attach-a-camera-to-a-ghost-prefab-when-using-unity-netcode/792539/4 (accessed May 04, 2025).

[26]“How to Get Started with Netcode in Unity ECS | Moetsi,” Moetsi.com, 2019. https://www.moetsi.com/ar-development/how-to-get-started-with-netcode-in-unity-ecs (accessed May 04, 2025).

[27]“Client and server worlds networking model | Netcode for Entities | 1.4.0,” Unity3d.com, 2024. https://docs.unity3d.com/Packages/com.unity.netcode@1.4/manual/client-server-worlds.html (accessed May 04, 2025).

[28]“Setting up client and server worlds | Netcode for Entities | 1.4.0,” Unity3d.com, 2024. https://docs.unity3d.com/Packages/com.unity.netcode@1.4/manual/set-up-client-server-worlds.html (accessed May 04, 2025).

[29]“Creating multiplayer gameplay | Netcode for Entities | 1.4.0,” Unity3d.com, 2024. https://docs.unity3d.com/Packages/com.unity.netcode@1.4/manual/creating-multiplayer-gameplay.html (accessed May 04, 2025).

[30]Unity Technologies, “Collision Behaviour in Multiplayer with netcode,” Unity Discussions, Jun. 10, 2024. https://discussions.unity.com/t/collision-behaviour-in-multiplayer-with-netcode/949854 (accessed May 04, 2025).

[31]Unity Technologies, “How to make Unity physics material work with netcode?,” Unity Discussions, Apr. 27, 2022. https://discussions.unity.com/t/how-to-make-unity-physics-material-work-with-netcode/879681 (accessed May 04, 2025).

[32]Freedom Coding, “Game Server Hosting (MULTIPLAY) || Unity Tutorial,” YouTube, Apr. 01, 2024. https://www.youtube.com/watch?v=D1wJO5SrUmc (accessed May 04, 2025).

[33]Freedom Coding, “Group Players Together Easily || Unity Matchmaker Tutorial,” YouTube, Apr. 08, 2024. https://www.youtube.com/watch?v=aUBP_L4rYrg (accessed May 04, 2025).

[34]“Method GetPayloadAllocationFromJsonAs
 | Multiplay | 1.1.1,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.services.multiplay@1.1/api/Unity.Services.Multiplay.IMultiplayService.GetPayloadAllocationFromJsonAs.html (accessed May 04, 2025).
 
 [35]Unity Technologies, “Multiplay API is not initialized,” Unity Discussions, Nov. 28, 2024. https://discussions.unity.com/t/multiplay-api-is-not-initialized/938020/3 (accessed May 04, 2025).
 
 [36]“Multiplay Hosting support,” Unity.com, 2025. https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/game-server-hosting-support (accessed May 04, 2025).
 
 [37]Unity Technologies, “InvalidOperationException: Unable to get IMultiplayService because Multiplay API is not initialized,” Unity Discussions, Apr. 09, 2025. https://discussions.unity.com/t/invalidoperationexception-unable-to-get-imultiplayservice-because-multiplay-api-is-not-initialized/1627553/2 (accessed May 04, 2025).
 
[38]Unity Technologies, “Unity Multiplay Service gets stuck in OnAllocate at GetPayloadAllocation,” Unity Discussions, Jun. 15, 2023. https://discussions.unity.com/t/unity-multiplay-service-gets-stuck-in-onallocate-at-getpayloadallocation/920755 (accessed May 04, 2025).

[39]Unity Technologies, “Exception thrown when calling ReadyServerForPlayersAsync with Multiplay 1.0.4 package,” Unity Discussions, Jun. 18, 2023. https://discussions.unity.com/t/exception-thrown-when-calling-readyserverforplayersasync-with-multiplay-1-0-4-package/920080/9 (accessed May 04, 2025).

[40]“Method StartServerQueryHandlerAsync
 | Multiplay | 1.1.1,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.services.multiplay@1.1/api/Unity.Services.Multiplay.IMultiplayService.StartServerQueryHandlerAsync.html (accessed May 04, 2025).

[41]“Property ServerConfig
 | Multiplay | 1.1.1,” Unity3d.com, 2023. https://docs.unity3d.com/Packages/com.unity.services.multiplay@1.1/api/Unity.Services.Multiplay.IMultiplayService.ServerConfig.html#Unity_Services_Multiplay_IMultiplayService_ServerConfig (accessed May 04, 2025).

[42]Jason Weimann (GameDev), “Full Tutorial: Multiplayer Matchmaking and Dedicated Server Setup for any game,” YouTube, Feb. 20, 2024. https://www.youtube.com/watch?v=yqntalzsgL0 (accessed May 04, 2025).

[43]Unity Technologies, “Static variables for multiplayer game session management,” Unity Discussions, Jul. 27, 2021. https://discussions.unity.com/t/static-variables-for-multiplayer-game-session-management/849933 (accessed May 04, 2025).

[44]“Deploy a Multiplay build configuration,” Unity.com, 2025. https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/deployment/deploy-multiplay-configuration (accessed May 04, 2025).

[45]“Working with the Multiplayer Services SDK,” Unity.com, 2025. https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/working-with-mps (accessed May 04, 2025).

[46]“Multiplay Hosting API Workflows,” Unity.com, 2025. https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/guides/api-workflows (accessed May 04, 2025).

[47]Unity Technologies, “Unity - Manual: The Multiplayer High Level API,” Unity3d.com, 2018. https://docs.unity3d.com/2018.2/Documentation/Manual/UNetUsingHLAPI.html (accessed May 04, 2025).

[48]KRIUNS, “Create Your Own Multiplayer Game Lobby with Unity Lobby Service - A Step-by-Step Guide | KRIUNS,” YouTube, Apr. 01, 2023. https://www.youtube.com/watch?v=AJcTQKPpdSw (accessed May 04, 2025).

[49]Memphis Game Developers, “Netcode for Entities: Client Side Prediction (Rollback),” YouTube, Jul. 30, 2023. https://www.youtube.com/watch?v=SKk5KnwAM64 (accessed May 04, 2025).

[50]Turbo Makes Games, “Make Camera Follow Entities in Unity ECS (Plus Cinemachine) - DOTS Tutorial [Old Version of ECS],” YouTube, Jun. 09, 2020. https://www.youtube.com/watch?v=JFv49-0vy_8 (accessed May 04, 2025).

[51]Turbo Makes Games, “How to Make a Multiplayer Game with DOTS - FULL COURSE,” YouTube, Mar. 15, 2024. https://www.youtube.com/watch?v=8efRGtRCGJ0 (accessed May 04, 2025).

[52]Tale Forge, “Physics in Unity ECS - Rigidbody, PhysicsShape, PhysicsBody,” YouTube, May 28, 2024. https://www.youtube.com/watch?v=_DA2yei9srQ (accessed May 04, 2025).

[53]Tale Forge, “Is Player Grounded? - Jump in Unity ECS - Unsafe Code, Pointers, OverlapBox,” YouTube, Jun. 30, 2024. https://www.youtube.com/watch?v=vT0XiVbjzlc (accessed May 04, 2025).

[54]BiteMe Games, “Using Unity with Git in 15 minutes,” YouTube, Jul. 18, 2023. https://www.youtube.com/watch?v=30qiV2YA7gA (accessed May 04, 2025).

[55]Unity MMO Developers, “Episode 4: Playfab Setup | uMMORPG Part 1 | Unity MMO Creators,” YouTube, Oct. 22, 2018. https://www.youtube.com/watch?v=ufT43p6Hbaw (accessed May 04, 2025).

[56]Alexandre Bruffa, “Building a Real-Time Multiplayer Game With Unity3D and Amazon GameLift,” YouTube, Jun. 09, 2023. https://www.youtube.com/watch?v=lZ4a_ZKanT4 (accessed May 04, 2025).

[57]Memphis Game Developers, “Getting started with Unity Server Hosting and Matchmaking,” YouTube, Sep. 24, 2022. https://www.youtube.com/watch?v=yUoxYOMzwx8 (accessed May 04, 2025).

[58]samyam, “How to Setup Dedicated Server Hosting and Matchmaking for Unity,” YouTube, May 03, 2023. https://www.youtube.com/watch?v=FjOZrSPL_-Y (accessed May 04, 2025).

[59]Unity Technologies, “Memory leak with Entity Query,” Unity Discussions, Aug. 12, 2019. https://discussions.unity.com/t/memory-leak-with-entity-query/754165 (accessed May 04, 2025).

[60]Unity Technologies, “ComputeBuffer warning when component uses ExecuteInEditMode,” Unity Discussions, May 16, 2022. https://discussions.unity.com/t/computebuffer-warning-when-component-uses-executeineditmode/780291/5 (accessed May 04, 2025).

[61]S. Staff, “🌟 C# Collection Types: HashSet vs. List 🚀 - Shawna Staff - Medium,” Medium, Sep. 07, 2023. https://medium.com/@shawnastaff/c-collection-types-hashset-vs-list-bae022e7b439 (accessed May 04, 2025).

[62]“ClientRpc | Unity Multiplayer Networking,” docs-multiplayer.unity3d.com, Apr. 28, 2023. https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc/








# LEFT TO DO
- Video (2 hours)
- Final test and build (1 hour)
