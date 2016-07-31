using UnityEngine;
using System.Collections;

//#define FINALIK

#if FINALIK
using RootMotion.FinalIK;
#endif

namespace CpvrLab.AVRtar
{

    public class MecanimIK : MonoBehaviour
    {
        public Transform rightHandGoal;
        public Transform leftHandGoal;
        public bool useFinalIK = true;

        public Vector3 leftHandForward = Vector3.forward;
        public Vector3 leftHandUp = Vector3.up;
        public Vector3 rightHandForward = Vector3.forward;
        public Vector3 rightHandUp = Vector3.up;

        Animator _animator;
        Quaternion _rotationShiftL;
        Quaternion _rotationShiftR;

#if FINALIK
    FullBodyBipedIK ik;
#endif


        void Awake()
        {
            _animator = GetComponent<Animator>();
            _rotationShiftL = Quaternion.LookRotation(leftHandForward.normalized, leftHandUp.normalized);
            _rotationShiftR = Quaternion.LookRotation(rightHandForward.normalized, rightHandUp.normalized);
        }

        void SetLeftHandWeight(float weight)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);

            // todo add finalik
        }

        void SetRightHandWeight(float weight)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);

            // todo add finalik
        }

        void OnAnimatorIK()
        {
            if (useFinalIK)
                return;


            SetLeftHandWeight(1.0f);
            SetRightHandWeight(1.0f);
            _animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGoal.rotation * _rotationShiftL);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGoal.position);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandGoal.rotation * _rotationShiftR);
            _animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandGoal.position);
        }
    }

}