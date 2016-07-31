using UnityEngine;
using System.Collections;

namespace CpvrLab.AVRtar
{

    /// <summary>
    /// Simple hand poser script inspired by the HandPoser in FinalIK
    /// </summary>
    public class HandPoser : MonoBehaviour
    {
        public float weight;
        public Transform poseRoot;
        
        private Transform[] _children;
        private Transform[] _poseChildren;

        protected void Start()
        {
            // Find the children
            _children = GetComponentsInChildren<Transform>();            
        }
        

        void LateUpdate()
        {
            if (weight <= 0f) return;

            // Get the children, if we don't have them already
            if (_poseChildren == null)
            {
                _poseChildren = poseRoot.GetComponentsInChildren<Transform>();
            }
            
            // Something went wrong
            if (_children.Length != _poseChildren.Length)
            {
                Debug.LogWarning("Number of children does not match with the pose");
                return;
            }
            
            // Lerping the localRotation and the localPosition
            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i] != transform)
                {
                    _children[i].localRotation = Quaternion.Lerp(_children[i].localRotation, _poseChildren[i].localRotation, weight);
                }
            }
        }
    }
}
