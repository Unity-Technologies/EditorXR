using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class WorkspaceModule : IModuleDependency<DeviceInputModule>, IConnectInterfaces, ISerializePreferences,
        IInterfaceConnector
    {
        [Serializable]
        class Preferences
        {
            [SerializeField]
            List<WorkspaceLayout> m_WorkspaceLayouts = new List<WorkspaceLayout>();

            public List<WorkspaceLayout> workspaceLayouts { get { return m_WorkspaceLayouts; } }
        }

        [Serializable]
        class WorkspaceLayout
        {
            [SerializeField]
            string m_Name;
            [SerializeField]
            Vector3 m_LocalPosition;
            [SerializeField]
            Quaternion m_LocalRotation;
            [SerializeField]
            Bounds m_ContentBounds;
            [SerializeField]
            string m_PayloadType;
            [SerializeField]
            string m_Payload;

            public string name
            {
                get { return m_Name; }
                set { m_Name = value; }
            }

            public Vector3 localPosition
            {
                get { return m_LocalPosition; }
                set { m_LocalPosition = value; }
            }

            public Quaternion localRotation
            {
                get { return m_LocalRotation; }
                set { m_LocalRotation = value; }
            }

            public Bounds contentBounds
            {
                get { return m_ContentBounds; }
                set { m_ContentBounds = value; }
            }

            public string payloadType
            {
                get { return m_PayloadType; }
                set { m_PayloadType = value; }
            }

            public string payload
            {
                get { return m_Payload; }
                set { m_Payload = value; }
            }
        }

        internal static readonly Vector3 DefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
        internal static readonly Quaternion DefaultWorkspaceTilt = Quaternion.AngleAxis(-45, Vector3.right);

        internal List<IWorkspace> workspaces { get { return m_Workspaces; } }

        readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();
        readonly List<IInspectorWorkspace> m_Inspectors = new List<IInspectorWorkspace>();

        internal event Action<IWorkspace> workspaceCreated;
        internal event Action<IWorkspace> workspaceDestroyed;

        internal static List<Type> workspaceTypes { get; private set; }

        internal Transform leftRayOrigin { private get; set; }
        internal Transform rightRayOrigin { private get; set; }

        internal bool preserveWorkspaces { get; set; }

        Preferences m_Preferences;

        static WorkspaceModule()
        {
            workspaceTypes = new List<Type>();
            typeof(IWorkspace).GetImplementationsOfInterface(workspaceTypes);
        }

        public void ConnectDependency(DeviceInputModule dependency)
        {
            workspaceCreated += workspace => { dependency.UpdatePlayerHandleMaps(); };
        }

        public void LoadModule()
        {
            preserveWorkspaces = Core.EditorVR.preserveLayout;

            ICreateWorkspaceMethods.createWorkspace = CreateWorkspace;
            IResetWorkspacesMethods.resetWorkspaceRotations = ResetWorkspaceRotations;
            IUpdateInspectorsMethods.updateInspectors = UpdateInspectors;
        }

        public void UnloadModule()
        {
            foreach (var workspace in m_Workspaces.ToList())
            {
                if (workspace.transform)
                    UnityObjectUtils.Destroy(workspace.transform.gameObject);
            }

            m_Workspaces.Clear();
        }

        public object OnSerializePreferences()
        {
            if (!preserveWorkspaces)
                return null;

            var preferences = new Preferences();
            var workspaceLayouts = preferences.workspaceLayouts;
            foreach (var workspace in workspaces)
            {
                var layout = new WorkspaceLayout();
                layout.name = workspace.GetType().FullName;
                layout.localPosition = workspace.transform.localPosition;
                layout.localRotation = workspace.transform.localRotation;
                layout.contentBounds = workspace.contentBounds;

                var serializeWorkspace = workspace as ISerializeWorkspace;
                if (serializeWorkspace != null)
                {
                    var payload = serializeWorkspace.OnSerializeWorkspace();
                    layout.payloadType = payload.GetType().FullName;
                    layout.payload = JsonUtility.ToJson(payload);
                }

                workspaceLayouts.Add(layout);
            }

            return preferences;
        }

        public void OnDeserializePreferences(object obj)
        {
            m_Preferences = (Preferences)obj;
        }

        internal void CreateWorkspace(Type t, Action<IWorkspace> createdCallback = null)
        {
            if (!typeof(IWorkspace).IsAssignableFrom(t))
                return;

            // HACK: MiniWorldWorkspace is not working in single pass yet
#if UNITY_EDITOR
            if (t == typeof(MiniWorldWorkspace) && PlayerSettings.stereoRenderingPath != StereoRenderingPath.MultiPass)
            {
                Debug.LogWarning("The MiniWorld workspace is not working on single pass, currently.");
                return;
            }
#endif

            var cameraTransform = CameraUtils.GetMainCamera().transform;

            var workspace = (IWorkspace)EditorXRUtils.CreateGameObjectWithComponent(t, CameraUtils.GetCameraRig(), false);
            m_Workspaces.Add(workspace);
            workspace.destroyed += OnWorkspaceDestroyed;
            this.ConnectInterfaces(workspace);

            var evrWorkspace = workspace as Workspace;
            if (evrWorkspace != null)
            {
                evrWorkspace.leftRayOrigin = leftRayOrigin;
                evrWorkspace.rightRayOrigin = rightRayOrigin;
            }

            //Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
            workspace.Setup();

            var offset = DefaultWorkspaceOffset;
            offset.z += workspace.vacuumBounds.extents.z;

            var workspaceTransform = workspace.transform;
            workspaceTransform.position = cameraTransform.TransformPoint(offset);
            ResetRotation(workspace, cameraTransform.forward);

            if (createdCallback != null)
                createdCallback(workspace);

            if (workspaceCreated != null)
                workspaceCreated(workspace);
        }

        void OnWorkspaceDestroyed(IWorkspace workspace)
        {
            m_Workspaces.Remove(workspace);

            this.DisconnectInterfaces(workspace);

            if (workspaceDestroyed != null)
                workspaceDestroyed(workspace);
        }

        internal void ResetWorkspaceRotations()
        {
            var cameraTransform = CameraUtils.GetMainCamera().transform;
            foreach (var ws in workspaces)
            {
                var forward = (ws.transform.position - cameraTransform.position).normalized;
                ResetRotation(ws, forward);
            }
        }

        static void ResetRotation(IWorkspace workspace, Vector3 forward)
        {
            workspace.transform.rotation = Quaternion.LookRotation(forward) * DefaultWorkspaceTilt;
        }

        internal void UpdateInspectors(GameObject obj = null, bool fullRebuild = false)
        {
            foreach (var inspector in m_Inspectors)
            {
                inspector.UpdateInspector(obj, fullRebuild);
            }
        }

        void AddInspector(IInspectorWorkspace inspectorWorkspace)
        {
            m_Inspectors.Add(inspectorWorkspace);
        }

        void RemoveInspector(IInspectorWorkspace inspectorWorkspace)
        {
            m_Inspectors.Remove(inspectorWorkspace);
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var allWorkspaces = target as IAllWorkspaces;
            if (allWorkspaces != null)
                allWorkspaces.allWorkspaces = workspaces;

            var inspectorWorkspace = target as IInspectorWorkspace;
            if (inspectorWorkspace != null)
                AddInspector(inspectorWorkspace);
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var inspectorWorkspace = target as IInspectorWorkspace;
            if (inspectorWorkspace != null)
                RemoveInspector(inspectorWorkspace);
        }

        public void CreateSerializedWorkspaces()
        {
            if (!preserveWorkspaces)
                return;

            foreach (var workspaceLayout in m_Preferences.workspaceLayouts)
            {
                var layout = workspaceLayout;
                var workspaceType = Type.GetType(workspaceLayout.name);
                if (workspaceType != null)
                {
                    if (Core.EditorVR.HiddenTypes.Contains(workspaceType))
                        continue;

                    if (Application.isPlaying && workspaceType.GetCustomAttributes(true).OfType<EditorOnlyWorkspaceAttribute>().Any())
                        continue;

                    CreateWorkspace(workspaceType, workspace =>
                    {
                        workspace.transform.localPosition = layout.localPosition;
                        workspace.transform.localRotation = layout.localRotation;
                        workspace.contentBounds = layout.contentBounds;

                        var serializeWorkspace = workspace as ISerializeWorkspace;
                        if (serializeWorkspace != null)
                        {
                            var payload = JsonUtility.FromJson(layout.payload, Type.GetType(layout.payloadType));
                            serializeWorkspace.OnDeserializeWorkspace(payload);
                        }
                    });
                }
            }
        }
    }
}
