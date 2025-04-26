using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.Rendering;

//Used to also be using UI

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{
    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkId>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //New
        //var prefab = SystemAPI.GetSingleton<Player>();

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach ((RefRO<NetworkId> networkId,
                 Entity entity) 
                 in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess()) {
                    
                    entityCommandBuffer.AddComponent<NetworkStreamInGame>(entity);

                    UnityEngine.Debug.Log("Setting Client as InGame");

                    Entity rpcEntity = entityCommandBuffer.CreateEntity();

                    entityCommandBuffer.AddComponent(rpcEntity, new GoInGameRequestRPC {
                        AuthPlayerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId
                    }); //Used to not have {}
                    entityCommandBuffer.AddComponent(rpcEntity, new SendRpcCommandRequest {
                        TargetConnection = entity
                    }); //Maybe take out the {}
                    //SendRpcCommandRequest request = new SendRpcCommandRequest { TargetConnection = entity }; // Maybe switch back to line above?


                    //Entity camera = entityCommandBuffer.Instantiate(prefab.Camera);

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