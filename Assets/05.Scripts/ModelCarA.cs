using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ModelCarA : MonoBehaviour
{
    NavMeshAgent agent = null;
    public Transform[] pointA;
    int index = 0;

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {

        float distance = Vector3.Distance(transform.position, pointA[index].position);
        agent.destination = pointA[index].position;
        if (distance < 2)
        {
            int len = pointA.Length;
            index = (index + 1) % len;

        }

    }
}
