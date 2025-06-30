using System;
using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Core.SceneEntities;
using UnityEngine;
using Core.Utilities;
using UnityEngine.Serialization;
using WaterSystem;


    public class RemoteWorkerDisplay : ClientDisplay
    {
        private List<WayPoint> wps;
        private Boat m_boat;
        
        public override bool AssignFollowTransform(InteractableObject _interactableObject, ulong targetClient)
        {
            NetworkObject netobj = _interactableObject.NetworkObject;

            transform.position = _interactableObject.GetCameraPositionObject().position;
            transform.rotation = _interactableObject.GetCameraPositionObject().rotation;

            bool success = NetworkObject.TrySetParent(netobj, true);

           
             m_boat = _interactableObject.NetworkObject.transform.GetComponent<Boat>();
             Debug.Log(m_boat);
             if(m_boat!=null)
             {
                 AutonomousBoatDriver _AutonomousBoatDriver = m_boat.GetComponent<AutonomousBoatDriver>();
                 var current = _AutonomousBoatDriver.StartingWaypoint;
                 wps = new List<WayPoint>();

                 while (current != null && !wps.Contains(current))
                 {
                     wps.Add(current);
                     current = current.nextWayPoint;
                 }
                 Debug.Log($"We got some waypoints{wps.Count}");
                 GetComponent<Boat_Indicator>()?.Init(m_boat);

                 GetComponentInChildren<OrthoCameraStable>().GetWaypointInfo(wps,_AutonomousBoatDriver);

                
                
                

             }
            return success;
        }

       
        public override InteractableObject GetFollowTransform()
        {
            throw new NotImplementedException();
        }

        public override Transform GetMainCamera()
        {
            throw new NotImplementedException();
        }

        public override void CalibrateClient(Action<bool> calibrationFinishedCallback)
        {
            throw new NotImplementedException();
        }

        public override void GoForPostQuestion()
        {
            throw new NotImplementedException();
        }
    }

