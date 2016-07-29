using UnityEngine;
using System.Collections;

namespace CpvrLab.VirtualTable {

    /// <summary>
    /// This script has to be added to a biped character model to follow head rotation and movements
    /// by playing appropriate animations and updating body position.
    /// </summary>
    public class BodyFollow : MonoBehaviour {

        [Tooltip("Follow head position.")]
        public bool followPosition = true;

        [Tooltip("Follow head rotation.")]
        public bool followRotation = true;

        [Tooltip("Play an animation while rotating the body.")]
        public bool useRotationAnimation = true;

        [Tooltip("Play an animation while moving the body.")]
        public bool useLocomotionAnimation = true;

        [Tooltip("Deceleration when turning the body towards the head.")]
        public float turnDeceleration = 500.0f;

        [Tooltip("Acceleration when turning the body towards the head.")]
        public float turnAcceleration = 1000.0f;

        [Tooltip("Max speed when turning the body towards the head")]
        public float maxTurnSpeed = 200.0f;

        [Tooltip("Maximum angle the head can rotate relative to the body.")]
        [Range(0, 360)]
        public float headRotationLimit = 180.0f;
                
        [Tooltip("Controlls at which velocity we want to see the full locomotion animation.")]
        public float locomotionMaxFadeIn = 0.4f;

        // todo: same inspector as fbbik with foldout for 
        public GameObject headGoal;
        public Vector3 headForward = Vector3.forward;
        public Vector3 headUp = Vector3.up;        
        
        private Animator _animator;

        private float _turnVelocity;
        private int _turnDirection;

        [Space]
        [Header("Advanced")]
        public float animatorLocomotionSpeed = 1.5f;

        void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        void FixedUpdate()
        {
            UpdateBodyRotation();

            UpdateBodyPosition();

            UpdateAnimatorParameters();
        }



        private void UpdateBodyRotation()
        {
            float angle = GetRelativeHeadAngle();
            float angleAbs = Mathf.Abs(angle);
            _turnDirection = angle > 0.0f ? 1 : -1;

            if(angleAbs < 5.0f) {
                // early out if the delta angle is too low
                _turnVelocity = 0.0f;
                return;
            }

            // keep the angle inside the headRotationLimit
            if(angleAbs > headRotationLimit * 0.5f) {
                float angleDelta = angleAbs - headRotationLimit * 0.5f;
                angleDelta *= _turnDirection;
                transform.Rotate(transform.up, angleDelta);
            }

            // calculate a new turn speed based on our goal angle
            UpdateTurnSpeed(angleAbs);

            Quaternion goalRotation = transform.localRotation * Quaternion.AngleAxis(angle, Vector3.up);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, goalRotation, _turnVelocity * Time.deltaTime);
        }

        private void UpdateBodyPosition()
        {
            

            transform.position = new Vector3(headGoal.transform.position.x, transform.position.y, headGoal.transform.position.z);

        }

