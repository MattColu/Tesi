using System.Collections.Generic;
using System.IO;
using KartGame.Custom.Demo;
using UnityEngine;

namespace KartGame.Custom {
    /// <summary>
    /// Draws trajectories from (a series of) replay files.
    /// </summary>
    public class TrajectoryViewer : MonoBehaviour
    {
        public string demoFolder;
        public string[] demoFiles;
        public Trajectory[] trajectories;
        private Color[] palette;

        void Awake() {
            palette = new Color[] {Color.black, Color.blue, Color.cyan, Color.gray, Color.green, Color.magenta, Color.red, Color.white, Color.yellow};
            
            List<Trajectory> validDemos = new();

            IEnumerable<string> fileIterator; 
            if (demoFolder != "") {
                if (!StatePlayer.CheckValidPath(demoFolder)) {
                    enabled = false;
                    return;
                }
                fileIterator = Directory.EnumerateFiles(demoFolder);
            } else {
                if (demoFiles.Length == 0) {
                    enabled = false;
                    return;
                }
                fileIterator = demoFiles;
            }

            foreach (string f in fileIterator) {
                if (StatePlayer.CheckValidFile(f, "state")) validDemos.Add(new(StatePlayer.ReadFromFile(f)));
            }
            trajectories = validDemos.ToArray();
        }

        void OnDrawGizmos() {
            if (Application.isPlaying) {
                for (int i = 0; i < trajectories.Length; i++) {
                    trajectories[i].Draw(palette[i%palette.Length]);
                }
            }
        }
    }
}