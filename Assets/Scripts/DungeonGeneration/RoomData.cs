using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomData
{
    private Vector2Int position;
    private List<Direction> connections = new List<Direction>();
    private bool isFinal = false;
    private bool isStart = false;
    [NonSerialized] private bool hasBeenEntered = false;

    public RoomData(Vector2Int pos, bool final = false)
    {
        position = pos;
        isFinal = final;
    }

    public Vector2Int Position { get => position; set => position = value; }
    public List<Direction> Connections { get => connections; set => connections = value; }
    public bool IsFinal { get => isFinal; set => isFinal = value; }
    public bool HasBeenEntered { get => hasBeenEntered; set => hasBeenEntered = value; }
    public bool IsStart { get => isStart; set => isStart = value; }
}
