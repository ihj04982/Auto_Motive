using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;

public class CarDriverAgent : Agent
{
    [System.Serializable]
    public class RewardInfo
    {
        public float nomovement = -0.1f; 
        public float mult_forward = 0.001f;
        public float mult_backward = -0.001f;
        //public float mult_road = 0.01f;
        //public float mult_gravel = 0.001f;
        public float mult_rightbarrier = -0.1f;
        public float mult_leftbarrier = -0.2f;
        public float mult_checkpoint = 1f;
        public float mult_target = 1f;

        //public float wall = -1.0f;         //OnCollisionEnter punish + endepisode
        //public float mult_car = -0.5f;      //OnCollisionEnter punish + endepisode
        public float rightdirection = 0.0001f;
    }

    public float Movespeed = 5;
    public float Turnspeed = 10;
    public RewardInfo rwd = new RewardInfo();
    private Rigidbody rb;
    private Vector3 recall_position;
    private Quaternion recall_rotation;
    private Bounds bnd;

    public GameObject target;

    public override void Initialize()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.drag = 1;
        rb.angularDrag = 5;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;

        //this.GetComponent<MeshCollider>().convex = true;

        this.GetComponent<DecisionRequester>().DecisionPeriod = 1;
        bnd = this.GetComponent<MeshRenderer>().bounds;

        recall_position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
        recall_rotation = new Quaternion(this.transform.rotation.x, this.transform.rotation.y, this.transform.rotation.z, this.transform.rotation.w);

        //trackCheckPoints = TrackCheckpoints.instance; // 더블체크
    }

    public override void OnEpisodeBegin() // 새로운 에피소드는 항상 이렇게 시작해 (초기화) 
    {
        //rb.velocity = Vector3.zero;
        //this.transform.position = recall_position;
        //this.transform.rotation = recall_rotation;
    }

    public override void CollectObservations(VectorSensor sensor) // behavior가 'this'의 다음 행보를 판단할 때 필요한 값 (수집할 정보) 
    {
        //1. 좌우로 레이를 쏜다 
        int layermask = 1 << 6; // 나중에 다시 지정! 

        // 레이의 기준점 정해주기
        Vector3 posbottomcar = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z);

        // 우측 레이 쏘기
        Vector3 posright = new Vector3(posbottomcar.x - 1.0f, posbottomcar.y, posbottomcar.z);
        // 좌측 레이 쏘기
        Vector3 posleft = new Vector3(posbottomcar.x + 1.0f, posbottomcar.y, posbottomcar.z);

        RaycastHit[] hitWhite = Physics.SphereCastAll(posbottomcar, 0.2f, posright, 10f, layermask);
        RaycastHit[] hitYellow = Physics.SphereCastAll(posbottomcar, 0.2f, posleft, 10f, layermask);

        //2. 맞는 방향으로 가고 있는게 뭔지 정의 
        bool isRight = false;
        bool isLeft = false;
        bool isRightDirection;
        foreach (RaycastHit hit in hitWhite)
        {
            if (hit.collider.gameObject.CompareTag("BarrierRight") == true)
            {
                isRight = true;
                break;
            }
        }
        foreach (RaycastHit hit in hitYellow)
        {
            if (hit.collider.gameObject.CompareTag("BarrierLeft") == true)
            {
                isLeft = true;
                break;
            }
        }
        isRightDirection = isRight || isLeft;

        //3. 콜렉트
        sensor.AddObservation(isRightDirection);
        if (isRightDirection == true)
        {
            AddReward(rwd.rightdirection);
        }
        sensor.AddObservation(isRight);
        sensor.AddObservation(isLeft);
    }
    public float forceMultiplier = 10;
    public override void OnActionReceived(ActionBuffers actions)
    {
        //decisionrequestor component needed
        //  space type: discrete
        //      branches size: 2 move, turn
        //          branch 0 size: 3  fwd, nomove, back
        //          branch 1 size: 3  left, noturn, right
    
        float mag = Mathf.Abs(rb.velocity.sqrMagnitude);

        switch (actions.DiscreteActions.Array[0])   //move
        {
            case 0:
                AddReward(rwd.nomovement);
                break;
            case 1:
                rb.AddRelativeForce(Vector3.back * Movespeed * Time.deltaTime, ForceMode.VelocityChange); //back
                AddReward(mag * rwd.mult_backward);
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

        float distanceToTarget = Vector3.Distance(this.transform.localPosition, target.transform.localPosition);

        // Reached target
        if (distanceToTarget < 2f)
        {
            EndEpisode();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        float mag = collision.relativeVelocity.sqrMagnitude;

        // 오른쪽에 부딪혔을때 
        if (collision.gameObject.CompareTag("BarrierRight") == true)
        {
            AddReward(mag * rwd.mult_rightbarrier);  //punish
        }
        // 왼쪽에 부딪혔을때
        else if (collision.gameObject.CompareTag("BarrierLeft") == true)
        {
            AddReward(mag * rwd.mult_leftbarrier);  //punish
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 체크 포인트 부딪혔을때 보상
        if (other.gameObject.CompareTag("Checkpoint"))
        {
            AddReward(rwd.mult_checkpoint);
        }
        // 타겟(목적지)  부딪혔을때 보상
        else if (other.gameObject.CompareTag("Target"))
        {
            AddReward(rwd.mult_target);
        }
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
    private bool isWheelsDown()
    {
        //raycast down from car = ground should be closely there
        return Physics.Raycast(this.transform.position, -this.transform.up, bnd.size.y * 0.55f);
    }
}
