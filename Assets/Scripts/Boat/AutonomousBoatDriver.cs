using System;
using System.Linq;
using Core.SceneEntities;
using UnityEngine;
using Core.Utilities;
using WaterSystem;
using OscSimpl;
using UnityEngine.Serialization;


[RequireComponent(typeof(Boat))]
public class AutonomousBoatDriver : MonoBehaviour
{
    public enum AutonomusState
    {
        Idle,
        Driving,
        Waiting
    };

    /// <summary>
    /// WaitingForPlayer,WaitingForPlayerToLeave,WaitingForPlayerToEnter,WaitingForPlayerToLeaveAgain,WaitingForPlayerToEnterAgain,WaitingForPlayerToLeaveAgainAgain,WaitingForPlayerToEnterAgainAgain,WaitingForPlayerToLeaveAgainAgainAgain,WaitingForPlayerToEnterAgainAgainAgain,WaitingForPlayerToLeaveAgainAgainAgainAgain,WaitingForPlayerToEnterAgainAgainAgainAgain,WaitingForPlayerToLeaveAgainAgainAgainAgainAgain,WaitingForPlayerToEnterAgainAgainAgainAgainAgain,WaitingForPlayerToLeaveAgainAgainAgainAgainAgainAgain,WaitingForPlayerToEnterAgainAgainAgainAgainAgainAgain,WaitingForPlayerToLeaveAgainAgainAgainAgainAgainAgainAgain,WaitingForPlayerToEnterAgainAgainAgainAgainAgainAgainAgain,WaitingForPlayerToLeaveAgainAgainAgainAgainAgainAgainAgainAgain,WaitingForPlayerToEnterAgainAgainAgainAgainAgainAgainAgainAgain,WaitingForPlayerToLeaveAgainAgainAgainAgainAgainAgainAgainAgainAgain,Waiting
    /// </summary>
    public WayPoint StartingWaypoint;

    private WayPoint current = null;
    public WayPoint NextWaypoint => current;
    private Boat m_Boat;
    [FormerlySerializedAs("m_pid")] public Core.Utilities.Pid m_throttle_pid;
    public Pid m_steering_pid;
    private float currentTargetSpeed;

    private float _lastInterventionTime;
    private const float minholdTime = 1f;
    private const float maxholdTime = 5;
    private float _interventionSteering;

    public OscIn oscIn;
    public OscOut oscOut;
  
    private int lastsentValue = 0;
    public AnimationCurve steeringCurve;
    
    private void SetupOSC()
    {
        if (oscIn == null)
        {
            oscIn = gameObject.GetComponent<OscIn>();
            if (oscIn == null) oscIn = gameObject.AddComponent<OscIn>();
        }

        if (!oscIn.isOpen)
        {
            oscIn.Open(oscIn.port);
        }

        //oscIn.MapFloat("/PSI_LINEAR/throttle", OnReceiveThrottle);
        oscIn.MapInt("/PSI_LINEAR/out", PSI_Intervention);

        if (oscOut == null)
        {
            oscOut = gameObject.GetComponent<OscOut>();
            if (oscOut == null) oscOut = gameObject.AddComponent<OscOut>();
            
            oscOut.Open(oscOut.port,oscOut.remoteIpAddress);
            
        }
    }

    void Start()
    {
        SetupOSC();
        m_Boat = GetComponent<Boat>();
        if (StartingWaypoint == null)
        {
            float distance = Single.MaxValue;
            foreach (WayPoint waypoint in FindObjectsByType<WayPoint>(FindObjectsSortMode.None))
            {
                var dist = Vector3.Distance(transform.position, waypoint.transform.position);
                if (dist < distance)
                {
                    distance = dist;
                    StartingWaypoint = waypoint;
                }
            }
        }

        currentTargetSpeed = 5;
        GoToNextWaypoint();


        for (float i = 0; i < 1; i += 0.1f) Debug.Log(Utils.Map(i, 0.5f, 1, 1, 10, true));
    }

