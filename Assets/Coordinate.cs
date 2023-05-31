// Store x,y int pair
public struct Coordinate
{
    public int x;
    public int y;

    public Coordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static bool operator ==(Coordinate a, Coordinate b)
    {
        return a.x == b.x &&
            a.y == b.y;
    }

    public static bool operator !=(Coordinate a, Coordinate b)
    {
        return a.x != b.x ||
            a.y != b.y;
    }

    public override string ToString()
    {
        return $"({x},{y})";
    }
}