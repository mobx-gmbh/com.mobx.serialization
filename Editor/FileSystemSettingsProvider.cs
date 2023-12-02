using Cysharp.Threading.Tasks;
using MobX.Utilities.Editor.Inspector;
using System.Collections.Generic;
using UnityEngine;

namespace MobX.Serialization.Editor
{
    public class FileSystemSettingsProvider : UnityEditor.SettingsProvider
    {
        private UnityEditor.SerializedObject _argsObject;
        private UnityEditor.SerializedProperty _argsProperty;

        private UnityEditor.SerializedObject _shutdownArgsObject;
        private UnityEditor.SerializedProperty _shutdownArgsProperty;

        private FoldoutHandler Foldout { get; } = new(nameof(FileSystemSettingsProvider));

        public FileSystemSettingsProvider(string path, UnityEditor.SettingsScope scopes,
            IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            UnityEditor.EditorGUILayout.Space(20);

            var initializationFlags = FileSystemEditorSettings.instance.InitializationFlags;
            initializationFlags =
                (InitializeFlags) UnityEditor.EditorGUILayout.EnumFlagsField("Initialization", initializationFlags);
            FileSystemEditorSettings.instance.InitializationFlags = initializationFlags;

            var shutdownFlags = FileSystemEditorSettings.instance.ShutdownFlags;
            shutdownFlags = (ShutdownFlags) UnityEditor.EditorGUILayout.EnumFlagsField("Shutdown", shutdownFlags);
            FileSystemEditorSettings.instance.ShutdownFlags = shutdownFlags;

            GUI.enabled = false;
            UnityEditor.EditorGUILayout.TextField("File System State", FileSystem.State.ToString());
            UnityEditor.EditorGUILayout.TextField("File System Root", FileSystem.RootFolder);
            GUI.enabled = true;

            UnityEditor.EditorGUILayout.Space();
            DrawButtons();
            UnityEditor.EditorGUILayout.Space();

            var labelWidth = UnityEditor.EditorGUIUtility.labelWidth;
            UnityEditor.EditorGUIUtility.labelWidth = 230;
            if (Foldout["Initialization"])
            {
                UnityEditor.EditorGUILayout.Space();
                DrawSetupArguments();
                UnityEditor.EditorGUILayout.Space();
            }
            if (Foldout["Shutdown"])
            {
                UnityEditor.EditorGUILayout.Space();
                DrawShutdownArguments();
                UnityEditor.EditorGUILayout.Space();
            }
            UnityEditor.EditorGUIUtility.labelWidth = labelWidth;

            FileSystemEditorSettings.instance.SaveSettings();

            if (GUI.changed)
            {
                Foldout.SaveState();
            }
        }

        private void DrawButtons()
        {
            GUILayout.BeginHorizontal("HelpBox");
            GUI.enabled = FileSystem.State == FileSystemState.Uninitialized;
            if (GUILayout.Button("Initialize"))
            {
                FileSystem.InitializeAsync(FileSystemEditorSettings.instance.Args).Forget();
            }
            GUI.enabled = FileSystem.State == FileSystemState.Initialized;
            if (GUILayout.Button("Shutdown"))
            {
                FileSystem.ShutdownAsync(FileSystemEditorSettings.instance.ShutdownArgs);
            }
            GUI.enabled = true;
            if (GUILayout.Button("Storage"))
            {
                Application.OpenURL(Application.persistentDataPath);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawShutdownArguments()
        {
            UnityEditor.EditorGUILayout.LabelField("File System Shutdown");
            var arguments = FileSystemEditorSettings.instance.FileSystemShutdownArguments;
            arguments = (FileSystemShutdownArgumentsAsset) UnityEditor.EditorGUILayout.ObjectField("Arguments",
                arguments, typeof(FileSystemShutdownArgumentsAsset), false);
            FileSystemEditorSettings.instance.FileSystemShutdownArguments = arguments;

            if (_shutdownArgsProperty != null && arguments == null)
            {
                _shutdownArgsObject = null;
                _shutdownArgsProperty = null;
            }
            if (_shutdownArgsProperty == null && arguments != null)
            {
                _shutdownArgsObject = new UnityEditor.SerializedObject(arguments);
                _shutdownArgsProperty = _shutdownArgsObject.FindProperty("args");
            }
            if (_shutdownArgsProperty != null && _shutdownArgsObject != null)
            {
                _shutdownArgsObject.Update();
                UnityEditor.EditorGUILayout.PropertyField(_shutdownArgsProperty);
                _shutdownArgsObject.ApplyModifiedProperties();
            }
        }

        private void DrawSetupArguments()
        {
            UnityEditor.EditorGUILayout.LabelField("File System Initialization");
            var arguments = FileSystemEditorSettings.instance.FileSystemArguments;
            arguments = (FileSystemArgumentsAsset) UnityEditor.EditorGUILayout.ObjectField("Arguments", arguments,
                typeof(FileSystemArgumentsAsset), false);
            FileSystemEditorSettings.instance.FileSystemArguments = arguments;

            if (_argsProperty != null && arguments == null)
            {
                _argsObject = null;
                _argsProperty = null;
            }
            if (_argsProperty == null && arguments != null)
            {
                _argsObject = new UnityEditor.SerializedObject(arguments);
                _argsProperty = _argsObject.FindProperty("args");
            }
            if (_argsProperty != null && _argsObject != null)
            {
                _argsObject.Update();
                UnityEditor.EditorGUILayout.PropertyField(_argsProperty);
                _argsObject.ApplyModifiedProperties();
            }
        }

        [UnityEditor.SettingsProviderAttribute]
        public static UnityEditor.SettingsProvider CreateSettingsProvider()
        {
            return new FileSystemSettingsProvider("Project/File System", UnityEditor.SettingsScope.Project);
        }
    }
}