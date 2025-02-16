using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

//Listens for player inputs

//put the system in the correct group
//Only runs on client
[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct NetcodePlayerInputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //Requires a connection to update
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<NetcodePlayerInput>();

    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //RefRW because we want ot write to it
        //MyValue is the value we created in my value authoring.cs - it is to make sure we can pass values into the server that are synchronizable.
        foreach ((
            RefRW<NetcodePlayerInput> netcodePlayerInput,
            RefRW<MyValue> myValue)
            in SystemAPI.Query<RefRW<NetcodePlayerInput>, RefRW<MyValue>>().WithAll<GhostOwnerIsLocal>()) {
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

            //CODE WAS REMOVED - PUT IN TESTMYVALUESYSTEM BECAUSE IT IS
            //CLIENT BASED HERE AND SERVER AUTHORITIVE THERE
            //if (Input.GetKey(KeyCode.Y)) {
            //    myValue.ValueRW.value = UnityEngine.Random.Range(100, 999);
            //    Debug.Log("Changed " + myValue.ValueRW.value);
            //}

            netcodePlayerInput.ValueRW.inputVector = inputVector;

            //Now we need a system that uses these inputs to move the actual ghost

            //Bad way to manage one-off shooting:
            if (Input.GetKeyDown(KeyCode.U)) {
                Debug.Log("Shoot! ");
                netcodePlayerInput.ValueRW.shoot.Set();
            } else {
                netcodePlayerInput.ValueRW.shoot = default;
            }

        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
