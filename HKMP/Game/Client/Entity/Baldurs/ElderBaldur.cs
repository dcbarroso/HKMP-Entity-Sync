using System;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // ElderBaldur has all the fields/properties/methods of HealthManagedEntity
    public class ElderBaldur : HealthManagedEntity
    {
        // Private fields created for ElderBaldur
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private readonly Walker _walker;

        private readonly AudioSource _audioSource;

        private readonly tk2dSpriteAnimator _animator;

        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public ElderBaldur(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.ElderBaldur, entityId, gameObject)
        {
            // Initializes fields for ElderBaldur instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Blocker Control");
            //_walker = gameObject.GetComponent<Walker>();
            //_audioSource = gameObject.GetComponent<AudioSource>();
            CreateAnimationEvents();
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();

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
                    Logger.Get().Info(this, $"Listing Roller Animations: {(string)clip.name}");
                }
            }
            else
            {
                Logger.Get().Warn(this, "Animator not found");
            }
            // Making each animation send an update
            // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dspriteanimator component
            /*_animator.AnimationEventTriggered = (caller, currentClip, currentFrame) => {
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
                    //Logger.Get().Info(this, $"Host: Sending ElderBaldur Turn");
                }
            };*/
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Init", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Init); }));
            _fsm.InsertMethod("Open", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Open); }));
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Close", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Close); }));
            _fsm.InsertMethod("Close2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Close2); }));
            _fsm.InsertMethod("Goop", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Goop); }));
            _fsm.InsertMethod("Roller", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Roller); }));
            _fsm.InsertMethod("Shot Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShotAntic); }));
            _fsm.InsertMethod("Fire", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Fire); }));
            _fsm.InsertMethod("Sleep 1", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Close); }));
            _fsm.InsertMethod("Sleep 2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Close2); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Husk state as active, and then enables our walker component
            Logger.Get().Info(this, "Host: Internally Initializing ElderBaldur");
            SendStateUpdate((byte)State.Active);
            //_walker.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // Removes our FSM transitions and disables walker component
            RemoveAllTransitions(_fsm);
            //_walker.Stop(Walker.StopReasons.Bored); // Stops enemy from 'drifting' when entering the room. 
            //_walker.enabled = false;
            Logger.Get().Info(this, "Client: Internally Initializing ElderBaldur");
            if (stateIndex == 1)
            {
                //UnityEngine.Object.Destroy(_gameObject);
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Entity Destroyed");
            }
        }

        //protected override void DestroyObject(GameObject gameObject) {}

        protected override void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            // Call the base implementation to execute the original functionality
            base.HealthManagerOnDieHook(orig, self, attackDirection, attackType, ignoreEvasion);

            // Send Dead State
            SendStateUpdate((byte)State.Dead);
        }

        protected override void InternalSwitchToSceneHost()
        {
            Logger.Get().Info(this, "ElderBaldur: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            //_walker.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Init:
                    _fsm.SetState("Direction");
                    break;
                case Animation.Open:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Close:
                    _fsm.SetState("Close2");
                    break;
                case Animation.Close2:
                    _fsm.SetState("Closed");
                    break;
                case Animation.Goop:
                    _fsm.SetState("Shot Antic");
                    break;
                case Animation.Roller:
                    _fsm.SetState("Shot Antic");
                    break;
                case Animation.ShotAntic:
                    _fsm.SetState("Fire");
                    break;
                case Animation.Fire:
                    _fsm.SetState("Roller Assign");
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
            if (animation == Animation.Init)
            {
                _fsm.ExecuteActions("Init", 7, 8);
            }

            if (animation == Animation.Open)
            {
                _fsm.ExecuteActions("Open", 1, 2, 3);
            }

            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Idle", 5);
            }

            if (animation == Animation.Close)
            {
                _fsm.ExecuteActions("Close", 1, 3, 4);
            }

            if (animation == Animation.Close2)
            {
                _fsm.ExecuteActions("Close2", 1, 3);
            }

            if (animation == Animation.Goop)
            {
                _fsm.ExecuteActions("Goop", 1, 2);
            }

            if (animation == Animation.Roller)
            {
                _fsm.ExecuteActions("Roller", 1, 2, 3);
            }

            if (animation == Animation.ShotAntic)
            {
                _fsm.ExecuteActions("Shot Antic", 1);
            }

            if (animation == Animation.Fire)
            {
                _fsm.ExecuteActions("Fire", 1, 3, 4, 5, 6, 7, 8, 9, 10, 11);
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
            Idle = 0,
            Init,
            Open,
            Close,
            Close2,
            Goop,
            Roller,
            ShotAntic,
            Fire
        }
    }
}