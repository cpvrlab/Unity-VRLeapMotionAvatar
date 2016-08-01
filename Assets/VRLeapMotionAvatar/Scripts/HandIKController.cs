//#define FINALIK

using UnityEngine;
using System.Collections;


#if FINALIK
using RootMotion.FinalIK;
#endif

namespace CpvrLab.AVRtar
{

    public class HandIKController : MonoBehaviour
    {
        public Transform leftHandGoal;
        public Transform rightHandGoal;
        public HandPoser leftHandPoser;
        public HandPoser rightHandPoser;

        public bool useFinalIK = true;

        public Vector3 leftHandForward = Vector3.forward;
        public Vector3 leftHandUp = Vector3.up;
        public Vector3 rightHandForward = Vector3.forward;
        public Vector3 rightHandUp = Vector3.up;

        Animator _animator;
        Quaternion _rotationShiftL;
        Quaternion _rotationShiftR;

        float _leftWeight;
        float _rightWeight;

#if FINALIK
        FullBodyBipedIK _ik;
#endif


        void Awake()
        {
            _animator = GetComponent<Animator>();
            _rotationShiftL = Quaternion.LookRotation(leftHandForward.normalized, leftHandUp.normalized);
            _rotationShiftR = Quaternion.LookRotation(rightHandForward.normalized, rightHandUp.normalized);

#if FINALIK
            _ik = GetComponent<FullBodyBipedIK>();
#endif
        }

        public void SetHandWeight(bool left, float weight)
        {
            if(left)            
                leftHandPoser.weight = weight;
            else
                rightHandPoser.weight = weight;


            if (!useFinalIK)
            {
                if (left)
                    _leftWeight = weight;
                else
                    _rightWeight = weight;
            }

#if FINALIK
            else {

                if (left)
                {
                    _ik.solver.leftHandEffector.positionWeight = weight;
                    _ik.solver.leftHandEffector.rotationWeight = weight;
                } else
                {
                    _ik.solver.rightHandEffector.positionWeight = weight;
                    _ik.solver.rightHandEffector.rotationWeight = weight;
                }
            }
#endif
        }
        

        void OnAnimatorIK()
        {
            if (useFinalIK)
                return;
                        
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _leftWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _leftWeight);

            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _rightWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, _rightWeight);


            _animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGoal.rotation * _rotationShiftL);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGoal.position);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandGoal.rotation * _rotationShiftR);
            _animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandGoal.position);
        }
    }

}