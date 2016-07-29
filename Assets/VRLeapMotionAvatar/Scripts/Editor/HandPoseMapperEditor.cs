using UnityEngine;
using UnityEditor;

namespace CpvrLab.VirtualTable {
    [CustomEditor(typeof(HandPoseMapper))]
    public class HandPoseMapperEditor : Editor {

        public HandPoseMapper script { get { return target as HandPoseMapper; } }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if(GUILayout.Button("Auto detect axes")) {
                script.CalculateAxes();
            }
        }
    }
}