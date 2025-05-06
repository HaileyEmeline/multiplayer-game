using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

//Further testing for connection between client and server
//This was set up before clients could even join; they could just send messages
//Pressing T still sends the number 56
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
