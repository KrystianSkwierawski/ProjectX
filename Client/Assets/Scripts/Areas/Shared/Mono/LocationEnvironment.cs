using System;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

namespace Assets.Scripts.Areas.Shared.Mono
{
    [RequireComponent(typeof(NavMeshSurface))]
    public sealed class LocationEnvironment : MonoBehaviour
    {
        public Pose GetEntryPose(LocationEnum source)
        {
            var portal = GetComponentsInChildren<DungeonPortal>()
                .Where(x => x.Destination == source)
                .FirstOrDefault();

            if (portal == null)
            {
                throw new InvalidOperationException($"Location {name} has no return portal to {source}.");
            }

            return portal.GetArrivalPose();
        }

        public void ValidateNavigation()
        {
            if (GetComponent<NavMeshSurface>().navMeshData == null)
            {
                throw new InvalidOperationException($"Location environment {name} has no baked NavMesh.");
            }
        }
    }
}
