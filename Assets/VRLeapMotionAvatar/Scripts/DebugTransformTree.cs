using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;


namespace CpvrLab.VirtualTable {
    public class DebugTransformTree : MonoBehaviour {

        Stack<Transform> stack = new Stack<Transform>();
        public Color color = Color.red;

#if UNITY_EDITOR
        
        void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, 0.01f);
            Handles.Label(transform.position, transform.gameObject.name);

            stack.Push(transform);
            while(stack.Count > 0) {
                Transform parent = stack.Pop();

                for(int i = 0; i < parent.childCount; i++) {
                    stack.Push(parent.GetChild(i));
                    Gizmos.DrawLine(parent.position, parent.GetChild(i).position);

                    Gizmos.color = color;
                    Gizmos.DrawSphere(parent.GetChild(i).position, 0.0025f);
                }
            }
        }
#endif
    }
}