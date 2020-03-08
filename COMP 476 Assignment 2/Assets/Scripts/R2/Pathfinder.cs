using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO:
public class Pathfinder 
{
    Graph<Vector3, float> graph;
    public List<Node<Vector3>> Open_List;
    public List<Node<Vector3>> Closed_List;
    public List<Edge<float, Vector3>> Closed_Edges;
    public List<Node<Vector3>> Examined_List;
    Node<Vector3> startNode;
    Node<Vector3> goalNode;

    public Pathfinder(Node<Vector3> startNode, Node<Vector3> goalNode, Graph<Vector3, float> graph)
    {
        this.graph = graph; 
        Open_List = new List<Node<Vector3>>();
        Closed_List = new List<Node<Vector3>>();
        Examined_List = new List<Node<Vector3>>();

        this.startNode = startNode;
        this.goalNode = goalNode;

        this.startNode.CostTo = 0f;
        this.startNode.EstimatedCost = (this.goalNode.Position - this.startNode.Position).magnitude;
        this.startNode.TotalCost = this.startNode.CostTo + this.startNode.EstimatedCost; 
        Open_List.Add(this.startNode);
    }

    public void FindShortestPath()
    {
        while (Open_List.Count != 0)
        {
            // TODO: Verify that this is ascending
            Open_List.Sort((p1, p2) => p1.TotalCost.CompareTo(p2.TotalCost));

            Node<Vector3> m = Open_List[0];

            if (m == this.goalNode)
                break;  // TODO: Define behaviour on break

            Open_List.Remove(m);

            Closed_List.Add(m);

            foreach (var n in GetChildren(m))
            {
                if (Closed_List.Contains(n))
                    continue;
                float cost = m.CostTo + (m.Position-n.Position).magnitude;

                if (Open_List.Contains(n) && cost < n.CostTo)
                    Open_List.Remove(n);

                if (Closed_List.Contains(n) && cost < n.CostTo)
                    Closed_List.Remove(n);

                if (!Open_List.Contains(n) && !Closed_List.Contains(n))
                {
                    Examined_List.Add(n);
                    Open_List.Add(n);
                    n.CostTo = cost;
                    n.EstimatedCost = (this.goalNode.Position - n.Position).magnitude;
                    n.TotalCost = n.CostTo + n.EstimatedCost; 
                }
            }
        }
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
