//#define FINALIK

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

#if FINALIK
using RootMotion;
using RootMotion.FinalIK;
#endif

namespace CpvrLab.AVRtar
{

    class LeapAvatarSetupWindow : EditorWindow
    {
        private Dictionary<HumanBodyBones, Transform> _fingerCopies = new Dictionary<HumanBodyBones, Transform>();
        private Animator _animator;
        private bool useFinalIK = false;
        private Transform _containerObject = null;

        [MenuItem("LeapMotion/Avatar Setup")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(LeapAvatarSetupWindow));
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.fixedHeight = 25.0f;
            
            EditorGUILayout.LabelField("Humanoid to AVRtar Setup", style, GUILayout.Height(style.fixedHeight));
            

#if FINALIK
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Use FinalIK", GUILayout.Width(80.0f));
            useFinalIK = EditorGUILayout.Toggle(useFinalIK, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
#endif

            DropAreaGUI();

        }

        // calculate plane normal given three world positions
        private Vector3 calculatePlaneNormal(Vector3 a, Vector3 b, Vector3 c, bool invert = false)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;

            Vector3 cross = Vector3.Cross(ab, ac);
            if (invert)
                cross *= -1;

            return cross.normalized;
        }

        // returns bend direction of a three transform chain
        Vector3 GetChainBendDirection(HumanBodyBones b0, HumanBodyBones b1, HumanBodyBones b2)
        {
            Transform t0 = _animator.GetBoneTransform(b0);
            Transform t1 = _animator.GetBoneTransform(b1);
            Transform t2 = _animator.GetBoneTransform(b2);

            Vector3 cross = Vector3.Cross((t1.transform.position - t0.transform.position).normalized, (t2.transform.position - t0.transform.position).normalized);
            return -Vector3.Cross(cross.normalized, (t2.transform.position - t0.transform.position).normalized);
        }

