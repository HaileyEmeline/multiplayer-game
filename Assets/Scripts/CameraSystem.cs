using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.TextCore.Text;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateAfter(typeof(SimulationSystemGroup))] 
[BurstCompile]
partial struct CameraSystem : ISystem
{

    //The camera at game scene is at z = -1; this makes sure the 
    //Camera is on the same plane
    public static readonly float3 offset = new float3(0, 0, -1);

    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<Player>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var camera = UnityEngine.Camera.main;

        //Finds the local client
        foreach (var character in SystemAPI.Query<CharacterAspect>().WithAll<GhostOwnerIsLocal>()) {
            var playerPosition = character.Transform.ValueRO.Position;

            //Move chamera to local client
            camera.transform.position = new UnityEngine.Vector3(playerPosition.x, playerPosition.y, offset.z); 
        }
    }

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
