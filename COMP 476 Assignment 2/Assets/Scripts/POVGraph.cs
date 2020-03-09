using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;
using System.Xml.Linq;
using System.IO;

[ExecuteInEditMode]

public class POVGraph : MonoBehaviour
{
    public Dictionary<string, float> lookUpTable;

    public Graph<Vector3, float> graph;
    public List<Edge<float, Vector3>> path; 
    public Pathfinder pathfinder;
    public Vector3 startPosition;
    public Vector3 goalPosition;
    public Node<Vector3> startNode;
    public Node<Vector3> goalNode;  
    private bool goalNodePlaced = false;

    private List<GameObject> clusters;
    private GameObject[] walls;
    private GameObject player;

    private const float SQUARE_BOUNDS = 4.92f;

    public enum Graph_Mode { Dynamic, Static  };
    public enum Heuristic{ Null, Euclidean, Cluster};

    public Heuristic HEURISTIC;

    [Tooltip("DYNAMIC *worse performance*: Select this option to have the graph adjust in realtime to changes in player and level geometry. " +
        "\n\nSTATIC *better performance*: Graph is only ever updated on Start() and when a goal node is placed or removed.")]
    public Graph_Mode GRAPH_MODE;

    [Tooltip("【﻿ＡＥＳＴＨＥＴＩＣＳ】")]
    public Color NODE_COLOR = Color.red;

    [Tooltip("Show the weights on the edges of the graph *worse performance*.")]
    public bool SHOW_WEIGHTS = false;

    [Tooltip("【﻿ＡＥＳＴＨＥＴＩＣＳ】")]
    [Range(0.0625f, 0.25f)]
    public float NODE_SIZE = 0.075f;

    [Tooltip("A higher value results in a more complex graph *worse performance*.")]
    [Range(0.1f, 9.4f)]
    public float GRAPH_RESOLUTION = 1.4f;

    [Tooltip("Adjusts the vertical height of the graph relative to the floor.")]
    [Range(0.001f, 1.0f)]
    public float VERTICAL_OFFSET = 0.001f;

    void Start()
    {
        initGraph();
        addNodesFromGeometry();
        castAddEdges();


        /*WARNING: 
         * Uncomment this section for ONLY if you've modified POVGraph (resolution, player radius, or by changing level geometry).
         * This will create a new look-up table as an XML file which will be read by ReadLookUpXML() to initialize POVGraph's look up table.
         * Make sure POV graph's heuristic mode is set to "Euclidean" when doing so! 
         * Comment out the call to ReadLookUpXML() in injectNodeOnClick() if making a new table.*/

        //setNodesMemberships();
        //WriteLookUpXML();

        /*WARNING*/

    }

    void Update()
    {
        // When Dynamic graph mode is enabled, the graph is re-initialized per frame. 
        if (GRAPH_MODE == Graph_Mode.Dynamic)
        {
            initGraph();
            addNodesFromGeometry();
            addStartAndGoal();
            castAddEdges();
        }
        injectNodeOnClick();
    }
    void OnDrawGizmos()
    {
        // Resolves null reference exception on project startup
        if (graph == null)
            Start();

        // Iterate through the graph's edge list and draw a line for each. 
        foreach (var edge in graph.Edges)
        {
            Gizmos.color = edge.EdgeColor;
            Gizmos.DrawLine(edge.From.Position, edge.To.Position);

            if (SHOW_WEIGHTS)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                Vector3 midpoint = edge.From.Position + 0.5f * (edge.To.Position - edge.From.Position);
                Handles.Label(midpoint, edge.Weight.ToString("F2"), style);
            }
        }

        // Iterate through graph's node list and draw a sphere for each. 
        foreach (var node in graph.Nodes)
        {
            Gizmos.color = node.NodeColor;
            if (goalNodePlaced && node.Position == goalNode.Position || goalNodePlaced && node.Position == startNode.Position)
            {
                node.Position = new Vector3(node.Position.x, VERTICAL_OFFSET, node.Position.z);
                Gizmos.DrawSphere(node.Position, NODE_SIZE * 1.5f);
                Gizmos.color = Color.cyan; ;
                Gizmos.DrawSphere(node.Position, NODE_SIZE * 0.75f);

            }
            else
                Gizmos.DrawSphere(node.Position, NODE_SIZE);
        }
        
