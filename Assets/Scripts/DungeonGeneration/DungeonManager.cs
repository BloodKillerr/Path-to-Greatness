using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [SerializeField] private GameObject[] roomPrefabs;

    [SerializeField] private GameObject[] finalRoomPrefabs;

    [Range(0f, 1f)]
    [SerializeField] private float branchProbability = 0.5f;
    [SerializeField] private int maxCorridorLength = 4;

    [Min(2)]
    [SerializeField] private int minRooms = 10;
    [Min(2)]
    [SerializeField] private int maxRooms = 20;

    private List<RoomData> graph = new List<RoomData>();

    private Dictionary<Vector2Int, GameObject> placedRooms = new();

    [SerializeField] private float roomSizeX = 50f;
    [SerializeField] private float roomSizeY = 50f;

    public GameObject portalPrefab;

    public int NextScene = 3;

    private int seed;

    public Dictionary<Vector2Int, GameObject> PlacedRooms { get => placedRooms; set => placedRooms = value; }

    public static DungeonManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (!SaveManager.IsLoadingSave)
        {
            seed = UnityEngine.Random.Range(0, int.MaxValue);
            GenerateDungeon(seed);
            Debug.Log($"[DEBUG] Generated dungeon seed={seed}");
        }
    }

    public void GenerateDungeon(int seed)
    {
        if (minRooms > maxRooms)
        {
            throw new Exception("minRooms must be ≤ maxRooms");
        }

        UnityEngine.Random.InitState(seed);
        BuildGraph();
        InstantiateFromGraph();

        if (!SaveManager.IsLoadingSave)
        {
            Player.Instance.gameObject.transform.position = Vector3.zero;
        }
    }

    public Vector2Int GetRoomPositionFromWorld(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / 25f);
        int y = Mathf.RoundToInt(worldPos.z / 25f);
        return new Vector2Int(x, y);
    }

    #region DFS
    private void BuildGraph()
    {
        graph.Clear();

        RoomData start = new RoomData(Vector2Int.zero);
        start.IsStart = true;
        graph.Add(start);

        int minNormal = minRooms - 1;
        int steps = Mathf.Max(0, minNormal - 1);
        RoomData current = start;

        for (int i = 0; i < steps; i++)
        {
            List<Direction> freeDirs = Enum.GetValues(typeof(Direction))
                .Cast<Direction>()
                .Where(d => !current.Connections.Contains(d)
                         && !graph.Any(r => r.Position == current.Position + DirectionExtensions.ToVector2Int(d)))
                .ToList();
            if (freeDirs.Count == 0)
            {
                break;
            }

            Direction dir = freeDirs[UnityEngine.Random.Range(0, freeDirs.Count)];
            Vector2Int newPos = current.Position + DirectionExtensions.ToVector2Int(dir);

            current.Connections.Add(dir);
            RoomData next = new RoomData(newPos);
            next.Connections.Add(DirectionExtensions.Opposite(dir));
            graph.Add(next);
            current = next;
        }

        Carve(start, maxRooms - 1, 0);

        PlaceFinalRoom();
    }

    private void Carve(RoomData node, int normalRoomTarget, int depth)
    {
        if (graph.Count >= normalRoomTarget)
        {
            return;
        }

        if (depth >= maxCorridorLength)
        {
            return;
        }

        List<Direction> directions = new List<Direction> {
            Direction.North, Direction.South,
            Direction.East,  Direction.West
        };
        Shuffle(directions);

        bool first = true;
        foreach (Direction d in directions)
        {
            if (graph.Count >= normalRoomTarget)
            {
                break;
            }

            if (!first && UnityEngine.Random.value > branchProbability)
            {
                break;
            }
            first = false;

            Vector2Int np = node.Position + DirectionExtensions.ToVector2Int(d);
            if (graph.Any(r => r.Position == np))
            {
                continue;
            }

            node.Connections.Add(d);
            RoomData neighbour = new RoomData(np);
            neighbour.Connections.Add(DirectionExtensions.Opposite(d));
            graph.Add(neighbour);
            Carve(neighbour, normalRoomTarget, depth + 1);
        }
    }

    void PlaceFinalRoom()
    {
        Dictionary<RoomData, int> distances = new Dictionary<RoomData, int>();
        Queue<RoomData> queue = new Queue<RoomData>();

        RoomData start = graph.First(r => r.Position == Vector2Int.zero);
        distances[start] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            RoomData node = queue.Dequeue();
            int distance = distances[node];
            foreach (Direction direction in node.Connections)
            {
                Vector2Int neighborPos = node.Position + DirectionExtensions.ToVector2Int(direction);
                RoomData neighbor = graph.First(r => r.Position == neighborPos);
                if (distances.ContainsKey(neighbor))
                {
                    continue;
                }
                distances[neighbor] = distance + 1;
                queue.Enqueue(neighbor);
            }
        }

        List<RoomData> leaves = graph
            .Where(r => !r.IsFinal
                        && !(r.Position == Vector2Int.zero)
                        && r.Connections.Count == 1)
            .ToList();

        if (leaves.Count == 0)
        {
            return;
        }

        RoomData farthestLeaf = leaves
            .OrderByDescending(r => distances[r])
            .First();

        List<Direction> freeDirections = Enum.GetValues(typeof(Direction))
            .Cast<Direction>()
            .Where(direction => !farthestLeaf.Connections.Contains(direction)
                        && !graph.Any(r => r.Position == farthestLeaf.Position + DirectionExtensions.ToVector2Int(direction)))
            .ToList();

        if (freeDirections.Count == 0)
        {
            return;
        }

        Direction chosenDirection = freeDirections[UnityEngine.Random.Range(0, freeDirections.Count)];
        farthestLeaf.Connections.Add(chosenDirection);

        Vector2Int finalPos = farthestLeaf.Position + DirectionExtensions.ToVector2Int(chosenDirection);
        RoomData finalRoom = new RoomData(finalPos, final: true);
        finalRoom.Connections.Add(DirectionExtensions.Opposite(chosenDirection));
        graph.Add(finalRoom);
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    #endregion

    void InstantiateFromGraph()
    {
        placedRooms.Clear();

        foreach (RoomData data in graph)
        {
            GameObject[] roomPool = data.IsFinal ? finalRoomPrefabs : roomPrefabs;

            string signature = string.Concat(
                data.Connections
                    .OrderBy(d => d)
                    .Select(d => DirectionExtensions.ToShortString(d))
            );

            GameObject prefab = roomPool.FirstOrDefault(p => p.name.EndsWith($"_{signature}"));
            if (prefab == null)
            {
                Debug.LogError($"No prefab matches signature '{signature}'");
                continue;
            }

            Vector3 worldPos = new Vector3(data.Position.x * roomSizeX, 0, data.Position.y * roomSizeY);
            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);

            if(data.IsFinal)
            {
                go.GetComponent<FinalRoomController>().Init(data);
                go.GetComponent<FinalRoomController>().SpawnPortal(portalPrefab, NextScene);
            }
            else
            {
                go.GetComponent<RoomController>().Init(data);
            }

            go.GetComponent<RoomController>().Init(data);
            placedRooms[data.Position] = go;
        }
    }

    public DungeonData CollectDungeonState()
    {
        DungeonData d = new DungeonData();

        d.seed = seed;

        d.playerWorldPosition = Player.Instance.transform.position;

        d.roomsEntered = graph
            .Where(r => r.HasBeenEntered)
            .Select(r => r.Position)
            .ToList();

        d.roomsCleared = new List<Vector2Int>();
        foreach (Vector2Int pos in d.roomsEntered)
        {
            if (!placedRooms.TryGetValue(pos, out var roomGo) || roomGo == null)
                continue;

            RoomController rc = roomGo.GetComponent<RoomController>();
            d.roomsCleared.Add(pos);
        }

        d.roomsState = new List<RoomControllerState>();
        foreach (var kv in placedRooms)
        {
            Vector2Int pos = kv.Key;
            GameObject go = kv.Value;
            RoomController rc = go.GetComponent<RoomController>();
            if (rc == null)
            {
                continue;
            }

            d.roomsState.Add(new RoomControllerState
            {
                position = pos,
                hasBeenEntered = rc.Data.HasBeenEntered,
                isFinished = rc.IsFinished,
                checkCompletion = rc.CheckCompletion
            });
        }

        SceneChangeDoor portal = FindAnyObjectByType<SceneChangeDoor>();
        if (portal != null)
        {
            d.portalData = new BossPortalData
            {
                position = portal.transform.position,
                rotation = portal.transform.rotation,
                sceneIndex = portal.SceneIndex,
            };
        }
        else
        {
            d.portalData = null;
        }

        return d;
    }

    public void RestoreDungeonState(DungeonData d)
    {
        seed = d.seed;
        GenerateDungeon(d.seed);

        foreach (RoomControllerState rs in d.roomsState)
        {
            RoomData roomData = GetRoomData(rs.position);
            if (roomData != null)
            {
                roomData.HasBeenEntered = rs.hasBeenEntered;
            }

            if (placedRooms.TryGetValue(rs.position, out var roomGo) && roomGo != null)
            {
                RoomController rc = roomGo.GetComponent<RoomController>();
                if (rc != null)
                {
                    rc.IsFinished = rs.isFinished;
                    rc.CheckCompletion = rs.checkCompletion;
                }
            }
        }

        if (d.portalData.sceneIndex != 0)
        {
            GameObject go = Instantiate(
                portalPrefab,
                d.portalData.position,
                d.portalData.rotation
            );
            SceneChangeDoor portal = go.GetComponent<SceneChangeDoor>();
            portal.SceneIndex = d.portalData.sceneIndex;
        }
    }

    public RoomData GetRoomData(Vector2Int pos) => graph.FirstOrDefault(r => r.Position == pos);
}
