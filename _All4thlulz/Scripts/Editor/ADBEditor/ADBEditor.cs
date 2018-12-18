#define EnablePumkinIntegration
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ADBEditor By All4thlulz
/// https://github.com/All4thlulz/ADBEditor
/// </summary>


namespace All4thlulz.Editor
{
#if UNITY_EDITOR
    public class ADBEditor : EditorWindow
    {
        private readonly string versionNum = "Beta 0.7.4";
       
        public enum LogLevel
        {
            Information = 0,
            Warning,
            Error,
            OutputLogOnly
        }

        public enum SimpleOutputLogCategories
        {
            BeginLog = 0,
            BeforeCopy,
            CopyDynamicBoneCollider,
            CopyDynamicBone,
            AfterCopy,
            BeforeFix,
            FixReferencesRootAndExclusions,
            FixReferencesDynamicBoneCollider,
            AfterFix,
            EndLog
        }

        private SimpleOutputLogCategories currentCategory = SimpleOutputLogCategories.BeginLog;

#if EnablePumkinIntegration
        //pumkin tools integration
        private EditorWindow pumkinUtilityWindow;
        private bool pumkinUtilityOpened = false;
        private string pumkinDownload = "https://github.com/rurre/PumkinsAvatarTools/releases";
#endif

        // ADBEditor
        private static string editorTitle = "ADBEditor";

        private GameObject oldModel;
        private GameObject newModel;
        private Vector3 newModelScale = Vector3.one;
        private DynamicBone[] copyBones;
        private DynamicBone[] existingBones;

        private Transform[] oldModelChildrens;
        private Transform[] newModelChildrens;

        private bool hasChangedNewModelTransform;

        // Fix references
        private GameObject ParentGameObject;
        private DynamicBoneCollider[] DynamicBoneColliders; // a reference of every possible dynamicbone collider childed to the parent
        private DynamicBone[] DynamicBoneComponents; // a reference to every dynamic bone component that exsits under the parent
        private Transform[] RootReferences; // a reference of every gameobject childed to the parent so we can replace any existing references with the correct ones

        // editor window gui
        private static int columnWidth = 300;

        private int _toolBarInt = 0;
#if EnablePumkinIntegration
        private readonly string[] _toolbarStrings = { "ADB Copy", "Fix References", "Settings", "Pumkins Tools" };
#else
        private readonly string[] _toolbarStrings = { "ADB Copy", "Fix References", "Settings"};
#endif
        private Vector2 _copyTabScrollPos = Vector2.zero;
        private Vector2 _fixTabScrollPos = Vector2.zero;
        private Vector2 _settingsTabScrollPos = Vector2.zero;

        private bool enableSceneSaveDialogue = false;
        private bool saveScenesAutomatically = false;
        private readonly string enableSceneSaveDialoguePrefKey = editorTitle + "." + "Enable Scene Save Dialogue";
        private readonly string saveScenesAutomaticallyPrefKey = editorTitle + "." + "Save Scenes Automatically";

        private bool enableRootGameObjectDialogue = true;
        private bool alwaysUseRootGameObject = false;
        private readonly string enableRootGameObjectDialoguePrefKey = editorTitle + "." + "Enable Root GameObject Dialogue";
        private readonly string alwaysUseRootGameObjectPrefKey = editorTitle + "." + "Always Use Root GameObject";

        private bool enableRemoveEmptyReferencesDialogue = true;
        private bool alwaysRemoveEmptyReferences = true;
        private readonly string enableRemoveEmptyReferencesDialoguePrefKey = editorTitle + "." + "Enable Empty References Check";
        private readonly string alwaysRemoveEmptyReferencesPrefKey = editorTitle + "." + "Always Remove Empty References";

        private bool enableTransformMismatchCheck = true;
        private bool alwaysMatchScale = true;
        private readonly string enableTransformMismatchCheckPrefKey = editorTitle + "." + "Enable Transform Mismatch Check";
        private readonly string alwaysMatchScalePrefKey = editorTitle + "." + "Always Match Scale";

        private bool enableConsoleDebugLogs = true;
        private bool onlyShowWarningAndErrorLogs = false;
        private readonly string enableConsoleDebugLogsPrefKey = editorTitle + "." + "Enable Console Debug Logs";
        private readonly string onlyShowWarningAndErrorLogsPrefKey = editorTitle + "." + "Only Show Warnings And Errors";

        private bool showOutputLogAtEnd = true;
        private readonly string showOutputLogAtEndPrefKey = editorTitle + "." + "Show Ouput Log After Run";

        private int outputLogCount = 0;
        private readonly string outputLogCountPrefKey = editorTitle + "." + "Output Log Count";

        //All output log categories as string
        private string allOutputLogStr = string.Empty;
        // Store all the output categories in their own string.
        // A bit obscure but this way we can iterate over all the categories in multiple situations as we dont need to know exactly what category it is to display the outputlog,
        //  just that the index order is the same for category and string array
        private string[] outputLogCategoryStrings = new string[10]; 
        private int[] outputLogWithErrors = new int[10];
        /*
        private string beginLogOutputLogStr = string.Empty;
        private string beforeCopyOutputLogStr = string.Empty;
        private string copyDynamicBoneCollidersOutputLogStr = string.Empty;
        private string copyDynamicBonesOutputLogStr = string.Empty;
        private string afterCopyOutputLogStr = string.Empty;
        private string beforeFixOutputLogStr = string.Empty;
        private string fixReferenceAndRootOutputLogStr = string.Empty;
        private string fixDynamicBoneCollidersOutputLogStr = string.Empty;
        private string afterFixOutputLogStr = string.Empty;
        private string endLogOutputLogStr = string.Empty;
        */

    private readonly string lastOutputLogPrefKey = "Last Output Log";
        


        //private readonly string logTitleDivider = "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬";
        private readonly string logTitleDivider = "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬";

        #region Editor Callbacks
        [MenuItem("All4thlulz/Advanced DynamicBone Editor")]
        static void Init()
        {
            EditorWindow.GetWindow<ADBEditor>(false, "ADBEditor");
            //EditorWindow win = EditorWindow.GetWindowWithRect<ADBEditor>(new Rect(0, 0, 310, 350));
            ADBEDependencyCheck.Check();
        }

        private void Awake()
        {
            LoadSettings();
        }

        private void OnFocus(){ LoadSettings(); }

        private void OnLostFocus(){}

        private void OnDestroy(){ ClosePumkinToolsUtility(); }
        #endregion

        private void SetCurrentCategory(SimpleOutputLogCategories category)
        {
            currentCategory = category;
            LogInformation("Current category has been set to: [" + category + "].",LogLevel.OutputLogOnly, null );
        }

        #region ADB Copy
        void CopyBothComponents()
        {
            AddOutputLogTitle("ADBPaste Copy All Components Start", SimpleOutputLogCategories.BeforeCopy);
            SetFixReferencesTarget(oldModel); //we need to check the oldModel only if we are running through the copy tab, so we assign it to the parent reference that fix references uses
            RemoveAllEmptyReferences();
            SetFixReferencesTarget(newModel); //go back to the newModel so we can copy and fix references as normal
            CheckTransformsMatch();
            SetTransformLists();
            SetCurrentCategory(SimpleOutputLogCategories.CopyDynamicBoneCollider);
            CopyDynamicBoneColliders();
            SetCurrentCategory(SimpleOutputLogCategories.CopyDynamicBone);
            CopyDynamicBones();
            SetCurrentCategory(SimpleOutputLogCategories.AfterCopy);
            UndoTransformMatch();
            FixAllDBReferences();
        }

        void CopyDynamicBoneCollidersOnly()
        {
            AddOutputLogTitle("ADBPaste Copy DynamicBone Colliders Only Start", SimpleOutputLogCategories.CopyDynamicBoneCollider);
            SetFixReferencesTarget(newModel);
            CheckTransformsMatch();
            SetTransformLists();
            CopyDynamicBoneColliders();
            UndoTransformMatch();
        }

        void CopyDynamicBonesOnly()
        {
            AddOutputLogTitle("ADBPaste Copy DynamicBones Only Start", SimpleOutputLogCategories.CopyDynamicBone);
            SetFixReferencesTarget(oldModel); //we need to check the old model only if we are running through the copy tab, so we assign it to the parent reference that fix references uses
            RemoveAllEmptyReferences();
            SetFixReferencesTarget(newModel);
            SetTransformLists();
            CopyDynamicBones();
            FixAllDBReferences();
        }

        void SetFixReferencesTarget(GameObject newReference)
        {
            if (ParentGameObject != null && ParentGameObject != newReference)
            {
                LogInformation("Fix References target will be changed from [" + ParentGameObject.name + "] to [" + newReference.name + "]", LogLevel.Warning, null);
                ParentGameObject = newReference;
            }
            else if (ParentGameObject == null)
            {
                LogInformation("Fix References target will be set to [" + newReference.name + "]", LogLevel.Warning, null);
                ParentGameObject = newReference;
            }
        }


