using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEditor.UI;

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
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach ((RefRO<NetworkId> networkId,
                 Entity entity) 
                 in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess()) {
                    
                    entityCommandBuffer.AddComponent<NetworkStreamInGame>(entity);

                    UnityEngine.Debug.Log("Setting Client as InGame");

                    Entity rpcEntity = entityCommandBuffer.CreateEntity();

                    entityCommandBuffer.AddComponent(rpcEntity, new GoInGameRequestRPC());
                    entityCommandBuffer.AddComponent(rpcEntity, new SendRpcCommandRequest());

        }
        entityCommandBuffer.Playback(state.EntityManager);
        
    }

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

}

public struct GoInGameRequestRPC : IRpcCommand {

}
