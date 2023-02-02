using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    private static PathGenerator _instance;

    public static PathGenerator Instance { get { return _instance; } }

    public Node[,] graph;

    public Board board;

    // Make sure one and only one instance of PathGenerator exists
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    // Return size of the map x by y
    public int MaxSize
    {
        get
        {
            return board.mapSizeX * board.mapSizeY;
        }
    }

    // Generate graph made out of nodes    
    private void GenerateGraph()
    {
        graph = new Node[board.mapSizeX, board.mapSizeY];

        int i = 0;

        // Generate graph made out of nodes 
        for (int y = 0; y < board.mapSizeY; y++)
        {
            for (int x = 0; x < board.mapSizeX; x++)
            {
                graph[x, y] = new Node();

                graph[x, y].x = x;
                graph[x, y].y = y;

                board.cubeTiles[i].x = x;
                board.cubeTiles[i].y = y;

                graph[x, y].cubeTile = board.cubeTiles[i];

                i++;
            }
        }


        // Assign neighbours to each nodes so that they can be referenced
        for (int y = 0; y < board.mapSizeY; y++)
        {
            for (int x = 0; x < board.mapSizeX; x++)
            {
                if (x > 0)
                {
                    graph[x, y].neighbours.Add(graph[x - 1, y]);

                    if (y > 0)
                        graph[x, y].neighbours.Add(graph[x - 1, y - 1]);

                    if (y < board.mapSizeY - 1)
                        graph[x, y].neighbours.Add(graph[x - 1, y + 1]);
                }

                if (x < board.mapSizeX - 1)
                {
                    graph[x, y].neighbours.Add(graph[x + 1, y]);

                    if (y > 0)
                        graph[x, y].neighbours.Add(graph[x + 1, y - 1]);

                    if (y < board.mapSizeY - 1)
                        graph[x, y].neighbours.Add(graph[x + 1, y + 1]);
                }


                if (y > 0)
                    graph[x, y].neighbours.Add(graph[x, y - 1]);

                if (y < board.mapSizeY - 1)
                    graph[x, y].neighbours.Add(graph[x, y + 1]);
            }
        }
    }  

    public List<Node> GeneratePath(CubeTile startingCube, CubeTile destinationCube, bool roadCreation = false, bool fly = false)
    {
        // Starting node of the path
        Node startNode = graph[startingCube.x, startingCube.y];

        // Destination of the path
        Node targetNode = graph[destinationCube.x, destinationCube.y];

        // Pre create a path to populate and return
        List<Node> path = new List<Node>();


        Heap<Node> openSet = new Heap<Node>(MaxSize);
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while(openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            // We found our target, retrace the path that the generator calculated and return it
            if(currentNode == targetNode)
            {
                RetracePath(startNode, targetNode);
                return path;
            }

            foreach (Node neighbour in currentNode.neighbours)
            {
                // No need to consider a node that we already checked || if node is not walkable || or if node is not walkable and we can not fly
                if (closedSet.Contains(neighbour) || !neighbour.cubeTile.isWalkable && neighbour != targetNode || !neighbour.cubeTile.isWalkable && !fly && neighbour != targetNode)
                    continue;

                // Calculate distance from one node to next
                float newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.hCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }              
        }

        void RetracePath(Node startNode, Node endNode)
        {
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }

            path.Reverse();
        }

        float GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.x - nodeB.x);
            int dstY = Mathf.Abs(nodeA.y - nodeB.y);

            if (dstX > dstY)
                return (14 * dstY + 10 * (dstX - dstY));

            return (14 * dstX + 10 * (dstY - dstX));
        }

        return path;
    }

    private float CostToEnterTile(Node sourceN, Node targetN, bool roadCreation)
    {
        float cost = 0;

        // If we are looking at a node that is diagonal
        // Then add to cost to not prioritize it
        if (sourceN.x != targetN.x && sourceN.y != targetN.y)
        {
            cost +=  1.001f;
        }
        return cost;
    }
    
    public int CheckDistance(CubeTile start, CubeTile destination, bool fly)
    {
        return GeneratePath(start, destination, false ,fly).Count;
    }
}
