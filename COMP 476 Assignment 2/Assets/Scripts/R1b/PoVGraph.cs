using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]

public class POVGraph : MonoBehaviour
{
    private Graph<Vector3, float> graph;
    private bool goalNodePlaced = false;
    private Vector3 goalNode;
    private Vector3 startNode;

    private GameObject[] walls;
    private GameObject player;

    private const float SQUARE_BOUNDS = 4.92f;

    public enum Graph_Mode { Dynamic, Static  };

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
    [Range(0.001f, 0.5f)]
    public float VERTICAL_OFFSET = 0.001f;

    void Start()
    {
        initGraph();
        addNodesFromGeometry();
        castAddEdges();

        Debug.Log("> ON START...");
        Debug.Log(">> NUMBER OF NODES: " + graph.Nodes.Count());
        Debug.Log(">> NUMBER OF EDGES: " + graph.Edges.Count());
    }
    void Update()
    {
        // When Dynamic graph mode is enabled, the graph is re-initialized per frame. 
        if (GRAPH_MODE == Graph_Mode.Dynamic)
        {
            initGraph();
            addNodesFromGeometry();

            if (goalNodePlaced)
            {
                graph.Nodes.Add(new Node<Vector3>() { Position = goalNode, NodeColor = Color.green });
                graph.Nodes.Add(new Node<Vector3>() { Position = startNode, NodeColor = Color.yellow });
            }

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
            Gizmos.DrawSphere(node.Position, NODE_SIZE);
        }

        // Leave this here as a reference to R2/R3. Will be used to "highlight" shortest and explored paths in A* search.  
        //var thickness = 10;

        //Vector3 verticalOffset = new Vector3(0f, 0f, 0f);
        //var p1 = graph.Edges[20].From.Position + verticalOffset;
        //var p2 = graph.Edges[20].To.Position + verticalOffset;
        //Handles.DrawBezier(p1, p2, p1, p2, Color.green, null, thickness);

        //p1 = graph.Edges[30].From.Position + verticalOffset;
        //p2 = graph.Edges[30].To.Position + verticalOffset;
        //Handles.DrawBezier(p1, p2, p1, p2, Color.yellow, null, thickness);

        //p1 = graph.Edges[40].From.Position + verticalOffset;
        //p2 = graph.Edges[40].To.Position + verticalOffset;
        //Handles.DrawBezier(p1, p2, p1, p2, Color.magenta, null, thickness);
    }

    void initGraph()
    {
        graph = new Graph<Vector3, float>();

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
                    bool insideCollider = false;
                    foreach(var wall_in in walls)
                        if (wall_in.GetComponent<Collider>().bounds.Contains(boundOffset))
                            insideCollider = true;

                    // The bound offset vertex is only added to the vertices container when no duplicate has been found and it is not within a collider. 
                    if(!insideCollider)
                    vertices.Add(boundOffset);
                }
                
                duplicateFound = false;
            }

            /* Comment out the block of code above and uncomment the code below to see the differences between filtered and unfiltered vertex processing. 
             * Spoiler Alert! With filtering is much better! */

            //// WITHOUT FILTERING
            //vertices.Add(new Vector3(bounds.min.x, VERTICAL_OFFSET, bounds.min.z) - playerExtents);
            //vertices.Add(new Vector3(bounds.min.x - playerExtents.x, VERTICAL_OFFSET, bounds.max.z + playerExtents.z));
            //vertices.Add(new Vector3(bounds.max.x, VERTICAL_OFFSET, bounds.max.z) + playerExtents);
            //vertices.Add(new Vector3(bounds.max.x + playerExtents.x, VERTICAL_OFFSET, bounds.min.z - playerExtents.z));
        }

        // Iterate through the vertices container and if the vertex is within the space of the map, add it as a node to the graph.
        foreach (var vertex in vertices)
            if (vertex.z < SQUARE_BOUNDS && vertex.z > -SQUARE_BOUNDS && vertex.x < SQUARE_BOUNDS && vertex.x > -SQUARE_BOUNDS)
                graph.Nodes.Add(new Node<Vector3>() { Position = vertex, NodeColor = NODE_COLOR });
    }

    void castAddEdges()
    {
        // Nested foreach-loops to take each node (nodeFrom) and raycast to all other nodes (nodeTo).
        foreach (var nodeFrom in graph.Nodes)
        {
            foreach (var nodeTo in graph.Nodes)
            {
                float distance = (nodeTo.Position - nodeFrom.Position).magnitude;
                RaycastHit[] hits;
                hits = Physics.RaycastAll(nodeFrom.Position, (nodeTo.Position - nodeFrom.Position).normalized, distance);

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
                    goalNode = hit.point;
                    startNode = new Vector3(player.transform.position.x, VERTICAL_OFFSET, player.transform.position.z);

                    initGraph();
                    addNodesFromGeometry();

                    if (!goalNodePlaced){
                        graph.Nodes.Add(new Node<Vector3>() { Position = goalNode, NodeColor = Color.green });
                        graph.Nodes.Add(new Node<Vector3>() { Position = startNode, NodeColor = Color.yellow });
                        goalNodePlaced = true;
                    }
                    else
                        goalNodePlaced = false;

                    castAddEdges();

                    Debug.Log("> ON INJECT...");
                    Debug.Log(">> NUMBER OF NODES: " + graph.Nodes.Count());
                    Debug.Log(">> NUMBER OF EDGES: " + graph.Edges.Count());
                }
            }
        }
    }


}