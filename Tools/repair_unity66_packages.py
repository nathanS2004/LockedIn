from pathlib import Path
import json, shutil
root=Path.cwd()
source=root/'Library/PackageCache/com.meta.xr.sdk.core@f8b4cfb2789f'
target=root/'Packages/com.meta.xr.sdk.core'
assert json.loads(((target if target.exists() else source)/'package.json').read_text())['version']=='85.0.0'
if not target.exists(): shutil.copytree(source,target)
def replace(relative,old,new):
 p=target/relative
 s=p.read_text(encoding='utf-8-sig')
 if new in s: return
 if old not in s: raise RuntimeError('Unexpected source: '+relative)
 p.write_text(s.replace(old,new),encoding='utf-8')
replace('Scripts/BuildingBlocks/RoomMesh/RoomMeshAnchor.cs','_mesh.GetInstanceID()','_mesh.GetEntityId()')
replace('Scripts/BuildingBlocks/RoomMesh/RoomMeshAnchor.cs','public int MeshID;','public EntityId MeshID;')
replace('Scripts/ImmersiveDebugger/Gizmos/GizmoRendererManager.cs','instance.GetInstanceID()','InstanceHandle.GetDebugId(instance.GetEntityId())')
replace('Scripts/ImmersiveDebugger/Utils/InstanceHandle.cs','instance.GetInstanceID()','GetDebugId(instance.GetEntityId())')
replace('Scripts/ImmersiveDebugger/Utils/InstanceHandle.cs','InstanceId = scene.handle;','InstanceId = GetDebugId(scene.handle);')
replace('Scripts/ImmersiveDebugger/Utils/InstanceHandle.cs','public Object Instance { get; }','''// Debugger-local handles preserve the SDK int interface without truncating Unity's 64-bit IDs.
        // Typed boxed keys distinguish SceneHandle from EntityId; zero remains the static sentinel.
        private static readonly System.Collections.Generic.Dictionary<object, int> DebugIds = new();
        internal static int GetDebugId(object key)
        {
            if (!DebugIds.TryGetValue(key, out int id))
            {
                id = checked(DebugIds.Count + 1);
                DebugIds.Add(key, id);
            }
            return id;
        }
        public Object Instance { get; }''')
p=root/'Packages/manifest.json'
s=json.loads(p.read_text()); s['dependencies']['com.unity.ai.inference']='2.6.1'
p.write_text(json.dumps(s,indent=2)+'\n')
print('Embedded Meta Core 85 compatibility patch applied; Sentis pinned to 2.6.1.')

replace('Editor/OVRTelemetry/OVRSceneChangeListener.cs',
        'EditorUtility.InstanceIDToObject(createGameObjectHierarchyEvent.instanceId)',
        'EditorUtility.EntityIdToObject(createGameObjectHierarchyEvent.entityId)')
replace('Editor/OVRTelemetry/OVRSceneChangeListener.cs',
        'EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId)',
        'EditorUtility.EntityIdToObject(changeGameObjectStructure.entityId)')

# MRUK uses the obsolete ID only in a diagnostic string.
source=root/'Library/PackageCache/com.meta.xr.mrutilitykit@6d21b459f6d3'
target=root/'Packages/com.meta.xr.mrutilitykit'
assert json.loads(((target if target.exists() else source)/'package.json').read_text())['version']=='85.0.0'
if not target.exists(): shutil.copytree(source,target)
replace('Core/Scripts/MRUKGlobalContext.cs','{GetInstanceID()}','{GetEntityId()}')

target=root/'Packages/com.meta.xr.sdk.core'
replace('Editor/BuildingBlocks/CustomHierarchyIcon.cs','EditorApplication.hierarchyWindowItemOnGUI','EditorApplication.hierarchyWindowItemByEntityIdOnGUI')
replace('Editor/BuildingBlocks/CustomHierarchyIcon.cs','HierarchyItemOnGUI(int instanceID','HierarchyItemOnGUI(EntityId instanceID')
replace('Editor/BuildingBlocks/CustomHierarchyIcon.cs','EditorUtility.InstanceIDToObject(instanceID)','EditorUtility.EntityIdToObject(instanceID)')
replace('Editor/BuildingBlocks/BlocksWindow.cs','int dropTargetInstanceID,','EntityId dropTargetInstanceID,')
replace('Editor/BuildingBlocks/BlocksWindow.cs','EditorUtility.InstanceIDToObject(dropTargetInstanceID)','EditorUtility.EntityIdToObject(dropTargetInstanceID)')
replace('Editor/BuildingBlocks/BlocksWindow.cs','DragAndDrop.AddDropHandler(HierarchyDropHandler)','DragAndDrop.AddDropHandlerV2(HierarchyDropHandler)')
replace('Editor/BuildingBlocks/BlocksWindow.cs','DragAndDrop.RemoveDropHandler(HierarchyDropHandler)','DragAndDrop.RemoveDropHandlerV2(HierarchyDropHandler)')
p=root/'Packages/manifest.json'
s=json.loads(p.read_text()); s['dependencies']['com.unity.modules.unitywebrequestaudio']='1.0.0'
p.write_text(json.dumps(s,indent=2)+'\n')

target=root/'Packages/com.meta.xr.sdk.core'
replace('Editor/BuildingBlocks/BlockData/AIBlocks/Providers/CredentialStorageEditor.cs','{obj.GetInstanceID()}','{obj.GetEntityId()}')

target=root/'Packages/com.meta.xr.sdk.core'
replace('Editor/Utils/SampleMetadata/UpdateManifestWithCodeSample.cs','            // Get the AndroidManifest.xml files and append the code sample.\n            var manifestFiles = Directory.GetFiles(Path.Combine(path, ".."), "AndroidManifest.xml", SearchOption.AllDirectories);\n            foreach (var manifestFile in manifestFiles)\n            {\n                UpdateManifest(manifestFile, listOfSamples);\n            }','            // Add application metadata to the source manifest that Gradle merges.\n            // Never scan build/intermediates: those outputs can disappear during generation.\n            var manifestFile = Path.Combine(path, "src", "main", "AndroidManifest.xml");\n            UpdateManifest(manifestFile, listOfSamples);')
