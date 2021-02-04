using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoMaper 
{
    public ArrayList mapedRooms = new ArrayList();

    int countRooms = 0;
    int countCorridors = 0;

    int countTunnel = 0;
    int countTunnelRooms = 0;
    int countTunnelCorridors = 0;

    int countHeavyRoom = 0;
    
    public void Start()
    {
        //Заполнение списков комнат 
        // a) Для бункера 
        addAbstractStartRoom();

        addAbstractCorridor();
        addAbstractCycleRoom();
        addAbstractCorridor();

        addArchetypeConnector();

        // b) Для туннелей 
        countRooms++;
        addAbstractTunnel();
        addAbstractConnectorTunnel();
        addAbstractTunnel();

        addArchetypeConnector();

        // c) Для строгого режима 
        addAbstractChainHeavyRoom();

    }
    //добавляем комнату между архитипами
    void addArchetypeConnector()
    {
        AbstractRoom room = new AbstractRoom();
        room.countDoorway = 2;
        countRooms++;
        room.idRoom = countRooms;

        mapedRooms.Add(room);

        Debug.Log("комната архитипа " + room.idRoom + " намечена");
    }

    //добавляем начало остова в список
    void addAbstractStartRoom()
    {
        AbstractRoom startRoom = new AbstractRoom();
        startRoom.countDoorway = 1;
        startRoom.idRoom = countRooms;

        mapedRooms.Add(startRoom);

        Debug.Log("старт " + startRoom.idRoom + " намечен");
    }
    //добавляем комнату в список
    void addAbstractCycleRoom()
    {
        AbstractRoom room = new AbstractRoom();
        room.countDoorway = 8;
        countRooms++;
        room.idRoom = countRooms;

        mapedRooms.Add(room);

        Debug.Log("комната " + room.idRoom + " намечена");
    }

    void addAbstractRoom()
    {
        AbstractRoom room = new AbstractRoom();
        room.countDoorway = 2;
        countRooms++;
        room.idRoom = countRooms;

        mapedRooms.Add(room);

        Debug.Log("комната " + room.idRoom + " намечена");
    }
    //добавляем коридор в список
    void addAbstractCorridor()
    {
        AbstractCorridor corridor = new AbstractCorridor();
        corridor.previousRoom = countRooms;
        corridor.nextRoom = countRooms + 1;
        corridor.idCorridor = countCorridors;
        countCorridors++;

        mapedRooms.Add(corridor);

        Debug.Log("коридор " + corridor.idCorridor + "  намечен");
    }
    //добавляем конец остова в список
    void addAbstractEndRoom()
    {
        addAbstractCorridor();
        AbstractRoom endRoom = new AbstractRoom();
        endRoom.countDoorway = 1;
        countRooms++;
        endRoom.idRoom = countRooms;

        mapedRooms.Add(endRoom);
        Debug.Log("конец " + endRoom.idRoom + " намечен");
    }
    //добавляем туннель 
    void addAbstractTunnel()
    {
        AbstractTunnel tunnel = new AbstractTunnel();

        tunnel.idArchetypeRoom = countRooms;

        tunnel.idTunnel = countTunnel;

        countTunnel++;

        mapedRooms.Add(tunnel);

        Debug.Log("туннель " + tunnel.idTunnel + " намечен");
    }
    //добавляем конектор между туннелями
    void addAbstractConnectorTunnel()
    {
        AbstractTunnelRoom room = new AbstractTunnelRoom();
        room.countDoorway = 2;
        countTunnelRooms++;
        room.idRoom = countTunnelRooms;

        mapedRooms.Add(room);

        Debug.Log("комната коннектора туннеля " + room.idRoom + " намечена");

    }
    void addAbstractChainHeavyRoom()
    {
        AbstractHeavyRoom heavyRoom = new AbstractHeavyRoom();

        mapedRooms.Add(heavyRoom);

        Debug.Log("Комната строгого режима " + heavyRoom.idHeavyRoom + " намечен");
    }


}