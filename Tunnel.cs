using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tunnel : MonoBehaviour {

    private int idTunnel { get; set; }
    public TunnelWay[] tunnelways;
    public DoorwayTunnel[] doorways;
    public MeshCollider meshCollider;

    public enum TunnelType
    {
        Straight,
        Cross,
        TLtunnel,
        TRtunnel,
        CurvR,
        CurvL,
        End,
        RuinedEnd,
        Connector
    }

    public TunnelType tunnelType;

    public enum StyleTunnel
    {
        light,
        heavy
    }

    public StyleTunnel tunnelStyle;

    public Bounds TunnelBounds
    {
        get { return meshCollider.bounds; }
    }
}
