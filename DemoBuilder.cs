using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bearroll;
using Bearroll.GDOC_Internal;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

public class DemoBuilder : SingletonMonoBehaviour<DemoBuilder>
{
    public static bool WorldIsReady = false;
    public bool SkipBuild = false;
    
    public Room StartRoomPrefab, HangarPrefab;
   
    public List<Room> BunkerDeadlockPrefabs = new List<Room>();
    public List<Tunnel> LigthTunnelDeadlockPrefabs = new List<Tunnel>();
    public List<Tunnel> LigthTunnelRoomDeadlockPrefabs = new List<Tunnel>();
    public List<Tunnel> HeavyTunnelDeadlockPrefabs = new List<Tunnel>();
    public List<Tunnel> HeavyTunnelRoomDeadlockPrefabs = new List<Tunnel>();
    public List<Room> HeavyLabDeadlockPrefabs = new List<Room>();
    public List<Room> BunkerConnectorPrefabs = new List<Room>();
    public List<Room> BunkerCorridorPrefabs = new List<Room>();
    public List<Room> HeavyConnectorPrefabs = new List<Room>();
    public List<HeavyRoom> HeavyRoomsPrefabs = new List<HeavyRoom>();
    public List<HeavyRoom> HeavyCorridorPrefabs = new List<HeavyRoom>();
    public List<Tunnel> LightTunnelPrefabs = new List<Tunnel>();
    public List<Tunnel> TunnelroomPrefabs = new List<Tunnel>();
    public List<Tunnel> HeavyTunnelPrefabs = new List<Tunnel>();

    DemoMaper demoMaper;

    List<Doorway> availableDoorways = new List<Doorway>();
    List<TunnelWay> availableTunnelways = new List<TunnelWay>();
    List<DoorwayTunnel> availableTunnelDoorways = new List<DoorwayTunnel>();
    List<HeavyRoomWay> availableHeavyRoomWays = new List<HeavyRoomWay>();
    List<Tunnel> listForCycleTunnel = new List<Tunnel>();
    List<Room> placedRooms = new List<Room>();
    List<Tunnel> placedTunnels = new List<Tunnel>();
    List<Tunnel> placedRoomTunnels = new List<Tunnel>();
    List<HeavyRoom> placedHeavyRooms = new List<HeavyRoom>();
    List<Room> placedRoomDeadlocks = new List<Room>();
    List<Tunnel> placedTunnelDeadlocks = new List<Tunnel>();

    int idTunnel = 0;
    int _curvTunnelcnt = 0;
    int IndexTypeofRoomTunnel = 0;
    int idRoomTunnel = 0;
    int idFirstConTunnel = 0;
    int idHeavyRoom = 50;

    int CheckPointsForHeavyRoom = 3;
    int RndCurvForHeavyRoom, RangeForHeavyRooms;

    HeavyRoom.HeavyRoomType CurvHeavyRoom;

    int idDeadlock = 10;

    int _DeadlockPrefabTunnelcnt = 0;
    int _countPrefabs = 0;

    int _cntBunkerDeadlock = 0;
    int _cntHLabDeadlock = 0;
    
    int cntLTunnelDeadlock = 0;
    int cntHTunnelDeadlock = 0;

    DoorwayTunnel.Orientation turnTunnel = DoorwayTunnel.Orientation.L;

    LayerMask roomLayerMask;

    System.Random _rnd;
    Queue<int> _generatedValues;

    [SerializeField] bool _isLogEnabled = true;
    [SerializeField] string _logTag = "DemoBuilder";
    ILogger _logger;

    bool _isLocalMode;
    
    Coroutine _workingCoroutine;

    void Start()
    {
        _logger = new Logger(new SCPLogHandler());
        _logger.logEnabled = _isLogEnabled;

        _isLocalMode = FindObjectOfType<SteamManager>() == null;
    }

    public void Build()
    {
        if (_isLocalMode)
        {
            LocalBuildInit();
        }
        else
        {
            if (NetworkServer.active)
                ServerBuildInit();
            else
                ClientBuildInit();   
        }

        demoMaper = new DemoMaper();
        demoMaper.Start();
        roomLayerMask = LayerMask.GetMask("Room");
        _workingCoroutine = StartCoroutine(GenerateLevel());    
    }

    public void ResetBuilderState()
    {
        foreach (Room r in placedRooms)
            Destroy(r.gameObject);
        foreach (Tunnel t in placedTunnels)
            Destroy(t.gameObject);
        foreach (HeavyRoom t in placedHeavyRooms)
            Destroy(t.gameObject);
        foreach (Tunnel t in placedRoomTunnels)
            Destroy(t.gameObject);
        foreach (Room r in placedRoomDeadlocks)
            Destroy(r.gameObject);
        foreach (Tunnel t in placedTunnelDeadlocks)
            Destroy(t.gameObject);
        
        listForCycleTunnel.Clear();

        placedRooms.Clear();
        placedTunnels.Clear();
        placedHeavyRooms.Clear();
        placedRoomTunnels.Clear();
        placedRoomDeadlocks.Clear();
        placedTunnelDeadlocks.Clear();

        availableDoorways.Clear();
        availableTunnelDoorways.Clear();
        availableTunnelways.Clear();
        availableHeavyRoomWays.Clear();

        idTunnel = 0;
        _curvTunnelcnt = 0;
        idRoomTunnel = 0;
        idFirstConTunnel = 0;
        idDeadlock = 10;
        
        idHeavyRoom = 50;

        _cntBunkerDeadlock = 0;
        _cntHLabDeadlock = 0;
        
        cntLTunnelDeadlock = 0;
        cntHTunnelDeadlock = 0;
    }
    
    void ServerBuildInit()
    {
        if (_rnd != null)
            return;
        
        int seed = new System.Random().Next(0, int.MaxValue);
        _rnd = new System.Random(seed);
        _generatedValues = new Queue<int>();//new List<int>();
        _logger.Log(_logTag, "DemoBuilder::Build on server");
    }

    void ClientBuildInit()
    {
        _generatedValues = LobbyManager.Instance.CurrentLobby.LevelGeneratorValues;
        _logger.Log(_logTag, $"DemoBuilder::Build on client, queue size = {_generatedValues.Count}");
    }

    void LocalBuildInit()
    {
        // when steam is not initialized
        ServerBuildInit();
    }

    void ResetLevelGenerator()
    {
        StopCoroutine(_workingCoroutine);
        _logger.LogWarning(_logTag, "Reset level generator");
        

        ResetBuilderState();
        _generatedValues?.Clear();
        _workingCoroutine = StartCoroutine(GenerateLevel());
    }

    int GetNextGeneratedValue(int min = 0, int max = 0)
    {
        if (_isLocalMode || NetworkServer.active)
        {
            var value = _rnd.Next(min, max);
            _generatedValues.Enqueue(value);
            return value;
        }
        
        return _generatedValues.Dequeue();
    }

