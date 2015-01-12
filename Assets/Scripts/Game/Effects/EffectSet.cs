/*
 * A bundle of different effects,
 * makes managing effects much more convenient.
 */

using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[System.Serializable]
public class EffectSet : IEffect {
    public UnlockSet unlocks                = new UnlockSet();

    public List<IEffect> effects = new List<IEffect>();
    public override void Apply(Company company) {
        foreach (IEffect e in effects) {
            company.activeEffects.Add(e);
            e.Apply(company);
        }
    }
    public override void Remove(Company company) {
        foreach (IEffect e in effects) {
            company.activeEffects.Remove(e);
            e.Remove(company);
        }
    }

    // Convenience methods for accessing the underlying effects list.
    public void Add(IEffect e) {
        effects.Add(e);
    }
    public void Remove(IEffect e) {
        effects.Remove(e);
    }
    public IEffect this[int index] {
        get { return effects[index]; }
    }

    public List<T> ofType<T>() {
        return effects.Where(e => e.GetType() == typeof(T)).Cast<T>().ToList();
    }
}