        void SetTransformLists()
        {
            //Get every transform from the old and new model references
            oldModelChildrens = oldModel.GetComponentsInChildren<Transform>();
            newModelChildrens = newModel.GetComponentsInChildren<Transform>();
        }

        void CopyDynamicBoneColliders()
        {
            AddOutputLogTitle("Copy Dynamic Bone Colliders Start", SimpleOutputLogCategories.CopyDynamicBoneCollider);

            int totalCopied = 0;
            int totalGameObjectsCreated = 0;
            //Transfer dynamic bone colliders first
            //Will create the gameobjects if they dont exist
            foreach (Transform oldModelTransform in oldModelChildrens)
            {
                string parentName = oldModelTransform.parent != null ? oldModelTransform.parent.name : oldModelTransform.gameObject.name;
                //GameObject newModelColliderParentReference = newModel;
                GameObject newModelColliderParentReference = null; //needs to not be initilised so we can determine if a parent exists or not

                DynamicBoneCollider oldModelCol;
                //If the transform contains a dynamic bone collider we continue
                if ((oldModelCol = (oldModelTransform.gameObject.GetComponent<DynamicBoneCollider>())) != null)
                {
                    bool objectValid = false;
                    //Check if the gameobject exists on the new model
                    Transform newModelColliderTransformReference = newModel.transform;
                    foreach (Transform newModelTransform in newModelChildrens)
                    {
                        //At the same time we grab the parent reference in case we need to create a new gameObject
                        if (newModelTransform.gameObject.name == parentName)
                        {
                            newModelColliderParentReference = newModelTransform.gameObject;
                        }

                        // If we have a match then we have an existing gameObject to work with
                        if (newModelTransform.gameObject.name == oldModelTransform.gameObject.name)
                        {
                            //Check if this gameobject already contains a dynamic bone collider
                            //Only logging information for now
                            if ((newModelTransform.gameObject.GetComponent<DynamicBoneCollider>()) != null)
                            {
                                //TODO - This log gets triggered before any valid object can be determined
                                LogInformation("An existing dynamic bone collider was found on [" + newModel.name + "] under [" + newModelTransform.gameObject.name + "]. \nMake sure this copy is not a duplicate.", LogLevel.OutputLogOnly, null);
                            }

                            newModelColliderTransformReference = newModelTransform;
                            objectValid = true;
                            break;
                        }
                    }

                    // If there was no matching gameobject we can create a new object to add the collider to
                    if (!objectValid)
                    {
                        LogInformation("A parent reference could not be found. Attempting to create parent heirachy", LogLevel.Error, null);

                        //If there is no parent we want to:
                        //create that parent heirachy if it exists or
                        //parent it to the root as a last resort
                        if (newModelColliderParentReference == null)
                        {
                            //Check parent heirachy
                            List<GameObject> missingParentsInReverseOrder = new List<GameObject>();
                            GameObject validParentOnNewModel = null;

                            //grab the parent heirachy from the (oldModel collider we want to copy) to (the oldModel root reference or until we find a valid parent)
                            Transform t = oldModelCol.gameObject.transform;
                            while (t.parent != null)
                            {
                                t = t.parent.transform;
                                foreach (var newModeltransform in newModelChildrens)
                                {
                                    // if we find a valid parent we can stop adding to the parent heirachy
                                    if (newModeltransform.gameObject.name == t.gameObject.name)
                                    {
                                        validParentOnNewModel = newModeltransform.gameObject;
                                        break;
                                    }
                                }

                                // if it was a valid parent we can stop going through the parents
                                if (validParentOnNewModel != null)
                                {
                                    break;
                                }

                                //if it wasnt a valid parent it means we can add it to the missing parents
                                missingParentsInReverseOrder.Add(t.gameObject);
                            }

                            // if for some reason we did not find a valid parent we can assume that our parent heirachy of missing gameobjects originates from the root
                            if (validParentOnNewModel == null)
                            {
                                validParentOnNewModel = newModel;
                            }

                            //we now want to reacreate the missing parent heirachy gameobjects from our valid parent
                            GameObject previousParentCreated = null;
                            if (missingParentsInReverseOrder.Count > 0)
                            {
                                
                                for (int i = missingParentsInReverseOrder.Count; i --> 0;)
                                {
                                    GameObject parentGo = new GameObject(missingParentsInReverseOrder[i].name);
                                    Undo.RegisterCreatedObjectUndo(parentGo, "Created GameObject For Missing Parent Heirachy");

                                    //if we are on the first itteration we want to parent it to the validParentOnNewModel,
                                    //otherwise we use the previous parent
                                    parentGo.transform.parent = i == missingParentsInReverseOrder.Count - 1 ? validParentOnNewModel.transform : previousParentCreated.transform;
                                    parentGo.transform.localPosition = missingParentsInReverseOrder[i].transform.localPosition;
                                    parentGo.transform.localRotation = missingParentsInReverseOrder[i].transform.localRotation;

                                    previousParentCreated = parentGo;
                                }

                                //newModelColliderParentReference must equal the last parent in the missing heirachy
                                newModelColliderParentReference = previousParentCreated;
                            }

                            //if newModelColliderParentReference is still null
                            if (newModelColliderParentReference == null)
                            {
                                newModelColliderParentReference = newModel;
                                LogInformation("A parent reference could not be found or created before creating a new dynamic collider GameObject." + "\n" + "Parenting [" + oldModelTransform.gameObject.name + "] to [" + newModel.name + "].", LogLevel.Error, null);
                            }
                            else
                            {
                                LogInformation("Parent heirachy has been created on [" + newModelColliderParentReference.name + "]. Please double check heirachy is correct for the dynamic bone colliders.", LogLevel.Warning, newModelColliderParentReference);
                            }
                        }
                        GameObject go = new GameObject(oldModelTransform.gameObject.name);
                        Undo.RegisterCreatedObjectUndo(go, "Created GameObject For DynamicBoneCollider");
                        go.transform.parent = newModelColliderParentReference.transform;
                        go.transform.localPosition = oldModelTransform.localPosition;
                        go.transform.localRotation = oldModelTransform.localRotation;
                        newModelColliderTransformReference = go.transform;

                        totalGameObjectsCreated++;
                        LogInformation("GameObject [" + go.name + "] has been created on the new model reference and has been childed to [" + go.transform.parent.gameObject.name + "].", LogLevel.OutputLogOnly, go);
                    }

                    // now transfer the dynamic bone collider to the gameobject
                    if (newModelColliderTransformReference != null)
                    {
                        // on a valid gameobject we need to register an undo with the component, otherwise we know the gameobject has already been registered
                        DynamicBoneCollider newModelCol = !objectValid ? newModelColliderTransformReference.gameObject.AddComponent<DynamicBoneCollider>() : Undo.AddComponent<DynamicBoneCollider>(newModelColliderTransformReference.gameObject);
                        // now copy all the values over
                        newModelCol.m_Bound = oldModelCol.m_Bound;
                        newModelCol.m_Center = oldModelCol.m_Center;
                        newModelCol.m_Direction = oldModelCol.m_Direction;

                        // run a check on the armatures to see if the import scale is different
                        // we will then use this to correct the scale of the height and radius of the copied collider
                        float multiplier = GetCorrectScaleMultipler(newModelCol.gameObject.transform, oldModelCol.gameObject.transform);
                        newModelCol.m_Height = oldModelCol.m_Height * multiplier;
                        newModelCol.m_Radius = oldModelCol.m_Radius * multiplier;
                        totalCopied++;
                    }
                    else
                    {
                        LogInformation("Failed to get gameobject reference for dynamic bone collider to be added. [" + oldModelTransform.gameObject.name + "].", LogLevel.Error, null);
                    }
                }
            }

            if (totalGameObjectsCreated > 0)
            {
                LogInformation("Created [" + totalGameObjectsCreated + "] GameObjects that did not exist for the dynamic bone collider componnets. \nSee the output log for more information.", LogLevel.Information, null);
            }

            string log = totalCopied > 0 ? "Total dynamic bone colliders copied [" + totalCopied + "]." : "[" + totalCopied + "] Dynamic bone colliders were found on original model [" + oldModel.name + "]";
            LogInformation(log, LogLevel.Information, null);
        }

