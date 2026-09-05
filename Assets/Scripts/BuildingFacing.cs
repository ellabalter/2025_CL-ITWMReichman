using UnityEngine;

[DisallowMultipleComponent]
public class BuildingFacing : MonoBehaviour
{
    [Tooltip("Extra yaw (degrees) added to this prefab when placed. Use 90/180/270 to correct prefabs whose native front doesn't point along +Z.")]
    public float yawOffset = 0f;
}
