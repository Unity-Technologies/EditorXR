using UnityEngine.InputNew;
using System.Collections.Generic;

namespace UnityEngine.VR.Proxies
{
    public interface IProxy
    {
        bool active
        {
            get;
        }

        TrackedObject trackedObjectInput
        {
            set;
        }

        Dictionary<Node, Transform> rayOrigins
        {
            get;
        }

        bool hidden
        {
            set;
        }

        Transform menuOrigin
        {
            get; set;
        }

        Transform menuInputOrigin
        {
            get; set;
        }
    }
}
