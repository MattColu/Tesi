using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KartGame.Custom {
    /// <summary>
    /// Collects references to all child checkpoints and provides info when queried.
    /// </summary>
    public class Track : MonoBehaviour
    {
        [SerializeField] private Transform Spawnpoint;
        public Collider[] Checkpoints {get; private set;}

        private void Awake(){
            List<Collider> checkpoints = new();
            FindAndAddCheckpoints(transform, checkpoints);
            Checkpoints = checkpoints.ToArray();
            if (Spawnpoint == null) SetSpawnpoint();
        }

        private void FindAndAddCheckpoints(Transform obj, List<Collider> checkpoints) {
            for (int i = 0; i < obj.childCount; i++) {
                Transform child = obj.GetChild(i);
                if (child.CompareTag("Checkpoint")) {
                    checkpoints.Add(child.GetComponent<Collider>());
                } else {
                    FindAndAddCheckpoints(child, checkpoints);
                }
            }
        }

        public Transform GetSpawnpoint(int kartIndex) {
            return Spawnpoint.transform.Find($"StartingSpot ({kartIndex})")??Spawnpoint.transform;
        }

        public Transform GetSpawnpoint() {
            return Spawnpoint.transform;
        }

        public void SetSpawnpoint() {
            Spawnpoint = transform.Find("Spawnpoint");
        }

        /// <summary>
        /// If the two closest checkpoints are consecutive, returns the latter of the two.
        /// Otherwise, returns the closest checkpoint that's pointing in the same direction as the line pos-checkpoint. 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public int GetNextCheckpoint(Vector3 pos) {
            var checkpointsSortedByDistance = Checkpoints
            .Select(c => (c, Vector3.Distance(pos, c.ClosestPoint(pos))))   //Map to ((Collider, int), distance from pos)
            .OrderBy((o) => o.Item2);                                       //Order by distance

            int closest0 = Array.IndexOf(Checkpoints, checkpointsSortedByDistance.ElementAt(0).c);
            int closest1 = Array.IndexOf(Checkpoints, checkpointsSortedByDistance.ElementAt(1).c);
            if (CheckSubsequent(closest0, closest1)) {
                if (CheckNext(closest0, closest1)) {
                    return closest1;
                } else {
                    return closest0;
                }
            } else {
                return Array.IndexOf(Checkpoints, checkpointsSortedByDistance.First(d => Vector3.Dot(pos - d.c.ClosestPoint(pos), d.c.transform.forward)>0).c);
            }
        }

        public bool CheckSubsequent(int a, int b) {
            if (Mathf.Abs(a-b) == 1) return true;
            if (a*b == 0 && a+b == Checkpoints.Length - 1) return true;
            return false;
        }

        public bool CheckNext(int a, int b) {
            if (b-a == 1) return true;
            if (a == Checkpoints.Length - 1 && b == 0) return true;
            return false;
        }

        public Bounds GetBoundingBox() {
            Bounds bounds = new(transform.position, Vector3.one);
            MeshFilter[] childMeshes = GetComponentsInChildren<MeshFilter>();
            foreach(MeshFilter meshFilter in childMeshes){
                bounds.Encapsulate(meshFilter.transform.position + meshFilter.sharedMesh.bounds.max);
                bounds.Encapsulate(meshFilter.transform.position - meshFilter.sharedMesh.bounds.min);
            }
            return bounds;
        }
    }
}