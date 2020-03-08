using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO:
public class Pathfinder : MonoBehaviour
{
    List<Node<Vector3>> Open_List;
    List<Node<Vector3>> Closed_List;
    List<Node<Vector3>> Examined_List;

    public Pathfinder(Node<Vector3> start, Node<Vector3> goal, Graph<Vector3, float> graph)
    {
        Open_List = new List<Node<Vector3>>();
        Closed_List = new List<Node<Vector3>>();
        Examined_List = new List<Node<Vector3>>();

        Open_List.Add(start);
    }


    // TODO:
    List<Node<Vector3>> FindShortestPath(Node<Vector3> start, Node<Vector3> goal)
    {

        while(Closed_List.Count != 0)
        {

        }
        return new List<Node<Vector3>>(); 
    }


}
