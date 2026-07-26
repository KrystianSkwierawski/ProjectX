using Assets.Scripts.Areas.Shared.Enums;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Sword : AbstractWeapon
    {
        protected override AudioTypeEnum ImpactAudioType => AudioTypeEnum.SwordImpact;

        protected override float BaseDamage => 40f;

        protected override float Speed => 100f;
    }
}
