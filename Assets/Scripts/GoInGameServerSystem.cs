using System;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

//Sets the server as InGame

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GoInGameServerSystem : ISystem
{
    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkId>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Creates a buffer, which is used to destroy the RPC after it is sent
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        //Used to help spawn the player object
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        foreach ((RefRO<ReceiveRpcCommandRequest> ReceiveRpcCommandRequest, 
                  Entity entity) 
                  in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRPC>().WithEntityAccess()) {
                      
                      //Add the Ingame component to the network connection
                      entityCommandBuffer.AddComponent<NetworkStreamInGame>(ReceiveRpcCommandRequest.ValueRO.SourceConnection);
                      
                      UnityEngine.Debug.Log("Client Connected to Server");

                      //Spawn the player object for the client
                      Entity playerEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerPrefabEntity);
                      
                      //Sets random location for player upon spawn
                      entityCommandBuffer.SetComponent(playerEntity, LocalTransform.FromPosition(new Unity.Mathematics.float3(
                        UnityEngine.Random.Range(-10, +10), 0, 0
                      )));

                      //Gets the network ID for the server to use in the ghostowner below
                      NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ReceiveRpcCommandRequest.ValueRO.SourceConnection);

                      //We set up hasOwner to be true in the prefab, so that the client owns
                      //the playable character instead of the server. Now we have to set that up here:
                      entityCommandBuffer.AddComponent(playerEntity, new GhostOwner {
                        NetworkId = networkId.Value,
                      });
                      //Says ghost owner is the one who sent the RPC, which was the cleint ^^


                      //Uses buffer to destroy the RPC
                      entityCommandBuffer.DestroyEntity(entity);
                  }

                  entityCommandBuffer.Playback(state.EntityManager);
    }

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
