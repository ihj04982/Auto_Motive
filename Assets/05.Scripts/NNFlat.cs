using Unity.MLAgents;           //for Agent class
using Unity.MLAgents.Actuators; //for OnActionReceived(ActionBuffers...
using Unity.MLAgents.Sensors;   //for CollectObservations(VectorSensor...
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(DecisionRequester))]
public class NNFlat : Agent
{
    public static NNFlat instance;

    private void Awake()
    {
        instance = this;
    }
    [System.Serializable]
    public class RewardInfo
    {
        public float nomovement = -0.001f;
        public float mult_forward = 0.001f;
        public float mult_backward = -0.001f;
        public float mult_barrier = -0.1f;
        public float win = 1.0f;
        public float lose = -1.0f;
        public float target = -1.0f;
        public float mult_car = -0.5f;
        public float rightdirection = 0.0001f;
        public float distance_2m = -0.001f;
    }
    public RewardInfo rwd = new RewardInfo();

    public float Movespeed = 35f;
    float Turnspeed = 90;
    float targetSpeed;
    private Transform target;
    public Rigidbody rb = null;
    private Vector3 recall_position;
    private Quaternion recall_rotation;
    private Bounds bnd;
    public ParticleSystem vfx;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

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


    public override void OnActionReceived(ActionBuffers actions)
    {
        //decisionrequestor component needed
        //  space type: discrete
        //      branches size: 2 move, turn
        //          branch 0 size: 3  fwd, nomove, back
        //          branch 1 size: 3  left, noturn, right

        if (isWheelsDown() == false)
        {
            return;
        }

        print(rb.velocity.sqrMagnitude);

        int layermask = 1 << 6; //6 is carperception 
        Vector3 posbottomcar = new Vector3(this.transform.position.x, this.transform.position.y + bnd.size.y + 0.5f, this.transform.position.z);
        Vector3 postargetcar = new Vector3(posbottomcar.x - 1.0f, posbottomcar.y, posbottomcar.z);
        RaycastHit[] hitTargetCar = Physics.SphereCastAll(posbottomcar, 10f, postargetcar, 20f, layermask);

        float defaultSpeed = Movespeed;
        foreach (RaycastHit hit in hitTargetCar)
        {
            if (hit.collider.gameObject.CompareTag("Target") == true)
            {
                
                print("반경 20m 안");
                target = hit.collider.gameObject.transform;
                targetSpeed = target.GetComponent<NavMeshAgent>().speed;
                var distance = Vector3.Distance(this.transform.position, target.position);
                UIManager.instance.SafeDistance0();
                if (distance >= 10)
                {
                    print("반경 안에 없음 Speed : " + Movespeed);
                    Movespeed = Mathf.Lerp(Movespeed, defaultSpeed, Time.deltaTime);
                    UIManager.instance.SafeDistance4();
                }
                else if (rb.velocity.sqrMagnitude > 20f)
                {
                    rb.velocity *= 0.8f;
                    vfx.Play();
                    UIManager.instance.SafeDistance4();
                    if (distance >= 5 && distance < 10)  
                    {
                        print("반경 10m안에 들어옴 Speed : " + Movespeed);
                        Movespeed = targetSpeed;
                        UIManager.instance.SafeDistance2();
                    }
                    if (distance >= 2 && distance < 5) 
                    {
                        Movespeed = targetSpeed;
                        print("반경 5m안에 들어옴 Speed : " + Movespeed);
                        UIManager.instance.SafeDistance0();
                    }
                    if (distance < 2) 
                    {
                        print("2m 거리유지못함 Speed : " + Movespeed);
                        UIManager.instance.SafeDistance0();
                        AddReward(rwd.distance_2m);
                    }
                }
            }
        }
        Movespeed = defaultSpeed;
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
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        this.transform.position = recall_position;
        this.transform.rotation = recall_rotation;
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

    public override void CollectObservations(VectorSensor sensor)
    {
        //Note: BehaviourParameters component - set VectorObservation size to 3, because here
        //      we are adding 3 observations manually

        //1. cast rays to both sides of car
        int layermask = 1 << 6; //6 is carperception 
        Vector3 posbottomcar = new Vector3(this.transform.position.x, this.transform.position.y + bnd.size.y + 0.5f, this.transform.position.z);
        Vector3 poswhite = new Vector3(posbottomcar.x + 1.0f, posbottomcar.y, posbottomcar.z); ;
        Vector3 posyellow = new Vector3(posbottomcar.x - 1.0f, posbottomcar.y, posbottomcar.z);
        RaycastHit[] hitWhite = Physics.SphereCastAll(posbottomcar, 0.2f, poswhite, 20f, layermask);
        RaycastHit[] hitYellow = Physics.SphereCastAll(posbottomcar, 0.2f, posyellow, 20f, layermask);

        //2. did white hit BarrierWhite, and yellow hit BarrierYellow?
        bool isWhite = false;
        bool isYellow = false;
        bool isRightDirection;
        foreach (RaycastHit hit in hitWhite)
        {
            if (hit.collider.gameObject.CompareTag("BarrierWhite") == true)
            {
                isWhite = true;
                break;
            }
        }
        foreach (RaycastHit hit in hitYellow)
        {
            if (hit.collider.gameObject.CompareTag("BarrierYellow") == true)
            {
                isYellow = true;
                break;
            }
        }
        isRightDirection = isWhite || isYellow;

        //3. manually send 3 observations to the neural network
        sensor.AddObservation(isRightDirection);
        if (isRightDirection == true)
        {
            AddReward(rwd.rightdirection);
        }
        sensor.AddObservation(isWhite);
        sensor.AddObservation(isYellow);
    }

    private void OnCollisionEnter(Collision collision)
    {
        float mag = collision.relativeVelocity.sqrMagnitude;

        if (collision.collider.CompareTag("Car") == true)
        {
            vfx.Play();
            AddReward(rwd.mult_car);
            EndEpisode();
        }
        else if (collision.collider.CompareTag("Win") == true)
        {
            vfx.Play();
            AddReward(rwd.win);
            EndEpisode();
        }
        else if (collision.collider.CompareTag("Lose") == true)
        {
            vfx.Play();
            AddReward(rwd.lose);
            EndEpisode();
        }
        if (collision.collider.CompareTag("Target") == true)
        {
            vfx.Play();
            AddReward(rwd.target);               //punish + end
            EndEpisode();
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        float mag = collision.relativeVelocity.sqrMagnitude;

        if (collision.gameObject.CompareTag("BarrierWhite") == true
            || collision.gameObject.CompareTag("BarrierYellow") == true)
        {
            AddReward(rwd.mult_barrier);  //punish
        }
    }
    private bool isWheelsDown()
    {
        //raycast down from car = ground should be closely there
        return Physics.Raycast(this.transform.position, -this.transform.up, bnd.size.y * 0.55f);
    }
}