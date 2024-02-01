using System;
using UnityEngine;

namespace KartGame.KartSystems
{
    [Serializable]
    public struct InputData
    {
        public bool Accelerate;
        public bool Brake;
        public float TurnInput;
        
        //CUSTOM
        public static InputData operator +(InputData x, InputData y) {
            return new InputData 
            {
                Accelerate = x.Accelerate || y.Accelerate,
                Brake = x.Brake || y.Brake,
                TurnInput = Mathf.Clamp(x.TurnInput + y.TurnInput, -1f, 1f)
            };
        }
    }

    public interface IInput
    {
        InputData GenerateInput();
    }

    public abstract class BaseInput : MonoBehaviour, IInput
    {
        /// <summary>
        /// Override this function to generate an XY input that can be used to steer and control the car.
        /// </summary>
        public abstract InputData GenerateInput();
    }
}
