using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CpvrLab.VirtualTable {
    public class FingerPoseMapper : MonoBehaviour {

        public Vector3 fingerForward = Vector3.forward;
        public Vector3 palmDirection = Vector3.down;

        public Transform[] bones = new Transform[4];

        public bool deformPosition = false;

        public Quaternion Reorientation()
        {
            return Quaternion.Inverse(Quaternion.LookRotation(fingerForward, -palmDirection));
        }

        public void CalculateAxes()
        {
            // use the first two bones in the bones array to calculate a directional vector
            Transform first = null;
            for(int i = 0; i < bones.Length; i++) {
                if(bones[i]) {
                    if(!first)
                        first = bones[i];
                    else {
                        fingerForward = bones[i].position - first.position;
                        fingerForward = Quaternion.Inverse(transform.rotation) * fingerForward;
                        break;
                    }
                }
            }

            fingerForward.Normalize();
        }

        public void UpdatePose(Transform[] otherBones)
        {
            for(int i = 0; i < bones.Length; ++i) {
                if(bones[i] != null) {
                    bones[i].rotation = otherBones[i].rotation * Reorientation();
                    if(deformPosition) {
                        bones[i].position = otherBones[i].position;
                    }
                }
            }
        }
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.color = Color.white;
            Handles.ArrowCap(0, transform.position, transform.rotation * Reorientation(), 0.05f);

            Quaternion fingerPointing = Quaternion.FromToRotation(Vector3.forward, fingerForward);
            Quaternion palmFacing = Quaternion.FromToRotation(Vector3.forward, palmDirection);

            Handles.color = new Color(1.0f, 0.4f, 0.0f);
            Handles.ArrowCap(0, transform.position, transform.rotation * fingerPointing, 0.05f);

            Handles.color = new Color(0.0f, 0.7f, 1.0f);
            Handles.ArrowCap(0, transform.position, transform.rotation * palmFacing, 0.05f);
        }
#endif
    }
}