        void CopyDynamicBones()
        {
            AddOutputLogTitle("Copy Dynamic Bones Start", SimpleOutputLogCategories.CopyDynamicBone);

            int totalCopied = 0;
            // Iterate through every gameobject in the old model that could contain the dynamic bones
            foreach (Transform oldModelTransform in oldModelChildrens)
            {
                //Find any dynamic bones on the oldModelTransform
                if ((copyBones = (oldModelTransform.gameObject.GetComponents<DynamicBone>())) != null)
                {
                    // If we found any, go through every transform on the new model to find the matching gameobject
                    foreach (Transform newModelTransform in newModelChildrens)
                    {
                        // Find the matching gameobject
                        // or run on the first iteration to allow different root names
                        if (newModelTransform == newModelTransform.root || newModelTransform.name == oldModelTransform.name)
                        {
                            //Now that we know which transform on the new model will contain our dynamic bone Iterate through all dynamic bones found on the old model for our current oldModelTransform
                            foreach (var db in copyBones)
                            {
                                //check if we have a dynamic bone with the same root reference
                                //Some use cases will have all their dynamic bones with the root set to the m_Root,
                                //but other use cases it might be safe to assume that you dont want to copy an already existing dynamic bone
                                //Only logging information for now
                                if ((existingBones = (newModelTransform.gameObject.GetComponents<DynamicBone>())) != null)
                                {
                                    foreach (var existingBone in existingBones)
                                    {
                                        if (existingBone.m_Root.name == db.m_Root.name)
                                        {
                                            LogInformation("An existing dynamic bone was found that uses the root reference of [" + existingBone.m_Root.name + "]. \nMake sure this copy is not a duplicate.", LogLevel.OutputLogOnly, null);
                                        }
                                    }
                                }

                                //add an empty dynamic bone to copy things to
                                DynamicBone curr = Undo.AddComponent<DynamicBone>(newModelTransform.gameObject);

                                // create an empty list to store multiple colliders found on a single gameobject so we can add them correctly
                                List<DynamicBoneColliderBase> copyColliders = new List<DynamicBoneColliderBase>();
                                //grab all dynamic bone collider components found on the new model
                                DynamicBoneCollider[] newModelDynamicBoneColliders = ParentGameObject.GetComponentsInChildren<DynamicBoneCollider>();

                                if (newModelDynamicBoneColliders.Length == 0)
                                {
                                    LogInformation("No dynamic bone colliders were found on or childed to [" + newModel.name + "\nPlease ensure the dynamic bone colliders are copied then run a Reference Fix.", LogLevel.Error, null);
                                }

                                {
                                    // loop through all the collider references on the old model dynamic bone
                                    foreach (var col in db.m_Colliders)
                                    {
                                        if (col == null)
                                        {
                                            LogInformation("An empty dynamic bone collider reference was found on [" + db.gameObject.name + "] \nSkipping the empty reference.", LogLevel.Warning, null);
                                            continue;
                                        }

                                        bool foundMatch = false;
                                        // for this collider reference run, through every collider stored from the new model and find 
                                        foreach (var newModelCol in newModelDynamicBoneColliders)
                                        {
                                            if (newModelCol.name == col.name)
                                            {
                                                copyColliders.Add(newModelCol as DynamicBoneColliderBase);
                                                foundMatch = true;
                                            }
                                        }

                                        if (!foundMatch)
                                        {
                                            LogInformation("Could not find a Dynamic Bone Collider GameObject match for [" + col.name + "] \nAdding original reference as placeholder.", LogLevel.Error, null);
                                            copyColliders.Add(col as DynamicBoneColliderBase);
                                        }
                                    }

                                }
                                float scaleCorrectionMultiplier = GetCorrectScaleMultipler(newModelTransform, oldModelTransform);

                                curr.m_Colliders = copyColliders;
                                curr.m_Damping = db.m_Damping;
                                curr.m_DampingDistrib = db.m_DampingDistrib;
                                curr.m_DistanceToObject = db.m_DistanceToObject;
                                curr.m_DistantDisable = db.m_DistantDisable;
                                curr.m_Elasticity = db.m_Elasticity;
                                curr.m_ElasticityDistrib = db.m_ElasticityDistrib;
                                curr.m_EndLength = db.m_EndLength;
                                curr.m_EndOffset = db.m_EndOffset * scaleCorrectionMultiplier;

                               
                                List<Transform> excludeTransforms = new List<Transform>();
                                foreach (Transform trans1 in db.m_Exclusions)
                                {
                                    if (trans1 == null)
                                    {
                                        LogInformation("An empty dynamic bone exclusion reference was found on [" + db.gameObject.name + "] \nSkipping the empty reference.", LogLevel.Warning, null);
                                        continue;
                                    }

                                    bool foundMatch = false;
                                    int duplicateCount = 0;
                                    string duplicateStringNames = string.Empty;
                                    foreach (Transform trans2 in newModelChildrens)
                                    {
                                        if (trans1.name == trans2.name)
                                        {
                                            excludeTransforms.Add(trans2);
                                            foundMatch = true;
                                            duplicateCount++;
                                            duplicateStringNames += trans2.name + ", ";
                                        }
                                    }

                                    if (duplicateCount > 1)
                                    {
                                        LogInformation("Two or more gameobjects where one is used as an exclusion reference have the same name [" + duplicateStringNames + "]", LogLevel.Error, null );
                                        EditorUtility.DisplayDialog("Duplicate Exclusion GameObject Names", "Two or more gameobjects have the same name [" + duplicateStringNames +  "]. This can cause multiple issues with copying references. \nPlease undo the copy, rename the gameobjects appropriately and then run the copy once more.", "I Understand, Continue.");
                                    }

                                    // if we didnt find a match throw an error and just add the original exclusion in as a reference
                                    if (!foundMatch)
                                    {
                                        LogInformation("Could not find an exclusion match for [" + trans1.name + "], on DynamicBone script with root [" + db.m_Root.name + "]. \nAdding original reference as placeholder.", LogLevel.Error, curr.gameObject);
                                        excludeTransforms.Add(trans1);
                                    }
                                }

                                curr.m_Exclusions = excludeTransforms;
                                curr.m_Force = db.m_Force * scaleCorrectionMultiplier;
                                curr.m_FreezeAxis = db.m_FreezeAxis;
                                curr.m_Gravity = db.m_Gravity * scaleCorrectionMultiplier;
                                curr.m_Inert = db.m_Inert;
                                curr.m_InertDistrib = db.m_InertDistrib;
                                curr.m_Radius = db.m_Radius * scaleCorrectionMultiplier;
                                curr.m_RadiusDistrib = db.m_RadiusDistrib;
                                curr.m_ReferenceObject = db.m_ReferenceObject;
                                //curr.m_Root = newModelTransform;
                                curr.m_Root = db.m_Root; // Use the old model root so that we can correcty fix references in the Fix References script
                                curr.m_Stiffness = db.m_Stiffness;
                                curr.m_StiffnessDistrib = db.m_StiffnessDistrib;
                                curr.m_UpdateMode = db.m_UpdateMode;
                                curr.m_UpdateRate = db.m_UpdateRate;

                                totalCopied++;
                            }
                        }
                    }
                }
            }

            string log = totalCopied > 0 ? "Total dynamic bones copied [" + totalCopied + "]." : "[" + totalCopied + "] Dynamic bones were found on original model [" + oldModel.name + "]";
            LogInformation(log, LogLevel.Information, totalCopied > 0 ? null : oldModel);
        }

        #region Transform Matching
        private void CheckTransformsMatch()
        {
            if (!enableTransformMismatchCheck)
            {
                LogInformation("CheckTransformsMismatch(): Skipping check. \nEnable Transform Mismatch Check (" + enableTransformMismatchCheck + ")", LogLevel.Information, null);
                return;
            }

            // get the magnitude for each scale we want to check
            float oldModelScaleMagnitude = oldModel.transform.localScale.magnitude;
            float newModelScaleMagnitude = newModel.transform.localScale.magnitude;

            //if scales do not match
            if (!Mathf.Approximately(oldModelScaleMagnitude, newModelScaleMagnitude))
            {
                if (alwaysMatchScale)
                {
                    LogInformation("CheckTransformsMismatch(): Matching scales automatically. \nAlways Match Scales (" + alwaysMatchScale + ")", LogLevel.Information, null);
                    MatchScale();
                }
                else if (EditorUtility.DisplayDialog("Transform Scale Mismatch","[" + newModel.name + "] Does not match the scale of [" + oldModel.name + "]. \n", "Match Scale", "Ignore Mismatch"))
                {
                    MatchScale();
                }
            }
        }

        private void MatchScale()
        {
            //store the current scale so we can return to it
            newModelScale = newModel.transform.localScale;



            // change the new model's scale to match the old model
            Undo.RegisterCompleteObjectUndo(newModel.transform, "Match oldModel Scale");
            newModel.transform.localScale = oldModel.transform.localScale;
            hasChangedNewModelTransform = true;

            LogInformation("[" + newModel.name + "] Scale has been modified to match [" + oldModel.name + "]", LogLevel.Warning, null);
        }

