using JetBrains.Annotations;
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

//used to include experimental graphview as well

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))] 
//[UpdateAfter(typeof(GhostInputSystemGroup))]

partial struct BulletSystem : ISystem
{

    public Entity playerEntity;
    public Entity mouseEntity;

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<NetcodePlayerInputSystem>();
        //state.RequireForUpdate<MousePosition>();
        
    }
    
    //[BurstCompile]
    public void OnUpdate(ref SystemState state) {

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        if (playerEntity == Entity.Null) {
            playerEntity = SystemAPI.GetSingletonEntity<PlayerPosition>();
        }

        int localPlayerNetworkId = -1;
        foreach (var ghostOwner in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<GhostOwnerIsLocal>()) {
            localPlayerNetworkId = ghostOwner.ValueRO.NetworkId;
        }


        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRW<Bullet> bullet,
            Entity entity)
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<Bullet>>().WithEntityAccess().WithAll<Simulate>()) {

            float moveSpeed = 30f;

            bool foundLocalInput = false;


            foreach (var (inputState, ghostOwner) in SystemAPI.Query<RefRO<PlayerInputStateGhost>, RefRO<GhostOwner>>()) { //used to have with all ghost owner
                
                if (ghostOwner.ValueRO.NetworkId == localPlayerNetworkId) {
                foundLocalInput = true;
                float3 aimDirection = inputState.ValueRO.aimDirection;

                Debug.Log("Aim Direction: " + aimDirection + ghostOwner.ValueRO.NetworkId);


                //if (math.lengthsq(aimDirection) > 0f) {
                  //  aimDirection = math.normalize(aimDirection);
                //} else {
                 //   aimDirection = new float3(1f, 0f, 0f);
                //}

                //Debug.Log("Aim Direction: " + aimDirection);
                //if ghost owner local, else set to all 0s
                localTransform.ValueRW.Position += aimDirection * moveSpeed * SystemAPI.Time.DeltaTime;

                //To destroy
                //Running client on server
                if (state.World.IsServer()) {
                    bullet.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                    if (bullet.ValueRW.timer <= 0f) {
                        entityCommandBuffer.DestroyEntity(entity);
                    }
                }

                } //with the if
            }

            if (foundLocalInput == false) {
                Debug.Log("No local input!");
                //break;
            }


            //var mousePos = SystemAPI.GetComponent<MousePosition>(mouseEntity);

            //Vector3 mousePos = Input.mousePosition;
            //var playerTranslation = SystemAPI.GetComponentData<Translation>(playerEntity);
            
            //Get player position
            var playerPosition = SystemAPI.GetComponent<PlayerPosition>(playerEntity);
            Vector3 playerPos = playerPosition.position; //CHANGE
            
            //Vector3 mousePos = MousePosition.ReferenceEqualsdirection;
            //Calculate angle of aim
            Vector3 lookDirection = Camera.main.ScreenToWorldPoint(new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0));
            Vector3 mod_direction = new Vector3(lookDirection.x, lookDirection.y, 0);
            //Vector3 lookAngle = new float3(mousePos - playerPos);// * Mathf.Rad2Deg;
            //Debug.Log("We made it here");
            //normalize direction>
            //var mosu = NetcodePlayerInputSystem.RefRO.mousePos;
            Vector3 localDirection = mod_direction;
            //float3 direction = math.normalize(localDirection - playerPos);
            //ORIGINAL BELOW THAT WORKS KINDA
            float3 direction = math.normalize(mod_direction - playerPos);
            direction[2] = 0f;
            //Debug.Log("Direction: " + direction);

            //physicsVelocity.ValueRW.Linear = direction * moveSpeed; // * SystemAPI.Time.DeltaTime;
            //firePoint.rotation = Quaternion.Euler(0,0,lookAngle);
            //localTransform.ValueRW.Rotation = Quaternion.Euler(0,0,lookAngle);
            
            //localTransform.ValueRW.Position += direction * moveSpeed * SystemAPI.Time.DeltaTime;

            
            //float3 aimDirection = mousePos - playerPos;
            //Debug.Log("aim direction: " + aimDirection);
            //localTransform.ValueRW.Position += new Unity.Mathematics.float3(1, 0, 0) * moveSpeed * SystemAPI.Time.DeltaTime;
            /*localTransform.ValueRW.Position += direction * moveSpeed * SystemAPI.Time.DeltaTime;

            //To destroy
            //Running client on server
            if (state.World.IsServer()) {
                bullet.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                if (bullet.ValueRW.timer <= 0f) {
                    entityCommandBuffer.DestroyEntity(entity);
                }
            }*/
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}
