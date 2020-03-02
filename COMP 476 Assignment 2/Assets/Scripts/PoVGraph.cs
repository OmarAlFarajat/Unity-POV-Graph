using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PoVGraph : MonoBehaviour
{
    private Graph<Vector3, float> graph;
    public GameObject[] walls;
    public GameObject player;
    Vector3[] vertices;

    // Start is called before the first frame update
    void Update()
    {
        graph = new Graph<Vector3, float>();
        float offset = 0.35f;
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-3.25f, offset, -0.125f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-4.5f, offset, 1f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-4.5f, offset, -1.25f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-0.26f, offset, -1f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-4.5f, offset, -2.75f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-4.5f, offset, -4.5f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-4.6f, offset, 1.95f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-4.5f, offset, 2.75f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(-4.5f, offset, 4.5f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(1, offset, -1.5f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(2.25f, offset, -2.8f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(4.3f, offset, -4.3f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(4.3f, offset, -2.5f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(2.3f, offset, -0.3f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(3.7f, offset, 1.1f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(4.5f, offset, 1.15f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(4.5f, offset, -0.15f), NodeColor = Color.yellow });
        graph.Nodes.Add(new Node<Vector3>() { Value = new Vector3(4.5f, offset, -1.5f), NodeColor = Color.yellow });



        if (walls == null)
            walls = GameObject.FindGameObjectsWithTag("Wall");

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        Vector3 playerExtents = new Vector3(player.GetComponent<CapsuleCollider>().bounds.extents.x, 0, player.GetComponent<CapsuleCollider>().bounds.extents.z);
        
        foreach (var wall in walls)
        {
            Bounds b = wall.GetComponent<BoxCollider>().bounds;

            Vector3[] points = new Vector3[4];
            points[0] = new Vector3(b.min.x, 0.50f, b.min.z) - playerExtents;
            points[1] = new Vector3(b.min.x - playerExtents.x, 0.50f, b.max.z + playerExtents.z);
            points[2] = new Vector3(b.max.x, 0.50f, b.max.z) + playerExtents;
            points[3] = new Vector3(b.max.x + playerExtents.x, 0.50f, b.min.z - playerExtents.z);

            foreach (var point in points)
            {
                if(point.z < 4.85f && point.z > -4.85f && point.x < 4.85f && point.x > -4.85f)
                graph.Nodes.Add(new Node<Vector3>() { Value = point, NodeColor = Color.yellow });
            }
        }

        bool hitSomething = false;
        foreach (var nodeFrom in graph.Nodes)
        {
            foreach (var nodeTo in graph.Nodes)
            {
                if (!(nodeFrom == nodeTo) && !(Physics.Linecast(nodeFrom.Value, nodeTo.Value, out RaycastHit hit) && (hit.collider.CompareTag("Wall") && !hit.collider.CompareTag("Player"))))
                {
                    graph.Edges.Add(new Edge<float, Vector3>() { Value = 1.0f, From = nodeFrom, To = nodeTo, EdgeColor = Color.grey });
                    hitSomething = true;
                }
            }
            if (!hitSomething)
                graph.Nodes.Remove(nodeFrom);
            else
                hitSomething = false;
        }



    }

    void OnDrawGizmos()
    {
        if(graph == null)
            Update();

        foreach(var node in graph.Nodes)
        {
            Gizmos.color = node.NodeColor;
            Gizmos.DrawSphere(node.Value, 0.0625f);
        }

        foreach (var edge in graph.Edges)
        {
            Gizmos.color = edge.EdgeColor;
            Gizmos.DrawLine(edge.From.Value, edge.To.Value);
        }
    }
}
