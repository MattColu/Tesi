using UnityEngine;

namespace KartGame.Custom {
    public class MoveAndBounce : MonoBehaviour
    {
        public GameObject bounds;
        public bool rotate;
        private Collider coll;
        private Vector3 force;
        private Vector3 rotForce;

        void Awake() {
            force = Random.onUnitSphere;
            coll = bounds.GetComponent<Collider>();
            if (rotate) {                
                RotateOnAxis rotComponent = gameObject.AddComponent<RotateOnAxis>();
                rotComponent.rotationSpeed = Random.insideUnitSphere * 0.5f;
            }
        }

        void FixedUpdate() {
            transform.position += 1.5f * Time.fixedDeltaTime * force;
            if (transform.position.x >= coll.bounds.max.x || transform.position.x <= coll.bounds.min.x) {
                force.x = -force.x;
            }
            if (transform.position.y >= coll.bounds.max.y || transform.position.y <= coll.bounds.min.y) {
                force.y = -force.y;
            }
            if (transform.position.z >= coll.bounds.max.z || transform.position.z <= coll.bounds.min.z) {
                force.z = -force.z;
            }
        } 
    }
}