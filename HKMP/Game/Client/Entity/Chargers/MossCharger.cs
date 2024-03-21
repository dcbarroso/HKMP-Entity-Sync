using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // MossCharger has all the fields/properties/methods of HealthManagedEntity
    public class MossCharger : HealthManagedEntity
    {
        // Private fields created for MossCharger
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public MossCharger(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.MossCharger, entityId, gameObject)
        {
            // Initializes variables for MossCharger instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Mossy Control");

            CreateAnimationEvents();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Detach", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Detach); }));
            _fsm.InsertMethod("Emerge", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Emerge); }));
            _fsm.InsertMethod("Charge", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Charge); }));
            _fsm.InsertMethod("Submerge", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Submerge); }));
            _fsm.InsertMethod("Submerge Grass effect", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SubmergeGrass); }));
            _fsm.InsertMethod("Submerge CD", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SubmergeCD); }));
            _fsm.InsertMethod("Play Range", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.PlayRange); }));
            _fsm.InsertMethod("Line Loop", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LineLoop); }));
            _fsm.InsertMethod("Burst", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Burst); }));
            _fsm.InsertMethod("Fly Left", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FlyLeft); }));
            _fsm.InsertMethod("Fly Right", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FlyRight); }));
            _fsm.InsertMethod("FlyUp", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FlyUp); }));
            _fsm.InsertMethod("Fly Down", 2, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FlyDown); }));
            _fsm.InsertMethod("In Air", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.InAir); }));
            _fsm.InsertMethod("Get Up", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.GetUp); }));
            _fsm.InsertMethod("Direction", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Direction); }));
            _fsm.InsertMethod("Run L", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RunL); }));
            _fsm.InsertMethod("Run R", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RunR); }));
            _fsm.InsertMethod("On Ground?", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.OnGround); }));
            _fsm.InsertMethod("Dig Start", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.DigStart); }));
            _fsm.InsertMethod("Dig", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Dig); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates MossCharger state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "MossCharger Destroyed");
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
            Logger.Get().Info(this, "MossCharger: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Emerge:
                    _fsm.SetState("Charge");
                    break;
                case Animation.Charge:
                    _fsm.SetState("Charge");
                    break;
                case Animation.Submerge:
                    _fsm.SetState("Submerge Grass effect");
                    break;
                case Animation.SubmergeGrass:
                    _fsm.SetState("Submerge CD");
                    break;
                case Animation.SubmergeCD:
                    _fsm.SetState("Play Range");
                    break;
                case Animation.PlayRange:
                    _fsm.SetState("Hidden");
                    break;
                case Animation.LineLoop:
                    _fsm.SetState("Line Loop");
                    break;
                case Animation.Burst:
                    _fsm.SetState("Burst");
                    break;
                case Animation.FlyLeft:
                    _fsm.SetState("In Air");
                    break;
                case Animation.FlyRight:
                    _fsm.SetState("In Air");
                    break;
                case Animation.FlyUp:
                    _fsm.SetState("In Air");
                    break;
                case Animation.FlyDown:
                    _fsm.SetState("In Air");
                    break;
                case Animation.InAir:
                    _fsm.SetState("Land");
                    break;
                case Animation.GetUp:
                    _fsm.SetState("Direction");
                    break;
                case Animation.Direction:
                    _fsm.SetState("Direction");
                    break;
                case Animation.RunL:
                    _fsm.SetState("Run L");
                    break;
                case Animation.RunR:
                    _fsm.SetState("Run R");
                    break;
                case Animation.OnGround:
                    _fsm.SetState("On Ground?");
                    break;
                case Animation.DigStart:
                    _fsm.SetState("Dig Start");
                    break;
                case Animation.Dig:
                    _fsm.SetState("Dig");
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
            if (animation == Animation.Detach)
            {
                _fsm.ExecuteActions("Detach", 1, 2, 3);
            }
            if (animation == Animation.Emerge)
            {
                _fsm.ExecuteActions("Emerge", 1, 2, 7, 8, 11, 12);
            }
            if (animation == Animation.Charge)
            {
                _fsm.ExecuteActions("Charge", 1, 2, 3, 4);
            }
            if (animation == Animation.Submerge)
            {
                _fsm.ExecuteActions("Submerge", 1, 2, 3, 5);
            }
            if (animation == Animation.SubmergeGrass)
            {
                _fsm.ExecuteActions("Submerge Grass effect", 1, 2);
            }
            if (animation == Animation.SubmergeCD)
            {
                _fsm.ExecuteActions("Submerge CD", 1, 2, 3, 5, 6);
            }
            if (animation == Animation.PlayRange)
            {
                _fsm.ExecuteActions("Play Range", 1);
            }
            if (animation == Animation.LineLoop)
            {
                _fsm.ExecuteActions("Line Loop", 3, 4);
            }
            if (animation == Animation.Burst)
            {
                _fsm.ExecuteActions("Burst", 1, 2, 3, 4, 5, 7, 8);
            }
            if (animation == Animation.FlyLeft)
            {
                _fsm.ExecuteActions("Fly Left", 2);
            }
            if (animation == Animation.FlyRight)
            {
                _fsm.ExecuteActions("Fly Right", 2);
            }
            if (animation == Animation.FlyUp)
            {
                _fsm.ExecuteActions("FlyUp", 2);
            }
            if (animation == Animation.FlyDown)
            {
                _fsm.ExecuteActions("Fly Down", 3);
            }
            if (animation == Animation.InAir)
            {
                _fsm.ExecuteActions("In Air", 1, 7, 8);
            }
            if (animation == Animation.GetUp)
            {
                _fsm.ExecuteActions("Get Up", 1);
            }
            if (animation == Animation.Direction)
            {
                _fsm.ExecuteActions("Direction", 1, 2, 3, 4);
            }
            if (animation == Animation.RunL)
            {
                _fsm.ExecuteActions("Run L", 1);
            }
            if (animation == Animation.RunR)
            {
                _fsm.ExecuteActions("Run R", 1);
            }
            if (animation == Animation.OnGround)
            {
                _fsm.ExecuteActions("On Ground?", 1);
            }
            if (animation == Animation.DigStart)
            {
                _fsm.ExecuteActions("Dig Start", 3, 4, 7);
            }
            if (animation == Animation.Dig)
            {
                _fsm.ExecuteActions("Dig", 3, 5);
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
            Detach = 0,
            Emerge,
            Charge,
            Submerge,
            SubmergeGrass,
            SubmergeCD,
            PlayRange,
            LineLoop,
            Burst,
            FlyLeft,
            FlyRight,
            FlyUp,
            FlyDown,
            InAir,
            GetUp,
            Direction,
            RunL,
            RunR,
            OnGround,
            DigStart,
            Dig
        }
    }
}