        private void UndoTransformMatch()
        {
            if (!hasChangedNewModelTransform)
            {
                return;
            }
            // return the new model transform to its starting scale
            Undo.RegisterCompleteObjectUndo(newModel.transform, "Return newModel Scale To Initial");
            newModel.transform.localScale = newModelScale;
            hasChangedNewModelTransform = false;

            LogInformation("[" + newModel.name + "] Transform has been returned to its starting scale.", LogLevel.Information, newModel);
        }

        /// <summary>
        /// Will return a multiplier value to fix any scale issues for the copying of certain components that are affected by scale.
        /// Does a check for import scale and local scale matching
        /// </summary>
        /// <param name="localNewModelTrans">The gameobject transform on the newModel that will contain the new component</param>
        /// <param name="localoldModeltrans">The gameobject transform on the oldModel that will contain the new component</param>
        /// <returns></returns>
        private float GetCorrectScaleMultipler(Transform localNewModelTrans, Transform localoldModeltrans)
        {
            float multiplier = 1.0f;

            if (!enableTransformMismatchCheck)
            {
                LogInformation("GetCorrectScaleMultipler(): Skipping check. \nEnable Transform Mismatch Check (" + enableTransformMismatchCheck + ")", LogLevel.Information, null);
                return multiplier;
            }


            //get the armature scales to check if the import scale is different
            // An armature with a scale of 1 will have an import scale of 1
            // An armature with a scale of 100 will have an import scale of 0.01 (default .fbx unit scale to unity unit)
            Transform newModelArmature = newModel.transform.Find("Armature");
            Transform oldModelArmature = oldModel.transform.Find("Armature");

            if (newModelArmature == null || oldModelArmature == null)
            {
                LogInformation("GetCorrectScaleMultipler(): An armature gameobject could not be found to check import scale. Skipping check. \nEnable Transform Mismatch Check (" + enableTransformMismatchCheck + ")", LogLevel.Error, null);
                return multiplier;
            }

            //if the current collider gameobject is not a child of the armature we do not want to rescale it
            if (!localNewModelTrans.IsChildOf(newModelArmature))
            {
                LogInformation("GetCorrectScaleMultipler(): Current newModel gameobject [" + localNewModelTrans.gameObject.name +  "] on [" + newModel.name + "]  is not a child of the armature. Skipping check.", LogLevel.OutputLogOnly, null);
                return multiplier;
            }

            //get the magnitudes to compare scales
            float newModelMagnitude = newModelArmature.localScale.magnitude;
            float oldModelMagnitude = oldModelArmature.localScale.magnitude;
            float newModelTransMagnitude = localNewModelTrans.localScale.magnitude;
            float oldModelTransMagnitude = localoldModeltrans.gameObject.transform.localScale.magnitude;


            //first check that the armature scales match
            //This is a quick way of seeing if there are any import scale differences
            if (!Mathf.Approximately(oldModelMagnitude, newModelMagnitude))
            {
                multiplier *= oldModelMagnitude / newModelMagnitude;
                LogInformation("GetCorrectScaleMultipler(): Detected an import scale mismatch. \nScale multiplier that will be used (" + multiplier + ").", LogLevel.OutputLogOnly, null);
            }

            // now check if the scales on the dynamic bone collider gameobject match
            if (!Mathf.Approximately(oldModelTransMagnitude, newModelTransMagnitude))
            {
                multiplier *= oldModelTransMagnitude / newModelTransMagnitude;
                LogInformation("GetCorrectScaleMultipler(): Detected gameobject scale mismatch. \nScale multiplier that will be used (" + multiplier + ").", LogLevel.OutputLogOnly, null);
            }

            return multiplier;
        }
        #endregion

        #endregion

        #region Fix References

        void FixAllDBReferences()
        {
            SetCurrentCategory(SimpleOutputLogCategories.BeforeFix);
            AddOutputLogTitle("Fix All DB References Start", SimpleOutputLogCategories.FixReferencesRootAndExclusions);

            FixRootAndExclusionReferences();
            FixDynamicColliderReferences();
        }

        void RemoveAllEmptyReferences()
        {
            AddOutputLogTitle("Remove All Empty References On All DB Start", SimpleOutputLogCategories.BeforeCopy);
            if (!CheckForEmptyReferences())
            {
                LogInformation("No empty references were found on any dynamic bones.",LogLevel.Information, null);
                return;
            }
            RemoveEmptyColliderReferences();
            RemoveEmptyExclusionReferences();
            this.ShowNotification(new GUIContent("Removed All Empty References"));
        }

        private bool CheckForEmptyReferences()
        {
            if (!ParentGameObject)
            {
                LogInformation("ParentGameObject has not been assigned.", LogLevel.Error, null);
                return false;
            }

            bool hasEmpty = false;

            SetParentReferences();

            int emptyColliderCount = 0;
            int totalColliderReferencesSearched = 0;
            int emptyExclusionCount = 0;
            int totalExclusionReferencesSearched = 0;
            foreach (var dynamicBoneComponent in DynamicBoneComponents)
            {
                foreach (var dynamicBoneColliderBase in dynamicBoneComponent.m_Colliders)
                {
                    if (dynamicBoneColliderBase == null)
                    {
                        emptyColliderCount++;
                        hasEmpty = true;
                    }

                    totalColliderReferencesSearched++;
                }

                foreach (var mExclusion in dynamicBoneComponent.m_Exclusions)
                {
                    if (mExclusion == null)
                    {
                        emptyExclusionCount++;
                        hasEmpty = true;
                    }

                    totalExclusionReferencesSearched++;
                }
            }

            if (emptyColliderCount > 0)
            {
                LogInformation("Found (" + emptyColliderCount + " out of " + totalColliderReferencesSearched + ") to be empty dynamic bone collider references.", LogLevel.Information, null);
            }

            if (emptyExclusionCount > 0)
            {
                LogInformation("Found (" + emptyExclusionCount + " out of " + totalExclusionReferencesSearched + ") to be empty exclusion gameobject references.", LogLevel.Information, null);
            }

            // give the user the choice to proceed or cancel the removal of empty references
            if (enableRemoveEmptyReferencesDialogue && hasEmpty)
            {
                if (alwaysRemoveEmptyReferences)
                {
                    LogInformation("CheckForEmptyReferences(): Removing empty references automatically. \nAlways remove empty references (" + alwaysRemoveEmptyReferences + ")", LogLevel.Information, null);
                    return true;
                }
                else if (EditorUtility.DisplayDialog("Empty References Found", "[" + (emptyColliderCount + emptyExclusionCount) + " out of " + (totalColliderReferencesSearched + totalExclusionReferencesSearched) +  "] References have been found to be empty or missing on [" + ParentGameObject.name + "]. \nDo you want to remove these empty references?", "Remove Empty References", "Ignore and Continue"))
                {
                    return true;
                }

                LogInformation("CheckForEmptyReferences(): Skipping removal of empty references. \nUser chose to ignore.", LogLevel.Information, null);
                return false;
            }

            LogInformation("CheckForEmptyReferences(): Skipping fix. \nEnable remove empty references dialogue (" + enableRemoveEmptyReferencesDialogue + ")", LogLevel.Information, null);
            return false;
        }

        void RemoveEmptyColliderReferences()
        {
            AddOutputLogTitle("Remove Empty DB Collider References Start", SimpleOutputLogCategories.BeforeCopy);

            if (!ParentGameObject)
            {
                LogInformation("ParentGameObject has not been assigned.", LogLevel.Error, null);
                return;
            }

            // Grab any neccessary references
            SetParentReferences();

            int totalColliderCount = 0;
            foreach (var dynamicBoneComponent in DynamicBoneComponents)
            {
                List<DynamicBoneColliderBase> cleanedList = new List<DynamicBoneColliderBase>();
                foreach (var dynamicBoneColliderBase in dynamicBoneComponent.m_Colliders)
                {
                    if (dynamicBoneColliderBase != null)
                    {
                        cleanedList.Add(dynamicBoneColliderBase);
                        totalColliderCount++;
                    }
                }

                Undo.RegisterCompleteObjectUndo(dynamicBoneComponent, "Remove Empty Collider References");
                dynamicBoneComponent.m_Colliders = cleanedList;
            }

            LogInformation("Empty collider references have been removed. (" + totalColliderCount + ") have been successfully copied back.", LogLevel.Information, null);
        }