        void SetupIK(GameObject modelGO, GameObject leftHandGoal, GameObject rightHandGoal, HandPoser leftHandPoser, HandPoser rightHandPoser)
        {
            var ikController = modelGO.AddComponent<HandIKController>();
            ikController.useFinalIK = useFinalIK;
            ikController.leftHandGoal = leftHandGoal.transform;
            ikController.rightHandGoal = rightHandGoal.transform;
            ikController.leftHandPoser = leftHandPoser;
            ikController.rightHandPoser = rightHandPoser;

            if (!useFinalIK)
                return;


#if FINALIK

            var fbbik = modelGO.AddComponent<FullBodyBipedIK>();
            
            // force auto detect of fbbik
            if (fbbik.references.isEmpty)
            {
                BipedReferences.AutoDetectReferences(ref fbbik.references, fbbik.transform, new BipedReferences.AutoDetectParams(true, false));
                fbbik.solver.rootNode = IKSolverFullBodyBiped.DetectRootNodeBone(fbbik.references);
                fbbik.solver.SetToReferences(fbbik.references, fbbik.solver.rootNode);
            }


            Transform goalContainer = new GameObject("ik_goals").transform;
            goalContainer.parent = _containerObject;

            leftHandGoal.transform.parent = goalContainer;
            rightHandGoal.transform.parent = goalContainer;

            // Add pole targets for the knees
            Transform elbowGoalLeft = CopyTransform(_animator.GetBoneTransform(HumanBodyBones.LeftLowerArm), "elbow_L").transform;
            Transform elbowGoalRight = CopyTransform(_animator.GetBoneTransform(HumanBodyBones.RightLowerArm), "elbow_R").transform;

            elbowGoalLeft.SetParent(goalContainer);
            elbowGoalRight.SetParent(goalContainer);

            // offset the goals in the bend direction of the arm
            elbowGoalLeft.position = elbowGoalLeft.position + GetChainBendDirection(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
            elbowGoalRight.position = elbowGoalRight.position + GetChainBendDirection(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);



            fbbik.solver.leftArmChain.bendConstraint.bendGoal = elbowGoalLeft;
            fbbik.solver.leftArmChain.bendConstraint.weight = 0.5f;
            fbbik.solver.rightArmChain.bendConstraint.bendGoal = elbowGoalRight;
            fbbik.solver.rightArmChain.bendConstraint.weight = 0.5f;


            Transform kneeGoalLeft = CopyTransform(_animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg), "knee_L").transform;
            Transform kneeGoalRight = CopyTransform(_animator.GetBoneTransform(HumanBodyBones.RightLowerLeg), "knee_R").transform;

            kneeGoalLeft.SetParent(goalContainer);
            kneeGoalRight.SetParent(goalContainer);

            // offset the goals in the bend direction of the arm
            kneeGoalLeft.position = kneeGoalLeft.position + GetChainBendDirection(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            kneeGoalRight.position = kneeGoalRight.position + GetChainBendDirection(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

            fbbik.solver.leftLegChain.bendConstraint.bendGoal = kneeGoalLeft;
            fbbik.solver.leftLegChain.bendConstraint.weight = 0.5f;
            fbbik.solver.rightLegChain.bendConstraint.bendGoal = kneeGoalRight;
            fbbik.solver.rightLegChain.bendConstraint.weight = 0.5f;

            //
            Transform footGoalLeft = CopyTransform(_animator.GetBoneTransform(HumanBodyBones.LeftFoot), "foot_L");
            Transform footGoalRight = CopyTransform(_animator.GetBoneTransform(HumanBodyBones.RightFoot), "foot_R");

            footGoalLeft.SetParent(goalContainer);
            footGoalRight.SetParent(goalContainer);

            // We set the target in the FBBIK but we leave the weights at zero for the feet.
            fbbik.solver.SetEffectorWeights(FullBodyBipedEffector.LeftFoot, 0.0f, 0.0f);
            fbbik.solver.leftFootEffector.target = footGoalLeft;
            fbbik.solver.SetEffectorWeights(FullBodyBipedEffector.RightFoot, 0.0f, 0.0f);
            fbbik.solver.rightFootEffector.target = footGoalRight;

            // Setup head effector
            Transform headEffector = CopyTransform(_animator.GetBoneTransform(HumanBodyBones.Head).transform, "headEffector");
            headEffector.gameObject.AddComponent<FBBIKHeadEffector>().ik = fbbik;
                        
            headEffector.SetParent(goalContainer);

            // Setup hand goals
            fbbik.solver.leftHandEffector.target = leftHandGoal.transform;
            fbbik.solver.leftHandEffector.positionWeight = 1.0f;
            fbbik.solver.leftHandEffector.rotationWeight = 1.0f;

            fbbik.solver.rightHandEffector.target = rightHandGoal.transform;
            fbbik.solver.rightHandEffector.positionWeight = 1.0f;
            fbbik.solver.rightHandEffector.rotationWeight = 1.0f;
#endif
        }
        
        private void OnObjectDropped(GameObject go)
        {
            _animator = null;
            _fingerCopies.Clear();
            _containerObject = null;

            GameObject modelInstance = Instantiate(go);
            _animator = modelInstance.GetComponent<Animator>();

            if (!_animator.isHuman)
            {
                Debug.LogError("AvatarSetup: Your model isn't a valid humanoid, please make sure to change your settings accordingly.");
                return;
            }

            // 1. Create container object
            _containerObject = new GameObject(go.name + " Avatar").transform;
            modelInstance.name = "model";
            modelInstance.transform.parent = _containerObject;

            // 1. create model specific hand object by copying the models hands
            Transform leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            var leftHandCopy = CopyTransformTree(leftHand, "modelHand_L", RememberFingerCopies);
            var rightHandCopy = CopyTransformTree(rightHand, "modelHand_R", RememberFingerCopies);

            // add hand poser script
            var leftHandPoser = leftHand.gameObject.AddComponent<HandPoser>();
            leftHandPoser.weight = 1.0f;
            leftHandPoser.poseRoot = leftHandCopy;
            var rightHandPoser = rightHand.gameObject.AddComponent<HandPoser>();
            rightHandPoser.poseRoot = rightHandCopy;
            rightHandPoser.weight = 1.0f;

            // add in an extra transform (in case we need to move the model hand to better fit the leap hand)
            // for example, it can happen that the models hands are too high up relative to the actual leap hand position
            // meaning the palm of the model is where the fingers of the actual leap hand are.
            Transform leftPalm = CopyTransform(leftHandCopy, "modelPalm_L");
            leftHandCopy.SetParent(leftPalm);
            Transform rightPalm = CopyTransform(rightHandCopy, "modelPalm_R");
            rightHandCopy.SetParent(rightPalm);

            // set up the hand mapper script which is needed to drive this hand using our already set up leap hands
            SetupHandMapper(leftPalm.gameObject, true);
            SetupHandMapper(rightPalm.gameObject, false);

            leftPalm.gameObject.AddComponent<DebugTransformTree>();
            rightPalm.gameObject.AddComponent<DebugTransformTree>();

            leftPalm.parent = _containerObject;
            rightPalm.parent = _containerObject;

            // Setup IK
            SetupIK(modelInstance, leftPalm.gameObject, rightPalm.gameObject, leftHandPoser, rightHandPoser);
            
        }

        void SetupHandMapper(GameObject hand, bool isLeft)
        {
            var handPoseMapper = hand.AddComponent<HandPoseMapper>();
            handPoseMapper.palm = hand.transform;

            // now we need to iterate over each finger
            // luckily they are mapped nicely one after the other in unity's enum
            // todo:    future proof this by switching to our own enum in case
            //          unity should ever change this layout.
            int start = (int)(HumanBodyBones.LeftThumbProximal);
            int end = (int)(HumanBodyBones.LeftLittleProximal);
            if (!isLeft)
            {
                start = (int)(HumanBodyBones.RightThumbProximal);
                end = (int)(HumanBodyBones.RightLittleProximal);
            }

            int fingerIndex = 0;
            for (int i = start; i <= end; i += 3)
            {
                Transform proximal = null;
                Transform intermediate = null;
                Transform distal = null;

                if(!_fingerCopies.TryGetValue((HumanBodyBones)i, out proximal))
                {
                    Debug.LogWarning("Didn't find a reference for this finger's transform: " + ((HumanBodyBones)i).ToString());
                    continue;
                }
                _fingerCopies.TryGetValue((HumanBodyBones)i + 1, out intermediate);
                _fingerCopies.TryGetValue((HumanBodyBones)i + 2, out distal);

                var fpm = proximal.gameObject.AddComponent<FingerPoseMapper>();
                fpm.bones = new Transform[4] {
                    null,
                    proximal,
                    intermediate,
                    distal
                };

                handPoseMapper.fingers[fingerIndex] = fpm;
                fingerIndex++;
            }
            handPoseMapper.invertPalm = !isLeft;
            handPoseMapper.CalculateAxes();
        }

        void RememberFingerCopies(Transform original, Transform copy)
        {
            // See if 'original' is a valid finger bone and remember it for later use
            int start = (int)(HumanBodyBones.LeftThumbProximal);
            int end = (int)(HumanBodyBones.RightLittleDistal);
            for (int i = start; i <= end; i++)
            {
                HumanBodyBones key = (HumanBodyBones)i;
                if (_animator.GetBoneTransform(key) == original)
                    _fingerCopies.Add(key, copy);
            }
        }
        struct TransformPair
        {
            public TransformPair(Transform o, Transform c)
            {
                orig = o;
                copy = c;
            }

            public Transform orig;
            public Transform copy;
        }
        Transform CopyTransformTree(Transform otherRoot, string newName = null, Action<Transform, Transform> copyMade = null)
        {
            string name = (newName != null) ? newName : otherRoot.name;

            Stack<TransformPair> stack = new Stack<TransformPair>();

            Transform root = CopyTransform(otherRoot, name);
            stack.Push(new TransformPair(otherRoot, root));

            while (stack.Count > 0)
            {
                TransformPair parentPair = stack.Pop();

                for (int i = 0; i < parentPair.orig.childCount; i++)
                {
                    Transform child = parentPair.orig.GetChild(i);
                    Transform childCopy = CopyTransform(child);
                    childCopy.SetParent(parentPair.copy);

                    // notify callback about the copy
                    if (copyMade != null)
                        copyMade(child, childCopy);

                    stack.Push(new TransformPair(child, childCopy));
                }
            }

            return root;
        }
        Transform CopyTransform(Transform other, string newName = null)
        {
            string name = (newName != null) ? newName : other.name;
            GameObject go = new GameObject(name);
            go.transform.rotation = other.rotation;
            go.transform.position = other.position;
            go.transform.localScale = other.localScale;

            return go.transform;
        }

        private void DropAreaGUI()
        {
            var evt = Event.current;

            // todo: actually spend a bit of time and make the gui look like something (nice)
            Color fontColor = Color.black;
            if (EditorGUIUtility.isProSkin)
                fontColor = Color.grey;

            GUIStyle dropAreaStyle = new GUIStyle(GUI.skin.box);
            dropAreaStyle.normal.textColor = fontColor;
            dropAreaStyle.hover.textColor = Color.red;

            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(dropArea, "Drop your custom humanoid model here", dropAreaStyle);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        if (DragAndDrop.objectReferences.Length > 1)
                        {

                            Debug.LogError("Please drop only one object that is a valid humanoid model.");
                            return;
                        }

                        var go = DragAndDrop.objectReferences.GetValue(0) as GameObject;

                        if (!go || (go.GetComponent<Animator>() == null))
                        {
                            Debug.LogError("Please drop a valid humanoid model in the drop zone.");
                            return;
                        }

                        OnObjectDropped(go);

                    }
                    break;
            }
        }
    }

}

