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
    }
    public Color NodeColor { get; set; }
    public TNodeType Position { get; set; }
    public Node<Vector3> Predecessor { get; set; }
    public string Membership { get; set; }

    // g()
    public float CostTo { get; set; }
    // h()
    public float EstimatedCost { get; set; }
    // f()
    public float TotalCost { get; set; }

}

public class Edge<TEdgeType, TNodeType>
{
    public Color EdgeColor { get; set; }
    public TEdgeType Weight { get; set; }
    public Node<TNodeType> From { get; set; }
    public Node<TNodeType> To { get; set; }
}