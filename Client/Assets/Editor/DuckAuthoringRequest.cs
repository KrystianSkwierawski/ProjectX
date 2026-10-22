using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectX.Editor
{
    [InitializeOnLoad]
    internal static class DuckAuthoringRequest
    {
        private const string Request = "Temp/duck-build.request";
        private const string Status = "Temp/duck-build.status";

        static DuckAuthoringRequest()
        {
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!File.Exists(Status) || File.ReadAllText(Status) != "waiting_for_edit_mode")
                {
                    File.WriteAllText(Status, "waiting_for_edit_mode");
                }

                return;
            }

            File.Delete(Request);
            try
            {
                DuckCharacterBuilder.Build();
                File.WriteAllText(Status, "complete");
            }
            catch (Exception exception)
            {
                File.WriteAllText(Status, exception.ToString());
                Debug.LogException(exception);
            }
        }
    }
}
