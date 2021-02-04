using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cross : MonoBehaviour
{

    public Doorway[] doorways;

    public MeshCollider meshCollider;

    public enum RoomType
    {
        Corridor,
        Cross,
        TRoom,
        Turn,
        End,
        Start
    }

    public RoomType roomType;

    public Bounds RoomBounds
    {
        get { return meshCollider.bounds; }
    }
}
