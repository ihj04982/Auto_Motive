using UnityEngine;
using Unity.MLAgents;   //need Package Manager, MLAgents package
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(DecisionRequester))]
public class CarAgent : Agent
{
    [System.Serializable]
    public class RewardInfo
    {
        public float mult_forward = 0.001f;
        public float mult_barrier = -0.8f;
        public float mult_car = -0.5f;

        public float win = 1.0f;                // 5.0
        public float lose = -1.0f;              //-1.0
    }

    public float Movespeed = 30f;
    public float Turnspeed = 100f;
    public RewardInfo rwd = new RewardInfo();   //reward+ (punish-) values
    public bool doEpisodes = true;
    private Rigidbody rb = null;                //use physics to move car
    private Vector3 recall_position;            //to reset car each episode, training happens in episodes/steps
    private Quaternion recall_rotation;
    private Vector3 bnd;


    public override void Initialize()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.drag = 1;
        rb.angularDrag = 5;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;

        this.GetComponent<DecisionRequester>().DecisionPeriod = 1;

         bnd = this.GetComponent<BoxCollider>().size;

        recall_position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
        recall_rotation = new Quaternion(this.transform.rotation.x, this.transform.rotation.y, this.transform.rotation.z, this.transform.rotation.w);


    }
    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        this.transform.position = recall_position;
        this.transform.rotation = recall_rotation;
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        //Purpose: translate neural network output (actions) into the gameobject doing something

        //decisionrequestor component needed
        //  space type: discrete
        //      branches size: 2 move, turn
        //          branch 0 size: 3  fwd, nomove, back
        //          branch 1 size: 3  left, noturn, right

        if (isWheelsDown() == false)
            return;

        float mag = rb.velocity.sqrMagnitude;

        switch (actions.DiscreteActions.Array[0])   //move
        {
            case 0:
                break;
            case 1:
                rb.AddRelativeForce(Vector3.back * Movespeed * Time.deltaTime, ForceMode.VelocityChange); //back
                break;
            case 2:
                rb.AddRelativeForce(Vector3.forward * Movespeed * Time.deltaTime, ForceMode.VelocityChange); //forward
                AddReward(mag * rwd.mult_forward);  //-1..1
                break;
        }

        switch (actions.DiscreteActions.Array[1]) //turn
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
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //Purpose: to override mlagent brain, and control car myself by keyboard. Why? sanity check for OnActionReceived coded right
        actionsOut.DiscreteActions.Array[0] = 0;
        actionsOut.DiscreteActions.Array[1] = 0;

        float move = Input.GetAxis("Vertical");     //-1..0..1  WASD, arrowws
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
        if (collision.gameObject.CompareTag("BarrierWhite") == true
            || collision.gameObject.CompareTag("BarrierYellow") == true)
        {
            AddReward(mag * rwd.mult_barrier);

            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Car") == true)
        {

            AddReward(mag * rwd.mult_car);
            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Win") == true)
        {

            AddReward(rwd.win);
            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Lose") == true)
        {

            AddReward(rwd.lose);
            if (doEpisodes == true)
                EndEpisode();
        }
    }
    private bool isWheelsDown()
    {
        //raycast down from car = ground should be closely there
        return Physics.Raycast(this.transform.position, -this.transform.up, 1f);
    }
}
