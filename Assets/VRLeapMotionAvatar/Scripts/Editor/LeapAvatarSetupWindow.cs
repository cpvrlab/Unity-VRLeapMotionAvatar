#define FINALIK

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace CpvrLab.AVRtar
{

    class LeapAvatarSetupWindow : EditorWindow
    {
        private Dictionary<HumanBodyBones, Transform> _fingerCopies = new Dictionary<HumanBodyBones, Transform>();
        private Animator _animator;
        private bool useFinalIK = false;

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

        private void OnObjectDropped(GameObject go)
        {
            _animator = null;
            _fingerCopies.Clear();

            GameObject modelInstance = Instantiate(go);
            _animator = modelInstance.GetComponent<Animator>();

            if (!_animator.isHuman)
            {
                Debug.LogError("AvatarSetup: Your model isn't a valid humanoid, please make sure to change your settings accordingly.");
                return;
            }


            // 1. create model specific hand object by copying the models hands
            Transform leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            var leftHandCopy = CopyTransformTree(leftHand, "modelHand_L", RememberFingerCopies);
            var rightHandCopy = CopyTransformTree(rightHand, "modelHand_R", RememberFingerCopies);

            // add the finalIK hand poser script to our model hands
            // todo: add finalik/mecanim support
            //var leftHandPoser = leftHand.gameObject.AddComponent<RootMotion.FinalIK.HandPoser>();
            //leftHandPoser.weight = 1.0f;
            //leftHandPoser.poseRoot = leftHandCopy;
            //var rightHandPoser = rightHand.gameObject.AddComponent<RootMotion.FinalIK.HandPoser>();
            //rightHandPoser.poseRoot = rightHandCopy;
            //rightHandPoser.weight = 1.0f;

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