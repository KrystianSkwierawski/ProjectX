using Assets.Scripts.Areas.Shared.Enums;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Fireball : AbstractWeapon
    {
        protected override AudioTypeEnum PrecastAudioType => AudioTypeEnum.FireballPrecast;

        protected override AudioTypeEnum CastAudioType => AudioTypeEnum.FireballCast;

        protected override AudioTypeEnum ImpactAudioType => AudioTypeEnum.FireballImpact;

        protected override float BaseDamage => 50f;

        protected override float Speed => 15f;
    }
}
