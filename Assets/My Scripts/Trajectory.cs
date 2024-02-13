using System.Collections.Generic;
using System.Linq;
using KartGame.Custom.Demo;
using UnityEngine;

namespace KartGame.Custom {
    public class Trajectory
    {
        public Trajectory(StateData[] queue) {
            points = (StateData[])queue.Clone();
        }
        public Trajectory(Queue<StateData> queue) {
            points = queue.ToArray();
        }
        public StateData[] points;
        
        public static float Evaluate(Trajectory ta, Trajectory tb) {
            StateData[] t1 = ta.points;
            StateData[] t2 = tb.points;

            int count = Mathf.Min(t1.Length, t2.Length);
            float total = 0f;
            float length = 0f;

            for (int i = 0; i < count; i++) {
                total += (t1[i].position - t2[i].position).sqrMagnitude;
            }
            // Accumulator is (total squared distance, last position)
            length = t1.Aggregate((0f, t1[0].position), (acc, x) => (acc.Item1 + (x.position - acc.position).sqrMagnitude, x.position)).Item1;

            return Mathf.Sqrt(total)/ Mathf.Sqrt(length);
        }
        public float Evaluate(Trajectory t) {
            return Evaluate(this, t);
        }

        public static void Draw(StateData[] points, Color color) {
            foreach (var p in points) {
                DrawTimestep(color, p);
            }
        }
        public void Draw() {
            Trajectory.Draw(points, Color.black);
        }

        public void Draw(Color color) {
            Trajectory.Draw(points, color);
        }

        public static void DrawTimestep(Color color, StateData point) {
            Gizmos.color = new(color.r, color.g, color.b, color.a);
            Gizmos.DrawIcon(point.position, "sphere.png", true, color);
        }
    }
}