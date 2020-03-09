using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using static POVGraph;

public class Pathfinder 
{
    Graph<Vector3, float> graph;
    public Heuristic heuristic; 
    public List<Node<Vector3>> Open_List;
    public List<Node<Vector3>> Closed_List;
    public List<Edge<float, Vector3>> Closed_Edges;
    public List<Node<Vector3>> Examined_List;
    Node<Vector3> startNode;
    Node<Vector3> goalNode;

    public Pathfinder(Node<Vector3> startNode, Node<Vector3> goalNode, Graph<Vector3, float> graph, Dictionary<string, float> LU_Table)
    {
        this.graph = graph; 
        Open_List = new List<Node<Vector3>>();
        Closed_List = new List<Node<Vector3>>();
        Examined_List = new List<Node<Vector3>>();

        this.startNode = startNode;
        this.goalNode = goalNode;

        this.startNode.CostTo = 0f;
        this.startNode.EstimatedCost = (this.goalNode.Position - this.startNode.Position).sqrMagnitude;

        switch (heuristic)
        {
            case Heuristic.Null:
                this.startNode.TotalCost = this.startNode.CostTo;
                break;
            case Heuristic.Euclidean:
                this.startNode.TotalCost = this.startNode.CostTo + this.startNode.EstimatedCost;
                break;
            case Heuristic.Cluster:
                if (startNode.Membership.Equals(goalNode.Membership))
                    this.startNode.TotalCost = this.startNode.CostTo + this.startNode.EstimatedCost;
                else
                {
                    Debug.Log("Using Look Up!");
                    this.startNode.TotalCost = this.startNode.CostTo + LU_Table[startNode.Membership + goalNode.Membership];
                }
                break;
        }

        Open_List.Add(this.startNode);
    }

    public List<Edge<float, Vector3>> FindShortestPath(Dictionary<string,float> LU_Table)
    {
        List<Edge<float, Vector3>> path = new List<Edge<float, Vector3>>(); 

        while (Open_List.Count != 0)
        {
            Open_List.Sort((p1, p2) => p1.TotalCost.CompareTo(p2.TotalCost));

            Node<Vector3> m = Open_List[0];

            if (m == this.goalNode)
            {
                while (m.Predecessor != null)
                {
                    path.Add(new Edge<float, Vector3>() { EdgeColor = Color.green, From = m, To = m.Predecessor });
                    m = m.Predecessor;
                }

                return path;   
            }

            Open_List.Remove(m);

            Closed_List.Add(m);

            foreach (var n in GetChildren(m))
            {
                if (Closed_List.Contains(n))
                    continue;
                float cost = m.CostTo + (m.Position-n.Position).sqrMagnitude;

                if (Open_List.Contains(n) && cost < n.CostTo)
                    Open_List.Remove(n);

                if (Closed_List.Contains(n) && cost < n.CostTo)
                    Closed_List.Remove(n);

                if (!Open_List.Contains(n) && !Closed_List.Contains(n))
                {
                    Examined_List.Add(n);
                    Open_List.Add(n);
                    n.Predecessor = m;      // PARENT
                    n.CostTo = cost;
                    switch (heuristic)
                    {
                        case Heuristic.Null:
                            n.TotalCost = n.CostTo /*+ n.EstimatedCost*/;
                            break;
                        case Heuristic.Euclidean:
                            n.EstimatedCost = (this.goalNode.Position - n.Position).sqrMagnitude;
                            n.TotalCost = n.CostTo + n.EstimatedCost;
                            break;
                        case Heuristic.Cluster:
                            if (m.Membership.Equals(n.Membership)) {
                                n.EstimatedCost = (this.goalNode.Position - n.Position).sqrMagnitude;
                                n.TotalCost = n.CostTo + n.EstimatedCost;
                            }
                            else
                            {
                                Debug.Log("Using Look-Up Table with key: " + m.Membership + n.Membership);

                                n.EstimatedCost = LU_Table[(m.Membership + n.Membership).Trim()];
                                n.TotalCost = n.CostTo + n.EstimatedCost;
                            }
                            break;
                    }
                }
            }
        }
        return null;    // SKETCH

    }

    public float GetShortestValue()
    {
        while (Open_List.Count != 0)
        {
            Open_List.Sort((p1, p2) => p1.TotalCost.CompareTo(p2.TotalCost));

            Node<Vector3> m = Open_List[0];

            if (m == this.goalNode)
            {
                return m.TotalCost;
            }

            Open_List.Remove(m);

            Closed_List.Add(m);

            foreach (var n in GetChildren(m))
            {
                if (Closed_List.Contains(n))
                    continue;
                float cost = m.CostTo + (m.Position - n.Position).sqrMagnitude;

                if (Open_List.Contains(n) && cost < n.CostTo)
                    Open_List.Remove(n);

                if (Closed_List.Contains(n) && cost < n.CostTo)
                    Closed_List.Remove(n);

                if (!Open_List.Contains(n) && !Closed_List.Contains(n))
                {
                    Examined_List.Add(n);
                    Open_List.Add(n);
                    n.Predecessor = m;      // PARENT
                    n.CostTo = cost;
                    n.EstimatedCost = (this.goalNode.Position - n.Position).sqrMagnitude;
                    n.TotalCost = n.CostTo + n.EstimatedCost;       // (!) Assumes that GetShortestValue() is only ever using Euclidean heuristic (for cluster look up table generation). 

                    //switch (heuristic)
                    //{
                    //    case Heuristic.Null:
                    //        n.TotalCost = n.CostTo /*+ n.EstimatedCost*/;
                    //        break;
                    //    case Heuristic.Euclidean:
                    //        n.TotalCost = n.CostTo + n.EstimatedCost;
                    //        break;
                    //}
                }
            }
        }

        return 0;   // SKETCH
    }

    List<Node<Vector3>> GetChildren(Node<Vector3> source)
    {
        List<Node<Vector3>> Children = new List<Node<Vector3>>(); 

        foreach(var n in this.graph.Edges)
            if (n.From == source)
                Children.Add(n.To);

        return Children;
        
    }

}
