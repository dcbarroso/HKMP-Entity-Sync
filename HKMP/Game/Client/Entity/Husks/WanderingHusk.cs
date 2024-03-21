using System;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // WanderingHusk has all the fields/properties/methods of HealthManagedEntity
    public class WanderingHusk : HealthManagedEntity
    {
        // Private fields created for WanderingHusk
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private readonly Walker _walker;

        private readonly AudioSource _audioSource;

        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public WanderingHusk(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.WanderingHusk, entityId, gameObject)
        {
            // Initializes variables for WanderingHusk instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Zombie Swipe");
            _walker = gameObject.GetComponent<Walker>();
            _audioSource = gameObject.GetComponent<AudioSource>();
            CreateAnimationEvents();
            var walkerAnimator = gameObject.GetComponent<Walker>().GetComponent<tk2dSpriteAnimator>();


            // Some animations are not controlled by the FSM. Hence we must make all animations in the entity's Walker component to trigger `AnimationEventTriggered` to send state updates. 
            if (walkerAnimator != null)
            {
                foreach (var clip in walkerAnimator.Library.clips)
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
                }
            }
            else
            {
                Logger.Get().Warn(this, "Walker animator not found");
            }
            // Making each animation send an update
            // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dspriteanimator component
            walkerAnimator.AnimationEventTriggered = (caller, currentClip, currentFrame) => {
                if (currentClip.name == "Idle")
                {
                    SendAnimationUpdate((byte)Animation.Idle);
                }
                else if (currentClip.name == "Walk")
                {
                    SendAnimationUpdate((byte)Animation.Walk);
                }
                else if (currentClip.name == "Turn")
                {
                    SendAnimationUpdate((byte)Animation.Turn);
                }
            };
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Anticipate", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Anticipate); }));
            _fsm.InsertMethod("Lunge", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Lunge); }));
            _fsm.InsertMethod("Cooldown", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Cooldown); }));
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Husk state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            _walker.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // Removes our FSM transitions and disables walker component
            RemoveAllTransitions(_fsm);
            _walker.Stop(Walker.StopReasons.Bored); // Stops enemy from 'drifting' when entering the room. 
            _walker.enabled = false;
            Logger.Get().Info(this, "InternalInitializeAsSceneClient invoked");
            if (stateIndex == (byte)State.Dead)
            {
                // Disables entity game object if state is set to dead
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Entity Destroyed");
            }
        }

        // Invoked when entity dies for host
        protected override void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            // Call the base implementation to execute the original functionality
            base.HealthManagerOnDieHook(orig, self, attackDirection, attackType, ignoreEvasion);

            // Send Dead State
            SendStateUpdate((byte)State.Dead);
        }

        protected override void InternalSwitchToSceneHost()
        {
            Logger.Get().Info(this, "Wandering Husk: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            _walker.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Walk:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Anticipate:
                    _fsm.SetState("Lunge");
                    break;
                case Animation.Cooldown:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Lunge:
                    _fsm.SetState("Cooldown");
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
            if (animation == Animation.Anticipate)
            {
                _fsm.ExecuteActions("Anticipate", 2, 3, 5);
            }

            if (animation == Animation.Lunge)
            {
                _fsm.ExecuteActions("Lunge", 1, 2);
            }

            if (animation == Animation.Cooldown)
            {
                _fsm.ExecuteActions("Cooldown", 1, 2);
            }

            if (animation == Animation.Idle)
            {
                // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                _audioSource.Stop();
            }

            if (animation == Animation.Walk)
            {
                _audioSource.Play();
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Walk");
            }

            if (animation == Animation.Turn)
            {
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Turn");
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
            Anticipate = 0,
            Lunge,
            Cooldown,
            Idle,
            Walk,
            Turn,
        }
    }
}