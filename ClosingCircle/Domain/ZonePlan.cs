using System.Collections.Generic;
using UnityEngine;

// The whole schedule: where the zone starts, and every step it takes. Stages are held in descending FromTime
// order because the round clock counts down.

namespace ClosingCircle.Domain
{
    public class ZonePlan
    {
        private readonly List<Stage> _stages = new List<Stage>();

        // 0,0 is the middle of every stock Holdfast map, so it is the only default that is right by
        // accident rather than wrong by inheritance.
        public float StartRadius = 200f;
        public Vector2 StartCentre = Vector2.zero;

        public IReadOnlyList<Stage> Stages => _stages;
        public int Count => _stages.Count;

        public void Clear() => _stages.Clear();

        public void Add(Stage stage)
        {
            // Captured here rather than at every call site, so config, rc and a pushed plan all get it right.
            stage.ConfiguredCentre = stage.Centre;

            _stages.Add(stage);
            _stages.Sort((a, b) => b.FromTime.CompareTo(a.FromTime));
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _stages.Count) return false;

            _stages.RemoveAt(index);
            return true;
        }

        // Stage is a struct behind a read-only list, so the resolver needs a way in.
        public void SetStageCentre(int index, Vector2 centre)
        {
            if (index < 0 || index >= _stages.Count) return;

            Stage stage = _stages[index];
            stage.Centre = centre;
            _stages[index] = stage;
        }

        public void ResolveStage(int index, Vector2 centre)
        {
            if (index < 0 || index >= _stages.Count) return;

            SetStageCentre(index, centre);

            Stage stage = _stages[index];
            stage.Resolved = true;
            _stages[index] = stage;
        }

        public void UnresolveStage(int index)
        {
            if (index < 0 || index >= _stages.Count) return;

            Stage stage = _stages[index];
            stage.Centre = stage.ConfiguredCentre;
            stage.Resolved = false;
            _stages[index] = stage;
        }

        // The radius the zone settles at once every stage has run, which is what the last stage leaves behind.
        public float FinalRadius => _stages.Count == 0 ? StartRadius : _stages[_stages.Count - 1].Radius;
    }
}
