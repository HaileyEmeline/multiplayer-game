using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using Unity.Physics.Extensions;
using Unity.Transforms;

//Listens for player inputs and executes them for the local client
//Only runs on client
[UpdateInGroup(typeof(GhostInputSystemGroup))]

partial struct NetcodePlayerInputSystem : ISystem
{
    public Entity mouseEntity;
    
    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //Requires a connection to update
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<NetcodePlayerInput>();

    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        //MyValue is the value we created in my value authoring.cs - it is to make sure we can pass values into the server that are synchronizable.
        //Loops through all incoming client inputs
        foreach ((
            RefRW<NetcodePlayerInput> netcodePlayerInput,
            RefRW<MyValue> myValue,
            RefRW<PhysicsVelocity> physicsVelocity,
            RefRW<PhysicsMass> physicsMass)
            in SystemAPI.Query<RefRW<NetcodePlayerInput>, RefRW<MyValue>, RefRW<PhysicsVelocity>, RefRW<PhysicsMass>>().WithAll<GhostOwnerIsLocal>()) {

            //This will run for each player input - client 1 can interfere with client2 inputs 
            //We include .WithAll<GhostOwnerIsLocal>() to prevent this

            //Simple left/right movement code:
            float2 inputVector = new float2();

            if (Input.GetKey(KeyCode.A)) {
                inputVector.x = -1f;
            }

            if (Input.GetKey(KeyCode.D)) {
                inputVector.x = +1f;
            }

            //Set the jump condition if jump key pressed
            if (Input.GetAxisRaw("Jump") > 0) {
                netcodePlayerInput.ValueRW.jump.Set();
            } else {
                netcodePlayerInput.ValueRW.jump = default;
            }

            netcodePlayerInput.ValueRW.inputVector = inputVector;


            PlayerAspect playerAspect = new PlayerAspect();

            //Bad way to manage one-off shooting:
            if (Input.GetMouseButtonDown(0)) { 
                Debug.Log("Shoot! ");
                netcodePlayerInput.ValueRW.shoot.Set();
            } else {
                netcodePlayerInput.ValueRW.shoot = default;
            }

        }
    }

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }


//Unfinished method to see if player was grounded - could be useful later in development,
//But not used in final project.
    private bool isGrounded(float3 playerPosition) {
        float rayLength = 1.1f; //Length of raycast to check for ground
        float3 rayStart = new float3(playerPosition.x, playerPosition.y - 1f, playerPosition.z);

        RaycastInput raycastInput = new RaycastInput {
            Start = rayStart,
            End = rayStart + new float3(0, -rayLength, 0),
            Filter = CollisionFilter.Default
        };

        bool truefalse = false;


        return truefalse;
    }
}

