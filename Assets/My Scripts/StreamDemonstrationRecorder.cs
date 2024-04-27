using System;
using System.IO;
using Unity.MLAgents.Demonstrations;
using UnityEngine;

namespace KartGame.Custom.Demo {
    public class StreamDemonstrationRecorder : MonoBehaviour
    {
        public bool substitutesFileWriter = true;

        private DemonstrationWriter m_DemoWriter;
        private bool isWriterAdded = false;
        private bool wantsToAddWriter = false;
        private DemonstrationRecorder m_DemoRecorder;
        private MemoryStream m_Stream;
        public event Action<byte[]> OnRecorderClosed;

        void Awake() {
            m_DemoRecorder = GetComponent<DemonstrationRecorder>();
            if (substitutesFileWriter) m_DemoRecorder.Close();
        }

        void LateUpdate() {
            if (!isWriterAdded && wantsToAddWriter) {
                m_DemoRecorder.AddDemonstrationWriterToAgent(m_DemoWriter);
                isWriterAdded = true;
                wantsToAddWriter = false;
            }
        }

        public void StartRecording() {
            m_Stream = new MemoryStream();
            m_DemoWriter = new DemonstrationWriter(m_Stream);
            wantsToAddWriter = true;
        }

        public void StopRecording() {
            m_DemoRecorder.RemoveDemonstrationWriterFromAgent(m_DemoWriter);
            m_DemoWriter.Close();
            OnRecorderClosed?.Invoke(m_Stream.ToArray());
            isWriterAdded = false;
            StartRecording();
        }

        public void SplitRecording() {
            StopRecording();
            StartRecording();
        }

        void OnDisable() {
            StopRecording();
        }
    }
}