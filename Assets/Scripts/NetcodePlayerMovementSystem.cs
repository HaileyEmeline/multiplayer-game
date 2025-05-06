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

    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {

        //Tries to make the playerposition struct to always update position
        playerEntity = state.EntityManager.CreateEntity(typeof(PlayerPosition));

    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        
        foreach ((RefRW<NetcodePlayerInput> netcodePlayerInput,
                  RefRW<LocalTransform> localTransform,
                  RefRW<PhysicsVelocity> physicsVelocity,
                  RefRW<PhysicsMass> physicsMass)
                  in SystemAPI.Query<RefRW<NetcodePlayerInput>, RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<PhysicsMass>>().WithAll<Simulate>()) //SWITCH TO SIMULATE?
        { //WithAll important for this updateingroup group type

            //Sets the move speed and jump height
            float moveSpeed = 10f;
            float jumpImpulse = 10f;

            physicsVelocity.ValueRW.Linear.x = netcodePlayerInput.ValueRO.inputVector.x * moveSpeed;  // Horizontal movement left-right (X)
            physicsVelocity.ValueRW.Linear.z = netcodePlayerInput.ValueRO.inputVector.y * moveSpeed;  // Horizontal movement forward-backward (Z)

            // Jump logic - apply upward force (impulse) when the jump button is pressed
            // Only applies when the player is not falling (thus grounded)
            if (netcodePlayerInput.ValueRO.jump.IsSet && Mathf.Abs(physicsVelocity.ValueRW.Linear.y) < 0.01f)
            { 

                physicsVelocity.ValueRW.Linear.y = 0; 

                //Modifies the physics y value of the player by the jump height
                physicsVelocity.ValueRW.Linear.y = netcodePlayerInput.ValueRO.inputVector.y + jumpImpulse;

                Debug.Log("Jump! ");
            } 
            else
            {
                netcodePlayerInput.ValueRW.jump = default;
            }
                      
        }
    }

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    //Unused in final project
    private void SetRotation(Entity player, LocalTransform playerTransform) { 
    
    }

    //Unused in final project
    private void SetVelocity(Entity player, PhysicsVelocity playerVelocity, PlayerAspect playerAspect) { 
        
        
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer();
        float3 linearVelocity = new float3(playerAspect.MoveSpeed * playerAspect.MoveDirection, playerVelocity.Linear.y, playerVelocity.Linear.z);

        entityCommandBuffer.SetComponent(player, new PhysicsVelocity {
            Linear = linearVelocity
        });
        //NEED MOVEMENT SPEED AND MOVEMENT DIRECTION AS VARIABLES HERE
    }
}

//Stores player position to send
public struct PlayerPosition : IComponentData
{ 
    public Vector3 position;
}

//Stores mouse position to send
public struct MousePosition : IInputComponentData {
    public float3 direction;
}
