using UnityEngine;

public class TunnelWay : Doorway {

    public Tunnel TunnelLink;
    public enum TypeDoorway 
    {
        enter,
        exit
    }
    public TypeDoorway Typeway;
}
