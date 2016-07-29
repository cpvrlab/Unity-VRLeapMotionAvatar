using UnityEngine;
using System.Collections;

namespace CpvrLab.VirtualTable {

    // lerps two transform trees together and copys the result on the current object
    public class HandPoseLerp : MonoBehaviour {

        public Transform poseRootA;
        public Transform poseRootB;
        private bool _initialized = false;
        private Transform[] _poseChildrenA;
        private Transform[] _poseChildrenB;
        private Transform[] _children;

        [Range(0f, 1f)]
        public float alpha = 0f;

        void Start()
        {
            // Find the children
            _children = (Transform[])GetComponentsInChildren<Transform>();
        }

        public void AutoMapping()
        {
            if(poseRootA == null) _poseChildrenA = new Transform[0];
            else _poseChildrenA = (Transform[])poseRootA.GetComponentsInChildren<Transform>(true);

            if(poseRootB == null) _poseChildrenB = new Transform[0];
            else _poseChildrenB = (Transform[])poseRootB.GetComponentsInChildren<Transform>(true);

            _initialized = true;
        }

        void LateUpdate()
        {
            if(!_initialized) AutoMapping();

            // Something went wrong
            if(_children.Length != _poseChildrenA.Length ||
                _children.Length != _poseChildrenB.Length) {
                Debug.LogWarning("Number of children does not match");
                return;
            }

            transform.rotation = Quaternion.Lerp(poseRootA.rotation, poseRootB.rotation, alpha);
            transform.position = Vector3.Lerp(poseRootA.position, poseRootB.position, alpha);

            // Lerping the localRotation and the localPosition
            for(int i = 0; i < _children.Length; i++) {
                if(_children[i] != transform) {
                    _children[i].localRotation = Quaternion.Lerp(_poseChildrenA[i].localRotation, _poseChildrenB[i].localRotation, alpha);
                    _children[i].localPosition = Vector3.Lerp(_poseChildrenA[i].localPosition, _poseChildrenB[i].localPosition, alpha);
                }
            }
        }
    }
}
