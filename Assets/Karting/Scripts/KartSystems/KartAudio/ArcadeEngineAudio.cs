﻿using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// This class produces audio for various states of the vehicle's movement.
    /// </summary>
    public class ArcadeEngineAudio : MonoBehaviour
    {
        [Tooltip("What audio clip should play when the kart starts?")]
        public AudioSource StartSound;
        [Tooltip("What audio clip should play when the kart does nothing?")]
        public AudioSource IdleSound;
        [Tooltip("What audio clip should play when the kart moves around?")]
        public AudioSource RunningSound;
        [Tooltip("What audio clip should play when the kart is drifting")]
        public AudioSource Drift;
        [Tooltip("Maximum Volume the running sound will be at full speed")]
        [Range(0.1f, 1.0f)]public float RunningSoundMaxVolume = 1.0f;
        [Tooltip("Maximum Pitch the running sound will be at full speed")]
        [Range(0.1f, 2.0f)] public float RunningSoundMaxPitch = 1.0f;
        [Tooltip("What audio clip should play when the kart moves in Reverse?")]
        public AudioSource ReverseSound;
        [Tooltip("Maximum Volume the Reverse sound will be at full Reverse speed")]
        [Range(0.1f, 1.0f)] public float ReverseSoundMaxVolume = 0.5f;
        [Tooltip("Maximum Pitch the Reverse sound will be at full Reverse speed")]
        [Range(0.1f, 2.0f)] public float ReverseSoundMaxPitch = 0.6f;

        [Range(0.1f, 1.0f)] public float DriftSoundMaxVolume = 1.0f;
        [Tooltip("Maximum Pitch the Drift sound will be when drifting")]

        ArcadeKart arcadeKart;

        void Awake()
        {
            arcadeKart = GetComponentInParent<ArcadeKart>();
            StartSound.volume = 0.0f;
            IdleSound.volume = 0.0f;
            RunningSound.volume = 0.0f;
            Drift.volume = 0.0f;
            ReverseSound.volume = 0.0f;
        }

        void Update()
        {

            float kartSpeed = 0.0f;
            if (arcadeKart != null)
            {
                kartSpeed = arcadeKart.LocalSpeed();
                if (Time.deltaTime > 0.3f || !arcadeKart.GetCanMove()) return; //Avoid sounds while loading and countdown
                Drift.volume = arcadeKart.IsDrifting && arcadeKart.GroundPercent > 0.0f ? arcadeKart.Rigidbody.velocity.magnitude / arcadeKart.GetMaxSpeed() * DriftSoundMaxVolume : 0.0f;
            }

            IdleSound.volume = Mathf.Lerp(0.6f, 0.0f, kartSpeed * 4);

            RunningSound.volume = Mathf.Lerp(0.1f, RunningSoundMaxVolume, Mathf.Abs(kartSpeed) * 1.2f);
            RunningSound.pitch = Mathf.Lerp(0.3f, RunningSoundMaxPitch, Mathf.Abs(kartSpeed) + (Mathf.Sin(Time.time) * .1f));
        }
    }
}
