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
