using System.Collections.Generic;
using UnityEngine;


namespace WaterSystem
{
    public class Boat : MonoBehaviour
    {
       
        public  List<Engine> LeftEngines = new List<Engine>();
        public  List<Engine> RightEngines = new List<Engine>();

        
        public float throttle;
        public float steering;
        public List<AudioSource> waterSound = new List<AudioSource>(); // Water sound clip

        Rigidbody m_rigidbody;
           
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            foreach (var e in GetComponentsInChildren<Engine>())
            {
                e.RB = m_rigidbody;
                Vector3 pos = transform.worldToLocalMatrix* e.transform.position;
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
        
        void Start()
        {
            
            if (waterSound.Count>0)  {
                waterSound.ForEach(wS => wS.time = UnityEngine.Random.Range(0f, waterSound.Count));
  
            }

        }

        // Update is called once per frame
        void Update()
        {

            float smlAmmount = 0.1f * Time.deltaTime;
            bool throttleInput = false;
            bool steerInput = false;
            if (Input.GetKey(KeyCode.W) ){ throttle += smlAmmount;
                throttleInput = true;
            }
            if (Input.GetKey(KeyCode.S)) {throttle -=  smlAmmount;throttleInput = true;}
            if (Input.GetKey(KeyCode.A)) {steering -= 0.3f*Time.deltaTime;steerInput = true;}
            
            if (Input.GetKey(KeyCode.D)) {steering += 0.3f*Time.deltaTime;steerInput = true;}

            if (throttleInput == false)
            {

                throttle = Mathf.MoveTowards(throttle, 0, smlAmmount);
                
            }

            if (steerInput==false)
            {
                  steering = Mathf.MoveTowards(steering, 0, smlAmmount);
            }

            
            throttle = Mathf.Clamp(throttle,-1,1);
            steering = Mathf.Clamp(steering,-1,1);
            float norm = (steering + 1) / 2;
            float t_leftPower = throttle - (steering>0? 0 :Mathf.Abs(steering));
            float t_rightPower = throttle - (steering<0? 0 :Mathf.Abs(steering));
           
          //  t_leftPower  = Mathf.Max(t_leftPower,  0f);
          //  t_rightPower = Mathf.Max(t_rightPower, 0f);
            
            foreach (var e in LeftEngines)
            {
                e.setPower(  t_leftPower);
                e.setDeflection(steering);
            }
            foreach (var e in RightEngines)
            {
                e.setPower (t_rightPower);
                e.setDeflection(steering);
            }


            if (waterSound.Count > 0)
            {
                var volume = m_rigidbody.linearVelocity.sqrMagnitude * 0.0001f;

                volume = Mathf.Clamp(volume, 0.05f, 1);
                waterSound.ForEach(wS => wS.volume = volume);
            }

        }
       
    }
}