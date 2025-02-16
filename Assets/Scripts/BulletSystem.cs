using JetBrains.Annotations;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct BulletSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach((
            RefRW<LocalTransform> localTransform,
            RefRW<Bullet> bullet,
            Entity entity)
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<Bullet>>().WithEntityAccess().WithAll<Simulate>()) {

            float moveSpeed = 30f;
            //Might not work because changes the z axis? in 2D?
            localTransform.ValueRW.Position += new Unity.Mathematics.float3(1, 0, 0) * moveSpeed * SystemAPI.Time.DeltaTime;

            //To destroy
            //Running client on server
            if (state.World.IsServer()) {
                bullet.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                if (bullet.ValueRW.timer <= 0f) {
                    entityCommandBuffer.DestroyEntity(entity);
                }
            }
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}
