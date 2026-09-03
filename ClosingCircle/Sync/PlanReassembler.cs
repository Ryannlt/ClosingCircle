using System.Collections.Generic;

// Collects the chunks of one pushed plan. Chunks can arrive out of order, a resend can repeat one, and a push
// can be superseded before it completes, so this holds exactly one sequence at a time and drops the rest.

namespace ClosingCircle.Sync
{
    public class PlanReassembler
    {
        private readonly Dictionary<int, string> _parts = new Dictionary<int, string>();

        private int _sequence = -1;
        private int _count;

        public void Reset()
        {
            _parts.Clear();
            _sequence = -1;
            _count = 0;
        }

        // Returns true only on the chunk that completes a plan, handing back the joined payload.
        public bool Accept(string message, out string payload)
        {
            payload = null;

            if (!PlanCodec.TryReadChunk(message, out int sequence, out int index, out int count, out string body))
                return false;

            // A newer push abandons whatever was half-assembled. An older one is a straggler and is ignored.
            if (sequence != _sequence)
            {
                if (sequence < _sequence) return false;

                _parts.Clear();
                _sequence = sequence;
                _count = count;
            }

            _parts[index] = body;
            if (_parts.Count != _count) return false;

            var joined = new System.Text.StringBuilder();
            for (int i = 0; i < _count; i++)
            {
                if (!_parts.TryGetValue(i, out string part)) return false;
                joined.Append(part);
            }

            payload = joined.ToString();
            _parts.Clear();
            return true;
        }
    }
}
