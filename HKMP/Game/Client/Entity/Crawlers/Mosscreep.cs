using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Mosscreep has all the fields/properties/methods of HealthManagedEntity
    public class Mosscreep : HealthManagedEntity
    {
        // Private fields created for Mosscreep
        private readonly PlayMakerFSM _fsm;
        private Animation _lastAnimation;
        private readonly tk2dSpriteAnimator _animator;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Mosscreep(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Mosscreep, entityId, gameObject)
        {
            // Initializes variables for Mosscreep instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            _fsm = gameObject.LocateMyFSM("Moss Walker");

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
                    //Logger.Get().Info(this, $"Listing Mosscreep Animations: {(string)clip.name}");
                }
                // Making each animation send an update
                // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dsprite_animator component
                /*_animator.AnimationEventTriggered = (caller, currentClip, currentFrame) =>
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
                };*/
            }
            else
            {
                Logger.Get().Warn(this, "Crawler _animator not found");
            }

            string[] allComponents = Retrieval.ComponentList(gameObject);
            foreach (string component in allComponents)
            {
                Logger.Get().Info(this, "Component List: " + component);            

            }
            
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Shake", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Shake); }));
            _fsm.InsertMethod("Wake", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Wake); }));
            _fsm.InsertMethod("Walk Start", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.WalkStart); }));
            _fsm.InsertMethod("Turn", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Turn); }));
            _fsm.InsertMethod("Hide", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Hide); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Mosscreep state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            //_animator.enabled = true;
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // disables walker component
            RemoveAllTransitions(_fsm);
            //_animator.enabled = false;
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Mosscreep Destroyed");
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
            Logger.Get().Info(this, "Mosscreep: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            //_animator.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Shake:
                    _fsm.SetState("Wake");
                    break;
                case Animation.Wake:
                    _fsm.SetState("Activate");
                    break;
                case Animation.WalkStart:
                    _fsm.SetState("WalkStart");
                    break;
                case Animation.Turn:
                    _fsm.SetState("Flip");
                    break;
                case Animation.Hide:
                    _fsm.SetState("Rest");
                    break;
            }

        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;

            if (animation == Animation.Shake)
            {
                _fsm.ExecuteActions("Shake", 1, 2, 3);
            }

            if (animation == Animation.Wake)
            {
                _fsm.ExecuteActions("Wake", 2, 3);
            }

            if (animation == Animation.WalkStart)
            {
                _fsm.ExecuteActions("WalkStart", 1, 2, 4);
            }

            if (animation == Animation.Turn)
            {
                _fsm.ExecuteActions("Turn", 1);
            }

            if (animation == Animation.Hide)
            {
                _fsm.ExecuteActions("Hide", 1, 2, 3, 5, 6);
            }

            /*// Client: Executes Animations
            if (animation == Animation.Idle)
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
            }*/
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
            Idle,
            Shake,
            Wake,
            WalkStart,
            Turn,
            Hide,
        }
    }
}