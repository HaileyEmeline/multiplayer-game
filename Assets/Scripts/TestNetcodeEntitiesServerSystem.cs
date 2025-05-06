using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

//Receives RPCs on the server side; done in other ways for other types of RPCs, this was just done to show how code works.
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct TestNetcodeEntitiesServerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach ((
            RefRO<SimpleRPC> SimpleRPC,
            RefRO<ReceiveRpcCommandRequest> receiveRPCCommandRequest,
            Entity entity)
            in SystemAPI.Query<
                RefRO<SimpleRPC>,
                RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess()) {

            UnityEngine.Debug.Log("Received RPC: " + SimpleRPC.ValueRO.value);
            entityCommandBuffer.DestroyEntity(entity);
        }
        entityCommandBuffer.Playback(state.EntityManager);
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
