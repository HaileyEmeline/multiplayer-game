using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

partial struct TestMyValueSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<MyValue> myValue,
            Entity entity)
            in SystemAPI.Query<RefRO<MyValue>>().WithEntityAccess()) {

                //state.World because it runs on both system and client.
                //UnityEngine.Debug.Log(myValue.ValueRO.value + " :: " + entity + " :: " + state.World);
            }
    }

}

//Tag makes it run only on the server - used to make variables update in server,
//Not just on local copy
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct TestMyValueServerSystem : ISystem {

    public void OnUpdate(ref SystemState state) {
        foreach (RefRW<MyValue> myValue in SystemAPI.Query<RefRW<MyValue>>()) {
            if (Input.GetKeyDown(KeyCode.Y)) {
                myValue.ValueRW.value = UnityEngine.Random.Range(100, 999);
                UnityEngine.Debug.Log("Changed " + myValue.ValueRW.value);
            }
        }
    }
}
