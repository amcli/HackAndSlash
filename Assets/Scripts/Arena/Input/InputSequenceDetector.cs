using System.Collections.Generic;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>The vocabulary of inputs that can take part in a command sequence.</summary>
    public enum InputToken { Attack, Guard, Dodge }

    /// <summary>The special moves a recognised sequence can trigger.</summary>
    public enum Command { None, ForesightSlash }

    /// <summary>
    /// A fighting-game style "motion input" recogniser. Register command
    /// sequences (ordered <see cref="InputToken"/>s plus the max time allowed
    /// between consecutive steps), then <see cref="Feed"/> every press as it
    /// happens; when the tail of the recent presses matches a registered
    /// sequence within its timing, that <see cref="Command"/> is returned.
    ///
    /// One detector can hold many sequences, so new abilities are added purely by
    /// registering more — nothing else changes. Uses unscaled time so a hitstop
    /// freeze never widens or breaks a player's input timing.
    /// </summary>
    public class InputSequenceDetector
    {
        readonly struct Stamp
        {
            public readonly InputToken Token;
            public readonly float Time;
            public Stamp(InputToken token, float time) { Token = token; Time = time; }
        }

        readonly struct Sequence
        {
            public readonly Command Command;
            public readonly InputToken[] Tokens;
            public readonly float MaxStepGap;
            public Sequence(Command command, InputToken[] tokens, float maxStepGap)
            {
                Command = command;
                Tokens = tokens;
                MaxStepGap = maxStepGap;
            }
        }

        readonly List<Sequence> _sequences = new();
        readonly List<Stamp> _recent = new();
        float _pruneWindow = 0.5f; // recent presses older than this can't complete any sequence

        public void Register(Command command, InputToken[] tokens, float maxStepGap)
        {
            _sequences.Add(new Sequence(command, tokens, maxStepGap));
            // Keep enough history for the longest sequence to ever match.
            _pruneWindow = Mathf.Max(_pruneWindow, maxStepGap * tokens.Length);
        }

        /// <summary>Record a press and return the command it completes, if any.</summary>
        public Command Feed(InputToken token)
        {
            float now = Time.unscaledTime;
            _recent.Add(new Stamp(token, now));
            Prune(now);

            foreach (var sequence in _sequences)
            {
                if (!Matches(sequence))
                    continue;
                _recent.Clear(); // consume the inputs so they can't re-trigger
                return sequence.Command;
            }
            return Command.None;
        }

        void Prune(float now)
        {
            int stale = 0;
            while (stale < _recent.Count && now - _recent[stale].Time > _pruneWindow)
                stale++;
            if (stale > 0)
                _recent.RemoveRange(0, stale);
        }

        /// <summary>True when the most-recent presses are exactly this sequence, in order and in time.</summary>
        bool Matches(Sequence sequence)
        {
            int length = sequence.Tokens.Length;
            if (_recent.Count < length)
                return false;

            int start = _recent.Count - length;
            for (int i = 0; i < length; i++)
            {
                if (_recent[start + i].Token != sequence.Tokens[i])
                    return false;
                if (i > 0 && _recent[start + i].Time - _recent[start + i - 1].Time > sequence.MaxStepGap)
                    return false;
            }
            return true;
        }
    }
}
