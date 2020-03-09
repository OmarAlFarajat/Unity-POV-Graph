using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Adapted from: https://www.youtube.com/watch?v=zdHvM6XU4rY

public class Graph<TNodeType,TEdgeType>
{
    public Graph()
    {
        Nodes = new List<Node<TNodeType>>();
        Edges = new List<Edge<TEdgeType, TNodeType>>();
    }

    public List<Node<TNodeType>> Nodes { get; private set; }
    public List<Edge<TEdgeType, TNodeType>> Edges { get; private set; }
}

public class Node<TNodeType>
{
    public Node()
    {
        CostTo = 0f;
        EstimatedCost = 0f;
        TotalCost = 0f;
        Predecessor = null;
        Membership = "";
    }
    public Color NodeColor { get; set; }
    public TNodeType Position { get; set; }

    // Used to trace back the path from goal node to start after A* search. 
    public Node<Vector3> Predecessor { get; set; }
    // Used for cluster heuristic
    public string Membership { get; set; }

    // See "A-star-pseudocode.png"
    // g() : Cost so Far
    public float CostTo { get; set; }
    // h() :  Heuristic value, i.e. Best guess.
    public float EstimatedCost { get; set; }
    // f() :  Combined cost
    public float TotalCost { get; set; }

}

public class Edge<TEdgeType, TNodeType>
{
    public Color EdgeColor { get; set; }
    public TEdgeType Weight { get; set; }
    public Node<TNodeType> From { get; set; }
    public Node<TNodeType> To { get; set; }
}