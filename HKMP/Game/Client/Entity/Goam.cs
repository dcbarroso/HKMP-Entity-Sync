using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;
//using UnityEngine.AnimationModule;

namespace Hkmp.Game.Client.Entity
{
    // Goam has all the fields/properties/methods of HealthManagedEntity
    public class Goam : Entity
    {
        // Private fields created for Goam
        private Animation _lastAnimation;

        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;
        private readonly tk2dSpriteAnimator _animator;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Goam(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Goam, entityId, gameObject)
        {
            // Initializes fields for Goam instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Worm Control");
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            CreateAnimationEvents();
            //_transform = gameObject.GetComponent<Transform>();
            //_climber = gameObject.GetComponent<Climber>();


            Component[] components = gameObject.GetComponents<Component>();

            // Iterate through all components
            foreach (Component component in components)
            {
                // Check if the component is a MonoBehaviour (script)
                if (component is MonoBehaviour)
                {
                    // Found a script, you can now access its methods and properties
                    MonoBehaviour script = (MonoBehaviour)component;
                    Logger.Get().Info(this, $"Found script: " + script.GetType().Name);

                    // Here you can perform additional checks or actions based on the found script
                    // For example, you could check if it has a specific method or property
                }
            };



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
                    Logger.Get().Info(this, $"Listing Goam Animations: {(string) clip.name}");
                }
            }
            else
            {
                Logger.Get().Warn(this, "Animator not found");
            }
            // Making each animation send an update
            // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dspriteanimator component
            _animator.AnimationEventTriggered = (caller, currentClip, currentFrame) =>
            {
                if (currentClip.name == "Fire")
                {
                    //SendAnimationUpdate((byte)Animation.Fire);
                    Logger.Get().Info(this, $"Goam {entityId} should be firing now");
                }
                if (currentClip.name == "TurnToFly")
                {
                    //SendAnimationUpdate((byte)Animation.TurnToFly);
                    Logger.Get().Info(this, $"Goam {entityId} should be turning now");
                }
                /*else if (currentClip.name == "Fly")
                {
                    SendAnimationUpdate((byte)Animation.Fly);
                }
                else if (currentClip.name == "Fire Long")
                {
                    SendAnimationUpdate((byte)Animation.FireLong);
                    Logger.Get().Info(this, $"Goam {entityId} should be firing long now");
                }*/
            };
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            /*_fsm.InsertMethod("Fire", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Fire); }));
            _fsm.InsertMethod("Fire Dribble", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FireDribble); }));
            _fsm.InsertMethod("Fire Anticipate", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FireAnticipate); }));
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));*/

            _fsm.InsertMethod("Retract", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Retract); }));
            _fsm.InsertMethod("Down", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Down); }));
            _fsm.InsertMethod("Burst", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Burst); }));
            _fsm.InsertMethod("Up", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Goam state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            //_climber.enabled = true;
            _animator.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // disables climber component
            //_climber.Stop(Walker.StopReasons.Bored); // Stops enemy from 'drifting' when entering the room. 
            //_climber.enabled = false;
            _animator.enabled = false;
            RemoveAllTransitions(_fsm);
            //Logger.Get().Info(this, "Goam InternalInitializeAsSceneClient invoked, attempted to disable climber");
            /*_transform.position = _transform.position;
            _transform.rotation = _transform.rotation;
            _transform.localScale = _transform.localScale;
            Logger.Get().Info(this, "Attempted to disable transform");*/
            if (stateIndex == 1)
            {
                //UnityEngine.Object.Destroy(_gameObject);
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Goam Destroyed");
            }
        }

        //protected override void DestroyObject(GameObject gameObject) {}

        protected override void InternalSwitchToSceneHost()
        {
            Logger.Get().Info(this, "Goam: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            //_climber.enabled = true;
            _animator.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Retract:
                    _fsm.SetState("Down");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Retract");
                    break;
                case Animation.Down:
                    _fsm.SetState("Burst Rocks?");
                    break;
                case Animation.Burst:
                    _fsm.SetState("Up");
                    break;
            }

        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            //base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;


            // Client: Executes Animations
            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Up");
            }

            if (animation == Animation.Burst)
            {
                _fsm.ExecuteActions("Burst");
                Logger.Get().Info(this, "Goam Executing Burst");
            }

            if (animation == Animation.Down)
            {
                _fsm.ExecuteActions("Down");
            }

            if (animation == Animation.Retract)
            {
                _fsm.ExecuteActions("Retract");
            }
            // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
            /*if (animation == Animation.Fire)
            {
                _animator.Play("Fire");
                Logger.Get().Info(this, "Goam Playing Fire");
            }
            if (animation == Animation.TurnToFly)
            {
                _animator.Play("TurnToFly");
                Logger.Get().Info(this, "Goam Playing TurnToFly");
            }*/
        }

        public override void UpdateState(byte stateIndex)
        {
        }
        private enum State
        {
            Active = 0,
        }

        private enum Animation
        {
            Idle = 0,
            Retract,
            Down,
            Burst
        }
    }
}