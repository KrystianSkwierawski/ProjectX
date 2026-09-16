using System;
using UnityEngine;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public enum LocationEnum
    {
        EnvironmentScene = 0,
        DungeonScene = 1,
        TemplateScene = 2
    }

    public sealed class DungeonPortal : MonoBehaviour
    {
        public const float InteractionDistance = 5f;

        [Tooltip("Location loaded after interacting with this portal.")]
        [SerializeField] private LocationEnum _destination = LocationEnum.DungeonScene;

        [Tooltip("Position and facing after arrival. Point its blue Z axis away from the portal.")]
        [SerializeField] private Transform _arrival;

        public LocationEnum Destination => _destination;

        public Pose GetArrivalPose()
        {
            if (_arrival == null)
            {
                throw new InvalidOperationException($"Portal {name} to {_destination} has no Arrival point.");
            }

            return new Pose(_arrival.position, Quaternion.Euler(0f, _arrival.eulerAngles.y, 0f));
        }
    }
}
