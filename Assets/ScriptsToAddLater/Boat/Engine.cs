using UnityEngine;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace WaterSystem
{
    public class Engine : MonoBehaviour
    {

        public Rigidbody RB; // The rigid body attatched to the boat
       

        public AudioSource engineSound; // Engine sound clip
        public ParticleSystem enginBurst;
        
        private ParticleSystem.EmissionModule ps_em;
        private ParticleSystem.MainModule ps_mainModule;

        
        
        
       [Range(0.1f,0.9f)]
       public const float efficiency = 0.6f;
       
       [Tooltip("power in W")]
         public  const float EnginePower = 150000;
         private float _enginePower;
       
     
     
        private NativeArray<float3> _point; // engine submerged check
        private float3[] _heights = new float3[1]; // engine submerged check
        private float3[] _normals = new float3[1]; // engine submerged check
        private int _guid;
        private float _yHeight;


        public float lastThrust;
        public float lastThrottle;

        public const float _maxDeflection=25;
        public float _deflectionTarget;
        public float _isDeflection;
        private void Awake()
        {
            if (engineSound) //ToDo: this could be coded into a corutine that gets triggered from the boat and awaites other allocations.
            {
                engineSound.time =
                    UnityEngine.Random.Range(0f, engineSound.clip.length); // randomly start the engine sound
            }

        _guid = GetInstanceID(); // Get the engines GUID for the buoyancy system
            _point = new NativeArray<float3>(1, Allocator.Persistent);

            if (enginBurst)
            {
                ps_em = enginBurst.emission;
                ps_mainModule = enginBurst.main;
            }

            _isDeflection = 0;
        }

       
      
        
        private void OnDisable()
        {
            _point.Dispose();
        }


        public void setPower(float inpower)
        {
            _enginePower = Mathf.Clamp(inpower, -1f, 1f);
            lastThrottle = _enginePower;
        }
        
        public void setDeflection(float steering)
        {
            
            
            steering = Mathf.Clamp(steering, -1f, 1f);

            _deflectionTarget = -steering * _maxDeflection;
            
          //  Debug.Log($"is: {_isDeflection}, new: {steering}, target:{_deflectionTarget}");
        }


        private void Update()
        {

            updateEngineEffects(_enginePower);



            if (!Mathf.Approximately(_isDeflection, _deflectionTarget))
            {

                _isDeflection = Mathf.MoveTowards(_isDeflection, _deflectionTarget,10*Time.deltaTime);
               

              
                transform.localRotation = Quaternion.Euler(0, _isDeflection, 0);
                
                    
            }
        }
        
        private void FixedUpdate(){
            if (_yHeight > -0.1f) // if the engine is deeper than 0.1
            {
                 
                float speed = RB.linearVelocity.magnitude; 
                float vRef = Mathf.Max(speed, 0.1f);

                
                float eff      = efficiency;

                if (_enginePower < 0f) {
                    // limit max reverse to 50 %
                    _enginePower = Mathf.Max(_enginePower, -0.5f);
                    // reduce reverse efficiency further if desired
                    eff     *= 0.5f;
                    //—or equivalently: vRef *= 2f; to halve thrust at speed
                }
                
                
                
                float p_actual = EnginePower * eff * _enginePower;
                
                float thrustNewton = p_actual / vRef;

                lastThrust = thrustNewton;
                
                RB.AddForceAtPosition(thrustNewton * transform.forward,  transform.position,ForceMode.Force);
            }

           
        }

        private void updateEngineEffects(float pwr)
        {
            //audio//
            
            if (engineSound)
            {
                engineSound.volume = Mathf.Lerp(engineSound.volume, pwr, Time.deltaTime);
                engineSound.pitch = Mathf.Lerp(engineSound.pitch, (pwr + 1) / 2f, Time.deltaTime);
            }
            
            //particles//

           
            var tmp = ps_em.rateOverTime;
            tmp.constant = Mathf.Lerp(4, 75, pwr);
            ps_em.rateOverTime = tmp;
            
            
            tmp = ps_mainModule.startSpeed;
            tmp.constant = Mathf.Lerp(-1, -4, pwr);
            ps_mainModule.startSpeed = tmp;



        }
      
        
       /* public void Turn(float modifier)
        {
            if (_yHeight > -0.1f) // if the engine is deeper than 0.1
            {
                modifier = Mathf.Clamp(modifier, -1f, 1f); // clamp for reasonable values
                RB.AddRelativeTorque(new Vector3(0f, steeringTorque, -steeringTorque * 0.5f) * modifier, ForceMode.Acceleration); // add torque based on input and torque amount
            }

            _currentAngle = Mathf.SmoothDampAngle(_currentAngle, 
                60f * -modifier, 
                ref _turnVel, 
                0.5f, 
                10f,
                Time.fixedTime);
            transform.localEulerAngles = new Vector3(0f, _currentAngle, 0f);
        }
        */
        // Draw some helper gizmos
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
              }
        
       
    }
}