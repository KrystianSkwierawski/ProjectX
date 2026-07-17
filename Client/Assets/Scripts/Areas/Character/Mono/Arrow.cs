using Assets.Scripts.Areas.Shared.Enums;
using UnityEngine;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Arrow : AbstractWeapon
    {
        protected override AudioTypeEnum PrecastAudioType => AudioTypeEnum.ArrowPrecast;

        protected override AudioTypeEnum CastAudioType => AudioTypeEnum.ArrowCast;

        protected override AudioTypeEnum ImpactAudioType => AudioTypeEnum.ArrowImpact;

        protected override float BaseDamage => 35f;

        protected override float Speed => 20f;

        protected override Quaternion RotationOffset => Quaternion.Euler(90f, 0f, 0f);
    }
}
