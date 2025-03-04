using JetBrains.Annotations;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

//We set ghost mode to owner predicted - without prediction, we have to wait for the 
//message to go to the server and come back. Instead, the client needs to predict what
//will happen and correct it right away. We need a system group to do this:
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct NetcodePlayerMovementSystem : ISystem
{

    private Entity playerEntity;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //Tries to make the playerposition struct to always update position
        playerEntity = state.EntityManager.CreateEntity(typeof(PlayerPosition));
        //state.EntityManager.SetComponentData(playerEntity, new PlayerPosition { position = new Vector3(0, 0, 0) });
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        
        foreach ((RefRW<NetcodePlayerInput> netcodePlayerInput,
                  RefRW<LocalTransform> localTransform,
                  RefRW<PhysicsVelocity> physicsVelocity,
                  RefRW<PhysicsMass> physicsMass)
                  in SystemAPI.Query<RefRW<NetcodePlayerInput>, RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<PhysicsMass>>().WithAll<Simulate>()) //SWITCH TO SIMULATE?
        { //WithAll important for this updateingroup group type

            float moveSpeed = 10f;
            float jumpImpulse = 10f;

            physicsVelocity.ValueRW.Linear.x = netcodePlayerInput.ValueRO.inputVector.x * moveSpeed;  // Horizontal movement left-right (X)
            physicsVelocity.ValueRW.Linear.z = netcodePlayerInput.ValueRO.inputVector.y * moveSpeed;  // Horizontal movement forward-backward (Z)

            // Jump logic - apply upward force (impulse) when the jump button is pressed
            //if (Input.GetKeyDown(KeyCode.W) && Mathf.Abs(physicsVelocity.ValueRW.Linear.y) < 0.01f) // Check if the player presses the jump key
            if (netcodePlayerInput.ValueRO.jump.IsSet && Mathf.Abs(physicsVelocity.ValueRW.Linear.y) < 0.01f)
            { //Input.GetAxisRaw("Jump") ^^
                //Debug.Log("Jump! ");
                //netcodePlayerInput.ValueRW.jump.Set();

                // Apply upward impulse (force) to the player's physics velocity
                 // Define jump force (you can adjust this value as needed)
                
                physicsVelocity.ValueRW.Linear.y = 0; //new Vector3(physicsVelocity.ValueRO.Linear.x, )
                //physicsVelocity.ValueRW.Linear.y += jumpImpulse;  // Apply vertical velocity for jumping
                physicsVelocity.ValueRW.Linear.y = netcodePlayerInput.ValueRO.inputVector.y + jumpImpulse;
                Debug.Log("Jump! ");
            } 
            else
            {
                netcodePlayerInput.ValueRW.jump = default;
            }
                      //float3 moveVector = new float3(netcodePlayerInput.ValueRO.inputVector.x, 0, netcodePlayerInput.ValueRO.inputVector.y);

                      //float moveVector = netcodePlayerInput.ValueRO.inputVector.x;
                      //float movePhysics = netcodePlayerInput.ValueRO.

                      //localTransform.ValueRW.Position.x += moveVector * moveSpeed * SystemAPI.Time.fixedDeltaTime;

                      //localTransform.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.fixedDeltaTime;

                      //Make sure scale does not change:
                      
                  }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    [BurstCompile]
    private void SetRotation(Entity player, LocalTransform playerTransform) { //Add player aspect?
        //Sets player to face direction moving - come back to this
    }

    [BurstCompile]
    private void SetVelocity(Entity player, PhysicsVelocity playerVelocity, PlayerAspect playerAspect) { //Add player aspect?
        
        
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer();
        float3 linearVelocity = new float3(playerAspect.MoveSpeed * playerAspect.MoveDirection, playerVelocity.Linear.y, playerVelocity.Linear.z);

        entityCommandBuffer.SetComponent(player, new PhysicsVelocity {
            Linear = linearVelocity
        });
        //NEED MOVEMENT SPEED AND MOVEMENT DIRECTION AS VARIABLES HERE
    }
}

public struct PlayerPosition : IComponentData
{ 
    public Vector3 position;
}

public struct MousePosition : IComponentData {
    public float3 direction;
}
