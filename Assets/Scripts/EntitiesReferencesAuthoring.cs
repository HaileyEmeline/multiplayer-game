using Unity.Entities;
using UnityEngine;

//Converts the gameobject prefabs into an entities prefab
public class EntitiesReferencesAuthoring : MonoBehaviour
{

    //For the player prefab
    public GameObject playerPrefabGameObject;

    //For the bullet prefab
    public GameObject bulletPrefabGameObject;

    public class Baker : Baker<EntitiesReferencesAuthoring> {
        public override void Bake(EntitiesReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences {
                playerPrefabEntity = GetEntity(authoring.playerPrefabGameObject, TransformUsageFlags.Dynamic),
                bulletPrefabEntity = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct EntitiesReferences : IComponentData {
    public Entity playerPrefabEntity;
    public Entity bulletPrefabEntity;
}
