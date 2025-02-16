using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct TestNetcodeEntitiesClientSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    //[BurstCompile]

    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.T)) {
            //Send RPC
            Entity rpcEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(rpcEntity, new SimpleRPC {
                value = 56
            });

            state.EntityManager.AddComponentData(rpcEntity, new SendRpcCommandRequest());
            UnityEngine.Debug.Log("Sending our RPC! ... ");
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
