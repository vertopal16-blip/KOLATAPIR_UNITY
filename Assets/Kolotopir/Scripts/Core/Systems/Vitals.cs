using System;

namespace Kolotopir.Core
{
    /// <summary>
    /// Valor vital (salud o aguante) almacenado como float (doc: "salud ... variable numérica de tipo float";
    /// "100 puntos de salud máxima ... el daño y la curación pueden usar valores decimales").
    /// </summary>
    public sealed class Vital
    {
        public float Current { get; private set; }
        public float Max { get; private set; }

        public event Action<Vital> Changed;

        public Vital(float max)
        {
            Max = Math.Max(1f, max);
            Current = Max;
        }

        public bool IsEmpty => Current <= 0f;
        public bool IsFull => Current >= Max - 0.0001f;
        public float Normalized => Max <= 0f ? 0f : Current / Max;

        /// <summary>Resta y devuelve la cantidad realmente aplicada. Ignora valores negativos o NaN.</summary>
        public float Reduce(float amount)
        {
            if (float.IsNaN(amount) || amount <= 0f || Current <= 0f) return 0f;
            float applied = Math.Min(Current, amount);
            Current -= applied;
            if (Current < 0.0001f) Current = 0f;
            Changed?.Invoke(this);
            return applied;
        }

        /// <summary>Suma y devuelve la cantidad realmente aplicada. No supera el máximo.</summary>
        public float Restore(float amount)
        {
            if (float.IsNaN(amount) || amount <= 0f) return 0f;
            float applied = Math.Min(Max - Current, amount);
            if (applied <= 0f) return 0f;
            Current += applied;
            Changed?.Invoke(this);
            return applied;
        }

        public void Fill()
        {
            Current = Max;
            Changed?.Invoke(this);
        }

        public void SetCurrent(float value)
        {
            if (float.IsNaN(value)) return;
            Current = OromMath.Clamp(value, 0f, Max);
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Cambia el máximo. Si aumenta, la diferencia se suma al valor actual (subir de nivel no deja
        /// a Nilo "herido"); si disminuye, el valor actual se recorta.
        /// </summary>
        public void SetMax(float newMax, bool keepDelta = true)
        {
            newMax = Math.Max(1f, newMax);
            if (Math.Abs(newMax - Max) < 0.0001f) return;
            float delta = newMax - Max;
            Max = newMax;
            if (keepDelta && delta > 0f && Current > 0f) Current += delta;
            Current = OromMath.Clamp(Current, 0f, Max);
            Changed?.Invoke(this);
        }
    }

    /// <summary>Modelo de daño: ataque, defensa y resistencias (doc: "el daño tendrá en cuenta el ataque, la defensa y las resistencias").</summary>
    public static class DamageModel
    {
        public const float MinimumDamage = 0.5f;

        public static float Mitigation(float defense, DamageType type)
        {
            float k = type == DamageType.Fisico ? 3f : 1.5f;
            return 100f / (100f + Math.Max(0f, defense) * k);
        }

        public static float Compute(float attack, DamageType type, float defense, float resistPhysical, float resistResonance, float multiplier = 1f)
        {
            if (attack <= 0f || multiplier <= 0f) return 0f;
            float raw = attack * multiplier * Mitigation(defense, type);
            float res = type == DamageType.Fisico ? resistPhysical : resistResonance;
            raw *= 1f - OromMath.Clamp(res, 0f, 0.85f);
            return Math.Max(MinimumDamage, raw);
        }

        /// <summary>Daño bloqueado con la espada: el bloqueo perfecto anula el golpe.</summary>
        public static float Blocked(float damage, bool perfect, float blockReduction = 0.7f)
        {
            if (perfect) return 0f;
            return damage * (1f - OromMath.Clamp01(blockReduction));
        }
    }
}
