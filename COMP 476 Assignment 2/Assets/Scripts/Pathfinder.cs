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
        // Outbound container of edges to be returned when path is found
        List<Edge<float, Vector3>> path = new List<Edge<float, Vector3>>();

        /* Adapted from the pseudocode found in "A-star-pseudocode.png"
         * SOURCE: 
         * Sharma, H., Alekseychuk, A., Leskovsky, P. et al. 
         * Determining similarity in histological images using graph-theoretic description and matching methods for content-based image retrieval in medical diagnostics. 
         * Diagn Pathol 7, 134 (2012). https://doi.org/10.1186/1746-1596-7-134 */
        while (Open_List.Count != 0)
        {
            // Get m, the node on top of the open list, with least total cost f()
            Open_List.Sort((p1, p2) => p1.TotalCost.CompareTo(p2.TotalCost));
            Node<Vector3> m = Open_List[0];

            if (m == this.goalNode)
            {
                // Adapted the pseudocode so that when the goal node is found, it builds an edge list going backwards from the goal node
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
                    Open_List.Remove(n);    // "Remove n from open list as new path is better"

                if (Closed_List.Contains(n) && cost < n.CostTo)
                    Closed_List.Remove(n);

                if (!Open_List.Contains(n) && !Closed_List.Contains(n))
                {
                    Examined_List.Add(n);
                    Open_List.Add(n);
                    n.Predecessor = m;      // Keep track of the predecessor node i.e. "parent" for backwards traversal of solution path
                    n.CostTo = cost;
                    // Adapted the pseudocode here to accomodate different heuristic types
                    switch (heuristic)
                    {
                        case Heuristic.Null:
                            n.TotalCost = n.CostTo;
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
                                n.EstimatedCost = LU_Table[m.Membership + n.Membership];
                                n.TotalCost = n.CostTo + n.EstimatedCost;
                            }
                            break;
                    }
                }
            }
        }
        // If this function ever returns null, something is wrong...
        return null;    // SKETCH

    }

    /* This is nearly identical to FindShortestPath (see "Diffs" below), but only returns the total cost of the path. 
     * This is only ever used when making the XML file for the look-up table. 
     * The shortest paths between clusters uses Euclidean heuristic */
    public float FindShortestValue()
    {
        while (Open_List.Count != 0)
        {
            Open_List.Sort((p1, p2) => p1.TotalCost.CompareTo(p2.TotalCost));

            Node<Vector3> m = Open_List[0];

            if (m == this.goalNode)
            {
                // Diff: Ends earlier here and just returns the total cost of the shortest path found. 
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
                    // Diff: Defaults to Euclidean heuristic always. 
                    n.EstimatedCost = (this.goalNode.Position - n.Position).sqrMagnitude;
                    n.TotalCost = n.CostTo + n.EstimatedCost;       // (!) Assumes that GetShortestValue() is only ever using Euclidean heuristic (for cluster look up table generation). 
                }
            }
        }

        return 0;   // SKETCH
    }

    List<Node<Vector3>> GetChildren(Node<Vector3> source)
    {
        // Helper function for the searches. Gets all nodes connected to the passed node (called children)

        List<Node<Vector3>> Children = new List<Node<Vector3>>(); 

        foreach(var n in this.graph.Edges)
            if (n.From == source)
                Children.Add(n.To);

        return Children;
        
    }

}
