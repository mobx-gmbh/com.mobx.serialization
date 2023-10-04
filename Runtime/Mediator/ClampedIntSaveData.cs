using MobX.Utilities;
using UnityEngine;

namespace MobX.Serialization.Mediator
{
    public class ClampedIntSaveData : IntSaveData
    {
        [SerializeField] private int minValue;
        [SerializeField] private int maxValue = 1;

        public override void SetValue(int value)
        {
            base.SetValue(value.Clamp(minValue, maxValue));
        }
    }
}