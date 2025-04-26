using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using Unity.Physics.Extensions;
using Unity.Transforms;

//Listens for player inputs

//put the system in the correct group
//Only runs on client
[UpdateInGroup(typeof(GhostInputSystemGroup))]

partial struct NetcodePlayerInputSystem : ISystem
{
    //public Vector3 mousePos;
    public Entity mouseEntity;
    
    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //Requires a connection to update
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<NetcodePlayerInput>();
        //state.RequireForUpdate<MousePosition>();

    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {


        /*if (mouseEntity == Entity.Null) {
            mouseEntity = SystemAPI.GetSingletonEntity<MousePosition>();
            if (mouseEntity == Entity.Null) {
                Debug.LogError("No entity with PlayerPosition found!");
                return;
            }

        }

        //Getting mouse position?
        float3 mousePos = new float3(Input.mousePosition.x, Input.mousePosition.y, 0f);

        float3 worldPosition = Camera.main.ScreenToWorldPoint(new float3(mousePos.x, mousePos.y, 0f));
        float3 finalPosition = new float3(worldPosition.x, worldPosition.y, 0f);
        SystemAPI.SetComponent(mouseEntity, new MousePosition {direction = finalPosition});
*/
        //MousePosition.ValueRW.direction = 

        //RefRW because we want ot write to it
        //MyValue is the value we created in my value authoring.cs - it is to make sure we can pass values into the server that are synchronizable.
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

            if (Input.GetAxisRaw("Jump") > 0) {
                netcodePlayerInput.ValueRW.jump.Set();
            } else {
                netcodePlayerInput.ValueRW.jump = default;
            }

            netcodePlayerInput.ValueRW.inputVector = inputVector;

            PlayerAspect playerAspect = new PlayerAspect();

            //Determines whether jumping or not - using inputevent.
            /* if (Input.GetKeyDown(KeyCode.W)) {
                Debug.Log("Jump! ");
                netcodePlayerInput.ValueRW.jump.Set();

                //physicsVelocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, playerAspect.jumpForce * math.up());
                //inputVector.y = +10f;

                float3 jumpVelocity = playerAspect.jumpForce * math.up();

                physicsVelocity.ValueRW.Linear += jumpVelocity;
            } else {
                netcodePlayerInput.ValueRW.jump = default;
            } */

            //CODE WAS REMOVED - PUT IN TESTMYVALUESYSTEM BECAUSE IT IS
            //CLIENT BASED HERE AND SERVER AUTHORITIVE THERE
            //if (Input.GetKey(KeyCode.Y)) {
            //    myValue.ValueRW.value = UnityEngine.Random.Range(100, 999);
            //    Debug.Log("Changed " + myValue.ValueRW.value);
            //}

            //Now we need a system that uses these inputs to move the actual ghost

            //Bad way to manage one-off shooting:
            if (Input.GetMouseButtonDown(0)) { //.GetMouseDown(0)
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


//Unfinished
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

/*
public struct MousePosition : IComponentData {
    public float3 direction;
} */