        private void UpdateAnimatorParameters()
        {
            VelocityInfo velocityInfo = headGoal.GetComponent<VelocityInfo>();

            // At the moment our animator has two states, one for turning and one for strafing
            // This might not be the ideal solution and a combination of a strafing locomotion
            // and turning locomotion animatorcontroller might look better. But for now we'll use this
            // That also means we need to switch between the turning and moving states and that's what we're doing here



            // get the velocity and angular velocity in local space of the body
            Vector3 localVelocity = Quaternion.Inverse(transform.rotation) * velocityInfo.avrgVelocity;  //velocityInfo.averageVelocity;
            Vector3 localAngularVelocity = Quaternion.Inverse(transform.rotation) * velocityInfo.avrgAngularVelocity;
            
            // remember the magnitude before normalizing the velocity
            float velocityMag = velocityInfo.avrgVelocity.magnitude;

            // now normalize our velocity
            // these values serve as the maximum possible values of strafe and forward in our animator
            localVelocity.Normalize();


            // scale the velocity magnitude using the locomotion speed parameter in case we want
            // our character to move slower or faster. This might come in handy if the character is 
            // experiencing moon walking symptoms            
            velocityMag /= animatorLocomotionSpeed;

            // finally we scale the velocity vector using our scaled magnitude
            // we don't want our character to immediatly have a strafe/forward value of 1
            // but rather fade it in based on current velocity
            // animatorLocomotionFadeThreshold serves as a parameter to controll at what velocity
            // we want to see values of 1
            localVelocity *= Mathf.Min(velocityMag / locomotionMaxFadeIn, 1.0f);


            float strafe = localVelocity.x;
            float forward = localVelocity.z;
            //var turn = ((localAngularVelocity.y > 0) ? -1 : 1) * velocityInfo.avrgAngularVelocity.magnitude * 0.01f;
            var turnAbs = Mathf.Min(Mathf.Abs(localAngularVelocity.y * 0.01f), 1.0f);
            var turn = ((localAngularVelocity.y > 0) ? -1 : 1) * turnAbs;
            
            // normal locomotion parameters
            _animator.SetFloat("Strafe", strafe, 0.1f, Time.deltaTime);
            _animator.SetFloat("Forward", forward, 0.1f, Time.deltaTime);
            
            _animator.SetFloat("Turn", turn, 0.1f, Time.deltaTime);

            // blend factor between the turn and locomotion blend trees
            _animator.SetFloat("LocomotionTurnBlend", turnAbs, 0.1f, Time.deltaTime);
            
            // dynamically change animation speed so that our feet don't drag on the floor but move relative to our speed
            // (currently only for locomotion, the turn animations just use a 1.0 speed. needs to be revisited in the future)
            _animator.speed = Mathf.Lerp(velocityMag, 1.0f, turnAbs);            
        }

        private void UpdateTurnSpeed(float distance)
        {
            // TODO:    It would be cool to have a continous curve for acceleration and
            //          deceleration up to max speed. We use a trapezoid for now. 
            //      |
            //    v |     /-------------\
            //      |    /               \
            //      ------------------------- t
            

            // 1. current speed required to reach our destination with a constant deceleration
            float decVel = Mathf.Sqrt(2.0f * turnDeceleration * distance);

            // 2. current speed + acceleration
            float currentSpeed = Time.fixedDeltaTime * turnAcceleration + _turnVelocity;

            // 3. clamp the velocity at max speed and mix the two acceleration parts together
            _turnVelocity = Mathf.Min(decVel, currentSpeed, maxTurnSpeed);
        }

        // calculates relative rotation between head and torso forward vectors
        // given a head direction vector in world space
        public float GetRelativeHeadAngle()
        {
            Transform head = headGoal.transform;
            Vector3 avatarForward = Vector3.forward;
            Vector3 avatarUp = Vector3.up;

            // transform world headDir into local space and project it onto the forward-right plane
            Vector3 localHeadDir = transform.worldToLocalMatrix * (head.rotation * headForward);
            localHeadDir = Vector3.ProjectOnPlane(localHeadDir, avatarUp);
            localHeadDir.Normalize();

            // Check the sign of the dot product between head and torso normals to see if our head is upside down
            // (upside down happens if we lift our chin so high that we are looking backwards)
            // flip the direction of the head direction if it's upside down
            if(Vector3.Dot(head.rotation * headUp, transform.rotation * avatarUp) < 0.0f)
                localHeadDir *= -1.0f;

            // calculate the angle between the projected head direction and the forward vector
            float angle = Mathf.Acos(Vector3.Dot(localHeadDir, avatarForward));

            // calculate the cross product in local space
            Vector3 cross = Vector3.Cross(avatarForward, localHeadDir);

            // determine the direction of the rotation using the dot product
            float dotFactor = Vector3.Dot(cross, avatarUp);
            angle *= (dotFactor > 0.0f) ? 1.0f : -1.0f;

            return angle * Mathf.Rad2Deg;
        }
    }

}