    public void PSI_Intervention(int input)
    {
        float tmp = Utils.Map(Mathf.Clamp(input, 0, 100), 0, 100, -1, 1, true);

        _interventionSteering = 1f - steeringCurve.Evaluate(1f - Mathf.Abs(tmp));
        _interventionSteering = Mathf.Sign(tmp) * _interventionSteering;
        _lastInterventionTime = Time.time;
      //  Debug.Log($"new Data:{input}");
    }

    private void GoToNextWaypoint()
    {
        if (current == null)
        {
            current = StartingWaypoint;
        }

        else
        {
            if (current.nextWayPoint != null)
            {
                current = current.nextWayPoint;
            }
            else
            {
                current = null;
                stopTheBoat();
            }
        }
    }

    private void stopTheBoat()
    {
        // m_Boat.Stop();
    }

    private float steering;
    // Update is called once per frame
    void Update()
    {
        if (current == null)
        {
            return;
        }

        float dist = Vector3.Distance(transform.position, current.transform.position);
        if (dist <= current.ArrivalRange)
        {
            ActivateWayPoint(current);
            GoToNextWaypoint();
        }


       // float angle = -Vector3.SignedAngle(transform.forward, (current.transform.position - transform.position).normalized,
      //      Vector3.up);

        const float maxAngle = 30;

        Vector3 forwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 toTargetXZ = current.transform.position - transform.position;
        toTargetXZ.y = 0f;
        toTargetXZ.Normalize();

        float angle = Vector3.Angle(forwardXZ, toTargetXZ);
        float sign = Mathf.Sign(Vector3.Dot(Vector3.up, Vector3.Cross(forwardXZ, toTargetXZ)));
        float signedAngle = angle * sign;
        
        
      //  float steering = Mathf.Clamp(angle, -maxAngle, maxAngle) / maxAngle;
      float normalizedAngle = Mathf.Clamp(-signedAngle / maxAngle, -1f, 1f);
      
      steering = steering*0.9f+0.1f*Mathf.Clamp(m_steering_pid.Update(0, normalizedAngle, Time.deltaTime),-1,1);
      //Debug.Log($"normal angle: {normalizedAngle},steering: {steering},angle: {signedAngle}");
        float throttle =
            m_throttle_pid.Update(currentTargetSpeed, m_Boat.getSpeed(),
                Time.deltaTime); // Mathf.Clamp(currentTargetSpeed - m_Boat.getSpeed(),-1,1);


        if (Time.time < _lastInterventionTime + minholdTime)
        {
            steering = _interventionSteering;
        }

        if (Time.time >= _lastInterventionTime + minholdTime && Time.time < _lastInterventionTime + maxholdTime)
        {
            var lerpValue = Utils.Map(Time.time,
                _lastInterventionTime + minholdTime,
                _lastInterventionTime + maxholdTime,
                0,
                1,
                true);
            steering = Mathf.Lerp(_interventionSteering, steering, lerpValue);
            SendSteering(steering);
        }
        else
        {
            SendSteering(steering);
        }


        throttle /= Utils.Map(Mathf.Abs(steering), 0.2f, 1, 1, 5, true);
        
        
        m_Boat.Move(throttle, steering);
    }

    private float lowPassFilter = 0;
    private void SendSteering(float steering)
    {

        lowPassFilter = steering;//lowPassFilter * 0.99f + steering * 0.01f;
        float amp =  Mathf.Sign(lowPassFilter) * steeringCurve.Evaluate(Mathf.Abs(lowPassFilter));

        var tmp = (int)Utils.Map(amp, -1, 1, 1, 99, true);
        if (lastsentValue != tmp)
        {
            lastsentValue = tmp;
            oscOut.Send("/PSI_LINEAR/in", tmp);
        }
    } 

    private void ActivateWayPoint(WayPoint wayPoint)
    {
        currentTargetSpeed = wayPoint.targetSpeed;
    }

    private void OnDisable()
    {
        if (oscIn != null && oscIn.isOpen)
        {
            oscIn.UnmapInt(PSI_Intervention);
            oscIn.Close();
        }

        if (oscOut != null && oscOut.isOpen)
        {
            oscOut.Close();
        }
    }
}