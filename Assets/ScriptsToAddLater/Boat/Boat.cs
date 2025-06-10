using System;
using System.Collections.Generic;
using Core.Networking;
using Core.SceneEntities;
using NUnit.Framework.Constraints;
using UnityEngine;


namespace WaterSystem
{
    public class Boat : InteractableObject
    {

        public bool isAutonomous = false;
        public  List<Engine> LeftEngines = new List<Engine>();
        public  List<Engine> RightEngines = new List<Engine>();

        
        private float throttle;
        private float steering;


        public float in_throttle;
        public float in_steering;
        public List<AudioSource> waterSound = new List<AudioSource>(); // Water sound clip

        Rigidbody m_rigidbody;
        //public float speed;   
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            foreach (var e in GetComponentsInChildren<Engine>())
            {
                e.RB = m_rigidbody;
                Vector3 pos =  transform.InverseTransformPoint(e.transform.position);
               Debug.DrawRay(transform.position,pos,Color.red,10);
                
                if (pos.x > 0)
                {
                    RightEngines.Add(e);
                }
                else
                {
                    LeftEngines.Add(e);
                }
            }

        }

        public override void SetStartingPose(Pose _pose)
        {
            throw new System.NotImplementedException();
        }

        public override void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_)
        {
           
        }

        public override Transform GetCameraPositionObject()
        {
           return transform.Find("CameraPosition");
        }

        public override void Stop_Action()
        {
            throw new System.NotImplementedException();
        }

        public override bool HasActionStopped()
        {
            throw new System.NotImplementedException();
        }

        void Start()
        {
            
            if (waterSound.Count>0)  {
                waterSound.ForEach(wS => wS.time = UnityEngine.Random.Range(0f, waterSound.Count));
  
            }

        }

        // Update is called once per frame
        void Update()
        {
            if (!isAutonomous)
            {
                float smlAmmount = 0.1f * Time.deltaTime;
                bool throttleInput = false;
                bool steerInput = false;
                if (Input.GetKey(KeyCode.W))
                {
                    in_throttle += smlAmmount;
                    throttleInput = true;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    in_throttle -= smlAmmount;
                    throttleInput = true;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    in_steering -= 0.3f * Time.deltaTime;
                    steerInput = true;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    in_steering += 0.3f * Time.deltaTime;
                    steerInput = true;
                }

                if (throttleInput == false)
                {

                    in_throttle = Mathf.MoveTowards(in_throttle, 0, smlAmmount);

                }

                if (steerInput == false)
                {
                    in_steering = Mathf.MoveTowards(in_steering, 0, smlAmmount);
                }
            }

            if (waterSound.Count > 0)
            {
                var volume = m_rigidbody.linearVelocity.sqrMagnitude * 0.0001f;

                volume = Mathf.Clamp(volume, 0.05f, 1);
                waterSound.ForEach(wS => wS.volume = volume);
            }
            
            //speed=m_rigidbody.linearVelocity.magnitude;

        }

        public void LateUpdate()
        {
            
            
            
            throttle = Mathf.Clamp(in_throttle,-1,1);
            steering = Mathf.Clamp(in_steering,-1,1);
            float absSteer = Mathf.Abs(steering);
            float leftPower, rightPower;

// decide which side is “inner” vs “outer”
            bool turningRight = steering > 0f;
            float outer = throttle;
            float inner;

// pick reduction factor based on throttle magnitude
            if (throttle >= 0.25f) {
                // max 50% reduction
                inner = throttle * (1f - 0.5f * absSteer);
            } else {
                // allow full swing into reverse
                inner = throttle * (1f - 2f * absSteer);
            }

// assign to each engine bank
            if (turningRight) {
                leftPower  = outer;
                rightPower = inner;
            } else {
                leftPower  = inner;
                rightPower = outer;
            }

            // Debug.Log($"inner{inner},\touter{outer}");
// clamp just in case
            leftPower  = Mathf.Clamp(leftPower,  -1f, 1f);
            rightPower = Mathf.Clamp(rightPower, -1f, 1f);
         
            
            foreach (var e in LeftEngines)
            {
                e.setPower(  leftPower);
                e.setDeflection(steering);
            }
            foreach (var e in RightEngines)
            {
                e.setPower (rightPower);
                e.setDeflection(steering);
            }
            
            
            
        }

        public float getSpeed()
        {
          return m_rigidbody.linearVelocity.magnitude;
        }

        public void Move(float _throttle, float _steering)
        {
            if (!isAutonomous) return;
           in_throttle=_throttle;
            in_steering=_steering;
        }
    }
}