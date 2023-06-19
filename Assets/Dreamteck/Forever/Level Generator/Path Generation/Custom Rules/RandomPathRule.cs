using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Forever
{
    public class RandomPathRule : CustomPathRule
    {
        public bool setYaw = false;
        public float targetYaw = 0f;
        public bool setPitch = false;
        public float targetPitch = 0f;
        public bool setRoll = false;
        public float targetRoll = 0f;
        [Space()]
        public bool setYawStep = false;
        public float yawStep = 0f;
        public bool setPitchStep = false;
        public float pitchStep = 0f;
        public bool setRollStep = false;
        public float rollStep = 0f;

        public override void OnBeforeGeneration(LevelPathGenerator generator)
        {
            base.OnBeforeGeneration(generator);
            if (!(generator is RandomPathGenerator)) return;
            RandomPathGenerator randomGenerator = (RandomPathGenerator)generator;
            if (setYaw) randomGenerator.SetTargetYaw(targetYaw);
            if (setPitch) randomGenerator.SetTargetPitch(targetPitch);
            if (setRoll) randomGenerator.SetTargetRoll(targetRoll);

            if (setYawStep) randomGenerator.SetYawStep(yawStep);
            if (setPitchStep) randomGenerator.SetPitchStep(pitchStep);
            if (setRollStep) randomGenerator.SetRollStep(rollStep);
        }
    }
}
