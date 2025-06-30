using System;
using System.Collections.Generic;
using Core.Networking;
using Core.SceneEntities;
using UnityEngine;
using OscSimpl;

namespace WaterSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class Boat : InteractableObject
    {
        public enum InputMode
        {
            Keyboard,
            OSC,
            Autonomous
        }

        [Header("Boat Configuration")]
        public InputMode controlMode = InputMode.Keyboard;
        public List<Engine> LeftEngines = new List<Engine>();
        public List<Engine> RightEngines = new List<Engine>();
        public List<AudioSource> waterSound = new List<AudioSource>();

        public OscIn oscIn;
       
        private float in_throttle;
        private float in_steering;
        private Rigidbody m_rigidbody;

        [SerializeField] private float rawInputThrottle;
        [SerializeField] private float rawInputSteering;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            foreach (var e in GetComponentsInChildren<Engine>())
            {
                e.RB = m_rigidbody;
                Vector3 pos = transform.InverseTransformPoint(e.transform.position);

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
            SetupOSC();
            if (waterSound.Count > 0)
            {
                waterSound.ForEach(wS => wS.time = UnityEngine.Random.Range(0f, waterSound.Count));
            }
        }

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

            oscIn.MapInt("/PSI_LINEAR/out", OnReceiveThrottle);
            oscIn.MapFloat("/boat/steering", OnReceiveSteering);
        }

        void Update()
        {
            if (controlMode == InputMode.Keyboard)
            {
                HandleKeyboardInput();
            }

            if (waterSound.Count > 0)
            {
                var volume = m_rigidbody.linearVelocity.sqrMagnitude * 0.0001f;
                volume = Mathf.Clamp(volume, 0.05f, 1);
                waterSound.ForEach(wS => wS.volume = volume);
            }
        }

        private void HandleKeyboardInput()
        {
            float smlAmmount = 0.1f * Time.deltaTime;
            bool throttleInput = false;
            bool steerInput = false;
            if (Input.GetKey(KeyCode.W))
            {
                in_throttle += smlAmmount;
                throttleInput = true;
                rawInputThrottle = 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                in_throttle -= smlAmmount;
                throttleInput = true;
                rawInputThrottle = -1f;
            }
            if (Input.GetKey(KeyCode.A))
            {
                in_steering -= 0.3f * Time.deltaTime;
                steerInput = true;
                rawInputSteering = -1f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                in_steering += 0.3f * Time.deltaTime;
                steerInput = true;
                rawInputSteering = 1f;
            }
            if (throttleInput == false)
            {
                in_throttle = Mathf.MoveTowards(in_throttle, 0, smlAmmount);
                rawInputThrottle = 0f;
            }
            if (steerInput == false)
            {
                in_steering = Mathf.MoveTowards(in_steering, 0, smlAmmount);
                rawInputSteering = 0f;
            }
        }

        public void LateUpdate()
        {
            float throttle = Mathf.Clamp(in_throttle, -1, 1);
            float steering = Mathf.Clamp(in_steering, -1, 1);
            float absSteer = Mathf.Abs(steering);
            float leftPower, rightPower;
            LastShouldSteering = steering;
            bool turningRight = steering > 0f;
            float outer = throttle;
            float inner;

            if (throttle >= 0.25f)
            {
                inner = throttle * (1f - 0.5f * absSteer);
            }
            else
            {
                inner = throttle * (1f - 2f * absSteer);
            }

            if (turningRight)
            {
                leftPower = outer;
                rightPower = inner;
            }
            else
            {
                leftPower = inner;
                rightPower = outer;
            }

            leftPower = Mathf.Clamp(leftPower, -1f, 1f);
            rightPower = Mathf.Clamp(rightPower, -1f, 1f);

            foreach (var e in LeftEngines)
            {
                e.setPower(leftPower);
                e.setDeflection(steering);
            }
            foreach (var e in RightEngines)
            {
                e.setPower(rightPower);
                e.setDeflection(steering);
            }
        }

        public float LastShouldSteering { get; private set; }

        public float getSpeed()
        {
            return m_rigidbody.linearVelocity.magnitude;
        }

        public void Move(float _throttle, float _steering)
        {
            if (controlMode != InputMode.Autonomous) return;
            in_throttle = _throttle;
            in_steering = _steering;
            rawInputThrottle = _throttle;
            rawInputSteering = _steering;
        }

        private void OnReceiveThrottle(int value)
        {
            if (controlMode != InputMode.OSC) return;
            in_throttle = Mathf.Clamp(((float)value/50f)-1, -1f, 1f);
            rawInputThrottle = value;
        }

        private void OnReceiveSteering(float value)
        {
            if (controlMode != InputMode.OSC) return;
            in_steering = Mathf.Clamp(value, -1f, 1f);
            rawInputSteering = value;
        }

        private void OnDisable()
        {
            if (oscIn != null && oscIn.isOpen)
            {
                oscIn.UnmapInt(OnReceiveThrottle);
                oscIn.UnmapFloat(OnReceiveSteering);
            }
        }

        public override void SetStartingPose(Pose _pose)
        {
            transform.SetPositionAndRotation(_pose.position, _pose.rotation);
            if (m_rigidbody)
            {
                m_rigidbody.linearVelocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public override void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_)
        {
        }

        public override Transform GetCameraPositionObject()
        {
            Transform camPos = transform.Find("CameraPosition");
            if (camPos == null)
            {
                GameObject go = new GameObject("CameraPosition");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0, 2, -5);
                return go.transform;
            }
            return camPos;
        }

        public override void Stop_Action()
        {
            in_throttle = 0;
            in_steering = 0;
            if (m_rigidbody)
            {
                m_rigidbody.linearVelocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public override bool HasActionStopped()
        {
            if (m_rigidbody == null) return true;
            return m_rigidbody.linearVelocity.sqrMagnitude < 0.01f && m_rigidbody.angularVelocity.sqrMagnitude < 0.01f;
        }
    }
}
