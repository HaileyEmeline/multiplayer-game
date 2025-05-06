using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.Rendering;

//This file introduces the client into the game upon connection
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{
    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

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
        
    }

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

}

public struct GoInGameRequestRPC : IRpcCommand {
    public FixedString64Bytes AuthPlayerId;
}