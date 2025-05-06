using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
//using Unity.NetCode;

//Attaches a ghost component to the player entity
public class PlayerAuthoring : MonoBehaviour
{

    public GameObject Camera; //new
    public class Baker : Baker<PlayerAuthoring> {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player());

            //Maybe remove? Initializes the IComponentData
            AddComponent(entity, new PlayerInputStateGhost());

        }
    }
}


public struct Player : IComponentData {

}


