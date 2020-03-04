using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public class PoVGraph : MonoBehaviour
{
    private Graph<Vector3, float> graph;
    public GameObject[] walls;
    public GameObject player;
    public float NODE_SIZE = 0.0625f;
    float VERTICAL_OFFSET = 0.001f;
    public new Camera camera;
    Vector3 goalNode;

    public bool goalNodePlaced = true;

    void Start()
    {
        initGraph();
        addNodesFromGeometry();
        castAddEdges();
    }
    void Update()
    {
        // Uncomment for debugging of radius and level geometry changes. Note: Placed goal nodes will reset every frame! 
        //initGraph();
        //addNodesFromGeometry();
        //castAddEdges();
        injectNodeOnClick();
    }
    void OnDrawGizmos()
    {
        if (graph == null)
            Start();

        foreach (var edge in graph.Edges)
        {
            Gizmos.color = edge.EdgeColor;
            Gizmos.DrawLine(edge.From.Value, edge.To.Value);
        }

        //var thickness = 10;


        //Vector3 verticalOffset = new Vector3(0f, 0f, 0f);
        //var p1 = graph.Edges[20].From.Value + verticalOffset;
        //var p2 = graph.Edges[20].To.Value + verticalOffset;
        //Handles.DrawBezier(p1, p2, p1, p2, Color.green, null, thickness);

        //p1 = graph.Edges[30].From.Value + verticalOffset;
        //p2 = graph.Edges[30].To.Value + verticalOffset;
        //Handles.DrawBezier(p1, p2, p1, p2, Color.yellow, null, thickness);

        //p1 = graph.Edges[40].From.Value + verticalOffset;
        //p2 = graph.Edges[40].To.Value + verticalOffset;
        //Handles.DrawBezier(p1, p2, p1, p2, Color.magenta, null, thickness);

        foreach (var node in graph.Nodes)
        {
            Gizmos.color = node.NodeColor;
            Gizmos.DrawSphere(node.Value, NODE_SIZE);
        }
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
        Vector3 playerExtents = new Vector3(player.GetComponentInChildren<CapsuleCollider>().bounds.extents.x, 0, player.GetComponent<CapsuleCollider>().bounds.extents.z);
        List<Vector3> vertices = new List<Vector3>();

        foreach (var wall in walls)
        {
            Bounds bounds = wall.GetComponent<Collider>().bounds;

            ///// WITH FILTERING
            Vector3[] boundOffsets = {   new Vector3(bounds.min.x, VERTICAL_OFFSET, bounds.min.z) - playerExtents,
                                            new Vector3(bounds.min.x - playerExtents.x, VERTICAL_OFFSET, bounds.max.z + playerExtents.z),
                                            new Vector3(bounds.max.x, VERTICAL_OFFSET, bounds.max.z) + playerExtents,
                                            new Vector3(bounds.max.x + playerExtents.x, VERTICAL_OFFSET, bounds.min.z - playerExtents.z) };

            bool duplicateFound = false;
            float duplicateThreshold = NODE_SIZE * 2;
            foreach (var boundOffset in boundOffsets)
            {
                foreach (var vertex in vertices)
                {
                    if (isEqualEnough(boundOffset, vertex, duplicateThreshold))
                    {
                        //Debug.Log("Found a dupe!");
                        duplicateFound = true;
                    }
                }

                if (!duplicateFound)
                    vertices.Add(boundOffset);
                else
                    duplicateFound = false;
            }

            //// WITHOUT FILTERING
            //vertices.Add(new Vector3(bounds.min.x, offset, bounds.min.z) - playerExtents);
            //vertices.Add(new Vector3(bounds.min.x - playerExtents.x, offset, bounds.max.z + playerExtents.z));
            //vertices.Add(new Vector3(bounds.max.x, offset, bounds.max.z) + playerExtents);
            //vertices.Add(new Vector3(bounds.max.x + playerExtents.x, offset, bounds.min.z - playerExtents.z));
        }

        //Debug.Log("NUMBER OF NODES: " + vertices.Count());


        foreach (var vertex in vertices)
            if (vertex.z < 4.92f && vertex.z > -4.92f && vertex.x < 4.92f && vertex.x > -4.92f)
                graph.Nodes.Add(new Node<Vector3>() { Value = vertex, NodeColor = Color.red });
    }

    void castAddEdges()
    {
        foreach (var nodeFrom in graph.Nodes)
        {
            foreach (var nodeTo in graph.Nodes)
            {
                RaycastHit[] hits;
                hits = Physics.RaycastAll(nodeFrom.Value, (nodeTo.Value - nodeFrom.Value).normalized, (nodeTo.Value - nodeFrom.Value).magnitude);

                bool wallHit = false;

                foreach (var hit in hits)
                {
                    if (hit.collider.CompareTag("Wall"))
                    {
                        wallHit = true;
                        break;
                    }
                }

                if (!wallHit && nodeFrom != nodeTo)
                    graph.Edges.Add(new Edge<float, Vector3>() { Value = 1.0f, From = nodeFrom, To = nodeTo, EdgeColor = Color.grey });
            }
        }
    }

    bool isEqualEnough(Vector3 node0, Vector3 node1, float enough)
    {
        return Mathf.Abs((node0 - node1).magnitude) < enough; 
    }

    void injectNodeOnClick()
    {
        //Debug.Log("INJECTED 1");
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("INJECTED 2");
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Floor"))
                {
                    //Debug.Log("INJECTED 3");
                    goalNode = hit.point;
 
                    if (!goalNodePlaced){
                        graph.Nodes.Add(new Node<Vector3>() { Value = goalNode, NodeColor = Color.green });
                        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(player.transform.position.x, VERTICAL_OFFSET,player.transform.position.z) , NodeColor = Color.yellow });
                        castAddEdges();
                        goalNodePlaced = true;
                    }
                    else
                    {
                        initGraph();
                        addNodesFromGeometry();
                        castAddEdges();
                        goalNodePlaced = false;
                    }
                }
            }
        }
    }
}