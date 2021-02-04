using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRoom : MonoBehaviour {

    private int idHeavyRoom { get; set; }
    public HeavyRoomWay[] heavyroomways;
    public Doorway[] doorways;
    public MeshCollider meshCollider;

    public enum HeavyRoomType
    {
        Straight,
        Cross,
        TLtunnel,
        TRtunnel,
        CurvR,
        CurvL,
        StraightRSemiBranch,
        StraightLSemiBranch,
        End
    }

    public HeavyRoomType heavyRoomType;

    public Bounds HeavyRoomBounds
    {
        get { return meshCollider.bounds; }
    }
}
