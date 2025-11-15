using UnityEngine;

public enum Direction
{
    North,
    South,
    East,
    West
}

public static class DirectionExtensions
{
    public static Vector2Int ToVector2Int(Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector2Int(0, 1),
            Direction.South => new Vector2Int(0, -1),
            Direction.East => new Vector2Int(1, 0),
            Direction.West => new Vector2Int(-1, 0),
            _ => Vector2Int.zero
        };
    }

    public static Direction Opposite(Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => direction
        };
    }

    public static string ToShortString(Direction direction)
    {
        return direction switch
        {
            Direction.North => "N",
            Direction.South => "S",
            Direction.East => "E",
            Direction.West => "W",
            _ => ""
        };
    }
}