using System.Collections;
using System.Collections.Generic;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    public class BroodingMawlek : HealthManagedEntity
    {
        private readonly PlayMakerFSM _fsm;
        private readonly PlayMakerFSM _fsmHead;
        private readonly PlayMakerFSM _fsmArmL;
        private readonly PlayMakerFSM _fsmArmR;
        private readonly Walker _walker;
        private Animation _lastAnimation;

        public BroodingMawlek(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.BroodingMawlek, entityId, gameObject)
        {
            _fsm = gameObject.LocateMyFSM("Mawlek Control");
            _fsmHead = gameObject.transform.GetChild(0).gameObject.LocateMyFSM("Mawlek Head");
            _fsmArmL = gameObject.transform.GetChild(1).gameObject.LocateMyFSM("Mawlek Arm Control");
            _fsmArmR = gameObject.transform.GetChild(4).gameObject.LocateMyFSM("Mawlek Arm Control");
            _walker = gameObject.GetComponent<Walker>();

            // Find all gameObjects attached to Entity gameObject
            string[] allChildNames = Retrieval.ChildrenList(gameObject);
            foreach(string childName in allChildNames )
            {
                Logger.Get().Info(this, "Child GameObject name: " + childName);
            }

            // Find all components attached to Entity gameObjects
            string[] allComponentNames = Retrieval.ComponentList(gameObject);
            foreach(string componentName in allComponentNames )
            {
                Logger.Get().Info(this, "Found script: " + componentName);
            }

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
                    Logger.Get().Info(this, $"Listing Mawlek Animations: {(string)clip.name}");
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

        // This includes all 4 FSMs
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Init", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Init); }));
            _fsm.InsertMethod("Dormant", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Dormant); }));

            _fsm.InsertMethod("Wake", 0, CreateUpdateMethod(() => {
                // Send the wake animation with a zero byte to indicate that it is not the Godhome variant
                SendAnimationUpdate((byte)Animation.Wake, new List<byte> { 0 });

                SendStateUpdate((byte)State.Active);
            }));

            _fsm.InsertMethod("GG Wake", 0, CreateUpdateMethod(() => {
                // Send the wake animation with a one byte to indicate that it is the Godhome variant
                SendAnimationUpdate((byte)Animation.Wake, new List<byte> { 1 });

                SendStateUpdate((byte)State.Active);
            }));

            _fsm.InsertMethod("Wake Jump", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.WakeJump); }));
            _fsm.InsertMethod("Wake In Air", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.WakeInAir); }));
            _fsm.InsertMethod("Wake Land", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.WakeLand); }));
            _fsm.InsertMethod("Title", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Title); }));
            _fsm.InsertMethod("Wake Roar", 0,CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.WakeRoar); }));
            _fsm.InsertMethod("Roar End", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RoarEnd); }));
            _fsm.InsertMethod("Music", 1, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Music); }));
            _fsm.InsertMethod("Start", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Start); }));
            _fsm.InsertMethod("Super Ready", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SuperReady); }));
            _fsm.InsertMethod("Super Jump", 1, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SuperJump); }));
            _fsm.InsertMethod("Super Spit", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SuperSpit); }));
            _fsm.InsertMethod("L", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SuperSpitL); }));
            _fsm.InsertMethod("R", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SuperSpitR); }));
            _fsm.InsertMethod("Shoot", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Shoot); }));
            _fsm.InsertMethod("Jump", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Jump); }));
            _fsm.InsertMethod("Land", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Land); }));
            _fsm.InsertMethod("Super Spit", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SuperSpit); }));
            _fsm.InsertMethod("Jump 2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Jump2); }));
            _fsm.InsertMethod("Land 2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Land2); }));
            _fsm.InsertMethod("Super Cooldown", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Cooldown); }));

            _fsmHead.InsertMethod("Dormant", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.HDormant); }));
            _fsmHead.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.HIdle); }));
            _fsmHead.InsertMethod("Shoot Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.HAntic); }));
            _fsmHead.InsertMethod("Shoot", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.HShoot); }));
            _fsmHead.InsertMethod("L", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.L); }));
            _fsmHead.InsertMethod("R", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.R); }));

            _fsmArmL.InsertMethod("Dormant", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LDormant); }));
            _fsmArmL.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LIdle); }));
            _fsmArmL.InsertMethod("Swipe Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LAntic); }));
            _fsmArmL.InsertMethod("Swipe", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LSwipe); }));
            _fsmArmL.InsertMethod("Swipe Cooldown", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LCooldown); }));

            _fsmArmR.InsertMethod("Dormant", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RDormant); }));
            _fsmArmR.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RIdle); }));
            _fsmArmR.InsertMethod("Swipe Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RAntic); }));
            _fsmArmR.InsertMethod("Swipe", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RSwipe); }));
            _fsmArmR.InsertMethod("Swipe Cooldown", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RCooldown); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            /*var activeStateName = _fsm.ActiveStateName;

            switch (activeStateName)
            {
                case "Init":
                case "Invincible":
                case "Sleep":
                    SendStateUpdate((byte)State.Asleep);
                    break;
                default:
                    SendStateUpdate((byte)State.Active);
                    break;
            }*/

            _walker.enabled = true;
            SendStateUpdate((byte)State.Active);
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            RemoveAllTransitions(_fsm);
            RemoveAllTransitions(_fsmHead);
            RemoveAllTransitions(_fsmArmL);
            RemoveAllTransitions(_fsmArmR);
            _walker.enabled = false;

            /*if (stateIndex.HasValue)
            {
                var healthManager = GameObject.GetComponent<HealthManager>();
                healthManager.IsInvincible = false;
                healthManager.InvincibleFromDirection = 0;

                var state = (State)stateIndex.Value;

                Logger.Get().Info(this, $"Initializing with state: {state}");

                if (state == State.Active)
                {
                    _fsm.ExecuteActions("Wake", 4, 6);

                    _fsm.ExecuteActions("Fly", 2, 5, 7, 8);
                }
            }*/
        }

        protected override void InternalSwitchToSceneHost()
        {
            // We first restore all transitions and then we set the state of the main FSM
            RestoreAllTransitions(_fsm);
            RestoreAllTransitions(_fsmHead);
            RestoreAllTransitions(_fsmArmL);
            RestoreAllTransitions(_fsmArmR);
            _walker.enabled = true;

            // Based on the last animation we received, we can put the FSM back in a proper state
            switch (_lastAnimation)
            {
                case Animation.Init:
                    _fsm.SetState("Dormant");
                    break;
                case Animation.Dormant:
                    _fsm.SetState("Dormant");
                    break;
                case Animation.Wake:
                    _fsm.SetState("Wake Jump");
                    break;
                case Animation.WakeJump:
                    _fsm.SetState("Wake In Air");
                    break;
                case Animation.WakeLand:
                    _fsm.SetState("Title");
                    break;
                case Animation.Title:
                    _fsm.SetState("Wake Roar");
                    break;
                case Animation.WakeRoar:
                    _fsm.SetState("RoarEnd");
                    break;
                case Animation.RoarEnd:
                    _fsm.SetState("Music");
                    break;
                case Animation.Music:
                    _fsm.SetState("Start");
                    break;
                case Animation.SuperReady:
                    _fsm.SetState("Super Select");
                    break;
                case Animation.SuperJump:
                    _fsm.SetState("Super Jump");
                    break;
                case Animation.SuperSpit:
                    _fsm.SetState("Detect Hero Pos");
                    break;
                case Animation.Shoot:
                    _fsm.SetState("Super Cooldown");
                    break;
                case Animation.Jump:
                    _fsm.SetState("In Air");
                    break;
                case Animation.Land:
                    _fsm.SetState("Aim Return");
                    break;
                case Animation.Jump2:
                    _fsm.SetState("In Air 2");
                    break;
                case Animation.Land2:
                    _fsm.SetState("Super Cooldown");
                    break;
                case Animation.Walk:
                    _fsm.SetState("Start");
                    break;
                case Animation.SuperSpitL:
                    _fsm.SetState("Shoot");
                    break;
                case Animation.SuperSpitR:
                    _fsm.SetState("Shoot");
                    break;

                case Animation.HDormant:
                    _fsmHead.SetState("Dormant");
                    break;
                case Animation.HIdle:
                    _fsmHead.SetState("Idle");
                    break;
                case Animation.HAntic:
                    _fsmHead.SetState("Detect Hero Pos");
                    break;
                case Animation.L:
                    _fsmHead.SetState("Shoot");
                    break;
                case Animation.R:
                    _fsmHead.SetState("Shoot");
                    break;
                case Animation.HShoot:
                    _fsmHead.SetState("Idle");
                    break;

                case Animation.LDormant:
                    _fsmArmL.SetState("Dormant");
                    break;
                case Animation.LIdle:
                    _fsmArmL.SetState("Idle");
                    break;
                case Animation.LAntic:
                    _fsmArmL.SetState("Swipe");
                    break;
                case Animation.LSwipe:
                    _fsmArmL.SetState("Swipe Cooldown");
                    break;
                case Animation.LCooldown:
                    _fsmArmL.SetState("Re attack Pause");
                    break;

                case Animation.RDormant:
                    _fsmArmR.SetState("Dormant");
                    break;
                case Animation.RIdle:
                    _fsmArmR.SetState("Idle");
                    break;
                case Animation.RAntic:
                    _fsmArmR.SetState("Swipe");
                    break;
                case Animation.RSwipe:
                    _fsmArmR.SetState("Swipe Cooldown");
                    break;
                case Animation.RCooldown:
                    _fsmArmR.SetState("Re attack Pause");
                    break;
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;

            if (animation == Animation.Init)
            {
                _fsm.ExecuteActions("Init", 12);
            }

            if (animation == Animation.Dormant)
            {
                _fsm.ExecuteActions("Dormant", 1);
            }

            if (animation == Animation.Wake)
            {
                var wakeType = animationInfo[0];

                if (wakeType == 0)
                {
                    // This is the non-godhome wake
                    _fsm.ExecuteActions("Wake", 1, 3);
                }

                _fsm.ExecuteActions("GG Wake", 1);
            }

            if (animation == Animation.WakeJump)
            {
                _fsm.ExecuteActions("Wake Jump", 1, 2, 3, 6);
            }

            if (animation == Animation.WakeInAir)
            {
                _fsm.ExecuteActions("Wake In Air", 1);
            }

            if (animation == Animation.WakeLand)
            {
                _fsm.ExecuteActions("Wake Land", 2, 3, 5, 6, 8, 9);
            }

            if (animation == Animation.WakeRoar)
            {
                _fsm.ExecuteActions("Wake Roar", 1, 2, 3, 4, 5, 6, 7, 8);
            }

            if (animation == Animation.RoarEnd)
            {
                _fsm.ExecuteActions("Roar End", 1, 2, 3, 4);
            }

            if (animation == Animation.Music)
            {
                _fsm.ExecuteActions("Music", 2, 3);
            }

            if (animation == Animation.Start)
            {
                _fsm.ExecuteActions("Start", 1, 4, 5, 6);
            }

            if (animation == Animation.SuperReady)
            {
                _fsm.ExecuteActions("Super Ready", 2, 3, 4, 6, 7);
            }

            if (animation == Animation.SuperJump)
            {
                _fsm.ExecuteActions("Super Jump", 5, 6, 7);
            }

            if (animation == Animation.SuperSpit)
            {
                _fsm.ExecuteActions("Super Spit", 1, 2, 3, 4, 5);
            }

            if (animation == Animation.SuperSpitL)
            {
                _fsm.ExecuteActions("L", 1, 2);
            }

            if (animation == Animation.SuperSpitR)
            {
                _fsm.ExecuteActions("R", 1, 2);
            }

            if (animation == Animation.Shoot)
            {
                _fsm.ExecuteActions("Shoot", 1, 2, 3, 4, 5, 6);
            }

            if (animation == Animation.Jump)
            {
                _fsm.ExecuteActions("Jump", 1, 2, 4, 5, 6);
            }

            if (animation == Animation.Land)
            {
                _fsm.ExecuteActions("Land", 2, 3, 4, 5);
            }

            if (animation == Animation.Jump2)
            {
                _fsm.ExecuteActions("Jump 2", 1, 2, 4, 5, 6);
            }

            if (animation == Animation.Land2)
            {
                _fsm.ExecuteActions("Land 2", 2, 3, 4, 5);
            }

            if (animation == Animation.Cooldown)
            {
                _fsm.ExecuteActions("Super Cooldown", 1, 2, 3, 4, 5, 6);
            }

            if (animation == Animation.HDormant)
            {
                _fsmHead.ExecuteActions("Dormant", 1);
            }

            if (animation == Animation.HIdle)
            {
                _fsmHead.ExecuteActions("Idle", 1);
            }

            if (animation == Animation.HAntic)
            {
                _fsmHead.ExecuteActions("Shoot Antic", 1);
            }

            if (animation == Animation.L)
            {
                _fsmHead.ExecuteActions("L", 1, 2);
            }

            if (animation == Animation.R)
            {
                _fsmHead.ExecuteActions("R", 1, 2);
            }

            if (animation == Animation.HShoot)
            {
                _fsmHead.ExecuteActions("Shoot", 1, 2, 3, 6);
            }

            if (animation == Animation.LDormant)
            {
                _fsmArmL.ExecuteActions("Dormant", 1);
            }

            if (animation == Animation.LIdle)
            {
                _fsmArmL.ExecuteActions("Idle", 1);
            }

            if (animation == Animation.LAntic)
            {
                _fsmArmL.ExecuteActions("Swipe Antic", 1, 2);
            }

            if (animation == Animation.LSwipe)
            {
                _fsmArmL.ExecuteActions("Swipe", 1, 2);
            }

            if (animation == Animation.LCooldown)
            {
                _fsmArmL.ExecuteActions("Swipe Cooldown", 1);
            }

            if (animation == Animation.RDormant)
            {
                _fsmArmR.ExecuteActions("Dormant", 1);
            }

            if (animation == Animation.RIdle)
            {
                _fsmArmR.ExecuteActions("Idle", 1);
            }

            if (animation == Animation.RAntic)
            {
                _fsmArmR.ExecuteActions("Swipe Antic", 1, 2);
            }

            if (animation == Animation.RSwipe)
            {
                _fsmArmR.ExecuteActions("Swipe", 1, 2);
            }

            if (animation == Animation.RCooldown)
            {
                _fsmArmR.ExecuteActions("Swipe Cooldown", 1);
            }
        }

        public override void UpdateState(byte stateIndex)
        {
        }

        private enum State
        {
            Asleep = 0,
            Active
        }

        private enum Animation
        {
            Init = 0,
            Dormant,
            Wake,
            WakeJump,
            WakeInAir,
            WakeLand,
            Title,
            WakeRoar,
            RoarEnd,
            Music,
            Start,
            SuperReady,
            SuperJump,
            Jump,
            Land,
            SuperSpit,
            Shoot,
            Jump2,
            Land2,
            Cooldown,
            Idle,
            Walk,
            Turn,
            HDormant,
            HIdle,
            HAntic,
            L,
            R,
            HShoot,
            LDormant,
            LIdle,
            LAntic,
            LSwipe,
            LCooldown,
            RDormant,
            RIdle,
            RAntic,
            RSwipe,
            RCooldown,
            SuperSpitL,
            SuperSpitR,
        }
    }
}