        void RemoveEmptyExclusionReferences()
        {
            AddOutputLogTitle("Removed Empty Exclusion References Start", SimpleOutputLogCategories.BeforeCopy);

            if (!ParentGameObject)
            {
                LogInformation("ParentGameObject has not been assigned.", LogLevel.Error, null);
                return;
            }

            // Grab any neccessary references
            SetParentReferences();

            int totalExclusionCount = 0;
            
            foreach (var dynamicBoneComponent in DynamicBoneComponents)
            {
                List<Transform> cleanedList = new List<Transform>();
                foreach (var mExclusion in dynamicBoneComponent.m_Exclusions)
                {
                    if (mExclusion != null)
                    {
                        cleanedList.Add(mExclusion);
                        totalExclusionCount++;
                    }
                }

                Undo.RegisterCompleteObjectUndo(dynamicBoneComponent, "Remove Empty Exclusion References");
                dynamicBoneComponent.m_Exclusions = cleanedList;
            }

            LogInformation("Empty exclusion references have been removed. (" + totalExclusionCount + ") have been successfully copied back.", LogLevel.Information, null);
        }

        /// <summary>
        /// Will look through any dynamic bone components and see if the root and exclusion references match gameObjects of the parent.
        /// </summary>
        private void FixRootAndExclusionReferences()
        {
            AddOutputLogTitle("Fix Root And Exclusions References Start", SimpleOutputLogCategories.FixReferencesRootAndExclusions);

            if (!ParentGameObject)
            {
                LogInformation("ParentGameObject has not been assigned.", LogLevel.Error, null);
                return;
            }

            // Grab any neccessary references
            SetParentReferences();

            if (DynamicBoneComponents.Length == 0)
            {
                LogInformation("No dynamic bone components were found on or childed to this parent object. \nPlease ensure the correct parent is assigned.", LogLevel.Error, null);
                return;
            }

            if (RootReferences.Length == 0)
            {
                LogInformation("No GameObject components were found on or childed to the on this parent object. \nPlease ensure the correct parent is assigned.", LogLevel.Error, null);
                return;
            }

            // Iterate over the DynamicBone Components.
            //These are the components that contain references to the Root and Exclusions that would be incorrect if an avatar is duplicated or a components is copied.
            int SuccessRootFix = 0;
            int SuccessExclusionFix = 0;
            int SkipCountRoot = 0;
            int SkipCountExclusion = 0;
            bool foundRootMatch = false;
            foreach (var dynamicBone in DynamicBoneComponents)
            {
                //Check that the root gameobject reference is correct for the parent
                if (dynamicBone.m_Root != null)
                {
                    //assign the correct root reference
                    for (int i = 0; i < RootReferences.Length; i++)
                    {
                        if (RootReferences[i].gameObject.name == dynamicBone.m_Root.name)
                        {
                            Undo.RecordObject(dynamicBone, "Correct DynamicBone Root reference.");
                            dynamicBone.m_Root = RootReferences[i];
                            foundRootMatch = true;
                            SuccessRootFix++;
                            break;
                        }
                    }
                }

                if(!foundRootMatch)
                {
                    SkipCountRoot++;
                    LogInformation("[" + dynamicBone.m_Root.name + "] gameobject reference could not be found for a dynamic bone component on [" + dynamicBone.gameObject.name + "]. \nSkipping root correction.", LogLevel.Error, null);
                }

                //iterate over any exclusion references if they exist
                if (dynamicBone.m_Exclusions.Count > 0)
                {
                    for (var index = 0; index < dynamicBone.m_Exclusions.Count; index++)
                    {
                        var dynamicBoneExclusion = dynamicBone.m_Exclusions[index];
                        if (!dynamicBoneExclusion)
                        {
                            LogInformation("An exclusion reference is missing in the DynamicBone root: " + dynamicBone.m_Root.gameObject.name +
                                           ".\nPlease assign an exclusion manually or remove the empty reference from the collider list.", LogLevel.Warning, null);
                            SkipCountExclusion++;
                            continue;
                        }

                        //assign the correct exclusion reference
                        bool foundAMatch = false;
                        for (int i = 0; i < RootReferences.Length; i++)
                        {
                            if (RootReferences[i].gameObject.name == dynamicBoneExclusion.gameObject.name)
                            {
                                Undo.RecordObject(dynamicBone, "Correct DynamicBoneExclusion reference.");
                                dynamicBone.m_Exclusions[index] = RootReferences[i];
                                SuccessExclusionFix++;
                                foundAMatch = true;
                                break;
                            }
                        }

                        if (!foundAMatch)
                        {
                            LogInformation("Could not find a matching GameObject for exclusion reference [" + dynamicBoneExclusion.gameObject.name + "]", LogLevel.Error, null);
                            SkipCountExclusion++;
                        }
                    }
                }
            }

            string logStringRoot = SkipCountRoot > 0 ? ParentGameObject.name + ": Corrected (" + SuccessRootFix + ") DynamicBone Root references. \nSkipped (" + SkipCountRoot + ") DynamicBone Root references." : ParentGameObject.name + ": Corrected (" + SuccessRootFix + ") DynamicBone Root references.";
            string logStringExclusion = SkipCountExclusion > 0 ? ParentGameObject.name + ": Corrected (" + SuccessExclusionFix + ") DynamicBone exclusion references. \n<color=red>Skipped</color> (" + SkipCountExclusion + ") DynamicBone Exclusion references." : ParentGameObject.name + ": Corrected (" + SuccessExclusionFix + ") DynamicBone exclusion references.";
            LogInformation(logStringRoot, SkipCountRoot > 0 ? LogLevel.Error : LogLevel.Information, null);
            LogInformation(logStringExclusion, SkipCountExclusion > 0 ? LogLevel.Error : LogLevel.Information, null);
        }

        /// <summary>
        /// Will look through any dynamic bone components and attempt to correct any dynamic bone collider references on duplicated or copied components.
        /// </summary>
        private void FixDynamicColliderReferences()
        {
            AddOutputLogTitle("Fix DynamicBoneCollider References Start", SimpleOutputLogCategories.FixReferencesDynamicBoneCollider);

            if (!ParentGameObject)
            {
                LogInformation("ParentGameObject has not been assigned.", LogLevel.Error, null);
                return;
            }

            // Grab any neccessary references
            SetParentReferences();

            if (DynamicBoneColliders.Length == 0)
            {
                LogInformation("No dynamic bone colliders were found on or childed to this parent object. \nPlease ensure the correct parent is assigned.", LogLevel.Error, null);
                return;
            }

            if (DynamicBoneComponents.Length == 0)
            {
                LogInformation("No dynamic bone components were found on or childed to the on this parent object. \nPlease ensure the correct parent is assigned.", LogLevel.Error, null);
                return;
            }

            // Iterate over the DynamicBone Components.
            //These are the components that contain references to the DynamicBoneColliders that would be incorrect if an avatar is duplicated or a components is copied.
            int SuccessCount = 0;
            int SkipCount = 0;
            foreach (var dynamicBone in DynamicBoneComponents)
            {
                //iterate over the colliders if any exist
                if (dynamicBone.m_Colliders.Count > 0)
                {
                    //correct the collider reference
                    //Iterate over each DynamicBoneCollider in this DynamicBone and assign the correct collider from our list of DynamicBoneColliders
                    for (var index = 0; index < dynamicBone.m_Colliders.Count; index++)
                    {
                        var dynamicBoneCollider = dynamicBone.m_Colliders[index];
                        if (!dynamicBoneCollider)
                        {
                            LogInformation("A collider reference is missing in the DynamicBone root: " + dynamicBone.m_Root.name +
                                ".\nPlease assign a collider manually or remove the empty reference from the collider list.", LogLevel.Warning, null);
                            SkipCount++;
                            continue;
                        }

                        //assign the correct collider
                        bool foundAMatch = false;
                        for (int i = 0; i < DynamicBoneColliders.Length; i++)
                        {
                            if (DynamicBoneColliders[i].gameObject.name == dynamicBoneCollider.gameObject.name)
                            {
                                Undo.RecordObject(dynamicBone, "Correct DynamicBoneCollider references.");
                                dynamicBone.m_Colliders[index] = DynamicBoneColliders[i];
                                //Debug.Log(dynamicBone.m_Colliders[index], dynamicBone.m_Colliders[index].gameObject);
                                SuccessCount++;
                                foundAMatch = true;
                                break;
                            }
                        }

                        if (!foundAMatch)
                        {
                            LogInformation("Could not find a matching DynamicBoneCollider for collider reference [" + dynamicBoneCollider.gameObject.name + "]", LogLevel.Error, null);
                            SkipCount++;
                        }
                    }
                }
            }

            string logString = SkipCount > 0 ? ParentGameObject.name + ": Corrected (" + SuccessCount + ") DynamicBoneCollider references. \n <color=red>Skipped</color> (" + SkipCount + ") DynamicBoneCollider references." : ParentGameObject.name + ": Corrected (" + SuccessCount + ") DynamicBoneCollider references.";
            LogInformation(logString, SkipCount > 0 ? LogLevel.Error : LogLevel.Information, null);

        }

        private void SetParentReferences()
        {
            RootReferences = ParentGameObject.GetComponentsInChildren<Transform>();
            DynamicBoneColliders = ParentGameObject.GetComponentsInChildren<DynamicBoneCollider>();
            DynamicBoneComponents = ParentGameObject.GetComponentsInChildren<DynamicBone>();
        }

