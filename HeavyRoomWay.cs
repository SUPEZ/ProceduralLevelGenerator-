using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRoomWay : Doorway {

    public bool forBranch;

    public enum TypeDoorway
    {
        enter,
        exit
    }
    public TypeDoorway Typeway;

    public enum Orientation
    {
        None,
        L,
        R
    }

    public Orientation Orient;
}
