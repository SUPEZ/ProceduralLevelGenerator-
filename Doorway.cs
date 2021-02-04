using UnityEngine;

public class Doorway : MonoBehaviour {

    public Room roomLink;

    public HeavyRoom heavyRoomLink;

    public MeshCollider meshCollider;

    private void OnDrawGizmos()
    {
        Ray ray = new Ray(transform.position, transform.rotation * Vector3.forward);

        Gizmos.color = Color.red;
        Gizmos.DrawRay (ray);
    }
    public Bounds Bounds

    {
        get { return meshCollider.bounds; }
    }

}
