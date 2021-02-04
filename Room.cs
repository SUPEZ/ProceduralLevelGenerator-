using UnityEngine;

public class Room : MonoBehaviour {

    public int idRoom { get; set; }
    public Doorway[] doorways;
    public MeshCollider meshCollider;

    public enum RoomType
    {
        Corridor,
        Cross,
        TRoom,
        Turn,
        End,
        Start,
        Connector
    }

    public RoomType roomType;

    public enum StyleRoom
    {
        Bunker,
        Heavy
    }

    public StyleRoom roomStyle;

    public Bounds RoomBounds
    {
        get { return meshCollider.bounds; }
    }
}
