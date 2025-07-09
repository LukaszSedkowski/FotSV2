using UnityEngine;

public class Node
{
    public int X, Y ;
    public int G, H;
    public Node Parent;

    public int F => G + H;

    public Node(int x, int y)
    {
        X = x;
        Y = y;
    }
}
