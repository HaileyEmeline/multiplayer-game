using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Unity.Transforms;

public class Player2DAuthoring : MonoBehaviour
{

    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity playerEntity = entityManager.CreateEntity();

        //Adding rigidbody2D so we can use physics! 
        PhysicsVelocity physicsVelocity = new PhysicsVelocity {Linear = float3.zero};
        entityManager.AddComponentData(playerEntity, physicsVelocity);

        //Adding boxcollider
        entityManager.AddComponentData(playerEntity, new LocalToWorld {Value = float4x4.TRS(new float3(transform.position.x, transform.position.y, 0f), quaternion.identity, new float3(1f, 1f, 1f))} );

        PhysicsCollider collider = new PhysicsCollider {
            Value = Unity.Physics.BoxCollider.Create(new BoxGeometry {
                Size = new float3(1f, 2f, 0),
                Orientation = quaternion.identity
            })
        };

        //Adds collider to the ground entity
        entityManager.AddComponentData(playerEntity, collider);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
