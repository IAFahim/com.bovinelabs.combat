using Unity.Entities;

namespace BovineLabs.Combat.Timeline.Locks
{
    public struct CombatLockClipData : IComponentData
    {
        public bool LockInput;
        public bool LockBrain;
        public bool LockTurn;
        public bool LockAvoidance;
    }
}
