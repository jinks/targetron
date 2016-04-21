using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Targetron
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Targetron : MonoBehaviour
    {
        public static GameObject GameObjectInstance;
        private static PluginConfiguration config;
        private const String VERSION = "1.4.4";
        private readonly int WINDOWID_GUI = GUIUtility.GetControlID(7225, FocusType.Passive);
        private readonly int WINDOWID_TOOLTIP = GUIUtility.GetControlID(7226, FocusType.Passive);
        private readonly int WINDOWID_CONTEXT = GUIUtility.GetControlID(7227, FocusType.Passive);

        //Window limits
        private const int minWindowWidth = 255;
        private const int maxWindowWidth = 600;
        private const int minWindowHeight = 76;
        private const int maxWindowHeight = 600;

        //Load image files
        private static readonly string root = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        private readonly WWW img1 = new WWW("file://" + root + "GameData/Targetron/Icons/target.png");
        private readonly WWW img2 = new WWW("file://" + root + "GameData/Targetron/Icons/rocket.png");
        private readonly WWW img3 = new WWW("file://" + root + "GameData/Targetron/Icons/nw-resize.png");
        private readonly WWW img4 = new WWW("file://" + root + "GameData/Targetron/Icons/sw-resize.png");

        private readonly WWW img5 = new WWW("file://" + root + "GameData/Targetron/Icons/name_asc.png");
        private readonly WWW img6 = new WWW("file://" + root + "GameData/Targetron/Icons/name_desc.png");
        private readonly WWW img7 = new WWW("file://" + root + "GameData/Targetron/Icons/dist_asc.png");
        private readonly WWW img8 = new WWW("file://" + root + "GameData/Targetron/Icons/dist_desc.png");

        //Textures for image files
        private static readonly Texture2D buttonTarget = new Texture2D(16, 16);
        private static readonly Texture2D buttonRocket = new Texture2D(16, 16);
        private static readonly Texture2D cursorResizeNW = new Texture2D(32, 32);
        private static readonly Texture2D cursorResizeSW = new Texture2D(32, 32);

        private static readonly Texture2D buttonNameAsc = new Texture2D(16, 16);
        private static readonly Texture2D buttonNameDesc = new Texture2D(16, 16);
        private static readonly Texture2D buttonDistAsc = new Texture2D(16, 16);
        private static readonly Texture2D buttonDistDesc = new Texture2D(16, 16);

        //Filters
        private static List<Filter> filters = new List<Filter>(10);
        private static List<VesselType> vesselTypes = new List<VesselType>(10);

        //GUI Styles
        private static readonly Texture2D contextBGN = new Texture2D(1, 1);
        private static readonly Texture2D contextBGA = new Texture2D(1, 1);
        private static readonly Texture2D contextBGH = new Texture2D(1, 1);
        private static GUIStyle contextStyle;
        private static GUIStyle contextStyle2;
        private static GUIStyle buttonStyle;
        private static GUIStyle buttonStyle2;
        private static GUIStyle buttonStyle3;
        private static GUIStyle centeredStyle;
        private static GUIStyle leftStyle;
        private static GUIStyle rightStyle;
        private static GUIStyle tooltipStyle;
        private static GUIStyle expandStyle;
        private static GUIStyle rowStyle;

        //Colors
        private static Color enabledColor = Color.white;
        private static Color enabledBGColor = Color.white;
        private static readonly Color disabledColor = Color.gray;
        private static readonly Color disabledBGColor = Color.gray;

        private static String tooltip = string.Empty;
        private static Rect tooltipPos = new Rect(0, 0, 0, 0);  //Window position and size
        private static Target contextActive;    //Vessel that was right clicked
        private static Target lcontextActive;    //Last vessel that was right clicked
        private static Rect contextPos = new Rect(0, 0, windowWidth, windowHeight);  //Window position and size
        private static ModuleDockingNode activeDockingNode; //Vessel that is being hovered over in context menu
        private static ModuleDockingNode lastActiveDockingNode; //Vessel that is being hovered over in context menu

        private const int windowWidth = 350; //Window width
        private const int windowHeight = 194; //Window height
        private static Rect pos = new Rect(0, 0, windowWidth, windowHeight);  //Window position and size
        private static Vector2 scrollPosition = Vector2.zero;  //Scroll Position
        private static int top;    //Measures height of target list
        private static long lastChecked = -10000000;   //Ticks at last target check
        private static float lastHeight = windowHeight;    //Expanded height for use when window is collapsed
        private static float lastWidth = windowWidth;    //Expanded height for use when window is collapsed
        private static bool expand = true;    //Toggle state for window
        private static bool toggleOn = true;    //Show/hide state for window
        private static int sortMode;    //0 = Distance Ascending, 1 = Distance Descending, 2 = Name Ascending, 3 = Name Descending
        private static bool inFlight;    //Toggle state for window
        private static String searchStr = string.Empty;   //Current search string
        private static int filterRC = -1;    //Last filter icon that was right clicked
        private static bool filterRCval;    //Last filter icon that was right clicked

        //For (optional) integration with Blizzy's Toolbar Plugin
        private IButton ToolbarButton;

        private static List<Target> targets = new List<Target>();  //List of available vessels
        private static Rect originalWindow;   //Original window position/size
        private static bool handleClicked;         //Bottom right resize handle active?
        private static bool handleClicked2;        //Bottom left resize handle active?
        private static bool resetCursor = true;
        private static Vector3 clickedPosition;    //Position where resize handle was originally clicked

        public void Awake()
        {
            DontDestroyOnLoad(this);

            //Load button and cursor textures
            img1.LoadImageIntoTexture(buttonTarget);
            img2.LoadImageIntoTexture(buttonRocket);
            img3.LoadImageIntoTexture(cursorResizeNW);
            img4.LoadImageIntoTexture(cursorResizeSW);

            img5.LoadImageIntoTexture(buttonNameAsc);
            img6.LoadImageIntoTexture(buttonNameDesc);
            img7.LoadImageIntoTexture(buttonDistAsc);
            img8.LoadImageIntoTexture(buttonDistDesc);

            //Load the texture and data structure to store toggle state
            if (filters.Count == 0)
            {
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/asteroid.png"), VesselType.SpaceObject, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/flag.png"), VesselType.Flag, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/eva.png"), VesselType.EVA, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/base.png"), VesselType.Base, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/station.png"), VesselType.Station, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/ship.png"), VesselType.Ship, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/lander.png"), VesselType.Lander, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/rover.png"), VesselType.Rover, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/probe.png"), VesselType.Probe, true));
                filters.Add(new Filter(new WWW("file://" + root + "GameData/Targetron/Icons/debris.png"), VesselType.Debris, true));
            }

            //Create a separate List of vessel types that are being filtered. This ensures the Debris/Other catches any undefined vessel types.
            if (vesselTypes.Count != 0) return;
            for (int i = 0; i < filters.Count; i++)
                vesselTypes.Add(filters[i].Type);
        }

        public void Start()
        {
            config = PluginConfiguration.CreateForType<Targetron>();
            config.load();
            //Load position and collapse state from config file
            pos = config.GetValue("pos", new Rect(Screen.width - windowWidth - 10, Screen.height / 3.0f - windowHeight / 2.0f, windowWidth, windowHeight));
            expand = config.GetValue("expand", true);
            toggleOn = config.GetValue("toggleOn", true);
            sortMode = config.GetValue("sortMode", 0);

            for (int i = 0; i < filters.Count; i++)
                filters[i].Enabled = config.GetValue("filter" + i, true);

            //Make sure width and height are within limits
            pos.width = Mathf.Clamp(pos.width, minWindowWidth, maxWindowWidth);
            pos.height = Mathf.Clamp(pos.height, minWindowHeight, maxWindowHeight);

            if (!ToolbarManager.ToolbarAvailable) return;
            ToolbarButton = ToolbarManager.Instance.add("Targetron", "tgbutton");
            ToolbarButton.Text = "Targetron " + VERSION;
            ToolbarButton.ToolTip = "Targetron " + VERSION;
            ToolbarButton.TexturePath = "Targetron/Icons/targetron";
            ToolbarButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            ToolbarButton.OnClick += e => toggleOn = !toggleOn;
            GameEvents.onVesselChange.Add(saveConfig);
        }

        private void saveConfig(Vessel data)
        {
            if (!expand)
                expandWindow();

            config.SetValue("version", VERSION);
            config.SetValue("pos", pos);
            config.SetValue("expand", expand);
            config.SetValue("toggleOn", toggleOn);
            for (int i = 0; i < filters.Count; i++)
                config.SetValue("filter" + i, filters[i].Enabled);
            config.SetValue("sortMode", sortMode);
            config.save();

            if (!expand)
                collapseWindow();
        }

        //Collapses the window
        private void collapseWindow()
        {
            if (!(pos.height > 20)) return;
            lastHeight = pos.height;
            lastWidth = pos.width;
            pos.height = 20;
            pos.width = leftStyle.CalcSize(new GUIContent("Targetron v" + VERSION)).x + 30;
            if (pos.x > (Screen.width - lastWidth) / 2.0f)
                pos.x += lastWidth - pos.width;
        }

        //Expands the window
        private void expandWindow()
        {
            if (pos.height != 20) return;
            pos.height = lastHeight;
            if (pos.x > (Screen.width - pos.width) / 2.0f)
                pos.x -= lastWidth - pos.width;
            pos.width = lastWidth;
        }

        public void OnGUI()
        {
            OnDraw();
        }

        public void Update()
        {
            if (FlightGlobals.fetch != null && FlightGlobals.fetch.vessels != null && FlightGlobals.fetch.activeVessel != null)  //Check if in flight
            {
                if (!inFlight)
                    inFlight = true;

                //Update the list of nearby vessels every 1s
                if (expand && toggleOn && DateTime.Now.Ticks > lastChecked + 10000000)
                {
                    lastChecked = DateTime.Now.Ticks;    //Save the list check time
                    //targets.Clear();    //Clear the target vessel list
                    bool found;
                    foreach (Vessel vessel in FlightGlobals.fetch.vessels)  //Iterate through each available vessel
                    {
                        if (!vessel.Equals(FlightGlobals.fetch.activeVessel))
                        {
                            found = false;
                            foreach (Target t in targets)
                            {
                                if (vessel.GetInstanceID() == t.vessel.GetInstanceID())
                                {
                                    found = true;
                                    t.update();
                                    break;
                                }
                            }
                            if (!found)
                                targets.Add(new Target(vessel, null, 0));    //Add the vessel to the target vessel list, if it is not the active ship
                        }
                    }
                    //foreach (Target t in targets)
                    //{
                    //    if (!FlightGlobals.fetch.vessels.Contains(t.vessel) || t.vessel.Equals(FlightGlobals.fetch.activeVessel))
                    //        targets.Remove(t);
                    //}
                    targets = targets.Where(target => FlightGlobals.fetch.vessels.Contains(target.vessel) && !target.vessel.Equals(FlightGlobals.fetch.activeVessel)).ToList();
                    switch (sortMode)
                    {
                        case 0:
                            targets.Sort(sortByDistanceA);   //Sort target vessels by their distance from the active ship
                            break;
                        case 1:
                            targets.Sort(sortByDistanceD);   //Sort target vessels by their distance from the active ship
                            break;
                        case 2:
                            targets.Sort(sortByDistanceA);
                            targets.Sort((x, y) => String.CompareOrdinal(x.vessel.GetName(), y.vessel.GetName()));
                            break;
                        case 3:
                            targets.Sort(sortByDistanceA);
                            targets.Sort((y, x) => String.CompareOrdinal(x.vessel.GetName(), y.vessel.GetName()));
                            break;
                    }

                    //Find available docks
                    List<uint> dockerIDs = new List<uint>();
                    foreach (Target t in targets)
                    {
                        t.availableDocks.Clear();
                        if (!t.vessel.loaded) continue;
                        //Iterate twice, first to get IDs of docked nodes, then to add other ports to list of available
                        for (int i = 0; i < 2; i++)
                        {
                            foreach (Part p in t.vessel.parts)  //Iterate through all parts
                            {
                                foreach (PartModule m in p.Modules) //Iterate through all modules of each part
                                {
                                    if (!m.ClassName.Equals("ModuleDockingNode")) continue;
                                    ModuleDockingNode d = (ModuleDockingNode)m; //Create specific instance to acccess docking node details
                                    if (i == 0) //First loop iteration, compile docked port IDs
                                    {
                                        if (d.state.ToLower().Contains("docked"))   //Port is a "docker"
                                        {
                                            dockerIDs.Add(d.part.flightID); //ID of current part
                                            dockerIDs.Add(d.dockedPartUId); //ID of docked part
                                        }
                                    }
                                    else    //Second iteration, add undocked modules to list of available
                                    {
                                        if (!dockerIDs.Contains(d.part.flightID))
                                            t.availableDocks.Add(d);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else if (inFlight)
            {
                inFlight = false;
                saveConfig();
                contextActive = null;
                filterRC = -1;
            }


            //Close the context menu on left click anywhere outside of it
            if (contextActive != null && Input.GetMouseButton(0) && !contextPos.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                contextActive = null;

            if (filterRC >= 0 && (Input.GetMouseButton(0) || Input.GetMouseButton(1)) && !pos.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                filterRC = -1;

            //Close the context menu on right click when in IVA
            if (contextActive != null && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal) && Input.GetMouseButton(1))
                contextActive = null;
        }

        private void saveConfig()
        {
            if (!expand)
                expandWindow();

            config.SetValue("version", VERSION);
            config.SetValue("pos", pos);
            config.SetValue("expand", expand);
            config.SetValue("toggleOn", toggleOn);
            for (int i = 0; i < filters.Count; i++)
                config.SetValue("filter" + i, filters[i].Enabled);
            config.SetValue("sortMode", sortMode);
            config.save();

            if (!expand)
                collapseWindow();
        }

        private void OnDraw()
        {
            if (!toggleOn || !inFlight) return;
            // Get the current skin for later restore
            GUISkin defSkin = GUI.skin;
            // Set the ksp skin
            GUI.skin = HighLogic.Skin;
            if (buttonStyle == null)
            {
                initStyles();

                //Collapse the window, if necessary
                if (!expand)
                    collapseWindow();
            }

            //Make sure the window isn't dragged off screen
            if (pos.x < 0)
                pos.x = 0;
            if (pos.y < 0)
                pos.y = 0;
            if (pos.x > Screen.width - pos.width)
                pos.x = Screen.width - pos.width;
            if (pos.y > Screen.height - pos.height)
                pos.y = Screen.height - pos.height;

            //Display the window
            pos = GUI.Window(WINDOWID_GUI, pos, drawTargeter, string.Empty);

            if (contextActive != null)
            {
                if (!contextActive.Equals(lcontextActive))
                {
                    String longestText = "Control Vessel";
                    //if (!contextActive.vessel.loaded)
                    //    longestText = "Queue Vessel Rename";
                    int numDocks = 0;
                    foreach (ModuleDockingNode m in contextActive.availableDocks)
                    {
                        numDocks++;
                        if (("Target " + m.part.partInfo.title).Length > longestText.Length)
                            longestText = "Target " + m.part.partInfo.title;
                    }

                    Vector2 textSize = contextStyle.CalcSize(new GUIContent(longestText));
                    float left = Input.mousePosition.x - 25;
                    float _top = Screen.height - Input.mousePosition.y + 10;
                    if (left + textSize.x + 5 > Screen.width)
                        left = Screen.width - textSize.x - 5;
                    if (left < 0)
                        left = 0;
                    if (_top + (numDocks + 3) * Mathf.RoundToInt(contextStyle.fixedHeight) > Screen.height)
                        _top = Screen.height - Mathf.RoundToInt(contextStyle.fixedHeight) - numDocks * Mathf.RoundToInt(contextStyle.fixedHeight);
                    if (_top < 0)
                        _top = 0;
                    contextPos = new Rect(left, _top, textSize.x + 5, (numDocks + 3) * Mathf.RoundToInt(contextStyle.fixedHeight));
                    lcontextActive = contextActive;
                }
                contextPos = GUI.Window(WINDOWID_CONTEXT, contextPos, drawContext, string.Empty);
                GUI.BringWindowToFront(WINDOWID_CONTEXT);
            }
            else
                lcontextActive = null;

            //Display the tooltip, if necessary
            if (contextActive == null && tooltip != string.Empty)
            {
                Vector2 textSize = tooltipStyle.CalcSize(new GUIContent(tooltip));
                float left = Input.mousePosition.x + 5;
                float _top = Screen.height - Input.mousePosition.y - 35;
                if (left + textSize.x + 10 > Screen.width)
                    left = Screen.width - textSize.x - 10;
                if (_top < 0)
                    _top = 0;
                tooltipPos = new Rect(left, _top, textSize.x + 10, textSize.y - 3);
                tooltipPos = GUI.Window(WINDOWID_TOOLTIP, tooltipPos, drawTooltip, string.Empty);
                GUI.BringWindowToFront(WINDOWID_TOOLTIP);
            }

            //Highlight target red if docking node
            /*if (FlightGlobals.fetch.VesselTarget is ModuleDockingNode)
                {
                    ModuleDockingNode d = (ModuleDockingNode)FlightGlobals.fetch.VesselTarget; //Create specific instance to acccess docking node details
                    d.part.SetHighlightColor(Color.red);
                    d.part.SetHighlight(true);
                }
                else if (contextActive != null && activeDockingNode != null && activeDockingNode.part.highlightType == Part.HighlightType.Disabled)
                    activeDockingNode.part.SetHighlight(true);
                
                if (lastActiveDockingNode != null && (contextActive == null || activeDockingNode == null || !lastActiveDockingNode.Equals(activeDockingNode)))
                {
                    lastActiveDockingNode.part.SetHighlight(false);
                    lastActiveDockingNode = null;
                }*/
            // Restore the skin so we don't interfere others plugins skins?
            GUI.skin = defSkin;
        }

        //Draw the tooltip window contents
        private void drawTooltip(int windowID)
        {
            tooltipStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f); //White
            GUI.Label(new Rect(5, 0, tooltipPos.width - 5, tooltipPos.height), tooltip, tooltipStyle);
        }

        //Draw the right click context menu window contents
        private void drawContext(int windowID)
        {
            int _top = 0;
            if (GUI.Button(new Rect(0, _top, contextPos.width, 20), "Target Vessel", contextStyle))
            {
                if (contextActive != null) FlightGlobals.fetch.SetVesselTarget(contextActive.vessel);
                contextActive = null;
            }
            _top += Mathf.RoundToInt(contextStyle.fixedHeight);
            if (GUI.Button(new Rect(0, _top, contextPos.width, 20), "Control Vessel", contextStyle))
            {
                saveConfig();
                //GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                if (contextActive != null) FlightGlobals.SetActiveVessel(contextActive.vessel);
                contextActive = null;
            }
            _top += Mathf.RoundToInt(contextStyle.fixedHeight);
            const string renameStr = "Rename Vessel";
            String renameHint = string.Empty;
            if (contextActive != null && !contextActive.vessel.loaded)
            {
                //renameStr = "Queue Vessel Rename";
                renameHint = "Too far to rename";
                GUI.enabled = false;
            }
            if (GUI.Button(new Rect(0, _top, contextPos.width, 20), new GUIContent(renameStr, renameHint), contextStyle))
            {
                if (contextActive != null) contextActive.vessel.RenameVessel();
                contextActive = null;
            }
            GUI.enabled = true;

            _top += Mathf.RoundToInt(contextStyle.fixedHeight);
            if (contextActive == null) return;
            lastActiveDockingNode = activeDockingNode;
            activeDockingNode = null;
            foreach (ModuleDockingNode m in contextActive.availableDocks)
            {
                if (Vector3.Distance(FlightGlobals.ActiveVessel.GetTransform().position, m.GetTransform().position) >= 196.0f)
                    GUI.enabled = false;
                GUIStyle temp = contextStyle;
                if (FlightGlobals.fetch.VesselTarget != null && FlightGlobals.fetch.VesselTarget.Equals(m))
                    temp = contextStyle2;

                if (new Rect(0, _top, contextPos.width, 20).Contains(Event.current.mousePosition))
                {
                    m.part.SetHighlightColor(Color.red);
                    m.part.SetHighlight(true, false);
                }
                else
                {
                    m.part.SetHighlightDefault();
                    m.part.SetHighlight(false, false);
                }

                if (GUI.Button(new Rect(0, _top, contextPos.width, 20), "Target " + m.part.partInfo.title, temp))
                {
                    FlightGlobals.fetch.SetVesselTarget(m);
                    m.part.SetHighlightDefault();
                    m.part.SetHighlight(false, false);
                    contextActive = null;
                }
                GUI.enabled = true;
                _top += Mathf.RoundToInt(contextStyle.fixedHeight);
            }
        }

        private void OnApplicationQuit()
        {
            //Save the expanded postion and collapse state
            saveConfig();
        }

        private void OnDestroy()
        {
            //Save the expanded postion and collapse state
            saveConfig();
            if (ToolbarButton != null) ToolbarButton.Destroy();
            GameEvents.onVesselChange.Remove(saveConfig);
        }

        //Sorts Vessels by their distance from the active vessel
        private static int sortByDistance(Target a, Target b)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.fetch.activeVessel == null || FlightGlobals.fetch.activeVessel.transform == null)
                return 0;
            if (a.vessel == null && b.vessel == null)
                return 0;
            if (a.vessel == null && b.vessel != null)
                return 1;
            if (a.vessel != null && b.vessel == null)
                return -1;
            if (a.vessel != null && (a.vessel.orbit != null && FlightGlobals.fetch.activeVessel.orbit.referenceBody == a.vessel.orbit.referenceBody))
            {
                if (b.vessel != null && (b.vessel.orbit == null || a.vessel.orbit.referenceBody != b.vessel.orbit.referenceBody))
                    return -1;
            }
            if (b.vessel != null && (b.vessel.orbit == null ||
                                     FlightGlobals.fetch.activeVessel.orbit.referenceBody != b.vessel.orbit.referenceBody))
                return a.lastDistance.CompareTo(b.lastDistance);
            if (b.vessel != null && (a.vessel != null && (a.vessel.orbit == null || a.vessel.orbit.referenceBody != b.vessel.orbit.referenceBody)))
                return 1;
            return a.lastDistance.CompareTo(b.lastDistance);
        }
        private static int sortByDistanceA(Target a, Target b)
        {
            return sortByDistance(a, b);
        }
        private static int sortByDistanceD(Target a, Target b)
        {
            return sortByDistance(b, a);
        }

        //Draw function for the window
        private void drawTargeter(int windowID)
        {
            int i, j;
            //Draw the search box first so it keeps its focus
            if (expand)
            {
                GUIStyle textFieldStyle = GUI.skin.GetStyle("TextField");
                textFieldStyle.padding = new RectOffset(3, 3, 1, 1);
                searchStr = GUI.TextField(new Rect(6, pos.height - 24, pos.width - 13 - 23 * filters.Count, 18), searchStr, textFieldStyle);
            }

            //Display the current target
            String curTarg = "Target: ";
            if (FlightGlobals.fetch != null && FlightGlobals.fetch.VesselTarget != null)    //Is there an active target?
                curTarg += FlightGlobals.fetch.VesselTarget.GetName();
            else
                curTarg += "(None)";

            //Display only title if collapsed
            float titleWidth = pos.width - 22 - 20;
            if (!expand)
            {
                curTarg = "Targetron v" + VERSION;
                titleWidth = pos.width - 30;
            }

            leftStyle.normal.textColor = new Color(1.0f, 0.858f, 0.0f); //Orange
            GUI.Label(new Rect(6, 0, titleWidth, 24), curTarg, leftStyle);

            if (expand)
            {
                GUIContent temp = null;
                switch (sortMode)
                {
                    case 0:
                        temp = new GUIContent(buttonDistAsc, "Sorting By Distance (Ascending)");
                        break;
                    case 1:
                        temp = new GUIContent(buttonDistDesc, "Sorting By Distance (Descending)");
                        break;
                    case 2:
                        temp = new GUIContent(buttonNameAsc, "Sorting By Name (Ascending)");
                        break;
                    case 3:
                        temp = new GUIContent(buttonNameDesc, "Sorting By Name (Descending)");
                        break;
                }

                if (GUI.Button(new Rect(pos.width - 22 - 20, 1, 20, 20), temp, buttonStyle))
                {
                    if (Event.current.button == 1)
                    {
                        sortMode--;
                        if (sortMode < 0)
                            sortMode = 3;
                    }
                    else
                    {
                        sortMode++;
                        if (sortMode > 3)
                            sortMode = 0;
                    }
                    switch (sortMode)
                    {
                        case 0:
                            targets.Sort(sortByDistanceA);   //Sort target vessels by their distance from the active ship
                            break;
                        case 1:
                            targets.Sort(sortByDistanceD);   //Sort target vessels by their distance from the active ship
                            break;
                        case 2:
                            targets.Sort(sortByDistanceA);
                            targets.Sort((x, y) => String.CompareOrdinal(x.vessel.GetName(), y.vessel.GetName()));
                            break;
                        case 3:
                            targets.Sort(sortByDistanceA);
                            targets.Sort((y, x) => String.CompareOrdinal(x.vessel.GetName(), y.vessel.GetName()));
                            break;
                    }
                }
            }

            //Display toggle for collapsing window
            String tooltipText = "Minimize";
            if (!expand)
                tooltipText = "Maximize";
            expand = GUI.Toggle(new Rect(pos.width - 20, 4, 20, 20), expand, new GUIContent(string.Empty, tooltipText), expandStyle);

            if (expand) //If window is expanded
            {
                if (pos.height == 20)
                    expandWindow(); //Expand window if it has not already been done

                //Display the scroll view for target list
                scrollPosition = GUI.BeginScrollView(new Rect(4, 24, pos.width - 8, pos.height - 27 - 22), scrollPosition, new Rect(1, 16, pos.width - 33, top > pos.height - 27 - 22 ? top : pos.height - 27 - 22), false, true);
                top = 0;

                int vesselFilter;
                float diff;
                foreach (Target target in targets)  //Iterate through each target vessel
                {
                    bool showVessel = true;

                    if (target == null || target.vessel == null || target.vessel.transform == null)
                        showVessel = false;
                    else if (searchStr.Length > 0 && !target.vessel.GetName().ToLower().Contains(searchStr.ToLower()))
                        showVessel = false;
                    else
                    {
                        for (i = 0; i < filters.Count; i++)
                        {
                            if (filters[i].matchType(target.vessel.vesselType, vesselTypes) && !filters[i].Enabled)
                                showVessel = false;
                        }
                    }
                    if (!showVessel) continue;
                    vesselFilter = 0;
                    for (i = 0; i < filters.Count; i++)
                    {
                        if (target.vessel.vesselType == filters[i].Type)
                            vesselFilter = i;
                    }
                    GUILayout.BeginHorizontal(new GUIContent(filters[vesselFilter].Texture), rowStyle, GUILayout.Width(pos.width - 33), GUILayout.Height(24));
                    GUILayout.Space(22);
                    //If the vessel name is too large to display, add ellipsis (...) and cut to length
                    try
                    {
                        String _name = target.vessel.GetName();
                        diff = pos.width - 55 - rightStyle.CalcSize(new GUIContent("(" + formatDistance(target.lastDistance) + ")")).x - 42;
                        if (leftStyle.CalcSize(new GUIContent(_name)).x - 2 >= diff - 2)
                        {
                            _name = _name.Substring(0, _name.Length - 1);
                            for (j = 0; j < target.vessel.GetName().Length; j++)
                            {
                                _name = _name.Substring(0, _name.Length - 1);
                                if (leftStyle.CalcSize(new GUIContent(_name + "...")).x + 2 <= diff - 2)
                                    break;
                            }
                            _name += "...";
                        }

                        //Set the text color
                        if (FlightGlobals.fetch.VesselTarget != null && FlightGlobals.fetch.VesselTarget.Equals(target.vessel))
                            leftStyle.normal.textColor = new Color(1.0f, 0.858f, 0.0f); //Orange for current target
                        else if (target.vessel.isCommandable)
                            leftStyle.normal.textColor = new Color(0.718f, 0.996f, 0.0f);   //Green if vessel can be controlled
                        else
                            leftStyle.normal.textColor = new Color(0.756f, 0.756f, 0.756f); //Gray for debris
                        rightStyle.normal.textColor = leftStyle.normal.textColor;   //Match distance color to name

                        //Display the vessel name
                        GUILayout.Label(_name, leftStyle, GUILayout.Width(diff), GUILayout.Height(24));

                        //Context opens on right click unless in IVA mode
                        int contextBtn = 1;
                        if (Camera.current != null && Camera.current.name != null && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal))
                            contextBtn = 0;

                        //Check if vessel name is right clicked (normal) or left clicked (IVA)
                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        lastRect.x -= 23;
                        lastRect.width = pos.width - 70;
                        if (lastRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseUp && Event.current.button == contextBtn)
                            contextActive = target;

                        //Display the distance
                        GUILayout.Label("(" + formatDistance(target.lastDistance) + ")", rightStyle, GUILayout.ExpandWidth(false));

                        //Add button to set vessel as target
                        if (GUILayout.Button(new GUIContent(buttonTarget, "Set as Target"), buttonStyle))
                            FlightGlobals.fetch.SetVesselTarget(target.vessel);

                        //Add button to control vessel
                        if (GUILayout.Button(new GUIContent(buttonRocket, "Control Vessel"), buttonStyle))
                        {
                            saveConfig();
                            //GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                            FlightGlobals.SetActiveVessel(target.vessel);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Targetron Error: Failed to draw vessel listing\n" + e.StackTrace);
                    }
                    GUILayout.EndHorizontal();
                    top += 24;
                }
                // For testing purposes
                /* for (int i = 0; i < 50; i++)
                 {
                     GUILayout.BeginHorizontal(GUILayout.Width(pos.width - 41), GUILayout.Height(24));
                     String name = "Totally sweet, but long name " + i*i*i;
                     float diff = pos.width - 41 - rightStyle.CalcSize(new GUIContent("(" + formatDistance(1024 * i) + ")")).x - 50;
                     if (leftStyle.CalcSize(new GUIContent(name)).x > diff)
                     {
                         for (int j = 0; j < name.Length - 2; j++)
                         {
                             name = name.Substring(0, name.Length - 1);
                             if (leftStyle.CalcSize(new GUIContent(name + "...")).x <= diff)
                                 break;
                         }
                         name += "...";
                     }

                     GUILayout.Label(name, leftStyle, GUILayout.Width(diff));
                     GUILayout.Label("(" + formatDistance(1024 * i) + ")", rightStyle, GUILayout.ExpandWidth(false));
                     if (GUILayout.Button(new GUIContent(buttonTarget, "Set as Target"), buttonStyle))
                     {
                     }
                     if (GUILayout.Button(new GUIContent(buttonRocket, "Control Vessel"), buttonStyle))
                     {
                     }
                     GUILayout.EndHorizontal();
                     top += 24;
                 }*/
                GUI.EndScrollView();

                //Draw each filter button, graying it out if disabled
                for (i = 0; i < filters.Count; i++)
                {
                    if (filters[i].Enabled)
                    {
                        GUI.contentColor = enabledColor;
                        GUI.backgroundColor = enabledBGColor;
                    }
                    else
                    {
                        GUI.contentColor = disabledColor;
                        GUI.backgroundColor = disabledBGColor;
                    }
                    if (
                        !GUI.Button(new Rect(pos.width - 28 - 23 * i, pos.height - 25, 24, 24),
                            new GUIContent(filters[i].Texture, filters[i].getName()), buttonStyle2)) continue;
                    filters[i].toggle();
                    if (Event.current.button == 1)
                    {
                        filterRC = i;
                        filterRCval = filters[i].Enabled;
                    }
                    else
                    {
                        if (filterRC >= 0)
                        {
                            if (i > filterRC)
                            {
                                for (j = filterRC + 1; j <= i; j++)
                                    filters[j].Enabled = filterRCval;
                            }
                            else
                            {
                                for (j = i; j < filterRC; j++)
                                    filters[j].Enabled = filterRCval;
                            }
                        }
                        filterRC = -1;
                    }
                }
                GUI.contentColor = enabledColor;
                GUI.backgroundColor = enabledBGColor;

                //Establish resize handles at bottom left and bottom right of the screen
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;    // Convert to GUI coords
                Rect windowHandle = new Rect(pos.x + pos.width - 4, pos.y + pos.height - 4, 4, 4);  //Bottom right handle
                Rect windowHandle2 = new Rect(pos.x, pos.y + pos.height - 6, 6, 6);     //Bottom left handle

                if (!handleClicked && !handleClicked2 && windowHandle.Contains(mousePos))    //Check if mouse is within bottom right handle
                {
                    //Set the cursor to NW resize
                    Cursor.SetCursor(cursorResizeNW, new Vector2(16, 16), CursorMode.Auto);
                    resetCursor = true;
                    if (Input.GetMouseButtonDown(0))    //Check if left mouse button is pressed
                    {
                        handleClicked = true;   //Set flag for bottom right handle
                        clickedPosition = mousePos; //Save click position
                        originalWindow = pos;   //Save original window position/size
                    }
                }
                else if (!handleClicked && !handleClicked2 && windowHandle2.Contains(mousePos))  //Check if mouse is within bottom left handle
                {
                    //Set the cursor to SW resize
                    Cursor.SetCursor(cursorResizeSW, new Vector2(16, 16), CursorMode.Auto);
                    resetCursor = true;
                    if (Input.GetMouseButtonDown(0))     //Check if left mouse button is pressed
                    {
                        handleClicked2 = true;  //Set flag for bottom left handle
                        clickedPosition = mousePos; //Save click position
                        originalWindow = pos;   //Save original window position/size
                    }
                }
                else if (!handleClicked && !handleClicked2 && resetCursor)
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);  //Reset the cursor
                    resetCursor = false;
                }

                if ((handleClicked || handleClicked2))    //If either resize handle is active
                {
                    // Resize window by dragging
                    if (Input.GetMouseButton(0))
                    {
                        if (handleClicked)  //Bottom right handle
                        {
                            pos.width = Mathf.Clamp(originalWindow.width + (mousePos.x - clickedPosition.x), minWindowWidth, maxWindowWidth);
                            pos.height = Mathf.Clamp(originalWindow.height + (mousePos.y - clickedPosition.y), minWindowHeight, maxWindowHeight);
                        }
                        else    //Bottom left handle
                        {
                            pos.width = Mathf.Clamp(originalWindow.width + (clickedPosition.x - mousePos.x), minWindowWidth, maxWindowWidth);
                            pos.x = originalWindow.x - pos.width + originalWindow.width;
                            pos.height = Mathf.Clamp(originalWindow.height + (mousePos.y - clickedPosition.y), minWindowHeight, maxWindowHeight);
                        }
                    }

                    // Finish resizing window
                    if (Input.GetMouseButtonUp(0))
                    {
                        handleClicked = false;
                        handleClicked2 = false;
                    }
                }
            }
            else if (pos.height > 20)   //Collapse flag set, check if collapse is needed
                collapseWindow();   //Collapse the window

            tooltip = GUI.tooltip;

            //Make window draggable with left mouse button
            if (!handleClicked && !handleClicked2 && !Input.GetMouseButton(1))
                GUI.DragWindow();
        }

        //Formats a distance in meters to a more meaningful measurement
        private String formatDistance(float distance)
        {
            if (distance < 1000f)
                return distance.ToString("N1") + " m";
            if (distance < 10000f)
                return distance.ToString("N0") + " m";
            if (distance < 1000000f)
                return (distance / 1000f).ToString("N1") + " km";
            if (distance < 10000000f)
                return (distance / 1000f).ToString("N0") + " km";
            if (distance < 1000000000f)
                return (distance / 1000000f).ToString("N1") + " Mm";
            if (distance < 10000000000f)
                return (distance / 1000000f).ToString("N0") + " Mm";
            if (distance < 1000000000000f)
                return (distance / 1000000000f).ToString("N1") + " Gm";
            if (distance < 10000000000000f)
                return (distance / 1000000000f).ToString("N0") + " Gm";
            if (distance < 1000000000000000f)
                return (distance / 1000000000000f).ToString("N1") + " Tm";
            if (distance < 10000000000000000f)
                return (distance / 1000000000000f).ToString("N0") + " Tm";
            if (distance < 1000000000000000000f)
                return (distance / 1000000000000000).ToString("N1") + " Pm";
            return (distance / 1000000000000000).ToString("N0") + " Pm";
        }

        public void initStyles()
        {
            //Initialize styles
            buttonStyle = new GUIStyle(GUI.skin.GetStyle("Button"))
            {
                padding = new RectOffset(1, 1, 1, 1),
                margin = new RectOffset(2, 0, 0, 0),
                fixedWidth = 18,
                fixedHeight = 18
            };

            buttonStyle2 = new GUIStyle(GUI.skin.GetStyle("Button"))
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(1, 0, 0, 0),
                fixedWidth = 22,
                fixedHeight = 22
            };

            buttonStyle3 = new GUIStyle(GUI.skin.GetStyle("Button"))
            {
                padding = new RectOffset(2, 2, 2, 2),
                margin = new RectOffset(2, 0, 0, 0),
                fixedWidth = 20,
                fixedHeight = 20
            };


            contextBGN.SetPixel(0, 0, new Color(0, 0, 0, 0.0f));
            contextBGN.Apply();
            contextBGH.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.6f));
            contextBGH.Apply();
            contextBGA.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            contextBGA.Apply();
            contextStyle = new GUIStyle(GUI.skin.GetStyle("Button"))
            {
                fontStyle = FontStyle.Normal,
                normal = { background = contextBGN, textColor = new Color(1.0f, 0.858f, 0.0f) },
                hover = { background = contextBGH, textColor = new Color(1.0f, 1.0f, 0.0f) },
                active = { background = contextBGA, textColor = new Color(0.858f, 0.736f, 0.0f) },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 3, 0),
                margin = new RectOffset(0, 0, 0, 0),
                wordWrap = false,
                fixedHeight = 18,
                clipping = TextClipping.Clip
            };

            contextStyle2 = new GUIStyle(contextStyle)
            {
                normal = { textColor = new Color(0.858f, 0.0f, 0.0f) },
                hover = { textColor = new Color(0.9f, 0.2f, 0.2f) },
                active = { textColor = new Color(0.758f, 0.0f, 0.0f) }
            };

            expandStyle = new GUIStyle(GUI.skin.GetStyle("Toggle"))
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                fixedWidth = 12,
                fixedHeight = 12
            };

            centeredStyle = new GUIStyle { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };

            leftStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
            {
                margin = new RectOffset(0, 2, 0, 0),
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                clipping = TextClipping.Clip
            };

            tooltipStyle = leftStyle;
            tooltipStyle.wordWrap = true;

            rightStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
            {
                margin = new RectOffset(2, 2, 0, 0),
                alignment = TextAnchor.UpperRight,
                clipping = TextClipping.Overflow,
                wordWrap = false
            };

            rowStyle = new GUIStyle { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };

            //Initialize Colors
            enabledColor = GUI.contentColor;
            enabledBGColor = GUI.backgroundColor;
        }
    }

    class Target
    {
        public Vessel vessel;
        public List<ModuleDockingNode> availableDocks;
        public uint defaultDock = 0;
        public float lastDistance = 0;
        public String lastVName = null;
        public VesselType lastVType = new VesselType();
        public String queuedVName = null;
        public VesselType queuedVType = new VesselType();

        public Target(Vessel vessel, List<ModuleDockingNode> availableDocks, uint defaultDock)
        {
            this.vessel = vessel;
            if (availableDocks == null)
                this.availableDocks = new List<ModuleDockingNode>();
            else
                this.availableDocks = availableDocks;
            this.defaultDock = defaultDock;
            update();
        }
        public void update()
        {
            /*if (lastVName != null && this.vessel.GetName().Length != 0 && !this.vessel.GetName().Equals(this.lastVName) || !this.vessel.vesselType.Equals(this.lastVType))
                Debug.Log("TARGETRON: RENAME DETECTED ON VESSEL " + this.lastVName + "-" + this.lastVType + " -> " + this.vessel.GetName() + "-" + this.vessel.vesselType);*/
            lastVName = vessel.GetName();
            lastVType = vessel.vesselType;
            try
            {
                lastDistance = Vector3.Distance(FlightGlobals.fetch.activeVessel.transform.position, vessel.transform.position);
            }
            catch (Exception e)
            {
                Debug.Log("Targetron Error: Failed to get distance\n" + e.StackTrace);
            }
        }
    }

    class Filter
    {
        public Texture2D Texture = new Texture2D(20, 20);
        public VesselType Type;
        public bool Enabled = true;

        public Filter(WWW textureLink, VesselType type, bool enabled)
        {
            textureLink.LoadImageIntoTexture(Texture);
            Type = type;
            Enabled = enabled;
        }

        public String getName()
        {
            if (Type.Equals(VesselType.Debris))
                return Type + "/Other";
            return Type.ToString();
        }

        public bool matchType(VesselType type, List<VesselType> vesselTypes)
        {
            if (Type.Equals(type))
                return true;
            if (Type.Equals(VesselType.Debris) && !vesselTypes.Contains(type))
                return true;
            return false;
        }

        public void toggle()
        {
            Enabled = !Enabled;
        }
    }

}
