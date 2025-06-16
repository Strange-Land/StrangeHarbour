using System;
using UnityEngine;
using Core.Utilities;
using WaterSystem;



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
    private WayPoint current=null;
    private Boat m_Boat;
    public Core.Utilities.Pid m_pid;
    private float currentTargetSpeed;
    void Start()
    {
        m_Boat = GetComponent<Boat>();
        if (StartingWaypoint == null)
        {float distance=Single.MaxValue;
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

    private void GoToNextWaypoint()
    {
        if(current==null){current=StartingWaypoint;}

        else
        {
            if (current.nextWayPoint != null)
            {
                current=current.nextWayPoint;
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

    // Update is called once per frame
    void Update()
    {
        float dist = Vector3.Distance(transform.position, current.transform.position);
        if (dist <= current.ArrivalRange)
        {
            ActivateWayPoint(current);
            GoToNextWaypoint();
        }
        
        
        float angle = Vector3.SignedAngle(transform.forward, current.transform.position - transform.position,Vector3.up);

        const float maxAngle = 30;
        
        float steering  = Mathf.Clamp(angle, -maxAngle, maxAngle)/maxAngle;
        float throttle = m_pid.Update(currentTargetSpeed,m_Boat.getSpeed(),Time.deltaTime);// Mathf.Clamp(currentTargetSpeed - m_Boat.getSpeed(),-1,1);

        throttle/=Utils.Map(Mathf.Abs(steering), 0.2f, 1, 1, 5, true);
        
        m_Boat.Move(throttle,steering);

    }

    
    
    
    private void ActivateWayPoint(WayPoint wayPoint)
    {
        currentTargetSpeed = wayPoint.targetSpeed;
    }
}
