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
    public Color NodeColor { get; set; }
    public TNodeType Position { get; set; }
}

public class Edge<TEdgeType, TNodeType>
{
    public Color EdgeColor { get; set; }
    public TEdgeType Weight { get; set; }
    public Node<TNodeType> From { get; set; }
    public Node<TNodeType> To { get; set; }
}