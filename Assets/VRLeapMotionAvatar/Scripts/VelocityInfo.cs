using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CpvrLab.VirtualTable {

    public class VelocityInfo : MonoBehaviour {

        public int sampleCount = 5;
        private float _sampleCountReciproc = 1.0f;

        public Vector3 avrgVelocity { get { return _avrgVelocity; } }
        public Vector3 avrgAngularVelocity { get { return _averageAngularVelocity; } }

        Vector3 _prevPosition;
        Quaternion _prevRotation;

        Vector3 _velocity;
        Vector3 _velocitySum;
        Vector3 _avrgVelocity;
                
        Vector3 _angularVelocity;
        Vector3 _angularVelocitySum;
        Vector3 _averageAngularVelocity;

        Queue<Vector3> _velocityCache = new Queue<Vector3>();
        Queue<Vector3> _angularVelocityCache = new Queue<Vector3>();

        void Awake()
        {
            UpdatePrevState();
            _sampleCountReciproc = 1.0f / (float)sampleCount;            
        }

        void LateUpdate()
        {
            _velocity = (transform.position - _prevPosition) / Time.deltaTime;

            _angularVelocity.x = CalcAngleDiff(transform.rotation.eulerAngles.x, _prevRotation.eulerAngles.x);
            _angularVelocity.y = CalcAngleDiff(transform.rotation.eulerAngles.y, _prevRotation.eulerAngles.y);
            _angularVelocity.z = CalcAngleDiff(transform.rotation.eulerAngles.z, _prevRotation.eulerAngles.z);
            _angularVelocity /= Time.deltaTime;
            
                        
            
            UpdatePrevState();
        }

        float CalcAngleDiff(float a, float b)
        {
            float result = b - a;
            while (result < -180) result += 360;
            while (result > 180) result -= 360;
            return result;
        }

        void UpdatePrevState()
        {
            // update previous state
            _prevPosition = transform.position;
            _prevRotation = transform.rotation;

            // update velocity rolling average
            _avrgVelocity = CalcRollingAvrgVec3(_velocity, ref _velocityCache, ref _velocitySum, sampleCount);
            
            // update angular velocity average
            _averageAngularVelocity = CalcRollingAvrgVec3(_angularVelocity, ref _angularVelocityCache, ref _angularVelocitySum, sampleCount);            
        }


        // calculate running average over a set of samples given the sum of that set
        float CalcRollingAvrgf(float newSample, ref Queue<float> samples, ref double sum, int maxSamples = 20)
        {
            // dequeue the oldest sample and remove it from the sum
            if(samples.Count >= maxSamples) {
                sum -= samples.Dequeue();
            }

            // add new sample
            samples.Enqueue(newSample);
            sum += newSample;

            // calculate average and mean
            return (float)(sum * (double)_sampleCountReciproc);
        }
        
        // calculate running average over a set of samples given the sum of that set
        Vector3 CalcRollingAvrgVec3(Vector3 newSample, ref Queue<Vector3> samples, ref Vector3 sum, int maxSamples = 20)
        {
            // dequeue the oldest sample and remove it from the sum
            if(samples.Count >= maxSamples) {
                sum -= samples.Dequeue();
            }

            // add new sample
            samples.Enqueue(newSample);
            sum += newSample;

            // calculate average and mean
            return sum * _sampleCountReciproc;
        }

            static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.color = new Color(0.0f, 0.0f, 1.0f, 0.2f);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, _velocity.normalized);
            Handles.ArrowCap(0, transform.position, rotation, _velocity.magnitude * 0.2f);

            Handles.color = new Color(0.0f, 0.6f, 1.0f);
            rotation = Quaternion.FromToRotation(Vector3.forward, _avrgVelocity.normalized);
            Handles.ArrowCap(0, transform.position, rotation, _avrgVelocity.magnitude * 0.2f);
        }
#endif
    }
}