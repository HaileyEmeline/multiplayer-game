using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Unity.Transforms;

public class GroundAuthoring : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Creates an entity manager
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        //Creates the ground entity
        Entity entity = entityManager.CreateEntity();

        //Add boxcollider2D
        entityManager.AddComponentData(entity, new LocalToWorld {Value = float4x4.TRS(new float3(transform.position.x, transform.position.y, 0f), quaternion.identity, new float3(1f, 1f, 1f))} );

        //Create the boxcollider2d for the ground entity
        PhysicsCollider collider = new PhysicsCollider {
            Value = Unity.Physics.BoxCollider.Create(new BoxGeometry {
                Size = new float3(10f, 1f, 0),
                Orientation = quaternion.identity
            })
        };

        //Adds collider to the ground entity
        entityManager.AddComponentData(entity, collider);

        //Makes ground static
        entityManager.AddComponent<PhysicsMass>(entity); //has no mass.

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
