using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Editor;
using Unity.MLAgents.SideChannels;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(DecisionRequester))]

public class NNTrack : Agent
{
    [System.Serializable]
    public class RewardInfo
    {                                           
        public float mult_forward = 0.001f; 
        public float mult_barrier = -0.8f; 
        public float mult_car = -0.5f;

        public float win = 1.0f;                // 5.0
        public float lose = -1.0f;              //-1.0
        public float target = -1.0f;              //-1.0



    }

    public float Movespeed = 30;
    public float Turnspeed = 100;
    public RewardInfo rwd = new RewardInfo();
    public bool doEpisodes = true;

    private Rigidbody rb = null;   
    private Vector3 recall_position;
    private Quaternion recall_rotation;
    private Bounds bnd;
    public ParticleSystem vfx;

    //혜린 추가
    public Transform target;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("Starting RollerAgent");
        //Monitor.SetActive(true);
    }

  

    // 초기화 작업을 위해 한번 호출되는 메소드
    public override void Initialize()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.drag = 1;
        rb.angularDrag = 5;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;

        this.GetComponent<MeshCollider>().convex = true;        
        this.GetComponent<DecisionRequester>().DecisionPeriod = 1;
        bnd = this.GetComponent<MeshRenderer>().bounds;

        recall_position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
        recall_rotation = new Quaternion(this.transform.rotation.x, this.transform.rotation.y, this.transform.rotation.z, this.transform.rotation.w);
    }


    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        this.transform.position = recall_position;
        this.transform.rotation = recall_rotation;
    }
 
    //브레인(정책)으로 부터 전달 받은 행동을 실행하는 메소드
    public override void OnActionReceived(ActionBuffers actions)
    {
        //decisionrequestor component needed
        //  space type: discrete
        //      branches size: 2 move, turn
        //          branch 0 size: 3  fwd, nomove, back
        //          branch 1 size: 3  left, noturn, right

        if (isWheelsDown() == false)
            return;

        float mag = Mathf.Abs(rb.velocity.sqrMagnitude);

        switch (actions.DiscreteActions.Array[0])   //move
        {
            case 0:
                break;
            case 1:
                rb.AddRelativeForce(Vector3.back * Movespeed * Time.deltaTime, ForceMode.VelocityChange); //back
                break;
            case 2:
                rb.AddRelativeForce(Vector3.forward * Movespeed * Time.deltaTime, ForceMode.VelocityChange); //forward
                AddReward(mag * rwd.mult_forward);
                break;

        }

        switch (actions.DiscreteActions.Array[1])   //turn
        {
            case 0:
                break;
            case 1:
                this.transform.Rotate(Vector3.up, -Turnspeed * Time.deltaTime); //left
                break;
            case 2:
                this.transform.Rotate(Vector3.up, Turnspeed * Time.deltaTime); //right
                break;
        }

    }
    protected Vector3 AgentVelocity
    {
        get { return rb.velocity; }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //Purpose:  for me to simulate the brain actions (I control the car with the keyboard)
        actionsOut.DiscreteActions.Array[0] = 0;
        actionsOut.DiscreteActions.Array[1] = 0;
        

        float move = Input.GetAxis("Vertical");     // -1..0..1  WASD arrowkeys
        float turn = Input.GetAxis("Horizontal");

        if (move < 0)
            actionsOut.DiscreteActions.Array[0] = 1;    //back
        else if (move > 0)
            actionsOut.DiscreteActions.Array[0] = 2;    //forward

        if (turn < 0)
            actionsOut.DiscreteActions.Array[1] = 1;    //left
        else if (turn > 0)
            actionsOut.DiscreteActions.Array[1] = 2;    //right


    }

    private void OnCollisionEnter(Collision collision)
    {
        float mag = collision.relativeVelocity.sqrMagnitude;

        if (collision.gameObject.CompareTag("Wall") == true)
        {
            vfx.Play();
            AddReward(mag * rwd.mult_barrier);
            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Car") == true)
        {
            vfx.Play();
            AddReward(mag * rwd.mult_car);
            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Win") == true)
        {
            vfx.Play();
            AddReward(rwd.win);
            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Lose") == true)
        {
            vfx.Play();
            AddReward(rwd.lose);
            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Target") == true)
        {
            vfx.Play();
            AddReward(rwd.target);
            if (doEpisodes == true)
                EndEpisode();
        }

    }
    private bool isWheelsDown()
    {
        //raycast down from car = ground should be closely there
        return Physics.Raycast(this.transform.position, -this.transform.up, bnd.size.y * 0.55f);
    }


}
