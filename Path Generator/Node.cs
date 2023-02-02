using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node>
{
    // Other nodes around this node
    public List<Node> neighbours;

    public CubeTile cubeTile;
    public int x;
    public int y;

    public float gCost;
    public float hCost;
    public Node parent;
    int heapIndex;

    // Constructor that instantiates a list to populate
    public Node()
    {
        neighbours = new List<Node>();
    }

    public float fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if(compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }

        return -compare;
    }
}
