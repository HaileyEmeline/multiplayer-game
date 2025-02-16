using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

//We set ghost mode to owner predicted - without prediction, we have to wait for the 
//message to go to the server and come back. Instead, the client needs to predict what
//will happen and correct it right away. We need a system group to do this:
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct NetcodePlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<NetcodePlayerInput> netcodePlayerInput,
                  RefRW<LocalTransform> localTransform) 
                  in  SystemAPI.Query<RefRO<NetcodePlayerInput>, RefRW<LocalTransform>>().WithAll<Simulate>()) { //WithAll important for this updateingroup group type

                      float moveSpeed = 10f;
                      float3 moveVector = new float3(netcodePlayerInput.ValueRO.inputVector.x, 0, netcodePlayerInput.ValueRO.inputVector.y);

                      localTransform.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.fixedDeltaTime;

                      //Make sure scale does not change:
                      
                  }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}

public struct PlayerPosition : IComponentData
{ 
    public float3 position;
}