        // If a goal node has been placed, then show a color-coded fill. 
        if (goalNodePlaced)
        {
            // Magenta color for examined nodes (nodes that went into the open list)
            foreach (var n in pathfinder.Examined_List)
            {
                if (n.Position != goalNode.Position && n.Position != startNode.Position)
                {
                    n.Position = new Vector3(n.Position.x, VERTICAL_OFFSET, n.Position.z);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(n.Position, NODE_SIZE);
                }
            }

            // Yellow color for nodes that are in the closed list after invocation of A* search
            foreach (var n in pathfinder.Closed_List)
            {
                if (n.Position != goalNode.Position && n.Position != startNode.Position)
                {
                    n.Position = new Vector3(n.Position.x, VERTICAL_OFFSET, n.Position.z);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(n.Position, NODE_SIZE);
                }
            }

            // Draw thick green lines to highlight the shortest path
            foreach (var edge in path)
            {
                var thickness = 20f;

                var p1 = new Vector3(edge.From.Position.x, VERTICAL_OFFSET, edge.From.Position.z);
                var p2 = new Vector3(edge.To.Position.x, VERTICAL_OFFSET, edge.To.Position.z);
                Handles.DrawBezier(p1, p2, p1, p2, Color.green, null, thickness);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(p1, NODE_SIZE);
                Gizmos.DrawSphere(p2, NODE_SIZE);
            }
        }
    }

    void initGraph()
    {
        graph = new Graph<Vector3, float>();

        lookUpTable = new Dictionary<string, float>();

        path = new List<Edge<float, Vector3>>();

        if (clusters == null)
        {
            List<GameObject> A = GameObject.FindGameObjectsWithTag("A").ToList<GameObject>();
            List<GameObject> B = GameObject.FindGameObjectsWithTag("B").ToList<GameObject>();
            List<GameObject> C = GameObject.FindGameObjectsWithTag("C").ToList<GameObject>();
            List<GameObject> D = GameObject.FindGameObjectsWithTag("D").ToList<GameObject>();
            List<GameObject> E = GameObject.FindGameObjectsWithTag("E").ToList<GameObject>();
            List<GameObject> F = GameObject.FindGameObjectsWithTag("F").ToList<GameObject>();

            clusters = A.Union<GameObject>(B).ToList<GameObject>();
            clusters = clusters.Union<GameObject>(C).ToList<GameObject>();
            clusters = clusters.Union<GameObject>(D).ToList<GameObject>();
            clusters = clusters.Union<GameObject>(E).ToList<GameObject>();
            clusters = clusters.Union<GameObject>(F).ToList<GameObject>();
        }

        if (walls == null)
            walls = GameObject.FindGameObjectsWithTag("Wall");

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }
    void addNodesFromGeometry()
    {
        // Player extents are flattened to x-z plane (y = 0)
        Vector3 playerExtents = new Vector3(player.GetComponentInChildren<CapsuleCollider>().bounds.extents.x, 0, player.GetComponent<CapsuleCollider>().bounds.extents.z);
        
        // A container that will hold the vertices extracted from the level geometry (the bounds of all colliders tagged "Wall"). 
        List<Vector3> vertices = new List<Vector3>();

        // Iterate through all walls in the scene
        foreach (var wall in walls)
        {
            // Get the bounds of the wall
            Bounds bounds = wall.GetComponent<Collider>().bounds;

            //// WITH FILTERING: Filters candidate node vertices for duplicates and by proximity, as well as nodes enveloped by colliders.
            
             /* Calculate the new positions of the bounding-radius-expanded bounds, based on collider bounds and player extents.
              * Flattened to x-z plane, y-component is set to the vertical offset from the floor of the level (adjustable for debugging and tolerancing raycasts from nodes). */
            Vector3[] boundOffsets = {      new Vector3(bounds.min.x,                   VERTICAL_OFFSET, bounds.min.z)  - playerExtents,
                                            new Vector3(bounds.min.x - playerExtents.x, VERTICAL_OFFSET, bounds.max.z   + playerExtents.z),
                                            new Vector3(bounds.max.x,                   VERTICAL_OFFSET, bounds.max.z)  + playerExtents,
                                            new Vector3(bounds.max.x + playerExtents.x, VERTICAL_OFFSET, bounds.min.z   - playerExtents.z) };

            bool duplicateFound = false;
            // Iterate through each of the bounds expanded points. 
            foreach (var boundOffset in boundOffsets)
            {
                /* Check if the bound offset is already vertices container, within a marging, if so, set duplicate flag to true.
                 * Note: The third parameter passed to isEqualEnough() is the margin with which two vertices are considered effectively equal.
                 * Increasing graph resolution lowers the margin, which results in more nodes and a more detailed graph. */
                foreach (var vertex in vertices)
                    if (isEqualEnough(boundOffset, vertex, 1.0f / GRAPH_RESOLUTION))
                        duplicateFound = true;  

                if (!duplicateFound)
                {
                    // Another foreach-loop with a bool flag to omit a bound offset vertex within a wall object's collider. 
                    bool insideWall = false;
                    foreach(var wall_in in walls)
                        if (wall_in.GetComponent<Collider>().bounds.Contains(boundOffset))
                            insideWall = true;

                    // The bound offset vertex is only added to the vertices container when no duplicate has been found and it is not within a collider. 
                    if(!insideWall)
                    vertices.Add(boundOffset);
                }
                
                duplicateFound = false;
            }
        }

        // Iterate through the vertices container and if the vertex is within the space of the map, add it as a node to the graph.
        foreach (var vertex in vertices)
            if (vertex.z < SQUARE_BOUNDS && vertex.z > -SQUARE_BOUNDS && vertex.x < SQUARE_BOUNDS && vertex.x > -SQUARE_BOUNDS)
            {
                graph.Nodes.Add(new Node<Vector3>() { Position = vertex, NodeColor = NODE_COLOR });
            }
    }

    void addStartAndGoal()
    {
        // Helper function used in Update() to ensure that start and goal nodes are included when in Dynamic mode (else they disappear as the graph is constantly updated). 

        if (goalNodePlaced)
        {
            graph.Nodes.Add(new Node<Vector3>() { Position = goalPosition, NodeColor = Color.green });
            goalNode = graph.Nodes[graph.Nodes.Count - 1];
            graph.Nodes.Add(new Node<Vector3>() { Position = startPosition, NodeColor = Color.yellow });
            startNode = graph.Nodes[graph.Nodes.Count - 1];
        }
    }
    void setNodesMemberships()
    {
        /* Is only relevant when a new look-up table is being generated. 
         * Attributes each node with membership to a cluster (i.e. A, B, C, etc.) */

        foreach (var n in graph.Nodes)
            foreach (var c in clusters)
                if (c.GetComponent<Collider>().bounds.Contains(n.Position))
                {
                    n.Membership = c.name;
                }
    }

    void WriteLookUpXML()
    {
        foreach (var start in graph.Nodes)
        {
            foreach (var goal in graph.Nodes)
            {
                // Ignores node pairs that are in the same cluster
                if (start.Membership.Equals(goal.Membership))
                    continue;
                /*The look-up table is implemented as a Dictionary<string,float>. 
                 * By concatinating start's and goal's memberships, we'll obtain keys such as AB or EF.*/
                string key = start.Membership + goal.Membership;
                // Get the shortest path between the node pairs
                float value = new Pathfinder(start, goal, graph, lookUpTable).FindShortestValue();

                /* If the key already exists in the table, then compare values and keep the lower value. 
                 * If the key doesn't exist, then just add it.*/
                if (lookUpTable.ContainsKey(key))
                {
                    if (value < lookUpTable[key])
                    {
                        lookUpTable.Remove(key);
                        lookUpTable.Add(key, value);
                    }
                }
                else
                    lookUpTable.Add(key, value);
            }
        }

        string path = "LUTable.xml";
        // Delete the file if it exists.
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        new XElement("root", lookUpTable.Select(kv => new XElement(kv.Key, kv.Value))).Save("LUTable.xml", SaveOptions.OmitDuplicateNamespaces);
    }
    void ReadLookUpXML()
    {
        lookUpTable = XElement.Parse(File.ReadAllText("LUTable.xml")).Elements().ToDictionary(k => k.Name.ToString(), v => float.Parse(v.Value.ToString()));

        // Leaving this here just in case, for debugging. 

        //foreach (var entry in lookUpTable)
        //{
        //    Debug.Log("AA" + " " + lookUpTable["AB"] + " " + lookUpTable["AC"] + " " + lookUpTable["AD"] + " " + lookUpTable["AE"] + " " + lookUpTable["AF"]);
        //    Debug.Log(lookUpTable["BA"] + " " + "BB" + " " + lookUpTable["BC"] + " " + lookUpTable["BD"] + " " + lookUpTable["BE"] + " " + lookUpTable["BF"]);
        //    Debug.Log(lookUpTable["CA"] + " " + lookUpTable["CB"] + " " + "CC" + " " + lookUpTable["CD"] + " " + lookUpTable["CE"] + " " + " " + lookUpTable["CF"]);
        //    Debug.Log(lookUpTable["DA"] + " " + lookUpTable["DB"] + " " + lookUpTable["DC"] + " " + "DD" + " " + lookUpTable["DE"] + " " + lookUpTable["DF"]);
        //    Debug.Log(lookUpTable["EA"] + " " + lookUpTable["EB"] + " " + lookUpTable["EC"] + " " + lookUpTable["ED"] + " " + "EE" + " " + lookUpTable["EF"]);
        //    Debug.Log(lookUpTable["FA"] + " " + lookUpTable["FB"] + " " + lookUpTable["FC"] + " " + lookUpTable["FD"] + " " + lookUpTable["FE"] + " " + "FF");
        //}

        }
    void castAddEdges()
    {
        // Nested foreach-loops to take each node (nodeFrom) and raycast to all other nodes (nodeTo).
        foreach (var nodeFrom in graph.Nodes)
        {
            foreach (var nodeTo in graph.Nodes)
            {
                float distance = (nodeTo.Position - nodeFrom.Position).magnitude;
                // Get all the hits
                RaycastHit[] hits;
                hits = Physics.RaycastAll(nodeFrom.Position, (nodeTo.Position - nodeFrom.Position).normalized, distance);

                // Check if in all those hits, there's a wall hit. 
                bool wallHit = false;
                foreach (var hit in hits)
                    if (hit.collider.CompareTag("Wall"))
                    {
                        wallHit = true;
                        break;
                    }

                // If there is no wall hit and so long as the "from" node is not equal to the "to" node, add the edge to the graph. 
                if (!wallHit && nodeFrom != nodeTo)
                    graph.Edges.Add(new Edge<float, Vector3>() { Weight = distance, From = nodeFrom, To = nodeTo, EdgeColor = Color.grey });          
            }
        }
    }

    bool isEqualEnough(Vector3 node0, Vector3 node1, float enough)
    {
        // Returns true if the magnitude of the difference between two positions is less than the margin "enough"
        return Mathf.Abs((node0 - node1).magnitude) < enough; 
    }

    void injectNodeOnClick() 
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Floor"))
                {
                    // Snapshot containers for the start and goal node positions
                    goalPosition = hit.point;
                    startPosition = new Vector3(player.transform.position.x, VERTICAL_OFFSET, player.transform.position.z);

                    // Conditional update i.e. reinitialization of graph
                    initGraph();
                    addNodesFromGeometry();

                    /* If the goal-placed flag is false, then we inject them and set the flag to true.
                     * If the flag is true, then the graph is just reinitialized without the start and goal nodes. 
                     * This allows for the behaviour of clicking to alternatingly add or remove the start and goal nodes with every click. */
                    if (!goalNodePlaced){
                        graph.Nodes.Add(new Node<Vector3>() { Position = goalPosition, NodeColor = Color.green });
                        goalNode = graph.Nodes[graph.Nodes.Count - 1];
                        graph.Nodes.Add(new Node<Vector3>() { Position = startPosition, NodeColor = Color.yellow });
                        startNode = graph.Nodes[graph.Nodes.Count - 1];
                        goalNodePlaced = true;
                    }
                    else
                        goalNodePlaced = false;

                    // Whether the start and goal nodes were placed or not, we now re-cast the edges. 
                    castAddEdges();

                    // Then, if goal node is placed...
                    if (goalNodePlaced)
                    {
                        // Update the look up table
                        ReadLookUpXML();        // WARNING: Uncomment this if making a new look up table. 
                        setNodesMemberships();  // Only relevant if writing a new look up table XML (see Update()) 

                        // Create a new pathfinder instance, update the heuristic type and get the shortest path. 
                        pathfinder = new Pathfinder(startNode, goalNode, graph, lookUpTable);
                        pathfinder.heuristic = HEURISTIC;
                        path = pathfinder.FindShortestPath(lookUpTable);
                    }
                }
            }
        }
    }


}
