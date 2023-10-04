using MobX.Utilities;
using UnityEngine;

namespace MobX.Serialization.Mediator
{
    public class ClampedFloatSaveData : FloatSaveData
    {
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 1f;

        public override void SetValue(float value)
        {
            base.SetValue(value.Clamp(minValue, maxValue));
        }
    }
}