        #endregion

        /// <summary>
        /// EDB Editor expected the armature root to be used, but ADB can use any root.
        /// This check is to both let the user know and to double check that the root is assigned for the best copy experience which would include references outside of the armature
        /// </summary>
        /// TODO - separate the replace action into a function
        private void CheckCopyReferencesAreRoot()
        {
            if (!enableRootGameObjectDialogue)
            {
                LogInformation("CheckCopyReferencesAreRoot(): Skipping due to user settings. \nEnable root transform checker (" + enableRootGameObjectDialogue + ")", LogLevel.OutputLogOnly, null);
                return;
            }

            if(oldModel != oldModel.transform.root.gameObject)
            {
                LogInformation("CheckCopyReferencesAreRoot(): oldModel reference is not the root gameobject", LogLevel.OutputLogOnly, null);

                if (alwaysUseRootGameObject)
                {
                    LogInformation("CheckCopyReferencesAreRoot(): Switched oldModel reference [" + oldModel.name + "] with the root gameobject [" + oldModel.transform.root.gameObject.name + "]. " +
                                   "\nAlways use root gameobject (" + alwaysUseRootGameObject + ")", LogLevel.OutputLogOnly, null);
                    oldModel = oldModel.transform.root.gameObject;
                }
                else if(EditorUtility.DisplayDialog("Old Model Reference Is Not Root GameObject", "ADBEditor allows the use of any gameobject as a reference but recommends that you use the root gameobject. " +
                                                                                                  "\n\nYou can disable this alert and or choose to automatically accept the changes from the settings tab. " +
                                                                                            "\n\nUse Root GameObject: Current reference [" + oldModel.name + "] will become [" + oldModel.transform.root.gameObject.name + "]" +
                                                                                            "\nIgnore: Current reference [" + oldModel.name + "] will remain the same.", "Use Root GameObject", "Ignore"))
                {
                    LogInformation("CheckCopyReferencesAreRoot(): Switched oldModel reference [" + oldModel.name +  "] with the root gameobject [" + oldModel.transform.root.gameObject.name + "]", LogLevel.OutputLogOnly, null);
                    oldModel = oldModel.transform.root.gameObject;
                }
                else
                {
                    LogInformation("CheckCopyReferencesAreRoot(): User has chosen to keep current oldModel reference as [" + oldModel.name + "].", LogLevel.OutputLogOnly, null);
                }

            }

            if (newModel != newModel.transform.root.gameObject)
            {
                LogInformation("CheckCopyReferencesAreRoot(): newModel reference is not the root gameobject", LogLevel.OutputLogOnly, null);

                if (alwaysUseRootGameObject)
                {
                    LogInformation("CheckCopyReferencesAreRoot(): Switched newModel reference [" + newModel.name + "] with the root gameobject [" + oldModel.transform.root.gameObject.name + "]. " +
                                   "\nAlways use root gameobject (" + alwaysUseRootGameObject + ")", LogLevel.OutputLogOnly, null);
                    newModel = newModel.transform.root.gameObject;
                }
                else if (EditorUtility.DisplayDialog("New Model Reference Is Not Root GameObject", "ADBEditor allows the use of any gameobject as a reference but recommends that you use the root gameobject. " +
                                                                                                   "\n\nYou can disable this alert and or choose to automatically accept the changes from the settings tab. " +
                                                                                             "\n\nUse Root GameObject: Current reference [" + newModel.name + "] will become [" + newModel.transform.root.gameObject.name + "]" +
                                                                                             "\nIgnore: Current reference [" + newModel.name + "] will remain the same.", "Use Root GameObject", "Ignore"))
                {
                    LogInformation("CheckCopyReferencesAreRoot(): Switched newModel reference [" + newModel.name + "] with the root gameobject [" + oldModel.transform.root.gameObject.name + "]", LogLevel.OutputLogOnly, null);
                    newModel = newModel.transform.root.gameObject;
                }
                else
                {
                    LogInformation("CheckCopyReferencesAreRoot(): User has chosen to keep current newModel reference as [" + newModel.name + "].", LogLevel.OutputLogOnly, null);
                }
            }
        }

        private void CheckFixReferenceIsRoot()
        {
            if (!enableRootGameObjectDialogue)
            {
                LogInformation("CheckFixReferenceIsRoot(): Skipping due to user settings. \nEnable root transform checker (" + enableRootGameObjectDialogue + ")", LogLevel.OutputLogOnly, null);
                return;
            }

            if (ParentGameObject != ParentGameObject.transform.root.gameObject)
            {
                LogInformation("CheckCopyReferencesAreRoot(): ParentGameObject reference is not the root gameobject", LogLevel.OutputLogOnly, null);

                if (alwaysUseRootGameObject)
                {
                    LogInformation("CheckFixReferenceIsRoot(): Switched ParentGameObject reference [" + ParentGameObject.name + "] with the root gameobject [" + ParentGameObject.transform.root.gameObject.name + "]. " +
                                   "\nAlways use root gameobject (" + alwaysUseRootGameObject + ")", LogLevel.OutputLogOnly, null);
                    ParentGameObject = ParentGameObject.transform.root.gameObject;
                }
                else if (EditorUtility.DisplayDialog("New Model Reference Is Not Root GameObject", "ADBEditor allows the use of any gameobject as a reference but recommends that you use the root gameobject. " +
                                                                                                   "\n\nYou can disable this alert and or choose to automatically accept the changes from the settings tab. " +
                                                                                                   "\n\nUse Root GameObject: Current reference [" + ParentGameObject.name + "] will become [" + ParentGameObject.transform.root.gameObject.name + "]" +
                                                                                                   "\nIgnore: Current reference [" + ParentGameObject.name + "] will remain the same.", "Use Root GameObject", "Ignore"))
                {
                    LogInformation("CheckFixReferenceIsRoot(): Switched ParentGameObject reference [" + ParentGameObject.name + "] with the root gameobject [" + ParentGameObject.transform.root.gameObject.name + "]", LogLevel.OutputLogOnly, null);
                    ParentGameObject = ParentGameObject.transform.root.gameObject;
                }
                else
                {
                    LogInformation("CheckFixReferenceIsRoot(): User has chosen to keep current ParentGameObject reference as [" + ParentGameObject.name + "].", LogLevel.OutputLogOnly, null);
                }
            }
        }

        // Will ask the user to save scenes or perform it automatically depending on user settings
        private bool AskUserToSaveScenes()
        {
            if (!enableSceneSaveDialogue)
            {
                LogInformation("AskUserToSaveScenes(): Skipping due to user settings. \nEnabled Scene Save Dialogue (" + enableSceneSaveDialogue +")", LogLevel.OutputLogOnly, null);
                return true;
            }

            if (!EditorApplication.isPlaying)
            {
                if (saveScenesAutomatically)
                {
                    if (EditorSceneManager.SaveOpenScenes())
                    {
                        LogInformation("AskUserToSaveScenes(): Saved all open scenes automatically. \nSave Scenes Automatically (" + saveScenesAutomatically + ")", LogLevel.OutputLogOnly, null);
                        return true;
                    }

                    LogInformation("Failed to save scenes", LogLevel.Error, null);
                    return false;
                }
                // ask the user to save any current work
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    this.ShowNotification(new GUIContent("Copy has been aborted."));
                    LogInformation("AskUserToSaveScenes(): User cancelled save. Exiting run", LogLevel.Information, null);
                    return false; // if the user cancels the save we exit out of the code
                }

                LogInformation("AskUserToSaveScenes(): Saved all currently modified scenes", LogLevel.OutputLogOnly, null);
                return true;
            }

            return true;
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label(versionNum, style, GUILayout.ExpandWidth(true));

            _toolBarInt = GUILayout.Toolbar(_toolBarInt, _toolbarStrings);

            switch (_toolBarInt)
            {
                case 0:
                    GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);
                    GUILayout.Label("Advanced Dynamic Bone Editor by All4thlulz.", EditorStyles.boldLabel);
                    GUILayout.Label("(Built From Mirai's Easy Dynamic Bones Paste)", EditorStyles.boldLabel);
                    GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);

                    _copyTabScrollPos = GUILayout.BeginScrollView(_copyTabScrollPos);

                    GUILayout.Label("Original Model:", GUILayout.Width(columnWidth));
                    oldModel = (GameObject)EditorGUILayout.ObjectField(oldModel, typeof(GameObject), true, GUILayout.Width(columnWidth));

                    GUILayout.Label("", GUILayout.Width(columnWidth));

                    GUILayout.Label("New Model:", GUILayout.Width(columnWidth));
                    newModel = (GameObject)EditorGUILayout.ObjectField(newModel, typeof(GameObject), true, GUILayout.Width(columnWidth));