    IEnumerator GenerateLevel()
    {
        var Startup = new WaitForSeconds(1);
        var interval = new WaitForFixedUpdate();

        if (!SkipBuild)
        {

            yield return Startup;
            //Выставление комнат по списку 
            for (int i = 0; i < demoMaper.mapedRooms.Count; i++)
            {
                if (demoMaper.mapedRooms[i].GetType() == typeof(AbstractRoom))
                {
                    _logger.Log(_logTag, "Комната");
                    PlaceRoom((AbstractRoom) demoMaper.mapedRooms[i]);
                    yield return interval;
                }
                else if (demoMaper.mapedRooms[i].GetType() == typeof(AbstractHeavyRoom))
                {
                    _logger.Log(_logTag, "Генератор комнат строгого режима");
                    //Остов
                    int cntFirstCP = 0;
                    int cntSecondCP = 0;
                    int cntThirdCP = 0;
                    int lastMainID = 0;
                    int lastFirstBranchID = 0;
                    int lastSecondBranchID = 0;

                    var queueCurvs = new List<HeavyRoom.HeavyRoomType>();

                    RndCurvForHeavyRoom = GetNextGeneratedValue(0, 2);

                    for (int cntCP = 1; cntCP <= CheckPointsForHeavyRoom; cntCP++)
                    {
                        RangeForHeavyRooms = GetNextGeneratedValue(3, 5);

                        if (cntCP != 3)
                        {
                            for (int cntRoom = 0; cntRoom < RangeForHeavyRooms; cntRoom++)
                            {
                                if (cntCP == 1)
                                    cntFirstCP++;
                                else if (cntCP == 2)
                                    cntSecondCP++;

                                PlaceCurrentHeavyRoom(HeavyRoom.HeavyRoomType.Straight);
                                yield return interval;
                            }

                            switch (RndCurvForHeavyRoom)
                            {
                                case 0:
                                    CurvHeavyRoom = HeavyRoom.HeavyRoomType.CurvL;
                                    RndCurvForHeavyRoom++;
                                    break;
                                case 1:
                                    CurvHeavyRoom = HeavyRoom.HeavyRoomType.CurvR;
                                    RndCurvForHeavyRoom--;
                                    break;
                                default: break;
                            }

                            queueCurvs.Add(CurvHeavyRoom);
                            PlaceCurrentHeavyRoom(CurvHeavyRoom);

                            yield return interval;
                        }
                        else
                        {
                            for (int cntRoom = 0; cntRoom < RangeForHeavyRooms; cntRoom++)
                            {
                                cntThirdCP++;
                                PlaceCurrentHeavyRoom(HeavyRoom.HeavyRoomType.Straight);
                                yield return interval;
                            }

                            PlaceCurrentHeavyRoom(HeavyRoom.HeavyRoomType.End);
                            yield return interval;
                        }
                    }

                    _logger.Log(_logTag, placedHeavyRooms.Count);

                    lastMainID = placedHeavyRooms.Last().idHeavyRoom;
                    //Ветки генерации
                    //1 ветка
                    var currentTRoom = HeavyRoom.HeavyRoomType.End;
                    var queueBranch = new List<HeavyRoom.HeavyRoomType>();
                    var queueBranchCurvs = new List<HeavyRoom.HeavyRoomType>();

                    int RndIdFirstBranch = GetNextGeneratedValue(0, cntFirstCP) + 50;
                    int RndIdSecondBranch = GetNextGeneratedValue(cntFirstCP + 1, cntFirstCP + cntSecondCP + 1) + 50;

                    if (queueCurvs[0] == HeavyRoom.HeavyRoomType.CurvL)
                    {
                        queueBranchCurvs.Add(HeavyRoom.HeavyRoomType.CurvR);
                        currentTRoom = HeavyRoom.HeavyRoomType.TLtunnel;
                    }
                    else if (queueCurvs[0] == HeavyRoom.HeavyRoomType.CurvR)
                    {
                        queueBranchCurvs.Add(HeavyRoom.HeavyRoomType.CurvL);
                        currentTRoom = HeavyRoom.HeavyRoomType.TRtunnel;
                    }

                    ReplaceHeavyRoom(RndIdFirstBranch, currentTRoom);

                    for (int ControlID = RndIdSecondBranch - 1; ControlID > RndIdFirstBranch; ControlID--)
                    {
                        foreach (HeavyRoom room in placedHeavyRooms)
                        {
                            if (room.idHeavyRoom == ControlID)
                            {
                                if (room.heavyRoomType == HeavyRoom.HeavyRoomType.CurvL)
                                    queueBranch.Add(HeavyRoom.HeavyRoomType.CurvR);
                                else if (room.heavyRoomType == HeavyRoom.HeavyRoomType.CurvR)
                                    queueBranch.Add(HeavyRoom.HeavyRoomType.CurvL);
                                else
                                    queueBranch.Add(room.heavyRoomType);
                            }
                        }
                    }

                    foreach (HeavyRoom.HeavyRoomType heavyRoomType in queueBranch)
                        PlaceCurrentHeavyRoom(heavyRoomType);

                    lastFirstBranchID = placedHeavyRooms[placedHeavyRooms.Count - 1].idHeavyRoom;

                    ReplaceHeavyRoom(RndIdSecondBranch, currentTRoom);

                    ClearListHeavyRoomWays();
                    queueBranch.Clear();

                    //2 ветка
                    int RndIdThirdBranch =
                        GetNextGeneratedValue(cntFirstCP + cntSecondCP + 2, cntFirstCP + cntSecondCP + cntThirdCP + 2) +
                        50;
                    int secondRndIdSecondBranch =
                        GetNextGeneratedValue(cntFirstCP + 1, cntFirstCP + cntSecondCP + 1) + 50;

                    if (queueCurvs[1] == HeavyRoom.HeavyRoomType.CurvL)
                    {
                        queueBranchCurvs.Add(HeavyRoom.HeavyRoomType.CurvR);
                        currentTRoom = HeavyRoom.HeavyRoomType.TLtunnel;
                    }
                    else if (queueCurvs[1] == HeavyRoom.HeavyRoomType.CurvR)
                    {
                        queueBranchCurvs.Add(HeavyRoom.HeavyRoomType.CurvL);
                        currentTRoom = HeavyRoom.HeavyRoomType.TRtunnel;
                    }

                    ReplaceHeavyRoom(RndIdThirdBranch, currentTRoom);

                    for (int ControlID = secondRndIdSecondBranch + 1; ControlID < RndIdThirdBranch; ControlID++)
                    {
                        foreach (HeavyRoom room in placedHeavyRooms)
                        {
                            if (room.idHeavyRoom == ControlID)
                            {
                                if (room.heavyRoomType == HeavyRoom.HeavyRoomType.TLtunnel)
                                    queueBranch.Add(HeavyRoom.HeavyRoomType.Straight);
                                else if (room.heavyRoomType == HeavyRoom.HeavyRoomType.TRtunnel)
                                    queueBranch.Add(HeavyRoom.HeavyRoomType.Straight);
                                else
                                    queueBranch.Add(room.heavyRoomType);
                            }
                        }
                    }

                    foreach (HeavyRoom.HeavyRoomType heavyRoomType in queueBranch)
                        PlaceCurrentHeavyRoom(heavyRoomType);

                    lastSecondBranchID = placedHeavyRooms[placedHeavyRooms.Count - 1].idHeavyRoom;

                    if (RndIdSecondBranch == secondRndIdSecondBranch)
                        ReplaceHeavyRoom(secondRndIdSecondBranch, HeavyRoom.HeavyRoomType.Cross);
                    else if (RndIdSecondBranch != secondRndIdSecondBranch)
                        ReplaceHeavyRoom(secondRndIdSecondBranch, currentTRoom);

                    ClearListHeavyRoomWays();
                    yield return interval;

                    //Комнаты для полезной нагрузки
                    int cntSemiBranch = 3;
                    var queueStraightRooms = new List<HeavyRoom>();

                    var currentFirstSemiBranchType = HeavyRoom.HeavyRoomType.End;
                    var currentSecondSemiBranchType = HeavyRoom.HeavyRoomType.End;

                    foreach (HeavyRoom room in placedHeavyRooms)
                    {
                        if (room.heavyRoomType == HeavyRoom.HeavyRoomType.Straight)
                            queueStraightRooms.Add(room);
                    }

                    if (queueCurvs[0] == HeavyRoom.HeavyRoomType.CurvR)
                        currentFirstSemiBranchType = HeavyRoom.HeavyRoomType.StraightLSemiBranch;
                    else if (queueCurvs[0] == HeavyRoom.HeavyRoomType.CurvL)
                        currentFirstSemiBranchType = HeavyRoom.HeavyRoomType.StraightRSemiBranch;

                    if (queueCurvs[1] == HeavyRoom.HeavyRoomType.CurvR)
                        currentSecondSemiBranchType = HeavyRoom.HeavyRoomType.StraightLSemiBranch;
                    else if (queueCurvs[1] == HeavyRoom.HeavyRoomType.CurvL)
                        currentSecondSemiBranchType = HeavyRoom.HeavyRoomType.StraightRSemiBranch;

                    for (int cntID = 1; cntID <= cntSemiBranch; cntID++)
                    {
                        bool deadlockPlaced = false;
                        var cloneQueueStraightRooms = new List<HeavyRoom>(queueStraightRooms);
                        _logger.Log(_logTag, "Кол-во прямых комнат " + queueStraightRooms.Count);
                        HeavyRoom RndRoom = queueStraightRooms[GetNextGeneratedValue(1, queueStraightRooms.Count)];
                        _logger.Log(_logTag, "Выбор " + RndRoom.idHeavyRoom);
                        if ((50 < RndRoom.idHeavyRoom) && (RndRoom.idHeavyRoom < 50 + cntFirstCP))
                        {
                            ReplaceHeavyRoom(RndRoom.idHeavyRoom, currentFirstSemiBranchType);
                            deadlockPlaced = true;
                        }
                        else if ((50 + cntFirstCP < RndRoom.idHeavyRoom) &&
                                 (RndRoom.idHeavyRoom < 50 + cntFirstCP + cntSecondCP + 1))
                            {
                            int cntRnd = GetNextGeneratedValue(0, 2);
                            if (cntRnd == 0)
                            {
                                ReplaceHeavyRoom(RndRoom.idHeavyRoom, currentFirstSemiBranchType);
                                deadlockPlaced = true;
                            }
                            else if (cntRnd == 1)
                            {
                                ReplaceHeavyRoom(RndRoom.idHeavyRoom, currentSecondSemiBranchType);
                                deadlockPlaced = true;
                            }
                        }
                        else if ((50 + cntFirstCP + cntSecondCP + 1 < RndRoom.idHeavyRoom) &&
                                 (RndRoom.idHeavyRoom <= 50 + cntFirstCP + cntSecondCP + cntThirdCP + 2))
                        {
                            ReplaceHeavyRoom(RndRoom.idHeavyRoom, currentSecondSemiBranchType);
                            deadlockPlaced = true;
                        }
                        else if ((lastMainID < RndRoom.idHeavyRoom) &&
                                 (RndRoom.idHeavyRoom <= lastFirstBranchID))
                        {
                            ReplaceHeavyRoom(RndRoom.idHeavyRoom, currentSecondSemiBranchType);
                            deadlockPlaced = true;
                        }
                        else if ((lastFirstBranchID < RndRoom.idHeavyRoom) &&
                                 (RndRoom.idHeavyRoom <= lastSecondBranchID))
                        {
                            ReplaceHeavyRoom(RndRoom.idHeavyRoom, currentSecondSemiBranchType);
                            deadlockPlaced = true;
                        }

                        if (deadlockPlaced == false)
                        {
                            Destroy(RndRoom.gameObject);
                            _logger.LogWarning(_logTag, "Тупик heavy не поставлен ");
                            ResetLevelGenerator();
                        }

                        foreach (HeavyRoom room in cloneQueueStraightRooms)
                        {
                            if (room.idHeavyRoom == RndRoom.idHeavyRoom + 1)
                                queueStraightRooms.Remove(room);

                            if (room.idHeavyRoom == RndRoom.idHeavyRoom - 1)
                                queueStraightRooms.Remove(room);
                        }

                        queueStraightRooms.Remove(RndRoom);
                    }
                }
                else if (demoMaper.mapedRooms[i].GetType() == typeof(AbstractCorridor))
                {
                    _logger.Log(_logTag, "Коридор");
                    PlaceCorridor((AbstractCorridor) demoMaper.mapedRooms[i]);
                    yield return interval;
                }
                else if (demoMaper.mapedRooms[i].GetType() == typeof(AbstractTunnelRoom))
                {
                    _logger.Log(_logTag, "Комната туннеля");
                    //Проверка логики
                    int cntLways = 0;
                    int cntRways = 0;

                    for (int o = 0; o < availableTunnelDoorways.Count; o++)
                    {
                        if (availableTunnelDoorways[o].Orient == DoorwayTunnel.Orientation.R)
                            cntRways++;
                        if (availableTunnelDoorways[o].Orient == DoorwayTunnel.Orientation.L)
                            cntLways++;

                    }
                    bool isChosen = false;
                    if (cntLways == 1 && cntRways == 1)
                    {
                        _logger.LogWarning(_logTag, "Недостаточно проемов в туннелях");
                        ResetLevelGenerator();
                    }
                    if (cntLways == 0 && cntRways == 0)
                    {
                        _logger.LogWarning(_logTag, "Нету проемов в туннелях");
                        ResetLevelGenerator();
                    }
                    if (cntRways > 1 && cntLways > 1)
                    {
                        int Randomchanсe = GetNextGeneratedValue(0, 2);
                        if (Randomchanсe == 1)
                        {
                            turnTunnel = DoorwayTunnel.Orientation.R;
                            _logger.Log(_logTag, "Правый");
                        }
                        else
                        if (Randomchanсe == 0)
                        {
                            turnTunnel = DoorwayTunnel.Orientation.L;
                            _logger.Log(_logTag, "Левый");
                        }
                        isChosen = true;
                    }
                    if (isChosen == false)
                    {
                        if (cntRways > 1)
                        {
                            turnTunnel = DoorwayTunnel.Orientation.R;
                            isChosen = true;
                            _logger.Log(_logTag, "Правый");
                        }
                        else
                        if (cntLways > 1)
                        {
                            turnTunnel = DoorwayTunnel.Orientation.L;
                            isChosen = true;
                            _logger.Log(_logTag, "Левый");
                        }
                    }
                    if (isChosen == false)
                    {
                        _logger.Log(_logTag, "Ориентация туннелей не выбрана");
                        ResetLevelGenerator();                    
                    }
                    PlaceConnectorTunnel();
                    yield return interval;
                    PlaceConnectorTunnel();
                }
                else if (demoMaper.mapedRooms[i].GetType() == typeof(AbstractTunnel))
                {
                    if (getTunnelNumber((AbstractTunnel) demoMaper.mapedRooms[i]) == 0)
                    {
                        _logger.Log(_logTag, "Метро");
                        for (int j = 0; j < 12; j++)
                        {
                            PlaceTunnel();
                            yield return interval;
                        }
                    }
                    else
                    {
                        //Корректировка
                        _logger.Log(_logTag, "Количество комнат для цикла = " + listForCycleTunnel.Count);

                        if (listForCycleTunnel.Count == 0)
                        {
                            _logger.LogWarning(_logTag, "Нету комнат в списке для цикла");
                            ResetLevelGenerator();
                        }
                        else
                        {
                            if (listForCycleTunnel[0].idTunnel > listForCycleTunnel[1].idTunnel)
                            {
                                idFirstConTunnel = 1;
                                Tunnel rep = listForCycleTunnel[0];
                                _logger.Log(_logTag, "Меняем порядок");
                                listForCycleTunnel[0] = listForCycleTunnel[1];
                                listForCycleTunnel[1] = rep;
                            }

                            Tunnel FirstforCycle, LastforCycle;
                            FirstforCycle = listForCycleTunnel[0];
                            LastforCycle = listForCycleTunnel[1];
                            listForCycleTunnel.Clear();
                            //Добавление остальных тунелей в список  
                            for (int cntForCycle = FirstforCycle.idTunnel + 1;
                                cntForCycle < LastforCycle.idTunnel;
                                cntForCycle++)
                            {
                                for (int cntID = 0 ; cntID < placedTunnels.Count ; cntID++)
                                {
                                    if (placedTunnels[cntID].idTunnel == cntForCycle)
                                    {
                                        listForCycleTunnel.Add(placedTunnels[cntID]);
                                        _logger.Log(_logTag, "Добавлен туннел для цикла = " + placedTunnels[cntID].idTunnel);
                                    }
                                }
                            }

                            listForCycleTunnel.Add(LastforCycle);
                            _logger.Log(_logTag, "Количество комнат для цикла = " + listForCycleTunnel.Count);
                        }
                        PlaceAfterConnectorTunnel();
                        yield return interval;
                        PlaceAnotherTunnelLine();
                    }
                }
            }

            yield return interval;

            var cloneAvailableDoorways = new List<Doorway>(availableDoorways);
            var cloneAvailableTunnelways = new List<TunnelWay>(availableTunnelways);
            var cloneAvailableTunnelDoorways = new List<DoorwayTunnel>(availableTunnelDoorways);

            //Корректировка цикла
            foreach (DoorwayTunnel doorwayTunnel in cloneAvailableTunnelDoorways)
            {
                if (doorwayTunnel.TunnelLink.tunnelType == Tunnel.TunnelType.Connector 
                    || (doorwayTunnel.TunnelLink.idTunnel == placedTunnels[placedTunnels.Count - 1].idTunnel 
                    && doorwayTunnel.Orient != turnTunnel))
                {
                    doorwayTunnel.gameObject.SetActive(false);
                    availableTunnelDoorways.Remove(doorwayTunnel);
                    _logger.Log(_logTag, "Удаляем ненужный проем");
                }
                yield return new WaitForSeconds(1 / 1000);
            }
            yield return interval;

            cloneAvailableTunnelDoorways = new List<DoorwayTunnel>(availableTunnelDoorways);
            
            //Тупики
            foreach (Doorway doorway in cloneAvailableDoorways)
            {
                if (doorway != null)
                {
                    PlaceDeadlockRoom(doorway);
                }

                yield return new WaitForSeconds(1 / 1000);
            }

            foreach (TunnelWay tunnelway in cloneAvailableTunnelways)
            {
                if (tunnelway != null)
                {
                    PlaceDeadlockTunnel(tunnelway);
                }

                yield return new WaitForSeconds(1 / 1000);
            }

            foreach (DoorwayTunnel doorwayTunnel in cloneAvailableTunnelDoorways)
            {
                if (doorwayTunnel != null)
                {
                    PlaceDeadlockTunnelRoom(doorwayTunnel);
                }

                yield return new WaitForSeconds(1 / 1000);
            }

            yield return interval;
            //Уничтожение коллайдеров комнат
            _logger.Log(_logTag, "Удаляем коллайдеры");
            foreach (Tunnel t in placedTunnels)
                Destroy(t.meshCollider.gameObject);
            foreach (Room r in placedRooms)
                Destroy(r.meshCollider.gameObject);
            foreach (Room d in placedRoomDeadlocks)
                Destroy(d.meshCollider.gameObject);
            foreach (HeavyRoom hr in placedHeavyRooms)
                Destroy(hr.meshCollider.gameObject);
            foreach (Tunnel dt in placedTunnelDeadlocks)
                Destroy(dt.meshCollider.gameObject);
            foreach (Tunnel rt in placedRoomTunnels)
                Destroy(rt.meshCollider.gameObject);

            // Спавн дверей
            //if (NetworkServer.active)
            // {
            // эти вещи надо вызывать и на клиенте и на сервере, чтобы они могли заинититься на клиенте тоже и подчистить себя, если необходимо
            NetworkDoorSpawner[] doorSpawners = GameObject.FindObjectsOfType<NetworkDoorSpawner>();
            for (int d = 0; d < doorSpawners.Length; d++)
                doorSpawners[d].Spawn();
            //}

            LootSpawnerManager.Instance.InitializeOnReady(); // Спавнеры
            LootSpawnerManager.Instance.ActivateSpawners(); // Спавнеры
            //for (int i = 0; i < placedTunnels.Count; i++)
            //{
            //    GDOC.ProcessNewObject(placedTunnels[i].gameObject);
            //}
            //for (int i = 0; i < placedRooms.Count; i++)
            //{
            //    GDOC.ProcessNewObject(placedRooms[i].gameObject);
            //}
            //for (int i = 0; i < placedRoomDeadlocks.Count; i++)
            //{
            //    GDOC.ProcessNewObject(placedRoomDeadlocks[i].gameObject);
            //}
            //for (int i = 0; i < placedHeavyRooms.Count; i++)
            //{
            //    GDOC.ProcessNewObject(placedHeavyRooms[i].gameObject);
            //}
            //for (int i = 0; i < placedTunnelDeadlocks.Count; i++)
            //{
            //    GDOC.ProcessNewObject(placedTunnelDeadlocks[i].gameObject);
            //}
            //for (int i = 0; i < placedRoomTunnels.Count; i++)
            //{
            //    GDOC.ProcessNewObject(placedRoomTunnels[i].gameObject);
            //}

            // refresh prefab's lightmaps
            ftLightmapsStorage[] bakedInfo = GameObject.FindObjectsOfType<ftLightmapsStorage>();
            for (int i = 0; i < bakedInfo.Length; i++)
            {
                ftLightmaps.RefreshScene(gameObject.scene, bakedInfo[i]);
            }

            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < bakedInfo.Length; i++)
            {
                ftLightmaps.RefreshScene2(gameObject.scene, bakedInfo[i]);
            }

            // refresh reflection probes
            ReflectionProbe[] reflectionProbes = GameObject.FindObjectsOfType<ReflectionProbe>();
            for (int i = 0; i < reflectionProbes.Length; i++)
            {
                if (reflectionProbes[i].mode == ReflectionProbeMode.Realtime &&
                    reflectionProbes[i].refreshMode == ReflectionProbeRefreshMode.OnAwake)
                {
                    reflectionProbes[i].RenderProbe();
                }
            }

            yield return new WaitForSeconds(0.5f);

            if (NetworkServer.active)
            {
                if (!LobbyManager.Instance.CurrentLobby.TryUpdateLevelGeneratorValues(_generatedValues))
                    _logger.LogError(_logTag, "Can't update level gen values for current lobby");
            }
        }
        _logger.LogWarning(_logTag, "World is ready");
        WorldIsReady = true;
    }

    #region GDOC
    public List<GDOC_Occludee> occludeeList = new List<GDOC_Occludee>();
    
    bool _alreadyGdoc = false;

    public void AddGddcOccludeeRecursive(List<MonoBehaviour> monoBehs)
    {
        _logger.Log(_logTag, "GDOC AddGddcOccludeeRecursive");

        GameObject[] renderers = null;
        GameObject[] lods = null;
        GameObject[] lodObjects = null;

        GDOC_Occludee[] occludees = GameObject.FindObjectsOfType<GDOC_Occludee>();
        occludeeList.AddRange(occludees);

        for (int i = 0; i < monoBehs.Count; i++)
        {
            lods = GetLods(monoBehs[i]);
            lodObjects = GetLodObjects(monoBehs[i]);
            renderers = GetMeshRenderersWithoutLodGroups(monoBehs[i]);

            renderers = renderers.Except(lodObjects).ToArray();
            renderers = renderers.Except(lods).ToArray();
            //lods = lods.Except(renderers.ToList()).ToArray();

            for (int j = 0; j < lods.Length; j++)
            {
                if (lods[j] != null)
                {
                    //GDOC_Occludee occludee = lods[j].gameObject.GetComponent<GDOC_Occludee>();
                    //if (occludee != null)
                    //{
                    //    Destroy(occludee);
                    //}

                    GDOC_Occludee occludee = lods[j].gameObject.AddComponent<GDOC_Occludee>();
                    occludee.mode = GDOC_OccludeeMode.MeshRendererGroup;
                    occludee.GrabLODGroupRenderers();
                    occludee.mode = GDOC_OccludeeMode.MeshRendererGroup;
                    GDOC.AddOccludee(occludee);

                    occludeeList.Add(occludee);
                }
            }

            for (int j = 0; j < renderers.Length; j++)
            {
                if (renderers[j] != null && renderers[j].gameObject.activeSelf)
                {
                    GDOC_Occludee occludee = renderers[j].gameObject.AddComponent<GDOC_Occludee>();
                    occludee.mode = GDOC_OccludeeMode.MeshRenderer;
                    GDOC.AddOccludee(occludee);

                    occludeeList.Add(occludee);
                }
            }
        }
    }

    public List<MonoBehaviour> demoRooms;
    
    public void EnableGDOCForCamera(GDOC gdoc)
    {
        if (!_alreadyGdoc)
        {
            _logger.Log(_logTag, "GDOC Calc start");

            AddGddcOccludeeRecursive(placedTunnels.ConvertAll(element => (MonoBehaviour) element));
            AddGddcOccludeeRecursive(placedRooms.ConvertAll(element => (MonoBehaviour) element));
            AddGddcOccludeeRecursive(placedRoomDeadlocks.ConvertAll(element => (MonoBehaviour) element));
            AddGddcOccludeeRecursive(placedHeavyRooms.ConvertAll(element => (MonoBehaviour) element));
            AddGddcOccludeeRecursive(placedTunnelDeadlocks.ConvertAll(element => (MonoBehaviour) element));
            AddGddcOccludeeRecursive(placedRoomTunnels.ConvertAll(element => (MonoBehaviour) element));

            if (demoRooms != null)
                AddGddcOccludeeRecursive(demoRooms);
            
            _logger.Log(_logTag, "[GDOC]: "+occludeeList.Count+" objects");

            _alreadyGdoc = true;
        }
        else
        {
            GDOC_Occludee[] occludees = GameObject.FindObjectsOfType<GDOC_Occludee>();

            if (occludees.Length != occludeeList.Count)
                occludeeList.AddRange(occludees.Except(occludeeList));

            for (int i = 0; i < occludeeList.Count; i++)
                GDOC.AddOccludee(occludeeList[i]);

            _logger.Log(_logTag, "[GDOC]: " + occludeeList.Count + " objects");
        }
    }

    public void RemoveAllOccludies()
    {
        _logger.Log(_logTag, "GDOC RemoveAllOccludies");

        for (int i = 0; i < occludeeList.Count; i++)
            GDOC.RemoveOccludee(occludeeList[i]);

        //occludeeList.Clear();
    }
    #endregion GDOC

    #region LOD
    public GameObject[] GetLods(MonoBehaviour monoBehaviour)
    {
        return monoBehaviour.GetComponentsInChildren<LODGroup>().ToList().ConvertAll(lod => lod.gameObject).ToArray();
    }

    public GameObject[] GetLodObjects(MonoBehaviour monoBehaviour)
    {
        var lodGroups = monoBehaviour.GetComponentsInChildren<LODGroup>().ToList();
        List<GameObject> lodGameObjects = new List<GameObject>();
        for (int i = 0; i < lodGroups.Count; i++)
        {
            LOD[] lods = lodGroups[i].GetLODs();
            for (int j = 0; j < lods.Length; j++)
            {
                for (int k = 0; k < lods[j].renderers.Length; k++)
                {
                    if(lods[j].renderers[k] != null && lods[j].renderers[k].gameObject != lodGroups[i].gameObject)
                        lodGameObjects.Add(lods[j].renderers[k].gameObject);
                }
            }
        }
        return lodGameObjects.ToArray();
    }

    public GameObject[] GetMeshRenderersWithoutLodGroups(MonoBehaviour monoBehaviour)
    {
        //List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
        //GameObjectUtil.IterateChildren(monoBehaviour.gameObject, go =>
        //    {
        //        LODGroup lodGroup = go.GetComponent<LODGroup>();

        //        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        //        if ((lodGroup == null || (go.transform.childCount == 0)) && mr != null)
        //        {
        //            meshRenderers.Add(mr);
        //        }
        //    },
        //    true,
        //    go => go.GetComponent<LODGroup>() && (go.transform.childCount > 0));

        //return meshRenderers.ConvertAll(mesh => mesh.gameObject).ToArray();

        return monoBehaviour.GetComponentsInChildren<MeshRenderer>().ToList().ConvertAll(m => m.gameObject).ToArray();
    }

    #endregion LOD

    #region Doorways
    //Списки проемов
    void AddDoorwaysToList(Room room, ref List<Doorway> list)
    {
        foreach (Doorway doorway in room.doorways)
        {
            int r = GetNextGeneratedValue(0, list.Count);
            list.Insert(r, doorway);
        }
    }

    void AddTunnelwaysToList(Tunnel tunnel, ref List<TunnelWay> list)
    {
        foreach (TunnelWay doorway in tunnel.tunnelways)
        {
            int r = GetNextGeneratedValue(0, list.Count);
            list.Insert(r, doorway);
        }
    }

    void AddTunnelDoorwaysToList(Tunnel tunnel, ref List<DoorwayTunnel> list)
    {
        foreach (DoorwayTunnel doorway in tunnel.doorways)
        {
            int r = GetNextGeneratedValue(0, list.Count);
            list.Insert(r, doorway);
        }
    }

    void AddHeavyRoomWaysToList(HeavyRoom heavyRoom, ref List<HeavyRoomWay> list)
    {
        foreach (HeavyRoomWay Way in heavyRoom.heavyroomways)
        {
            int r = GetNextGeneratedValue(0, list.Count);
            list.Insert(r, Way);

        }
    }  
    #endregion Doorways
    
    #region Rooms
    //Работа с комнатами
    void PlaceRoom(AbstractRoom mapedRoom)
    {
        //Проверка логики
        // Начало?
        if (mapedRoom.countDoorway == 1 && mapedRoom.idRoom == 0)
        {
            //Создаем начало бункера при помощи скрипта
            Room startRoom = Instantiate(StartRoomPrefab) as StartBunker;
            startRoom.transform.parent = this.transform;
            startRoom.idRoom = mapedRoom.idRoom;

            //Берем все дверные проемы из данной комнаты и рандомно добавляем их в список доступных комнат
            AddDoorwaysToList(startRoom, ref availableDoorways);

            //Позиция комнаты
            startRoom.transform.position = Vector3.zero;
            startRoom.transform.rotation = Quaternion.identity;

            placedRooms.Add(startRoom);
            _logger.Log(_logTag, "Placed start Bunker");
        }
        // Коннектор из бункера в туннели?
        if (mapedRoom.countDoorway == 2 && (mapedRoom.idRoom == 2))
        {
            Room currentRoom = Instantiate(BunkerConnectorPrefabs[GetNextGeneratedValue(0, BunkerConnectorPrefabs.Count)]) as Room;
            currentRoom.transform.parent = this.transform;
            currentRoom.idRoom = mapedRoom.idRoom;

            //Создание списков проемов
            List<Doorway> currentRoomDoorways = new List<Doorway>();
            
            //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
            availableDoorways.Add(currentRoom.doorways[0]);
            currentRoomDoorways.Add(currentRoom.doorways[0]);

            availableTunnelways.Add((TunnelWay)currentRoom.doorways[1]);
            bool roomPlaced = false;

            //берем все доступные проемы
            foreach (Doorway availableDoorway in availableDoorways)
            {
                //берем все доступные проемы для данной комнаты
                foreach (Doorway currentDoorway in currentRoomDoorways)
                {

                    if ((currentDoorway.roomLink.idRoom == 2)
                        && (availableDoorway.roomLink.roomType == Room.RoomType.Corridor)
                        )
                    {
                        //позиция комнаты 
                        PositionRoomAtDoorway(currentRoom, currentDoorway, availableDoorway);

                        //проверка на overlaps
                        if (CheckOverlaps(currentRoom))
                            continue;

                        roomPlaced = true;

                        //добавляем комнату в список поставленных комнат
                        placedRooms.Add(currentRoom);

                        //удаляем соединенные проемы 
                        currentDoorway.gameObject.SetActive(false);

                        availableDoorways.Remove(currentDoorway);

                        availableDoorway.gameObject.SetActive(false);

                        availableDoorways.Remove(availableDoorway);

                        //выход из цикла если комната на месте
                        break;
                    }
                }
                //выход из цикла если комната на месте
                if (roomPlaced)
                {
                    _logger.Log(_logTag, "Place коннектор " + currentRoom.idRoom + " from List");
                    break;
                }
            }
            if (!roomPlaced)
            {
                Destroy(currentRoom.gameObject);
                ResetLevelGenerator();
            }
        }
        // Коннектор из туннелей в heavy?
        if (mapedRoom.countDoorway == 2 && (mapedRoom.idRoom == 4))
        {
            _logger.Log(_logTag, "Коннектор в Heavy");
            ConnectorTunToHeavy currentRoom = Instantiate(HeavyConnectorPrefabs[GetNextGeneratedValue(0, HeavyConnectorPrefabs.Count)]) as ConnectorTunToHeavy;
            currentRoom.transform.parent = this.transform;
            currentRoom.idRoom = mapedRoom.idRoom;

            //Создание списков проемов
            var allAvailableDoorways = new List<DoorwayTunnel>(availableTunnelDoorways);
            var currentRoomDoorways  = new List<DoorwayTunnel>();

            //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
            availableHeavyRoomWays.Add(currentRoom.HeavyRoomWays[0]);
            currentRoomDoorways.Add((DoorwayTunnel)currentRoom.doorways[0]);
            availableTunnelDoorways.Add((DoorwayTunnel)currentRoom.doorways[0]);

            bool roomPlaced = false;
            //берем все доступные проемы
            foreach (DoorwayTunnel availableDoorway in allAvailableDoorways)
            {
                //берем все доступные проемы для данной комнаты
                foreach (DoorwayTunnel currentDoorway in currentRoomDoorways)
                {
                    if (
                        currentDoorway.GetType() == typeof(DoorwayTunnel)
                        && availableDoorway.TunnelLink.tunnelStyle == Tunnel.StyleTunnel.heavy
                        && availableDoorway.Orient == turnTunnel
                        )
                    {
                        //позиция комнаты 
                        PositionRoomAtDoorway(currentRoom, currentDoorway, availableDoorway);
                        
                        //проверка на overlaps
                        if (CheckOverlaps(currentRoom))
                            continue;
                        
                        roomPlaced = true;

                        //добавляем комнату в список поставленных комнат
                        placedRooms.Add(currentRoom);

                        //удаляем соединенные проемы 
                        currentDoorway.gameObject.SetActive(false);

                        availableTunnelDoorways.Remove(currentDoorway);

                        availableDoorway.gameObject.SetActive(false);

                        availableTunnelDoorways.Remove(availableDoorway);

                        //выход из цикла если комната на месте
                        break;
                    }
                }
                //выход из цикла если комната на месте
                if (roomPlaced)
                {
                    _logger.Log(_logTag, "Place коннектор " + currentRoom.idRoom + " from List");
                    break;
                }
            }
            if (!roomPlaced)
            {
                Destroy(currentRoom.gameObject);
                ResetLevelGenerator();
            }
        }
        // Циклическая комната бункера(Ангар)?
        if (mapedRoom.countDoorway > 2 && mapedRoom.idRoom == 1)
        {
            Room currentRoom = Instantiate(HangarPrefab) as Room;
            currentRoom.transform.parent = this.transform;
            currentRoom.idRoom = mapedRoom.idRoom;

            //Создание списков проемов
            var allAvailableDoorways = new List<Doorway>(availableDoorways);
            var currentRoomDoorways  = new List<Doorway>();
            AddDoorwaysToList(currentRoom, ref currentRoomDoorways);

            //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
            AddDoorwaysToList(currentRoom, ref availableDoorways);

            bool roomPlaced = false;
            //берем все доступные проемы
            foreach (Doorway availableDoorway in allAvailableDoorways)
            {
                //берем все доступные проемы для данной комнаты
                foreach (Doorway currentDoorway in currentRoomDoorways)
                {
                    if (availableDoorway.roomLink.idRoom == currentDoorway.roomLink.idRoom - 1 && availableDoorway.roomLink.roomType == Room.RoomType.Corridor)
                    {
                        //позиция комнаты 
                        PositionRoomAtDoorway(currentRoom, currentDoorway, availableDoorway);

                        //проверка на overlaps
                        if (CheckOverlaps(currentRoom))
                            continue;

                        roomPlaced = true;

                        //добавляем комнату в список поставленных комнат
                        placedRooms.Add(currentRoom);
                        //previousRoom = currentRoom;

                        //удаляем соединенные проемы 
                        currentDoorway.gameObject.SetActive(false);

                        availableDoorways.Remove(currentDoorway);

                        availableDoorway.gameObject.SetActive(false);

                        availableDoorways.Remove(availableDoorway);

                        //выход из цикла если комната на месте
                        break;
                    }
                }
                //выход из цикла если комната на месте
                if (roomPlaced)
                {
                    _logger.Log(_logTag, "Place cycle room from List");
                    break;
                }
            }
            if (!roomPlaced)
            {
                Destroy(currentRoom.gameObject);
                ResetLevelGenerator();
            }
        }
    }

    void PlaceCorridor(AbstractCorridor mapedCorridor)
    {
        Room currentCorridor = Instantiate(BunkerCorridorPrefabs[GetNextGeneratedValue(0, BunkerCorridorPrefabs.Count)]) as Room;
        currentCorridor.transform.parent = this.transform;
        currentCorridor.idRoom = mapedCorridor.idCorridor;

        //Создание списков проемов
        var allAvailableDoorways    = new List<Doorway>(availableDoorways);
        var currentCorridorDoorways = new List<Doorway>();
        AddDoorwaysToList(currentCorridor, ref currentCorridorDoorways);
        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddDoorwaysToList(currentCorridor, ref availableDoorways);

        bool roomPlaced = false;
        //берем все доступные проемы
        foreach (Doorway availableDoorway in allAvailableDoorways)
        {
            //берем все доступные проемы для данной комнаты
            foreach (Doorway currentDoorway in currentCorridorDoorways)
            {
                if (availableDoorway.roomLink.idRoom == currentDoorway.roomLink.idRoom
                    || (availableDoorway.roomLink.idRoom * 10) == currentDoorway.roomLink.idRoom
                    || (availableDoorway.roomLink.idRoom * 10 + 5) == currentDoorway.roomLink.idRoom)
                {
                    //позиция комнаты 
                    PositionRoomAtDoorway(currentCorridor, currentDoorway, availableDoorway);

                    //проверка на overlaps
                    if (CheckOverlaps(currentCorridor))
                        continue;

                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedRooms.Add(currentCorridor);
                    //previousRoom = currentCorridor;

                    //удаляем соединенные проемы 
                    currentDoorway.gameObject.SetActive(false);

                    availableDoorways.Remove(currentDoorway);

                    availableDoorway.gameObject.SetActive(false);

                    availableDoorways.Remove(availableDoorway);

                    //выход из цикла если комната на месте
                    break;
                }
            }
            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                _logger.Log(_logTag, "Place " + currentCorridor.idRoom + " Corridor");
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentCorridor.gameObject);
            ResetLevelGenerator();
        }
    }
    #endregion Rooms

    #region Dead Ends
    //Работа с тупиками
    void PlaceDeadlockRoom(Doorway doorway)
    {

        Room deadlock = null;
        if (doorway.heavyRoomLink == null || doorway.roomLink != null)
        {
            deadlock = Instantiate(BunkerDeadlockPrefabs[_cntBunkerDeadlock]) as Room;
            _cntBunkerDeadlock++;
        }
        else
        if (doorway.roomLink == null || doorway.heavyRoomLink != null)
        {
            deadlock = Instantiate(HeavyLabDeadlockPrefabs[_cntHLabDeadlock]) as Room;
            _cntHLabDeadlock++;
        }
        deadlock.transform.parent = this.transform;
        deadlock.idRoom = idDeadlock;
        idDeadlock++;

        //позиция комнаты 
        PositionRoomAtDoorway(deadlock, deadlock.doorways[0], doorway);
        //проверка на overlaps
        if (CheckOverlaps(deadlock))
        {
            Destroy(deadlock.gameObject);
            ResetLevelGenerator();
        }
        else
        {
            placedRoomDeadlocks.Add(deadlock);

            deadlock.doorways[0].gameObject.SetActive(false);

            doorway.gameObject.SetActive(false);

            availableDoorways.Remove(doorway);

            _logger.Log(_logTag, "Поставлен тупик  " + deadlock.idRoom + " со стилем " + deadlock.roomStyle);
        }
    }

    Tunnel FindCurrentTunnelroomPrefab(Tunnel.TunnelType tunnelType, List<Tunnel> tunnelPrefabs)
    {
        foreach (Tunnel tunnel in tunnelPrefabs)
        {
            if (tunnel.tunnelType == tunnelType)
            {
                return tunnel;
            }
        }
        return null;
    }

    void PlaceDeadlockTunnel(TunnelWay tunnelWay)
    {
        Tunnel deadlock = null;
        switch (tunnelWay.TunnelLink.tunnelStyle)
        {
            case Tunnel.StyleTunnel.light:
                deadlock = Instantiate(FindCurrentTunnelroomPrefab(Tunnel.TunnelType.RuinedEnd, LigthTunnelDeadlockPrefabs)) as Tunnel;
                break;
            case Tunnel.StyleTunnel.heavy:
                deadlock = Instantiate(FindCurrentTunnelroomPrefab(Tunnel.TunnelType.RuinedEnd, HeavyTunnelDeadlockPrefabs)) as Tunnel;
                break;
        }
        deadlock.transform.parent = this.transform;
        deadlock.idTunnel = idDeadlock;
        idDeadlock++;

        //позиция комнаты 
        PositionTunnelAtDoorway(ref deadlock, deadlock.tunnelways[0], tunnelWay);

        _logger.Log(_logTag, "Проем для " + tunnelWay.TunnelLink.idTunnel);
        //проверка на overlaps
        if (CheckOverlaps(deadlock))
        {
            Destroy(deadlock.gameObject);
            ResetLevelGenerator();
        }
        else
        {
            placedTunnelDeadlocks.Add(deadlock);

            deadlock.tunnelways[0].gameObject.SetActive(false);

            availableTunnelways.Remove(deadlock.tunnelways[0]);

            tunnelWay.gameObject.SetActive(false);

            availableTunnelways.Remove(tunnelWay);

            _logger.Log(_logTag, "Тупик туннеля " + deadlock.idTunnel + " поставлен ");
        }
    }
    
    void PlaceDeadlockTunnelRoom(DoorwayTunnel doorwayTunnel)
    {
        Tunnel deadlock = null;
        switch (doorwayTunnel.TunnelLink.tunnelStyle)
        {
            case Tunnel.StyleTunnel.light:
                deadlock = Instantiate(LigthTunnelRoomDeadlockPrefabs[GetNextGeneratedValue(0, LigthTunnelRoomDeadlockPrefabs.Count)]) as Tunnel;
                break;
            case Tunnel.StyleTunnel.heavy:
                deadlock = Instantiate(HeavyTunnelRoomDeadlockPrefabs[GetNextGeneratedValue(0, HeavyTunnelRoomDeadlockPrefabs.Count)]) as Tunnel;
                break;
        }
        deadlock.transform.parent = this.transform;
        
        //позиция комнаты 
        PositionTunnelAtDoorway(ref deadlock, deadlock.doorways[0], doorwayTunnel);

        //проверка на overlaps
        if (CheckOverlaps(deadlock))
        {
            Destroy(deadlock.gameObject);
            _logger.Log(_logTag, "Тупик комнаты туннеля не поставлен ");
            ResetLevelGenerator();
        }
        else
        {
            deadlock.doorways[0].gameObject.SetActive(false);
            availableTunnelDoorways.Remove(deadlock.doorways[0]);
            doorwayTunnel.gameObject.SetActive(false);
            availableTunnelDoorways.Remove(doorwayTunnel);
            deadlock.idTunnel = idDeadlock;
            idDeadlock++;
            _logger.Log(_logTag, "Тупик комнаты туннеля " + deadlock.idTunnel + " поставлен ");
            placedTunnelDeadlocks.Add(deadlock);
        }
    }
    #endregion Dead Ends

    #region Tunnels
    //Работа с туннелями
    void PlaceConnectorTunnel()
    {
        Tunnel currentRoom = null;
        if (idRoomTunnel == 0)
        {
            IndexTypeofRoomTunnel = GetNextGeneratedValue(0, TunnelroomPrefabs.Count);
        }
        currentRoom = Instantiate(TunnelroomPrefabs[IndexTypeofRoomTunnel]) as Tunnel;
        currentRoom.transform.parent = this.transform;
        currentRoom.idTunnel = idRoomTunnel;

        //Смена ориентации проёмов в случае левого TurnTunnel
        if (turnTunnel == DoorwayTunnel.Orientation.L) {
            _logger.LogWarning(_logTag, "Смена направлений проемов комнаты туннеля");
            foreach (DoorwayTunnel doorwayTunnel in currentRoom.doorways)
            {
                if (doorwayTunnel.Orient == DoorwayTunnel.Orientation.L)
                {
                    doorwayTunnel.Orient = DoorwayTunnel.Orientation.R;
                }
                else
                if (doorwayTunnel.Orient == DoorwayTunnel.Orientation.R)
                {
                    doorwayTunnel.Orient = DoorwayTunnel.Orientation.L;
                }
            }
        }
        //Создание списков проемов
        var allAvailableDoorways = new List<DoorwayTunnel>(availableTunnelDoorways);
        var currentRoomDoorways = new List<DoorwayTunnel>();
        AddTunnelDoorwaysToList(currentRoom, ref currentRoomDoorways);

        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddTunnelDoorwaysToList(currentRoom, ref availableTunnelDoorways);

        bool roomPlaced = false;
        //берем все доступные проемы
        foreach (DoorwayTunnel availableDoorway in allAvailableDoorways)
        {
            //берем все доступные проемы для данной комнаты
            foreach (DoorwayTunnel currentDoorway in currentRoomDoorways)
            {
                if (availableDoorway.Orient == turnTunnel
                    && currentDoorway.Orient != availableDoorway.Orient
                    && availableDoorway.TunnelLink.tunnelType != Tunnel.TunnelType.Connector
                    )
                {
                    //позиция комнаты 
                    PositionTunnelAtDoorway(ref currentRoom, currentDoorway, availableDoorway);

                    //проверка на overlaps
                    if (CheckOverlaps(currentRoom))
                        continue;

                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedRoomTunnels.Add(currentRoom);

                    listForCycleTunnel.Add(availableDoorway.TunnelLink);
                    _logger.Log(_logTag, "Комната для цикла с ID " + availableDoorway.TunnelLink.idTunnel);

                    //удаляем соединенные проемы 
                    currentDoorway.gameObject.SetActive(false);

                    availableTunnelDoorways.Remove(currentDoorway);

                    availableDoorway.gameObject.SetActive(false);

                    availableTunnelDoorways.Remove(availableDoorway);

                    //выход из цикла если комната на месте
                    break;
                }
            }
            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                idRoomTunnel++;
                _logger.Log(_logTag, "Ставим комнату тунеля с ID " + currentRoom.idTunnel);
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentRoom.gameObject);
            _logger.Log(_logTag, "Комната тунеля не ставиться ");
            ResetLevelGenerator();
        }
    }

    void PlaceAfterConnectorTunnel()
    {
        Tunnel currentTunnel= Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Cross , HeavyTunnelPrefabs)) as Tunnel;
        
        currentTunnel.transform.parent = this.transform;
        currentTunnel.idTunnel = idTunnel;
        
        //Создание списков проемов
        var currentTunnelDoorways = new List<DoorwayTunnel>();

        AddTunnelDoorwaysToList(currentTunnel, ref currentTunnelDoorways);

        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddTunnelDoorwaysToList(currentTunnel, ref availableTunnelDoorways);

        AddTunnelwaysToList(currentTunnel, ref availableTunnelways);

        bool roomPlaced = false;

        //берем все доступные проемы
        foreach (DoorwayTunnel availableDoorway in availableTunnelDoorways)
        {
            //берем все доступные проемы для данной комнаты
            foreach (DoorwayTunnel currentTunnelDoorway in currentTunnelDoorways)
            {
                //коннектор - тунель
                if (currentTunnelDoorway.Orient != availableDoorway.Orient
                    && availableDoorway.TunnelLink.tunnelType == Tunnel.TunnelType.Connector
                    && availableDoorway.TunnelLink.idTunnel == idFirstConTunnel
                    )
                {
                    //позиция комнаты 
                    PositionTunnelAtDoorway(ref currentTunnel, currentTunnelDoorway, availableDoorway);

                    //проверка на overlaps
                    if (CheckOverlaps(currentTunnel))
                        continue;

                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedTunnels.Add(currentTunnel);
                    
                    //удаляем соединенные проемы 
                    currentTunnelDoorway.gameObject.SetActive(false);

                    availableTunnelDoorways.Remove(currentTunnelDoorway);

                    availableDoorway.gameObject.SetActive(false);

                    availableTunnelDoorways.Remove(availableDoorway);

                    //выход из цикла если комната на месте
                    break;
                }
            }
            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                idTunnel++;
                _logger.Log(_logTag, "Ставим " + currentTunnel.idTunnel + " тунель");
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentTunnel.gameObject);
            _logger.Log(_logTag, "Тунель не поставлен");
            ResetLevelGenerator();
        }
    }

    bool isCurrentTunnelConnector(DoorwayTunnel doorway)
    {
        if (doorway.roomLink != null
            && (doorway.roomLink.roomType == Room.RoomType.Connector
            && doorway.roomLink.idRoom == 2))
        {
            _logger.Log(_logTag, "Это комната");
            return true;
        }
        else if (doorway.roomLink == null
            && (doorway.TunnelLink.tunnelType == Tunnel.TunnelType.Connector
            && doorway.TunnelLink.idTunnel == idFirstConTunnel))
        {
            _logger.Log(_logTag, "Это комната тунеля");
            return true;
        }
        return false;
    }

    Tunnel FindCurrentTunnelPrefab(Tunnel.TunnelType tunnelType, List<Tunnel> tunnelPrefabs)
    {
        foreach (Tunnel tunnel in tunnelPrefabs)
        {
            if (tunnel.tunnelType == tunnelType)
            {
                return tunnel;
            }
        }
        return null;
    }

    void PlaceBeforeConnectorTunnel()
    {
        Tunnel currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Cross, LightTunnelPrefabs)) as Tunnel;
        currentTunnel.transform.parent = this.transform;
        currentTunnel.idTunnel = idTunnel;
       
        //Создание списков проемов
        var currentTunnelways = new List<TunnelWay>();
        AddTunnelwaysToList(currentTunnel, ref currentTunnelways);
        AddTunnelwaysToList(currentTunnel, ref availableTunnelways);
        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddTunnelDoorwaysToList(currentTunnel, ref availableTunnelDoorways);
        bool roomPlaced = false;
        //берем все доступные проемы

        foreach (TunnelWay availableDoorway in availableTunnelways)
        {
            //берем все доступные проемы для данной комнаты
            foreach (TunnelWay currentTunnelDoorway in currentTunnelways)
            {
                //тунель - тунель
                if (availableDoorway.Typeway == TunnelWay.TypeDoorway.exit
                    && currentTunnelDoorway.Typeway == TunnelWay.TypeDoorway.enter
                    && availableDoorway.TunnelLink.tunnelStyle == Tunnel.StyleTunnel.heavy)
                {
                    //позиция комнаты 
                    PositionTunnelAtDoorway(ref currentTunnel, currentTunnelDoorway, availableDoorway);

                    //проверка на overlaps
                    if (CheckOverlaps(currentTunnel))
                    {
                        continue;
                    }
                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedTunnels.Add(currentTunnel);

                    //удаляем соединенные проемы 
                    currentTunnelDoorway.gameObject.SetActive(false);

                    availableTunnelways.Remove(currentTunnelDoorway);

                    availableDoorway.gameObject.SetActive(false);

                    availableTunnelways.Remove(availableDoorway);

                    //выход из цикла если комната на месте
                    break;
                }

            }
            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                idTunnel++;
                _logger.Log(_logTag, "Place " + currentTunnel.idTunnel + " тунель");
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentTunnel.gameObject);
            ResetLevelGenerator();
        }
    }

    int getTunnelNumber(AbstractTunnel tunnel)
    {
        return tunnel.idTunnel;
    }

    void PlaceTunnel()
    {
        Tunnel currentTunnel = null;
        //Проверка логики
        if (placedTunnels.Count == 0)
        {
            var _RandomTypeTunnel = GetNextGeneratedValue(0, 3);
            switch (_RandomTypeTunnel)
            {
                case 0: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.CurvL, LightTunnelPrefabs)); break;
                case 1: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.CurvR, LightTunnelPrefabs)); break;
                case 2: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Straight, LightTunnelPrefabs)); break;
            }
        }
        else
        if (placedTunnels.Count == 1)
        {
            currentTunnel = Instantiate(LightTunnelPrefabs[GetNextGeneratedValue(0, LightTunnelPrefabs.Count)]) as Tunnel;
        }
        else
        if (placedTunnels.Count > 1)
        {
            if (placedTunnels[placedTunnels.Count - 1].doorways.Length == 0)
            {
                var _RandomTypeTunnel = GetNextGeneratedValue(0, 3);
                switch (_RandomTypeTunnel)
                {
                    case 0: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Cross, LightTunnelPrefabs)); break;
                    case 1: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.TLtunnel, LightTunnelPrefabs)); break;
                    case 2: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.TRtunnel, LightTunnelPrefabs)); break;
                }
            }
            else
            if (placedTunnels[placedTunnels.Count - 1].doorways.Length != 0)
            {
                var _RandomTypeTunnel = GetNextGeneratedValue(0, 3);
                switch (_RandomTypeTunnel)
                {
                    case 0: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.CurvL, LightTunnelPrefabs)); break;
                    case 1: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.CurvR, LightTunnelPrefabs)); break;
                    case 2: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Straight, LightTunnelPrefabs)); break;
                }
            }
        }

        if (currentTunnel.tunnelType == Tunnel.TunnelType.CurvL || currentTunnel.tunnelType == Tunnel.TunnelType.CurvR)
        {
            _curvTunnelcnt++;
            if (_curvTunnelcnt >= 2)
            {
                Destroy(currentTunnel.gameObject);
                currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Straight, LightTunnelPrefabs));

                /*
                var _RandomTypeTunnel = GetNextGeneratedValue(0, 4);
                switch (_RandomTypeTunnel)
                {
                    case 0: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Cross, LightTunnelPrefabs)); break;
                    case 1: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.TLtunnel, LightTunnelPrefabs)); break;
                    case 2: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.TRtunnel, LightTunnelPrefabs)); break;
                    case 3: currentTunnel = Instantiate(FindCurrentTunnelPrefab(Tunnel.TunnelType.Straight, LightTunnelPrefabs)); break;
                }
                */
            }
        }
        currentTunnel.transform.parent = this.transform;
        currentTunnel.idTunnel = idTunnel;
        
        //Создание списков проемов
        var allAvailableTunnelways = new List<TunnelWay>(availableTunnelways);
        var currentTunnelways = new List<TunnelWay>();

        AddTunnelwaysToList(currentTunnel, ref currentTunnelways);
        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddTunnelwaysToList(currentTunnel, ref availableTunnelways);
        _logger.Log(_logTag, "Кол-во свободных проходов тунелей " + availableTunnelways.Count);
        _logger.Log(_logTag, "Кол-во свободных дверей тунелей " + availableTunnelDoorways.Count);
        _logger.Log(_logTag, "тип тунеля " + currentTunnel.tunnelType);

        bool roomPlaced = false;
        //берем все доступные проемы
        foreach (TunnelWay availableTunnelway in allAvailableTunnelways)
        {
            //берем все доступные проемы для данной комнаты
            foreach (TunnelWay currentTunnelDoorway in currentTunnelways)
            {
                //тунель - тунель
                if (availableTunnelway.Typeway == TunnelWay.TypeDoorway.exit
                    && currentTunnelDoorway.Typeway == TunnelWay.TypeDoorway.enter)
                {
                    //позиция комнаты 
                    PositionTunnelAtDoorway(ref currentTunnel, currentTunnelDoorway, availableTunnelway);

                    //проверка на overlaps
                    if (CheckOverlaps(currentTunnel))
                    {
                        continue;
                    }
                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedTunnels.Add(currentTunnel);


                    //удаляем соединенные проемы 
                    currentTunnelDoorway.gameObject.SetActive(false);

                    availableTunnelways.Remove(currentTunnelDoorway);

                    availableTunnelway.gameObject.SetActive(false);

                    availableTunnelways.Remove(availableTunnelway);

                    //выход из цикла если комната на месте
                    break;
                }
            }

            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                //запись в список проемов туннелей оставшихся проемов
                AddTunnelDoorwaysToList(currentTunnel, ref availableTunnelDoorways);
                idTunnel++;
                _logger.Log(_logTag, "Туннель " + currentTunnel.idTunnel + " поставлен");
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentTunnel.gameObject);
            ResetLevelGenerator();
        }
    }
    
    void PlaceAnotherTunnelLine()
    {
        //Выставление тунелей для второй линии 
        foreach (Tunnel t in listForCycleTunnel)
        {
            _logger.Log(_logTag, "ID копируемой комнаты " + t.idTunnel);

            if (t.tunnelType == Tunnel.TunnelType.TLtunnel)
                PlaceHeavyTunnel(Tunnel.TunnelType.TRtunnel);
            else if (t.tunnelType == Tunnel.TunnelType.TRtunnel)
                PlaceHeavyTunnel(Tunnel.TunnelType.TLtunnel);
            else
                PlaceHeavyTunnel(t.tunnelType);
        }
    }

    void PlaceHeavyTunnel(Tunnel.TunnelType tunnelType)
    {
        Tunnel currentTunnel = Instantiate(FindCurrentTunnelPrefab(tunnelType, HeavyTunnelPrefabs)) as Tunnel;

        currentTunnel.transform.parent = this.transform;
        currentTunnel.idTunnel = idTunnel;

        //Создание списков проемов
        var allAvailableTunnelways = new List<TunnelWay>(availableTunnelways);
        var currentTunnelways = new List<TunnelWay>();

        AddTunnelwaysToList(currentTunnel, ref currentTunnelways);
        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddTunnelDoorwaysToList(currentTunnel, ref availableTunnelDoorways);
        AddTunnelwaysToList(currentTunnel, ref availableTunnelways);

        bool roomPlaced = false;
        //берем все доступные проемы
        foreach (TunnelWay availableDoorway in allAvailableTunnelways)
        {
            //берем все доступные проемы для данной комнаты
            foreach (TunnelWay currentTunnelDoorway in currentTunnelways)
            {
                //тунель - тунель
                if (availableDoorway.Typeway == TunnelWay.TypeDoorway.exit
                    && currentTunnelDoorway.Typeway == TunnelWay.TypeDoorway.enter
                    && availableDoorway.TunnelLink.tunnelStyle == Tunnel.StyleTunnel.heavy
                    )
                {
                    //позиция комнаты 
                    PositionTunnelAtDoorway(ref currentTunnel, currentTunnelDoorway, availableDoorway);

                    //проверка на overlaps
                    if (CheckOverlaps(currentTunnel))
                        continue;

                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedTunnels.Add(currentTunnel);

                    //удаляем соединенные проемы 
                    currentTunnelDoorway.gameObject.SetActive(false);

                    availableTunnelways.Remove(currentTunnelDoorway);

                    availableDoorway.gameObject.SetActive(false);

                    availableTunnelways.Remove(availableDoorway);
                    
                    //выход из цикла если комната на месте
                    break;
                }
            }
            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                //запись в список проемов туннелей оставшихся проемов
                idTunnel++;
                _logger.Log(_logTag, "Туннель " + currentTunnel.idTunnel + " поставлен");
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentTunnel.gameObject);
            ResetLevelGenerator();
        }
    }
    #endregion Tunnels

    #region Heavy Rooms
    //Работа с зоной строгого режима
    void PlaceHeavyRoom()
    {
        HeavyRoom currentRoom = Instantiate(HeavyRoomsPrefabs[GetNextGeneratedValue(0, HeavyRoomsPrefabs.Count)]) as HeavyRoom;
        currentRoom.transform.parent = this.transform;
        currentRoom.idHeavyRoom = idHeavyRoom;

        //Создание списков проемов
        var allAvailableHeavyRoomWays = new List<HeavyRoomWay>(availableHeavyRoomWays);
        var currentHeavyRoomWays = new List<HeavyRoomWay>();

        AddHeavyRoomWaysToList(currentRoom, ref currentHeavyRoomWays);
        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddHeavyRoomWaysToList(currentRoom, ref availableHeavyRoomWays);
        bool roomPlaced = false;
        //берем все доступные проемы
        foreach (HeavyRoomWay availableHeavyRoomWay in allAvailableHeavyRoomWays)
        {
            //берем все доступные проемы для данной комнаты
            foreach (HeavyRoomWay currentHeavyRoomWay in currentHeavyRoomWays)
            {
                //тунель - тунель
                if (availableHeavyRoomWay.Typeway == HeavyRoomWay.TypeDoorway.exit
                    && currentHeavyRoomWay.Typeway == HeavyRoomWay.TypeDoorway.enter)
                {
                    //позиция комнаты 
                    PositionHeavyRoomAtDoorway(ref currentRoom, currentHeavyRoomWay, availableHeavyRoomWay);

                    //проверка на overlaps
                    if (CheckOverlaps(currentRoom))
                        continue;

                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedHeavyRooms.Add(currentRoom);

                    //удаляем соединенные проемы 
                    currentHeavyRoomWay.gameObject.SetActive(false);

                    availableHeavyRoomWays.Remove(currentHeavyRoomWay);

                    availableHeavyRoomWay.gameObject.SetActive(false);

                    availableHeavyRoomWays.Remove(availableHeavyRoomWay);

                    //выход из цикла если комната на месте
                    break;
                }
            }
            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                //запись в список проемов туннелей оставшихся проемов
                idHeavyRoom++;
                _logger.Log(_logTag, "Комната зоны " + currentRoom.idHeavyRoom + " поставлена");
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentRoom.gameObject);
            ResetLevelGenerator();
        }
    }

    HeavyRoom FindCurrentHeavyRoomPrefab(HeavyRoom.HeavyRoomType roomType, List<HeavyRoom> tunnelPrefabs)
    {
        foreach (HeavyRoom room in HeavyRoomsPrefabs)
        {
            if (room.heavyRoomType == roomType)
                return room;
        }
        return null;
    }

    void PlaceCurrentHeavyRoom(HeavyRoom.HeavyRoomType heavyRoomType)
    {
        HeavyRoom currentRoom = null;
        foreach (HeavyRoom room in HeavyRoomsPrefabs)
        {
            if (room.heavyRoomType == heavyRoomType)
                currentRoom = Instantiate(room) as HeavyRoom;
        }
        currentRoom.transform.parent = this.transform;
        currentRoom.idHeavyRoom = idHeavyRoom;

        //Создание списков проемов
        var allAvailableHeavyRoomWays = new List<HeavyRoomWay>(availableHeavyRoomWays);
        var currentHeavyRoomWays = new List<HeavyRoomWay>();

        AddHeavyRoomWaysToList(currentRoom, ref currentHeavyRoomWays);
        //Берем дв. проемы из данной комнаты и добавляем в список доступных проемов
        AddHeavyRoomWaysToList(currentRoom, ref availableHeavyRoomWays);

        bool roomPlaced = false;
        //берем все доступные проемы
        foreach (HeavyRoomWay availableHeavyRoomWay in allAvailableHeavyRoomWays)
        {
            //берем все доступные проемы для данной комнаты
            foreach (HeavyRoomWay currentHeavyRoomWay in currentHeavyRoomWays)
            {
                //тунель - тунель
                if (availableHeavyRoomWay.Typeway == HeavyRoomWay.TypeDoorway.exit
                    && currentHeavyRoomWay.Typeway == HeavyRoomWay.TypeDoorway.enter)
                {
                    //позиция комнаты 
                    PositionHeavyRoomAtDoorway(ref currentRoom, currentHeavyRoomWay, availableHeavyRoomWay);
                    /*
                    //проверка на overlaps
                    if (CheckOverlaps(currentRoom))
                    {
                        continue;
                    }
                    */
                    roomPlaced = true;

                    //добавляем комнату в список поставленных комнат
                    placedHeavyRooms.Add(currentRoom);

                    //удаляем соединенные проемы 
                    currentHeavyRoomWay.gameObject.SetActive(false);

                    availableHeavyRoomWays.Remove(currentHeavyRoomWay);

                    availableHeavyRoomWay.gameObject.SetActive(false);

                    availableHeavyRoomWays.Remove(availableHeavyRoomWay);

                    //выход из цикла если комната на месте
                    break;
                }
            }
            //выход из цикла если комната на месте
            if (roomPlaced)
            {
                //запись в список проемов туннелей оставшихся проемов
                idHeavyRoom++;
                _logger.Log(_logTag, "Комната зоны " + currentRoom.idHeavyRoom + " поставлена");
                break;
            }
        }
        if (!roomPlaced)
        {
            Destroy(currentRoom.gameObject);
            ResetLevelGenerator();
        }
    }

    void ReplaceHeavyRoom(int IDRoom, HeavyRoom.HeavyRoomType currentTypeRoom)
    {
        HeavyRoom currentRoom = Instantiate(FindCurrentHeavyRoomPrefab(currentTypeRoom , HeavyRoomsPrefabs));
        
        HeavyRoom RoomForReplace = null;
        foreach (HeavyRoom room in placedHeavyRooms)
        {
            if (room.idHeavyRoom == IDRoom)
                RoomForReplace = room;
        }
        currentRoom.transform.parent = this.transform;
        currentRoom.transform.position = RoomForReplace.transform.position;
        currentRoom.transform.rotation = RoomForReplace.transform.rotation;

        currentRoom.idHeavyRoom = RoomForReplace.idHeavyRoom;

        placedHeavyRooms.Remove(RoomForReplace);
        Destroy(RoomForReplace.gameObject);

        //добавляем комнату в список поставленных комнат
        placedHeavyRooms.Add(currentRoom);

        //удаляем соединенные проемы 
        foreach (HeavyRoomWay way in currentRoom.heavyroomways)
        {
            if (!way.forBranch)
                Destroy(way.gameObject);
            else if (way.forBranch)
                availableHeavyRoomWays.Add(way);
        }
        foreach (Doorway way in currentRoom.doorways)
        {
            if (way != null)
                availableDoorways.Add(way);
        }
    }

    void ClearListHeavyRoomWays()
    {
        foreach (HeavyRoomWay way in availableHeavyRoomWays)
            Destroy(way.gameObject);

        availableHeavyRoomWays.Clear();
    }
    #endregion Heavy Rooms

    #region Prefabs Positioning
    //Выставление префабов на позиции
    void PositionRoomAtDoorway(Room room, Doorway roomDoorway, Doorway targetDoorway)
    {
        room.transform.position = Vector3.zero;
        room.transform.rotation = Quaternion.identity;

        //крутим так как же как в прошлом проеме
        Vector3 targetDoorwayEuler = targetDoorway.transform.eulerAngles;
        Vector3 roomDoorwayEuler = roomDoorway.transform.eulerAngles;
        float deltaAngle = Mathf.DeltaAngle(roomDoorwayEuler.y, targetDoorwayEuler.y);
        Quaternion currentRoomTargetRotation = Quaternion.AngleAxis(deltaAngle, Vector3.up);
        room.transform.rotation = currentRoomTargetRotation * Quaternion.Euler(0, 180f, 0);

        //позиция комнаты 
        Vector3 roomPositionOffset = roomDoorway.transform.position - room.transform.position;
        room.transform.position = targetDoorway.transform.position - roomPositionOffset;
    }

    void PositionTunnelAtDoorway(ref Tunnel tunnel, TunnelWay tunnelDoorway, Doorway targetDoorway)
    {
        tunnel.transform.position = Vector3.zero;
        tunnel.transform.rotation = Quaternion.identity;

        //крутим также как в прошлом проеме
        Vector3 targetDoorwayEuler = targetDoorway.transform.eulerAngles;
        Vector3 roomDoorwayEuler = tunnelDoorway.transform.eulerAngles;
        float deltaAngle = Mathf.DeltaAngle(roomDoorwayEuler.y, targetDoorwayEuler.y);
        Quaternion currentRoomTargetRotation = Quaternion.AngleAxis(deltaAngle, Vector3.up);
        tunnel.transform.rotation = currentRoomTargetRotation * Quaternion.Euler(0, 180f, 0);

        //позиция комнаты 
        Vector3 roomPositionOffset = tunnelDoorway.transform.position - tunnel.transform.position;
        tunnel.transform.position = targetDoorway.transform.position - roomPositionOffset;
    }

    void PositionHeavyRoomAtDoorway(ref HeavyRoom room, HeavyRoomWay roomDoorway, HeavyRoomWay targetDoorway)
    {
        room.transform.position = Vector3.zero;
        room.transform.rotation = Quaternion.identity;

        //крутим так как же как в прошлом проеме
        Vector3 targetDoorwayEuler = targetDoorway.transform.eulerAngles;
        Vector3 roomDoorwayEuler = roomDoorway.transform.eulerAngles;
        float deltaAngle = Mathf.DeltaAngle(roomDoorwayEuler.y, targetDoorwayEuler.y);
        Quaternion currentRoomTargetRotation = Quaternion.AngleAxis(deltaAngle, Vector3.up);
        room.transform.rotation = currentRoomTargetRotation * Quaternion.Euler(0, 180f, 0);

        //позиция комнаты 
        Vector3 roomPositionOffset = roomDoorway.transform.position - room.transform.position;
        room.transform.position = targetDoorway.transform.position - roomPositionOffset;
    }
    #endregion Prefabs Positioning

    #region Overlap Checking
    //Проверка на Overlap
    
    bool CheckOverlaps(Object room)
    {
        Bounds bounds = new Bounds();
        Transform roomTransform = null;
        Object currentRoomGameObject = null;

        if (room.GetType() == typeof(Room)
            || room.GetType() == typeof(ConnectorTunToHeavy)
            || room.GetType() == typeof(SCP1018room))
        {
            roomTransform = ((Room)room).transform;
            currentRoomGameObject = ((Room)room).gameObject;
            bounds = ((Room)room).RoomBounds;
        }
        else if (room.GetType() == typeof(Tunnel))
        {
            roomTransform = ((Tunnel)room).transform;
            currentRoomGameObject = ((Tunnel)room).gameObject;
            bounds = ((Tunnel)room).TunnelBounds;
        }
        else if (room.GetType() == typeof(HeavyRoom))
        {
            roomTransform = ((HeavyRoom)room).transform;
            currentRoomGameObject = ((HeavyRoom)room).gameObject;
            bounds = ((HeavyRoom)room).HeavyRoomBounds;
        }
        bounds.Expand(-0.1f);
        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.size / 2, roomTransform.rotation, roomLayerMask);
        if (colliders.Length > 0)
        {
            //игнорирование прикосновений с данной комнатой
            foreach (Collider c in colliders)
            {
                if (c.transform.parent.gameObject.Equals(currentRoomGameObject))
                {
                    continue;
                }
                else
                {
                    _logger.LogError(_logTag, "Overlap detected");
                    return true;
                }
            }
        }
        return false;
    }

    #endregion Overlap Checking
}