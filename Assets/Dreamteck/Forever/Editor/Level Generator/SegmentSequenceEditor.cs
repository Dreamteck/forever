namespace Dreamteck.Forever
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SegmentSequenceEditor
    {
        private EditorGUIEvents input = new EditorGUIEvents();
        public delegate void SequenceHandler(SegmentSequence[] sequences);
        public Object undoObject;
        public SegmentSequence[] sequences = new SegmentSequence[0];
        public Rect viewRect = new Rect();
        public Vector2 windowPosition = Vector2.zero;

        private SegmentDefinition[] segmentArray = null;
        private float thumbnailSize = 100f;
        private int thumbnailPadding = 5;
        private int dragIndex = -1;
        private Vector2 dragOffset = Vector2.zero;
        private Vector2 scroll = Vector2.zero;
        private SegmentDefinition changeDefinition = null;
        private bool renameSequence = false;
        private SegmentSequence editSequence = null;
        private Rect segmentPanelRect = new Rect();

        private static Texture2D transparentBlack = null;
        private static Texture2D buttonNormal = null;
        private static Texture2D buttonHover = null;
        private static GUIStyle dragBox, thumbButton, defaultBoxStyle, defaultButtonStyle, thumbnailLabel, headerToggle, customSequenceLabel;
        private bool guiInitialized = false;

        public delegate void EmptyHandler();
        public event EmptyHandler onWillChange;
        public event EmptyHandler onChanged;
        public event SequenceHandler onApplySequences;
        private bool changed = false;
        private SegmentSequenceSettingsWindow sequenceSettingsWindow = null;
        private List<SegmentSequence> sequenceAddress = new List<SegmentSequence>();

        public SegmentSequenceEditor(Object undoObject, SegmentSequenceCollection collection, Rect rect)
        {
            sequences = collection.sequences;
            this.undoObject = undoObject;
            viewRect = rect;
            guiInitialized = false;
            InitStaticObjects();
        }

        void OnSequenceSettingsWillChange()
        {
            if (onWillChange != null) onWillChange();
            if (onChanged != null) onChanged();
        }

        private void InitStaticObjects()
        {
            if (transparentBlack == null)
            {
                transparentBlack = new Texture2D(1, 1);
                transparentBlack.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
                transparentBlack.Apply();

                buttonNormal = new Texture2D(1, 1);
                buttonNormal.SetPixel(0, 0, DreamteckEditorGUI.lightColor);
                buttonNormal.Apply();

                buttonHover = new Texture2D(1, 1);
                buttonHover.SetPixel(0, 0, ForeverPrefs.highlightColor);
                buttonHover.Apply();
            }
            if (dragBox == null)
            {
                dragBox = new GUIStyle(GUI.skin.box);
                dragBox.fontSize = 40;
                dragBox.fontStyle = FontStyle.Bold;
                dragBox.alignment = TextAnchor.MiddleCenter;
            }
            if (thumbButton == null)
            {
                thumbButton = new GUIStyle(GUI.skin.button);
                thumbButton.fontSize = 9;
                thumbButton.padding = new RectOffset(1, 1, 1, 1);
                thumbButton.normal.background = transparentBlack;
                thumbButton.normal.textColor = Color.white;
                thumbButton.alignment = TextAnchor.MiddleCenter;
            }

            if(customSequenceLabel == null)
            {
                customSequenceLabel = new GUIStyle(GUI.skin.label);
                customSequenceLabel.fontSize = 20;
                Color col = customSequenceLabel.normal.textColor;
                col.a = 0.75f;
                customSequenceLabel.normal.textColor = col;
                customSequenceLabel.alignment = TextAnchor.MiddleCenter;
            }
        }

        public void DrawEditor()
        {
            input.Update();
            if (!guiInitialized)
            {
                defaultBoxStyle = new GUIStyle(GUI.skin.box);
                defaultBoxStyle.normal.background = DreamteckEditorGUI.blankImage;
                defaultBoxStyle.margin = new RectOffset(0, 0, 0, 0);
                defaultButtonStyle = new GUIStyle(GUI.skin.button);
                defaultButtonStyle.normal.background = buttonNormal;
                defaultButtonStyle.normal.textColor = DreamteckEditorGUI.iconColor;
                defaultButtonStyle.hover.background = buttonHover;
                defaultButtonStyle.hover.textColor = DreamteckEditorGUI.highlightContentColor;
                defaultButtonStyle.margin = new RectOffset(2, 2, 2, 2);
                thumbnailLabel = new GUIStyle(GUI.skin.label);
                thumbnailLabel.normal.textColor = DreamteckEditorGUI.lightColor;
                thumbnailLabel.fontSize = 10;
                thumbnailLabel.alignment = TextAnchor.MiddleCenter;
                headerToggle = new GUIStyle(GUI.skin.toggle);
                headerToggle.normal.textColor = Color.white;
                guiInitialized = true;
            }


            if(sequenceAddress.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("<", GUILayout.Width(35)))
                {
                    sequenceAddress.RemoveAt(sequenceAddress.Count - 1);
                }
                GUIContent content = new GUIContent(undoObject.name);
                float min = 0f, max = 0f;
                GUI.skin.label.CalcMinMaxWidth(content, out min, out max);
                GUILayout.Label(undoObject.name, GUILayout.Width(min));
                for (int i = 0; i < sequenceAddress.Count; i++)
                {
                    content = new GUIContent("/ " + sequenceAddress[i].name);
                    GUI.skin.label.CalcMinMaxWidth(content, out min, out max);
                    GUILayout.Label(content, GUILayout.Width(min));
                }
                EditorGUILayout.EndHorizontal();
            }
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(viewRect.width), GUILayout.Height(viewRect.height));
            if (sequenceAddress.Count == 0)
            {
                for (int i = 0; i < sequences.Length; i++)
                {
                    SequenceUI(sequences[i]);
                }

                if (GUILayout.Button("Add Sequence", GUILayout.Width(100), GUILayout.Height(50)))
                {
                    SegmentSequence[] newSequence = new SegmentSequence[sequences.Length + 1];
                    sequences.CopyTo(newSequence, 0);
                    newSequence[newSequence.Length - 1] = new SegmentSequence();
                    sequences = newSequence;
                    if (onApplySequences != null) onApplySequences(sequences);
                }
            } else SequenceUI(sequenceAddress[sequenceAddress.Count-1]);
            GUILayout.EndScrollView();

            if (changeDefinition != null && !changeDefinition.nested)
            {
                
                EditorGUI.DrawRect(segmentPanelRect, Color.white);
                GUILayout.BeginArea(segmentPanelRect);
                GameObject last = changeDefinition.prefab;
                last = (GameObject)EditorGUILayout.ObjectField(last, typeof(GameObject), false);
                if (last != changeDefinition.prefab)
                {
                    if (onWillChange != null) onWillChange();
                    changeDefinition.prefab = last;
                    changed = true;
                }
                GUILayout.EndArea();
                if (input.mouseLeftDown && !segmentPanelRect.Contains(Event.current.mousePosition)) changeDefinition = null;
                else if(input.mouseLeftDown) input.Use();
            }

            if (dragIndex >= 0)
            {
                changed = true;
                if (input.mouseLeftUp)
                {
                    dragIndex = -1;
                    segmentArray = null;
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.75f);
                    float dragSize = thumbnailSize * 0.75f; 
                    DrawDefinition(new Rect(Event.current.mousePosition.x - dragSize * 0.5f + dragOffset.x, Event.current.mousePosition.y - dragSize * 0.5f + dragOffset.y, dragSize, dragSize), editSequence.segments[dragIndex]);
                    GUI.color = Color.white;
                    if(dragOffset.magnitude > 0.05f)
                    {
                        dragOffset = Vector2.Lerp(dragOffset, Vector2.zero, 0.1f);
                        changed = true;
                    }
                }
            }
        }

        int GetSequenceIndex(SegmentSequence sequence)
        {
            for (int i = 0; i < sequences.Length; i++)
            {
                if (sequences[i] == sequence) return i;
            }
            return -1;
        }

        void DuplicateSequence(object obj)
        {
            Undo.RecordObject(undoObject, "Duplicate Sequence");
            SegmentSequence sequence = (SegmentSequence)obj;
            int index = GetSequenceIndex(sequence);
            SegmentSequence newSequence = sequence.Duplicate();
            Dreamteck.ArrayUtility.Insert(ref sequences, index, newSequence);
            if (onApplySequences != null) onApplySequences(sequences);
        }

        void MoveSequenceUp(object obj)
        {
            Undo.RecordObject(undoObject, "Move Sequence Up");
            SegmentSequence sequence = (SegmentSequence)obj;
            int index = GetSequenceIndex(sequence);
            sequences[index] = sequences[index - 1];
            sequences[index - 1] = sequence;
        }

        void MoveSequenceDown(object obj)
        {
            Undo.RecordObject(undoObject, "Move Sequence Down");
            SegmentSequence sequence = (SegmentSequence)obj;
            int index = GetSequenceIndex(sequence);
            sequences[index] = sequences[index + 1];
            sequences[index + 1] = sequence;
        }

        void RemoveSequence(object obj)
        {
            SegmentSequence sequence = (SegmentSequence)obj;
            int index = GetSequenceIndex(sequence);
            Undo.RecordObject(undoObject, "Remove sequence");
            SegmentSequence[] newSequences = new SegmentSequence[sequences.Length - 1];
            for (int i = 0; i < sequences.Length; i++)
            {
                if (i < index) newSequences[i] = sequences[i];
                else if (i == index) continue;
                else newSequences[i - 1] = sequences[i];
            }
            sequences = newSequences;
            if (onApplySequences != null) onApplySequences(sequences);
        }

        void SetRename(object obj)
        {
            editSequence = (SegmentSequence)obj;
            renameSequence = true;
        }

        void CreateNestedSequence(object obj)
        {
            SegmentSequence sequence = (SegmentSequence)obj;
            SegmentDefinition definition = new SegmentDefinition("Nested Sequence");
            AddDefinition(sequence, definition);
        }

        void SequenceUI(SegmentSequence sequence)
        {
            //Toolbar
            Vector2 thumbSize = Vector2.one * thumbnailSize;
            if (sequence.type == SegmentSequence.Type.RandomByChance) thumbSize.y += 16f;
            float width = viewRect.width;
            float totalThumbWidth = thumbSize.x + thumbnailPadding;
            int elementsPerRow = Mathf.FloorToInt(width / totalThumbWidth);
            int rows = Mathf.CeilToInt((float)sequence.segments.Length / elementsPerRow);
            if (rows == 0) rows = 1;
            int row = 0, col = 0;
            float height = rows * (thumbSize.y + thumbnailPadding) + thumbnailPadding;

            #region Header
            GUI.backgroundColor = ForeverPrefs.highlightColor * DreamteckEditorGUI.lightColor;
            EditorGUILayout.BeginHorizontal(defaultBoxStyle, GUILayout.Width(width));
            GUI.backgroundColor = Color.white;
            if (sequenceAddress.Count == 0)
            {
                sequence.enabled = EditorGUILayout.Toggle(sequence.enabled, headerToggle, GUILayout.Width(20));
            }
            if (renameSequence && editSequence == sequence)
            {
                if (onWillChange != null) onWillChange();
                sequence.name = GUILayout.TextField(sequence.name);
                if(input.enterDown) renameSequence = false;
                if (input.mouseLeftDown && input.mouseRightDown)
                {
                    if (!GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) renameSequence = false;
                }
            }
            else
            {
                if (sequence.name == "") sequence.name = "Sequence";
                string nameText = sequence.name;
                if (!sequence.isCustom) nameText += " - " + sequence.type.ToString();
                GUILayout.Label(nameText);
            }

            if (GUILayout.Button("Settings", GUILayout.Width(60)))
            {
                if (sequenceSettingsWindow == null)
                {
                    sequenceSettingsWindow = EditorWindow.GetWindow<SegmentSequenceSettingsWindow>(true);
                    sequenceSettingsWindow.onWillChange += OnSequenceSettingsWillChange;
                }
                sequenceSettingsWindow.Init(sequence, this);
            }

            EditorGUILayout.EndVertical();
            Rect headerRect = GUILayoutUtility.GetLastRect();
            if(headerRect.Contains(Event.current.mousePosition))
            {
                if (input.mouseRightDown)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Move Up"), false, MoveSequenceUp, sequence);
                    menu.AddItem(new GUIContent("Move Down"), false, MoveSequenceDown, sequence);
                    menu.AddItem(new GUIContent("Rename"), false, SetRename, sequence);
                    menu.AddItem(new GUIContent("Duplicate"), false, DuplicateSequence, sequence);
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete"), false, RemoveSequence, sequence);
                    menu.ShowAsContext();
                    input.Use();
                }
            }
            #endregion

            //Sequence canvas
            GUI.backgroundColor = DreamteckEditorGUI.lightColor;
            if(sequence.isCustom) GUILayout.Box("", defaultBoxStyle, GUILayout.Width(width), GUILayout.Height(thumbSize.y + thumbnailPadding * 2f));
            else GUILayout.Box("", defaultBoxStyle, GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = Color.white;
            Rect groupRect = GUILayoutUtility.GetLastRect();
            if(sequence.type == SegmentSequence.Type.RandomByChance && sequence.randomizer == null)
            {
                EditorGUILayout.HelpBox("Missing Randomizer. Go to the Sequence Settings and assign a Randomizer object.", MessageType.Error);
            }
            GUI.BeginGroup(groupRect);

            if (sequence.isCustom)
            {
                GUI.Label(new Rect(0f, groupRect.height / 2f - 30f, groupRect.width, 50), "Custom Sequence", customSequenceLabel);
                CustomSequence customSequence = sequence.customSequence;
                customSequence = (CustomSequence)EditorGUI.ObjectField(new Rect(groupRect.width / 2f - 100, groupRect.height / 2f  + 15, 200, 16), customSequence, typeof(CustomSequence), false);
                if(customSequence != sequence.customSequence)
                {
                    if (onWillChange != null) onWillChange();
                    sequence.customSequence = customSequence;
                }
                GUI.EndGroup();
                EditorGUILayout.Space();
                return;
               
            }

            #region Drag Reorder
            int dragTarget = -1;
            float closestDist = float.MaxValue;
            Vector2 closestCenter = Vector2.zero;
            if (dragIndex >= 0 && editSequence == sequence)
            {
                for (int i = 0; i < sequence.segments.Length; i++)
                {
                    Vector2 thumbCenter = new Vector2(thumbnailPadding + col * totalThumbWidth + thumbnailSize / 2, (thumbnailSize + thumbnailPadding) * row + thumbnailPadding + thumbnailSize / 2);
                    float dist = Vector2.Distance(thumbCenter, Event.current.mousePosition);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        dragTarget = i;
                        closestCenter = thumbCenter;
                    }
                    col++;
                    if (col >= elementsPerRow)
                    {
                        col = 0;
                        row++;
                    }
                }
                
                if(dragTarget >= 0 && dragTarget != dragIndex)
                {
                    bool before = Event.current.mousePosition.x < closestCenter.x;
                    int side = before ? -1 : 1;
                    EditorGUI.DrawRect(new Rect(closestCenter.x + side * thumbnailSize / 2 -thumbnailPadding, closestCenter.y - thumbnailSize / 2, thumbnailPadding*2, thumbnailSize), ForeverPrefs.highlightColor);
                    if (input.mouseLeftUp)
                    {
                        if (dragTarget < 0) dragTarget = 0;
                        ReorderSegments(ref sequence.segments, dragIndex, dragTarget);
                        dragIndex = -1;
                    }
                }

            }
            #endregion

            #region Draw Definitions
            col = row = 0;
            for (int i = 0; i < sequence.segments.Length; i++)
            {
                Rect thumbRect = new Rect(thumbnailPadding + col * totalThumbWidth, (thumbSize.y + thumbnailPadding) * row + thumbnailPadding, thumbSize.x, thumbSize.y);
                if (segmentArray != sequence.segments || dragIndex != i)
                {
                    if (!sequence.segments[i].nested && sequence.segments[i].prefab == null) //dont remove in future, just show as NULL in the window
                    {
                        RemoveSegment(ref sequence.segments, i);
                        i--;
                        continue;
                    }
                    if (DefinitionUI(new Vector2(thumbRect.x, thumbRect.y), sequence.segments[i], i))
                    {
                        if (input.mouseLeftDown && !renameSequence)
                        {
                            dragIndex = i;
                            dragOffset = new Vector2(thumbRect.x + thumbnailSize / 2f, thumbRect.y + thumbnailSize / 2f) - Event.current.mousePosition;
                            editSequence = sequence;
                            segmentArray = sequence.segments;
                            input.Use();
                        } else if (input.mouseRightDown)
                        {
                            editSequence = sequence;
                            if (sequence.segments[i].nested)
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Edit"), false, EnterNestedSequence, sequence.segments[i]);
                                menu.AddItem(new GUIContent("Remove"), false, RemoveSegment, i);
                                menu.ShowAsContext();
                                input.Use();
                            } else
                            {
                                int index = i;
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Change"), false, SetChangeSegment, sequence.segments[i]);
                                menu.AddItem(new GUIContent("Select in Project"), false, SelectDefinitionPrefab, sequence.segments[i]);
                                menu.AddItem(new GUIContent("Duplicate"), false, delegate {
                                    Undo.RecordObject(undoObject, "Duplicate Segment");
                                   ArrayUtility.Insert(ref sequence.segments, index, sequence.segments[index].Duplicate());
                                    changed = true;
                                });
                                menu.AddItem(new GUIContent("Remove"), false, RemoveSegment, i);
                                menu.ShowAsContext();
                                input.Use();
                            }

                            input.Use();
                        }
                    }
                    if(sequence.type == SegmentSequence.Type.RandomByChance)
                    {
                        float chance = sequence.segments[i].randomPickChance;
                        chance = GUI.HorizontalSlider(new Rect(thumbRect.x, thumbRect.y + thumbSize.y - 16f, thumbSize.x, 16f), chance, 0f, 1f);
                        GUI.Label(new Rect(thumbRect.x, thumbRect.y + thumbSize.y - 32f, thumbSize.x, 16f), Mathf.RoundToInt(chance * 100) + "% Chance", thumbnailLabel);
                        if (chance != sequence.segments[i].randomPickChance)
                        {
                            onWillChange();
                            sequence.segments[i].randomPickChance = chance;
                        }
                    }
                }
                col++;
                if (col >= elementsPerRow)
                {
                    col = 0;
                    row++;
                }
            }
            #endregion
            GUI.EndGroup();

            Rect canvasRect = GUILayoutUtility.GetLastRect();
            if (canvasRect.Contains(Event.current.mousePosition) && input.mouseRightDown){
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("New Nested Sequence"), false, CreateNestedSequence, sequence);
                menu.ShowAsContext();
            }

            SegmentDefinition newSegment = null;
            List<GameObject> newObj = SegmentField(GUILayoutUtility.GetLastRect());

            for (int i = 0; i < newObj.Count; i++)
            {
                if (newObj[i] != null)
                {
                    newSegment = new SegmentDefinition(newObj[i]);
                }
                if (newSegment != null && newSegment.prefab != null)
                {
                    if (onWillChange != null)
                    {
                        onWillChange();
                    }
                    AddDefinition(sequence, newSegment);
                    changed = true;
                }
            }

           
            if (changed)
            {
                changed = false;
                if (onChanged != null)
                {
                    onChanged();
                }
            }
            EditorGUILayout.Space();
        }

        void AddDefinition(SegmentSequence sequence, SegmentDefinition segment)
        {
            SegmentDefinition[] newSegments = new SegmentDefinition[sequence.segments.Length + 1];
            sequence.segments.CopyTo(newSegments, 0);
            newSegments[newSegments.Length - 1] = segment;
            sequence.segments = newSegments;
        }

        void ReorderSegments(ref SegmentDefinition[] segments, int from, int to)
        {
            if (from == to) return;
            if (onWillChange != null) onWillChange();
            SegmentDefinition segment = segments[from];
            if (from < to)
            {
                for (int i = from; i < to; i++) segments[i] = segments[i + 1];
            } else
            {
                for (int i = from; i > to; i--) segments[i] = segments[i - 1];
            }
            segments[to] = segment;
            changed = true;
        }

        void InsertSegment(ref SegmentDefinition[] segments, int index, SegmentDefinition insert)
        {
            if (onWillChange != null) onWillChange();
            SegmentDefinition[] newSegments = new SegmentDefinition[segments.Length + 1];
            for (int i = 0; i < newSegments.Length; i++)
            {
                if (i < index) newSegments[i] = segments[i];
                else if (i == index) newSegments[i] = insert;
                else newSegments[i] = segments[i - 1];
            }
            segments = newSegments;
            changed = true;
        }

        void RemoveSegment(object obj)
        {
            int index = (int)obj;
            if (editSequence.segments[index].nested && editSequence.segments[index].nestedSequence.segments.Length > 0 && !EditorUtility.DisplayDialog("Remove segment", "Are you sure you want to delete this nested sequence? This will delete all segments inside as well.", "Yes", "No")) return;
            RemoveSegment(ref editSequence.segments, index);
        }

        void SetChangeSegment(object obj)
        {
            changeDefinition = (SegmentDefinition)obj;
            float horizontalOffset = 0f;
            if (input.mousPos.x + 200 > Screen.width) horizontalOffset = -200;
            segmentPanelRect = new Rect(input.mousPos.x + horizontalOffset, input.mousPos.y - 30, 200, 30);
        }

        void SelectDefinitionPrefab(object obj)
        {
            SegmentDefinition definition = (SegmentDefinition)obj;
            Selection.activeGameObject = definition.prefab;
        }

        void RemoveSegment(ref SegmentDefinition[] segments, int index)
        {
            if (onWillChange != null) onWillChange();
            SegmentDefinition[] newSegments = new SegmentDefinition[segments.Length - 1];
            for (int i = 0; i < newSegments.Length; i++)
            {
                if (i < index) newSegments[i] = segments[i];
                else newSegments[i] = segments[i + 1];
            }
            segments = newSegments;
            changed = true;
        }

        void EnterNestedSequence(object obj)
        {
            SegmentDefinition definition = (SegmentDefinition)obj;
            if (!definition.nested) return;
            sequenceAddress.Add(definition.nestedSequence);
        }

        bool DefinitionUI(Vector2 position, SegmentDefinition definition, int index)
        {
            Rect rect = new Rect(position.x, position.y, thumbnailSize, thumbnailSize);
            DrawDefinition(rect, definition);
            return rect.Contains(Event.current.mousePosition);
        }

        void DrawDefinition(Rect rect, SegmentDefinition definition)
        {
            GUI.BeginGroup(rect);
            Rect localRect = new Rect(0, 0, rect.width, rect.height);
            if (definition.nested)
            {
                EditorGUI.DrawRect(localRect, DreamteckEditorGUI.lightDarkColor);
                GUI.Label(new Rect(3, 3, localRect.width - 6, 14), definition.nestedSequence.name, thumbnailLabel);
                if (definition.nestedSequence.segments.Length == 0)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    GUI.Label(new Rect(3, localRect.height / 2 - 5, localRect.width - 6, 14), "EMPTY", thumbnailLabel);
                    GUI.color = Color.white;
                } else
                {
                    Rect container = new Rect(thumbnailPadding / 2, 14 + thumbnailPadding / 2, localRect.width - thumbnailPadding, localRect.height - 14 - thumbnailPadding);
                    Vector2 ts = new Vector2(container.width/2-4, container.height / 2 - 4);
                    int maxCount = Mathf.Min(definition.nestedSequence.segments.Length, 4);
                    int col = 0, row = 0;
                    for (int i = 0; i < maxCount; i++)
                    {
                        if (definition.nestedSequence.segments[i].nested) continue;
                        Rect imgRect = new Rect(4 + col * (ts.x + 4), 14 + thumbnailPadding + row * (ts.y + 4), ts.x, ts.y);
                        Texture2D thumbnail = AssetPreview.GetAssetPreview(definition.nestedSequence.segments[i].prefab);
                        if (thumbnail != null) GUI.DrawTexture(imgRect, thumbnail);
                        col++;
                        if(col > 1)
                        {
                            col = 0;
                            row++;
                        }
                    }
                }
            }
            else
            {
                Texture2D thumbnail = AssetPreview.GetAssetPreview(definition.prefab);
                EditorGUI.DrawRect(localRect, DreamteckEditorGUI.lightDarkColor);
                if (thumbnail != null) GUI.DrawTexture(localRect, thumbnail);
                Color prevColor = GUI.color;
                GUI.color = Color.clear;
                GUI.Box(localRect, new GUIContent("", definition.prefab.name));
                GUI.color = prevColor;
                GUI.Label(new Rect(3, 3, localRect.width - 6, 14), definition.prefab.name, thumbnailLabel);
            }
            GUI.EndGroup();
        }

        List<GameObject> SegmentField(Rect rect)
        {
            List<GameObject> objList = new List<GameObject>();
            if (rect.Contains(Event.current.mousePosition))
            {
                Object[] obj = new Object[0];
                bool hasDrag = GetDraggedObjects(ref obj);
                if (hasDrag)
                {
                    for (int i = 0; i < obj.Length; i++)
                    {
                        if (obj[i] != null && obj[i] is GameObject && ((GameObject)obj[i]).GetComponent<LevelSegment>())
                        {
                            objList.Add((GameObject)obj[i]);
                        }
                    }
                }
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    GUI.Box(rect, "+", dragBox);
                    GUI.color = Color.white;
                    obj = objList.ToArray();
                }
            }
            return objList;
        }

        public static bool GetDraggedObjects(ref Object[] dragged)
        {
            if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (Event.current.type == EventType.DragPerform)
                {
                    dragged = DragAndDrop.objectReferences;
                    DragAndDrop.AcceptDrag();
                }
            }
            return DragAndDrop.objectReferences.Length > 0;
        }
    }
}
