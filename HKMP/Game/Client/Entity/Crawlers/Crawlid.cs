using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Crawlid has all the fields/properties/methods of HealthManagedEntity
    public class Crawlid : HealthManagedEntity
    {
        // Private fields created for Crawlid
        private readonly PlayMakerFSM _fsm;
        private Animation _lastAnimation;
        private readonly tk2dSpriteAnimator _animator;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Crawlid(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Crawlid, entityId, gameObject)
        {
            // Initializes variables for Crawlid instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            _fsm = gameObject.LocateMyFSM("Crawler");

            CreateAnimationEvents();

            // Some animations are not controlled by the FSM. Hence we must make all animations in the entity's Walker component to trigger `AnimationEventTriggered` to send state updates. 
            if (_animator != null)
            {
                foreach (var clip in _animator.Library.clips)
                {
                    // Skip clips with no frames
                    if (clip.frames.Length == 0)
                    {
                        continue;
                    }
                    var firstFrame = clip.frames[0];
                    // Enable event triggering on the first frame
                    // Whenever frame displayed has triggerEvent set to true, AnimationEventTriggered is called
                    firstFrame.triggerEvent = true;
                    // Also include the clip name as event info
                    firstFrame.eventInfo = clip.name;
                    //Logger.Get().Info(this, $"Listing Crawlid Animations: {(string)clip.name}");
                }
            }
            else
            {
                Logger.Get().Warn(this, "Crawler _animator not found");
            }
            // Making each animation send an update
            // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dsprite_animator component
            _animator.AnimationEventTriggered = (caller, currentClip, currentFrame) =>
            {
                if (currentClip.name == "walk")
                {
                    SendAnimationUpdate((byte)Animation.Walk);
                }
                else if (currentClip.name == "turn")
                {
                    SendAnimationUpdate((byte)Animation.Turn);
                }
                if (currentClip.name == "idle")
                {
                    SendAnimationUpdate((byte)Animation.Idle);
                }
            };
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            //_fsm.InsertMethod("Walk", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Walk); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Crawlid state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            _animator.enabled = true;
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // disables walker component
            RemoveAllTransitions(_fsm);
            _animator.enabled = false;
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Crawlid Destroyed");
            }
        }

        protected override void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            // Call the base implementation to execute the original functionality
            base.HealthManagerOnDieHook(orig, self, attackDirection, attackType, ignoreEvasion);

            // Send Dead State
            SendStateUpdate((byte)State.Dead);
        }

        protected override void InternalSwitchToSceneHost()
        {
            Logger.Get().Info(this, "Crawlid: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            _animator.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Walk:
                    _fsm.SetState("Walk");
                    break;
            }

        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;

            // Client: Executes Animations
            if(animation == Animation.Idle)
            {
                // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
                _animator.Play("idle");
            }
            if (animation == Animation.Walk)
            {
                _animator.Play("walk");
            }
            if (animation == Animation.Turn)
            {
                _animator.Play("turn");
            }
        }

        public override void UpdateState(byte stateIndex)
        {
        }

        private enum State
        {
            Active = 0,
            Dead
        }

        private enum Animation
        {
            Walk = 0,
            Turn,
            Idle
        }
    }
}