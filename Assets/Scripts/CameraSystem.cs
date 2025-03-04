using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.TextCore.Text;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateAfter(typeof(SimulationSystemGroup))] //was presentation
[BurstCompile]
partial struct CameraSystem : ISystem
{

    //Eventually change these - we want the last one to be determined by scope size
    //Also might want to make x and y somewhat dependent on the mouse position
    public static readonly float3 offset = new float3(0, 0, -1);

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<Player>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var camera = UnityEngine.Camera.main;

        foreach (var character in SystemAPI.Query<CharacterAspect>().WithAll<GhostOwnerIsLocal>()) {
            var playerPosition = character.Transform.ValueRO.Position;
            camera.transform.position = new UnityEngine.Vector3(playerPosition.x, playerPosition.y, offset.z); 
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