                    GUILayout.Label("", GUILayout.Width(columnWidth));

                    EditorGUI.BeginDisabledGroup(!oldModel || !newModel);

                    if (GUILayout.Button("Advanced Copy"))
                    {
                        BeginOutputLog();
                        CheckCopyReferencesAreRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }

                        AddOutputLogTitle("ADBPaste Advanced Copy Start", SimpleOutputLogCategories.BeforeCopy);
                        CopyBothComponents();
                        this.ShowNotification(new GUIContent("Advanced Copy Complete. Check Results."));
                        EndOutputLog();
                    }

                    GUILayout.Label("", GUILayout.Width(columnWidth));
                    GUILayout.Label("Copy Components Separately", GUILayout.Width(columnWidth));

                    if (GUILayout.Button("Copy Dynamic Bone Colliders"))
                    {
                        BeginOutputLog();
                        CheckCopyReferencesAreRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }

                        CopyDynamicBoneCollidersOnly();
                        this.ShowNotification(new GUIContent("Copied Dynamic Bone Colliders"));
                        EndOutputLog();
                    }

                    if (GUILayout.Button("Copy Dynamic Bones"))
                    {
                        BeginOutputLog();
                        CheckCopyReferencesAreRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }

                        CopyDynamicBonesOnly();
                        this.ShowNotification(new GUIContent("Copied Dynamic Bones."));
                        EndOutputLog();
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Label("", GUILayout.Width(columnWidth));
                    GUILayout.EndScrollView();

                    ClosePumkinToolsUtility();
                    break;
                case 1:
                    GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);
                    GUILayout.Label("Fix References On Copied Avatar Components", EditorStyles.boldLabel);
                    GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);

                    _fixTabScrollPos = GUILayout.BeginScrollView(_fixTabScrollPos);

                    GUILayout.Label("Parent GameObject:", GUILayout.Width(columnWidth));
                    ParentGameObject = (GameObject)EditorGUILayout.ObjectField(ParentGameObject, typeof(GameObject), true, GUILayout.Width(columnWidth));

                    GUILayout.Label("", GUILayout.Width(columnWidth));

                    EditorGUI.BeginDisabledGroup(!ParentGameObject);
                    if (GUILayout.Button("Fix DynamicBone References"))
                    {
                        BeginOutputLog();
                        CheckFixReferenceIsRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }

                        FixAllDBReferences();
                        this.ShowNotification(new GUIContent("Fixed Dynamic Bone References"));
                        EndOutputLog();
                    }
                    if (GUILayout.Button("Remove All Empty References"))
                    {
                        BeginOutputLog();
                        CheckFixReferenceIsRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }
                        RemoveAllEmptyReferences();
                        this.ShowNotification(new GUIContent("Removed All Empty References"));
                        EndOutputLog();
                    }

                    GUILayout.Label("", GUILayout.Width(columnWidth));
                    GUILayout.Label("Fix References Separately", GUILayout.Width(columnWidth));

                    if (GUILayout.Button("Fix Root and Exclusion GameObject References"))
                    {
                        BeginOutputLog();
                        CheckFixReferenceIsRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }

                        FixRootAndExclusionReferences();
                        this.ShowNotification(new GUIContent("Fixed Root And Exclusion References"));
                        EndOutputLog();
                    }
                    if (GUILayout.Button("Fix Dynamic Collider References"))
                    {
                        BeginOutputLog();
                        CheckFixReferenceIsRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }
                        
                        FixDynamicColliderReferences();
                        this.ShowNotification(new GUIContent("Fixed Dynamic Bone Collider References"));
                        EndOutputLog();
                    }
                    if (GUILayout.Button("Remove Empty Collider References"))
                    {
                        BeginOutputLog();
                        CheckFixReferenceIsRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }

                        if (CheckForEmptyReferences())
                        {
                            RemoveEmptyColliderReferences();
                        }
                        this.ShowNotification(new GUIContent("Removed Empty Collider References"));
                        EndOutputLog();
                    }
                    if (GUILayout.Button("Remove Empty Exclusion References"))
                    {
                        BeginOutputLog();
                        CheckFixReferenceIsRoot();
                        if (!AskUserToSaveScenes())
                        {
                            return;
                        }

                        if (CheckForEmptyReferences())
                        {
                            RemoveEmptyExclusionReferences();
                        }
                        this.ShowNotification(new GUIContent("Removed Empty Exclusion References"));
                        EndOutputLog();
                    }
                    //if (GUILayout.Button("Fix Cloth Collider References"))
                    //{
                    //    throw new NotImplementedException();
                    //}
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Label("", GUILayout.Width(columnWidth));
                    GUILayout.EndScrollView();

                    ClosePumkinToolsUtility();
                    break;
                case 2:
                    GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);
                    GUILayout.Label("Various Options For Advanced DB Paste", EditorStyles.boldLabel);
                    GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);
                    GUILayout.Label("", GUILayout.Width(columnWidth));

                    EditorGUI.BeginChangeCheck();
                    _settingsTabScrollPos = GUILayout.BeginScrollView(_settingsTabScrollPos);

                    enableSceneSaveDialogue = EditorGUILayout.BeginToggleGroup("Enable Ask To Save Dialogue", enableSceneSaveDialogue);
                    EditorGUI.BeginDisabledGroup(!enableSceneSaveDialogue);
                    EditorGUI.indentLevel++;
                    saveScenesAutomatically = EditorGUILayout.ToggleLeft("Save All Open Scenes Automatically", saveScenesAutomatically);
                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndToggleGroup();

                    GUILayout.Space(3);
                    enableRootGameObjectDialogue = EditorGUILayout.BeginToggleGroup("Enable Reference Check For Root GameObject", enableRootGameObjectDialogue);
                    EditorGUI.BeginDisabledGroup(!enableRootGameObjectDialogue);
                    EditorGUI.indentLevel++;
                    alwaysUseRootGameObject = EditorGUILayout.ToggleLeft("Always Switch Current Reference For Root", alwaysUseRootGameObject);
                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndToggleGroup();

                    GUILayout.Space(3);
                    enableRemoveEmptyReferencesDialogue = EditorGUILayout.BeginToggleGroup("Enable Empty References Check", enableRemoveEmptyReferencesDialogue);
                    EditorGUI.BeginDisabledGroup(!enableRemoveEmptyReferencesDialogue);
                    EditorGUI.indentLevel++;
                    alwaysRemoveEmptyReferences = EditorGUILayout.ToggleLeft("Remove All Empty References Automatically", alwaysRemoveEmptyReferences);
                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndToggleGroup();

                    GUILayout.Space(3);
                    enableTransformMismatchCheck = EditorGUILayout.BeginToggleGroup("Enable Transform Mismatch Check", enableTransformMismatchCheck);
                    EditorGUI.BeginDisabledGroup(!enableTransformMismatchCheck);
                    EditorGUI.indentLevel++;
                    alwaysMatchScale = EditorGUILayout.ToggleLeft("Always Match Scale", alwaysMatchScale);
                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndToggleGroup();

                    GUILayout.Space(3);
                    enableConsoleDebugLogs = EditorGUILayout.BeginToggleGroup("Enable Console Debug Logs", enableConsoleDebugLogs);
                    EditorGUI.indentLevel++;
                    onlyShowWarningAndErrorLogs = EditorGUILayout.ToggleLeft("Only Show Warnings And Errors In Console", onlyShowWarningAndErrorLogs);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndToggleGroup();

                    showOutputLogAtEnd = EditorGUILayout.ToggleLeft("Open Output Log After Run", showOutputLogAtEnd);

                    GUILayout.Label("", GUILayout.Width(columnWidth));
                    GUILayout.EndScrollView();
                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveSettings();
                    }

                    EditorGUI.BeginDisabledGroup(allOutputLogStr == String.Empty);
                    if (GUILayout.Button("Open Outputlog"))
                    {
                        this.ShowNotification(new GUIContent("Open Output Log"));
                        OpenOutputLog();
                    }
                    GUILayout.Space(10);
                    EditorGUI.EndDisabledGroup();

                    ClosePumkinToolsUtility();
                    break;
                case 3:
#if EnablePumkinIntegration
                    /**************************************************************************/
                    Pumkin.PumkinsAvatarTools[] editors = (Pumkin.PumkinsAvatarTools[])Resources.FindObjectsOfTypeAll(typeof(Pumkin.PumkinsAvatarTools));
                    if (editors.Length > 0)
                    {
                        editors[0].OnGUI();
                    }
                    else
                    {
                        GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);
                        GUILayout.Label("This tab requires Pumkin Avatar Tools", EditorStyles.boldLabel);
                        GUILayout.Label("https://github.com/rurre/VRCAvatarTools", EditorStyles.boldLabel);
                        GUILayout.Label("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", EditorStyles.boldLabel);
                        GUILayout.Label("", GUILayout.Width(columnWidth));

                        if (GUILayout.Button("Open Temporary PumkinsAvatarTools Here"))
                        {
                            Type o = null;
                            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                foreach (var type in assembly.GetTypes())
                                {
                                    if (type.Name == "PumkinsAvatarTools")
                                    {
                                        o = type;
                                        break;
                                    }
                                }
                            }

                            if (o != null)
                            {
                                //PumkinsAvatarTools.ShowWindow();
                                pumkinUtilityWindow = EditorWindow.GetWindowWithRect<Pumkin.PumkinsAvatarTools>(new Rect(0,200,200,0),true, "ADBEditor Pumkin Utility",false);
                                pumkinUtilityOpened = true;
                                EditorWindow.FocusWindowIfItsOpen<ADBEditor>();
                            }
                            else
                            {
                                if (EditorUtility.DisplayDialog("Download Pumkin Avatar Tools",
                                    "Pumking avatar tools could not be found in this project. Please download and import the tool from the github page to utilise this feature.",
                                    "Open Github Page", "Cancel"))
                                {
                                    Application.OpenURL(pumkinDownload);
                                }
                                else
                                {
                                    this.ShowNotification(new GUIContent("Pumkims Avatar Tools Could Not Be Found"));
                                }
                            }
                        }

                        if (GUILayout.Button("Download Latest Pumkins Avatar Tools Release"))
                        {
                            if (EditorUtility.DisplayDialog("Download Pumkin Avatar Tools",
                                "Would you like to open the GitHub link: " + pumkinDownload + "?",
                                "Open Github Page", "Cancel"))
                            {
                                Application.OpenURL(pumkinDownload);
                            }
                        }

                    }
#else
                    _toolBarInt = 0;
#endif
                    break;
                default:
                    _toolBarInt = 0;
                    throw new NotImplementedException();
            }
        }


        private void ClosePumkinToolsUtility()
        {
#if EnablePumkinIntegration
            if (pumkinUtilityOpened)
            {
                if (pumkinUtilityWindow != null)
                {
                    pumkinUtilityWindow.Close();
                }
                pumkinUtilityOpened = false;
            }
#endif
        }


            #region Debugging
        private void LogInformation(String text, LogLevel logLevel, [CanBeNull] GameObject go)
        {
            switch ((int)logLevel)
            {
                case 0:
                    if (enableConsoleDebugLogs && !onlyShowWarningAndErrorLogs)
                    {
                        Debug.Log(text, go);
                    }

                    AddLogToOutputLog(GetStringWithoutNewLines(text), logLevel);
                    break;
                case 1:
                    if (enableConsoleDebugLogs)
                    {
                        Debug.LogWarning(text, go);
                    }
                    AddLogToOutputLog(GetStringWithoutNewLines(text), logLevel);
                    break;
                case 2:
                    if (enableConsoleDebugLogs)
                    {
                        Debug.LogError(text, go);
                    }
                    AddLogToOutputLog(GetStringWithoutNewLines(text), logLevel);
                    break;
                case 3:
                    string str = GetStringWithoutNewLines(text);
                    AddLogToOutputLog(str, LogLevel.OutputLogOnly);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private string GetStringWithoutNewLines(string text)
        {
            return text.Replace("\r", " ").Replace("\n", " ");
        }

        private void BeginOutputLog()
        {
            //set all strings to empty
            for (var i = 0; i < outputLogCategoryStrings.Length; i++)
            {
                outputLogCategoryStrings[i] = string.Empty;
                outputLogWithErrors[i] = 0;
            }

            SetCurrentCategory(SimpleOutputLogCategories.BeginLog);
            outputLogCount = 0;
            LogInformation(DateTime.Now + " Begin output log ADBP version " + versionNum + "... \n" + logTitleDivider, LogLevel.OutputLogOnly, null);
        }

        private void EndOutputLog()
        {
            SetCurrentCategory(SimpleOutputLogCategories.EndLog);
            AddOutputLogTitle("...output log end [" + DateTime.Now + "]", SimpleOutputLogCategories.EndLog);
            SaveSettings(); // save current outputlog to string pref
            if (showOutputLogAtEnd)
            {
                OpenOutputLog();
            }
        }

        private void AddOutputLogTitle(string text, SimpleOutputLogCategories category)
        {
            SetCurrentCategory(category);
            LogInformation(logTitleDivider, LogLevel.OutputLogOnly,  null);
            LogInformation(text, LogLevel.OutputLogOnly, null);
            LogInformation(logTitleDivider, LogLevel.OutputLogOnly, null);
        }

        private void AddLogToOutputLog(string text, LogLevel logLevel)
        {
            string prefix;
            switch ((int)logLevel)
            {
                case 0:
                    prefix = "\nLog: ";
                    break;
                case 1:
                    prefix = "\nWarning: ";
                    break;
                case 2:
                    prefix = "\nError: ";
                    break;
                case 3:
                    prefix = "\n";
                    break;
                default:
                    throw new NotImplementedException();
            }

            int index = (int)currentCategory;
            for (int i = 0; i < outputLogCategoryStrings.Length; i++)
            {
                if (i == index)
                {
                    outputLogCategoryStrings[i] += prefix + text;
                    if (logLevel == LogLevel.Error)
                    {
                        outputLogWithErrors[i]++;
                    }
                    outputLogCount++;
                    break;
                }
            }

            //add the current log to
            //allOutputLogStr = allOutputLogStr + prefix + text;
        }

        private void OpenOutputLog()
        {
            ADBOuputLogWindow.Init(outputLogCategoryStrings, outputLogWithErrors,  outputLogCount);
        }
            #endregion

            #region Simple EditorPrefs

        private void LoadSettings()
        {
            enableSceneSaveDialogue = EditorPrefs.GetBool(enableSceneSaveDialoguePrefKey, false);
            saveScenesAutomatically = EditorPrefs.GetBool(saveScenesAutomaticallyPrefKey, false);

            enableRootGameObjectDialogue = EditorPrefs.GetBool(enableRootGameObjectDialoguePrefKey, true);
            alwaysUseRootGameObject = EditorPrefs.GetBool(alwaysUseRootGameObjectPrefKey, false);

            enableRemoveEmptyReferencesDialogue = EditorPrefs.GetBool(enableRemoveEmptyReferencesDialoguePrefKey, true);
            alwaysRemoveEmptyReferences = EditorPrefs.GetBool(alwaysRemoveEmptyReferencesPrefKey, true);

            enableTransformMismatchCheck = EditorPrefs.GetBool(enableTransformMismatchCheckPrefKey, true);
            alwaysMatchScale = EditorPrefs.GetBool(alwaysMatchScalePrefKey, true);

            enableConsoleDebugLogs = EditorPrefs.GetBool(enableConsoleDebugLogsPrefKey, true);
            onlyShowWarningAndErrorLogs = EditorPrefs.GetBool(onlyShowWarningAndErrorLogsPrefKey, true);

            showOutputLogAtEnd = EditorPrefs.GetBool(showOutputLogAtEndPrefKey, true);

            outputLogCount = EditorPrefs.GetInt(outputLogCountPrefKey, 0);

            allOutputLogStr = EditorPrefs.GetString(lastOutputLogPrefKey, string.Empty);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetBool(enableSceneSaveDialoguePrefKey, enableSceneSaveDialogue);
            EditorPrefs.SetBool(saveScenesAutomaticallyPrefKey, saveScenesAutomatically);

            EditorPrefs.SetBool(enableRootGameObjectDialoguePrefKey, enableRootGameObjectDialogue);
            EditorPrefs.SetBool(alwaysUseRootGameObjectPrefKey, alwaysUseRootGameObject);

            EditorPrefs.SetBool(enableRemoveEmptyReferencesDialoguePrefKey, enableRemoveEmptyReferencesDialogue);
            EditorPrefs.SetBool(alwaysRemoveEmptyReferencesPrefKey, alwaysRemoveEmptyReferences);

            EditorPrefs.SetBool(enableTransformMismatchCheckPrefKey, enableTransformMismatchCheck);
            EditorPrefs.SetBool(alwaysMatchScalePrefKey, alwaysMatchScale);

            EditorPrefs.SetBool(enableConsoleDebugLogsPrefKey, enableConsoleDebugLogs);
            EditorPrefs.SetBool(onlyShowWarningAndErrorLogsPrefKey, onlyShowWarningAndErrorLogs);

            EditorPrefs.SetBool(showOutputLogAtEndPrefKey, showOutputLogAtEnd);

            EditorPrefs.SetInt(outputLogCountPrefKey, outputLogCount);

            EditorPrefs.SetString(lastOutputLogPrefKey, allOutputLogStr);
        }
            #endregion
    }
#endif
}