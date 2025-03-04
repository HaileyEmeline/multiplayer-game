using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
//using Unity.NetCode;

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

            //Entity Camera = GetEntity(TransformUsageFlags.Dynamic); //new
            //AddComponent(entity, Camera);
            /* AddComponent(GetEntity(TransformUsageFlags.Dynamic), new Player 
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic),

                Camera = GetEntity(authoring.Camera, TransformUsageFlags.Dynamic);
            }); */
        }
    }
}


public struct Player : IComponentData {

}


