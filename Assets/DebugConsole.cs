#define DEBUG_CONSOLE
#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

#if (UNITY_EDITOR)
#define DEBUG
#endif

#if (UNITY_IOS || UNITY_ANDROID)
#define MOBILE
#endif

using UnityEngine.UI;

using System.Linq;

// V.M10.D31.2011.R1
/************************************************************************
* DebugConsole.cs
* Copyright 2011 Calvin Rien
* (http://the.darktable.com)
*
* Derived from version 2.0 of Jeremy Hollingsworth's DebugConsole
*
* Copyright 2008-2010 By: Jeremy Hollingsworth
* (http://www.ennanzus-interactive.com)
*
* Licensed for commercial, non-commercial, and educational use.
*
* THIS PRODUCT IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND. THE
* LICENSOR MAKES NO WARRANTY REGARDING THE PRODUCT, EXPRESS OR IMPLIED.
* THE LICENSOR EXPRESSLY DISCLAIMS AND THE LICENSEE HEREBY WAIVES ALL
* WARRANTIES, EXPRESS OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, ALL
* IMPLIED WARRANTIES OF MERCHANTABILITY AND ALL IMPLIED WARRANTIES OF
* FITNESS FOR A PARTICULAR PURPOSE.
* ************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using System.Reflection;
using Debug = System.Diagnostics.Debug;

/// <summary>
/// Provides a game-mode, multi-line console with command binding, logging and watch vars.
///
/// ==== Installation ====
/// Just drop this script into your project. To use from JavaScript(UnityScript), just make sure
/// you place this script in a folder such as "Plugins" so that it is compiled before your js code.
///
/// See the following Unity docs page for more info on this:
/// http://unity3d.com/support/documentation/ScriptReference/index.Script_compilation_28Advanced29.html
///
/// ==== Usage (Logging) ====
///
/// To use, you only need to access the desired static Log functions. So, for example, to log a simple
/// message you would do the following:
///
/// \code
/// DebugConsole.Log("Hello World!");
/// DebugConsole.LogWarning("Careful!");
/// DebugConsole.LogError("Danger!");
///
/// // Now open it
/// DebugConsole.IsOpen = true;
/// \endcode
///
/// You can log any object that has a functional ToString() method.
///
/// Those static methods will automatically ensure that the console has been set up in your scene for you,
/// so no need to worry about attaching this script to anything.
///
/// See the comments for the other static functions below for details on their use.
///
/// ==== Usage (DebugCommand Binding) ====
///
/// To use command binding, you create a function to handle the command, then you register that function
/// along with the string used to invoke it with the console.
///
/// So, for example, if you want to have a command called "ShowFPS", you would first create the handler like
/// this:
///
/// \code
/// // JavaScript
/// function ShowFPSCommand(args)
/// {
///     //...
///   return "value you want printed to console";
/// }
///
/// // C#
/// public object ShowFPSCommand(params string[] args)
/// {
///     //...
///   return "value you want printed to console";
/// }
/// \endcode
///
/// Then, to register the command with the console to be run when "ShowFPS" is typed, you would do the following:
///
/// \code
/// DebugConsole.RegisterCommand("ShowFPS", ShowFPSCommand);
/// \endcode
///
/// That's it! Now when the user types "ShowFPS" in the console and hits enter, your function will be run.
///
/// You can also use anonymous functions to register commands
/// \code
/// DebugConsole.RegisterCommand("echo", args => {if (args.Length < 2) return ""; args[0] = ""; return string.Join(" ", args);});
/// \endcode
///
/// If you wish to capture input entered after the command text, the args array will contain every space-separated
/// block of text the user entered after the command. "SetFOV 90" would pass the string "90" to the SetFOV command.
///
/// Note: Typing "/?" followed by enter will show the list of currently-registered commands.
///
/// ==== Usage (Watch Vars) ===
///
/// For the Watch Vars feature, you need to use the provided class, or your own subclass of WatchVarBase, to store
/// the value of your variable in your project. You then register that WatchVar with the console for tracking.
///
/// Example:
/// \code
/// // JavaScript
/// var myWatchInt = new WatchVar<int>("PowerupCount", 23);
///
/// myWatchInt.Value = 230;
///
/// myWatchInt.UnRegister();
/// myWatchInt.Register();
/// \endcode
///
/// As you use that WatchVar<int> to store your value through the project, its live value will be shown in the console.
///
/// You can create a WatchVar<T> for any object that has a functional ToString() method;
///
/// If you subclass WatchVarBase, you can create your own WatchVars to represent more types than are currently built-in.
/// </summary>
///
#if DEBUG_CONSOLE
public partial class DebugConsole : MonoBehaviour
{
    readonly string VERSION = "3.0";
    readonly string ENTRYFIELD = "DebugConsoleEntryField";

    /// <summary>
    /// This is the signature for the DebugCommand delegate if you use the command binding.
    ///
    /// So, if you have a JavaScript function named "SetFOV", that you wanted run when typing a
    /// debug command, it would have to have the following definition:
    ///
    /// \code
    /// function SetFOV(args)
    /// {
    ///     //...
    ///   return "value you want printed to console";
    /// }
    /// \endcode
    /// </summary>
    /// <param name="args">The text typed in the console after the name of the command.</param>



    public delegate object DebugCommand(params string[] args);

    /// <summary>
    /// How many lines of text this console will display.
    /// </summary>
    public int maxLinesForDisplay = 500;

    /// <summary>
    /// Default color of the standard display text.
    /// </summary>
    public Color defaultColor = Message.defaultColor;
    public Color warningColor = Message.warningColor;
    public Color errorColor = Message.errorColor;
    public Color systemColor = Message.systemColor;
    public Color inputColor = Message.inputColor;
    public Color outputColor = Message.outputColor;

    /// <summary>
    /// Used to check (or toggle) the open state of the console.
    /// </summary>
    public static bool IsOpen
    {
        get { return DebugConsole.Instance._isOpen; }
        set { DebugConsole.Instance._isOpen = value; }
    }

    /// <summary>
    /// Static instance of the console.
    ///
    /// When you want to access the console without a direct
    /// reference (which you do in mose cases), use DebugConsole.Instance and the required
    /// GameObject initialization will be done for you.
    /// </summary>
    static DebugConsole Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(DebugConsole)) as DebugConsole;

                if (_instance != null)
                {
                    return _instance;
                }

                GameObject console = new GameObject("__Debug Console__");
                _instance = console.AddComponent<DebugConsole>();
            }

            return _instance;
        }
    }

    /// <summary>
    /// Key to press to toggle the visibility of the console.
    /// </summary>
    public static KeyCode toggleKey = KeyCode.BackQuote;
    static DebugConsole _instance;
    Dictionary<string, DebugCommand> _cmdTable = new Dictionary<string, DebugCommand>();
    Dictionary<string, WatchVarBase> _watchVarTable = new Dictionary<string, WatchVarBase>();
    string _inputString = string.Empty;
    Rect _windowRect;
#if MOBILE
  Rect _fakeWindowRect;
  Rect _fakeDragRect;
  bool dragging = false;
  GUIStyle windowOnStyle;
  GUIStyle windowStyle;
#if UNITY_EDITOR
  Vector2 prevMousePos;
#endif
#endif

    Vector2 _logScrollPos = Vector2.zero;
    Vector2 _rawLogScrollPos = Vector2.zero;
    Vector2 _watchVarsScrollPos = Vector2.zero;
    Vector3 _guiScale = Vector3.one;
    Matrix4x4 restoreMatrix = Matrix4x4.identity;
    bool _scaled = false;
    bool _isOpen;
    bool _justOpened = false;
    StringBuilder _displayString = new StringBuilder();
    FPSCounter fps;
    bool dirty;

    bool debuggingMouse;
    int framesToUpdateMax = 3;
    int framesToUpdateCounter = 0;

    #region GUI position values
    // Make these values public if you want to adjust layout of console window
    readonly Rect scrollRect = new Rect(10, 20, 280, 362);
    readonly Rect inputRect = new Rect(10, 388, 228, 24);
    readonly Rect enterRect = new Rect(240, 388, 50, 24);
    readonly Rect toolbarRect = new Rect(16, 416, 266, 25);
    Rect messageLine = new Rect(4, 0, 264, 20);
    int lineOffset = -4;
    string[] tabs = new string[] { "Log", "Copy Log", "Watch Vars" };

    // Keep these private, their values are generated automatically
    Rect nameRect;
    Rect valueRect;
    Rect innerRect = new Rect(0, 0, 0, 0);
    int innerHeight = 0;
    int toolbarIndex = 0;
    GUIContent guiContent = new GUIContent();
    GUI.WindowFunction[] windowMethods;
    GUIStyle labelStyle;
    #endregion

    /// <summary>
    /// This Enum holds the message types used to easily control the formatting and display of a message.
    /// </summary>
    public enum MessageType
    {
        NORMAL,
        WARNING,
        ERROR,
        SYSTEM,
        INPUT,
        OUTPUT
    }

    /// <summary>
    /// Represents a single message, with formatting options.
    /// </summary>
    struct Message
    {
        string text;
        string formatted;
        MessageType type;

        public Color color { get; private set; }

        public static Color defaultColor = Color.white;
        public static Color warningColor = Color.yellow;
        public static Color errorColor = Color.red;
        public static Color systemColor = Color.green;
        public static Color inputColor = Color.green;
        public static Color outputColor = Color.cyan;

        public Message(object messageObject) : this(messageObject, MessageType.NORMAL, Message.defaultColor)
        {
        }

        public Message(object messageObject, Color displayColor) : this(messageObject, MessageType.NORMAL, displayColor)
        {
        }

        public Message(object messageObject, MessageType messageType) : this(messageObject, messageType, Message.defaultColor)
        {
            switch (messageType)
            {
                case MessageType.ERROR:
                    color = errorColor;
                    break;
                case MessageType.SYSTEM:
                    color = systemColor;
                    break;
                case MessageType.WARNING:
                    color = warningColor;
                    break;
                case MessageType.OUTPUT:
                    color = outputColor;
                    break;
                case MessageType.INPUT:
                    color = inputColor;
                    break;
            }
        }

        public Message(object messageObject, MessageType messageType, Color displayColor)
        {
            this.text = messageObject == null ? "<null>" : messageObject.ToString();

            this.formatted = string.Empty;
            this.type = messageType;
            this.color = displayColor;
        }

        public static Message Log(object message)
        {
            return new Message(message, MessageType.NORMAL, defaultColor);
        }

        public static Message System(object message)
        {
            return new Message(message, MessageType.SYSTEM, systemColor);
        }

        public static Message Warning(object message)
        {
            return new Message(message, MessageType.WARNING, warningColor);
        }

        public static Message Error(object message)
        {
            return new Message(message, MessageType.ERROR, errorColor);
        }

        public static Message Output(object message)
        {
            return new Message(message, MessageType.OUTPUT, outputColor);
        }

        public static Message Input(object message)
        {
            return new Message(message, MessageType.INPUT, inputColor);
        }

        public override string ToString()
        {
            switch (type)
            {
                case MessageType.ERROR:
                    return string.Format("[{0}] {1}", type, text);
                case MessageType.WARNING:
                    return string.Format("[{0}] {1}", type, text);
                default:
                    return ToGUIString();
            }
        }

        public string ToGUIString()
        {
            if (!string.IsNullOrEmpty(formatted))
            {
                return formatted;
            }

            switch (type)
            {
                case MessageType.INPUT:
                    formatted = string.Format(">>> {0}", text);
                    break;
                case MessageType.OUTPUT:
                    var lines = text.Trim('\n').Split('\n');
                    var output = new StringBuilder();

                    foreach (var line in lines)
                    {
                        output.AppendFormat("= {0}\n", line);
                    }

                    formatted = output.ToString();
                    break;
                case MessageType.SYSTEM:
                    formatted = string.Format("# {0}", text);
                    break;
                case MessageType.WARNING:
                    formatted = string.Format("* {0}", text);
                    break;
                case MessageType.ERROR:
                    formatted = string.Format("** {0}", text);
                    break;
                default:
                    formatted = text;
                    break;
            }

            return formatted;
        }
    }

    class History
    {
        List<string> history = new List<string>();
        int index = 0;

        public void Add(string item)
        {
            history.Add(item);
            index = 0;
        }

        string current;

        public string Fetch(string current, bool next)
        {
            if (index == 0)
            {
                this.current = current;
            }

            if (history.Count == 0)
            {
                return current;
            }

            index += next ? -1 : 1;

            if (history.Count + index < 0 || history.Count + index > history.Count - 1)
            {
                index = 0;
                return this.current;
            }

            var result = history[history.Count + index];

            return result;
        }
    }

    List<Message> _messages = new List<Message>();
    History _history = new History();
    static float timeAtLastSkip;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(this, true);
            return;
        }

        _instance = this;
    }

    void OnEnable()
    {
        var scale = Screen.dpi / 160.0f;

        if (scale != 0.0f && scale >= 1.1f)
        {
            _scaled = true;
            _guiScale.Set(scale, scale, scale);
        }

        windowMethods = new GUI.WindowFunction[] { LogWindow, CopyLogWindow, WatchVarWindow };

        fps = new FPSCounter();
        StartCoroutine(fps.Update());

        nameRect = messageLine;
        valueRect = messageLine;

        Message.defaultColor = defaultColor;
        Message.warningColor = warningColor;
        Message.errorColor = errorColor;
        Message.systemColor = systemColor;
        Message.inputColor = inputColor;
        Message.outputColor = outputColor;
#if MOBILE
    this.useGUILayout = false;
    _windowRect = new Rect(5.0f, 5.0f, 300.0f, 450.0f);
    _fakeWindowRect = new Rect(0.0f, 0.0f, _windowRect.width, _windowRect.height);
    _fakeDragRect = new Rect(0.0f, 0.0f, _windowRect.width - 32, 24);
#else
        _windowRect = new Rect(30.0f, 30.0f, 300.0f, 450.0f);
#endif

        LogMessage(Message.System(string.Format(" DebugConsole version {0}", VERSION)));
        LogMessage(Message.System(" Copyright 2008-2010 Jeremy Hollingsworth "));
        LogMessage(Message.System(" Ennanzus-Interactive.com "));
        LogMessage(Message.System(" type '/?' for available commands."));
        LogMessage(Message.Log(""));

        //#console commands
        this.RegisterCommandCallback("close", CMDClose);
        this.RegisterCommandCallback("clear", CMDClear);
        this.RegisterCommandCallback("sys", CMDSystemInfo);
        this.RegisterCommandCallback("/?", CMDHelp);

        RegisterCustomTangledeepCommands();


    }

    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    void OnGUI()
    {
        var evt = Event.current;

        if (_scaled)
        {
            restoreMatrix = GUI.matrix;

            GUI.matrix = GUI.matrix * Matrix4x4.Scale(_guiScale);
        }

        while (_messages.Count > maxLinesForDisplay)
        {
            _messages.RemoveAt(0);
        }
#if (!MOBILE && DEBUG) || UNITY_EDITOR
        // Toggle key shows the console in non-iOS dev builds

        //Shep: If we're letting the outside world have development builds, we still wanna
        //hide the console

        //#if UNITY_EDITOR
        if ((evt.keyCode == toggleKey || evt.keyCode == KeyCode.Quote || evt.keyCode == KeyCode.DoubleQuote) && evt.type == EventType.KeyUp)
        {
            _isOpen = !_isOpen;
            if (_isOpen)
            {
                _justOpened = true;
            }
        }
        //#endif

#endif
#if MOBILE
    if (Input.touchCount == 1) {
      var touch = Input.GetTouch(0);
#if DEBUG
      // Triple Tap shows/hides the console in iOS/Android dev builds.
      if (evt.type == EventType.Repaint && (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended) && touch.tapCount == 3) {
        _isOpen = !_isOpen;
      }
#endif
      if (_isOpen) {
        var pos = touch.position;
        pos.y = Screen.height - pos.y;

        if (dragging && (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)) {
          dragging = false;
        }
        else if (!dragging && (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary)) {
          var dragRect = _fakeDragRect;

          dragRect.x = _windowRect.x * _guiScale.x;
          dragRect.y = _windowRect.y * _guiScale.y;
          dragRect.width *= _guiScale.x;
          dragRect.height *= _guiScale.y;

          // check to see if the touch is inside the dragRect.
          if (dragRect.Contains(pos)) {
            dragging = true;
          }
        }

        if (dragging && evt.type == EventType.Repaint) {
#if UNITY_ANDROID
          var delta = touch.deltaPosition * 2.0f;
#elif UNITY_IOS
          var delta = touch.deltaPosition;
          delta.x /= _guiScale.x;
          delta.y /= _guiScale.y;
#endif
          delta.y = -delta.y;

          _windowRect.center += delta;
        }
        else {
          var tapRect = scrollRect;
          tapRect.x += _windowRect.x * _guiScale.x;
          tapRect.y += _windowRect.y * _guiScale.y;
          tapRect.width -= 32;
          tapRect.width *= _guiScale.x;
          tapRect.height *= _guiScale.y;

          if (tapRect.Contains(pos)) {
            var scrollY = (tapRect.center.y - pos.y) / _guiScale.y;

            switch (toolbarIndex) {
            case 0:
              _logScrollPos.y -= scrollY;
              break;
            case 1:
              _rawLogScrollPos.y -= scrollY;
              break;
            case 2:
              _watchVarsScrollPos.y -= scrollY;
              break;
            }
          }
        }
      }
    }
    else if (dragging && Input.touchCount == 0) {
      dragging = false;
    }
#endif
        if (!_isOpen)
        {
            return;
        }

        labelStyle = GUI.skin.label;

        innerRect.width = messageLine.width;
#if !MOBILE
        _windowRect = GUI.Window(-1111, _windowRect, windowMethods[toolbarIndex], string.Format("Debug Console v{0}\tfps: {1:00.0}", VERSION, fps.current));
        GUI.BringWindowToFront(-1111);
#else
    if (windowStyle == null) {
      windowStyle = new GUIStyle(GUI.skin.window);
      windowOnStyle = new GUIStyle(GUI.skin.window);
      windowOnStyle.normal.background = GUI.skin.window.onNormal.background;
    }

    GUI.BeginGroup(_windowRect);
#if UNITY_EDITOR
    if (GUI.RepeatButton(_fakeDragRect, string.Empty, GUIStyle.none)) {
      Vector2 delta = (Vector2) Input.mousePosition - prevMousePos;
      delta.y = -delta.y;

      _windowRect.center += delta;
      dragging = true;
    }

    if (evt.type == EventType.Repaint) {
      prevMousePos = Input.mousePosition;
    }
#endif
    GUI.Box(_fakeWindowRect, string.Format("Debug Console v{0}\tfps: {1:00.0}", VERSION, fps.current), dragging ? windowOnStyle : windowStyle);
    windowMethods[toolbarIndex](0);
    GUI.EndGroup();
#endif

        if (GUI.GetNameOfFocusedControl() == ENTRYFIELD)
        {
            if (evt.isKey && evt.type == EventType.KeyUp)
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    EvalInputString(_inputString);
                    _inputString = string.Empty;
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    _inputString = _history.Fetch(_inputString, true);
                }
                else if (evt.keyCode == KeyCode.DownArrow)
                {
                    _inputString = _history.Fetch(_inputString, false);
                }
            }
        }

        if (_scaled)
        {
            GUI.matrix = restoreMatrix;
        }

        if (dirty && evt.type == EventType.Repaint)
        {
            _logScrollPos.y = 50000.0f;
            _rawLogScrollPos.y = 50000.0f;

            BuildDisplayString();
            dirty = false;
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
    #region StaticAccessors

    /// <summary>
    /// Prints a message string to the console.
    /// </summary>
    /// <param name="message">Message to print.</param>
    public static object Log(object message)
    {
        DebugConsole.Instance.LogMessage(Message.Log(message));

        return message;
    }

    public static object LogFormat(string format, params object[] args)
    {
        return Log(string.Format(format, args));
    }

    /// <summary>
    /// Prints a message string to the console.
    /// </summary>
    /// <param name="message">Message to print.</param>
    /// <param name="messageType">The MessageType of the message. Used to provide
    /// formatting in order to distinguish between message types.</param>
    public static object Log(object message, MessageType messageType)
    {
        DebugConsole.Instance.LogMessage(new Message(message, messageType));

        return message;
    }

    /// <summary>
    /// Prints a message string to the console.
    /// </summary>
    /// <param name="message">Message to print.</param>
    /// <param name="displayColor">The text color to use when displaying the message.</param>
    public static object Log(object message, Color displayColor)
    {
        DebugConsole.Instance.LogMessage(new Message(message, displayColor));

        return message;
    }

    /// <summary>
    /// Prints a message string to the console.
    /// </summary>
    /// <param name="message">Messate to print.</param>
    /// <param name="messageType">The MessageType of the message. Used to provide
    /// formatting in order to distinguish between message types.</param>
    /// <param name="displayColor">The color to use when displaying the message.</param>
    /// <param name="useCustomColor">Flag indicating if the displayColor value should be used or
    /// if the default color for the message type should be used instead.</param>
    public static object Log(object message, MessageType messageType, Color displayColor)
    {
        DebugConsole.Instance.LogMessage(new Message(message, messageType, displayColor));

        return message;
    }

    /// <summary>
    /// Prints a message string to the console using the "Warning" message type formatting.
    /// </summary>
    /// <param name="message">Message to print.</param>
    public static object LogWarning(object message)
    {
        DebugConsole.Instance.LogMessage(Message.Warning(message));

        return message;
    }

    /// <summary>
    /// Prints a message string to the console using the "Error" message type formatting.
    /// </summary>
    /// <param name="message">Message to print.</param>
    public static object LogError(object message)
    {
        DebugConsole.Instance.LogMessage(Message.Error(message));

        return message;
    }

    /// <summary>
    /// Clears all console output.
    /// </summary>
    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    public static void Clear()
    {
        DebugConsole.Instance.ClearLog();
    }

    /// <summary>
    /// Execute a console command directly from code.
    /// </summary>
    /// <param name="commandString">The command line you want to execute. For example: "sys"</param>
    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    public static void Execute(string commandString)
    {
        DebugConsole.Instance.EvalInputString(commandString);
    }

    /// <summary>
    /// Registers a debug command that is "fired" when the specified command string is entered.
    /// </summary>
    /// <param name="commandString">The string that represents the command. For example: "FOV"</param>
    /// <param name="commandCallback">The method/function to call with the commandString is entered.
    /// For example: "SetFOV"</param>
    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    public static void RegisterCommand(string commandString, DebugCommand commandCallback)
    {
        DebugConsole.Instance.RegisterCommandCallback(commandString, commandCallback);
    }

    /// <summary>
    /// Removes a previously-registered debug command.
    /// </summary>
    /// <param name="commandString">The string that represents the command.</param>
    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    public static void UnRegisterCommand(string commandString)
    {
        DebugConsole.Instance.UnRegisterCommandCallback(commandString);
    }

    /// <summary>
    /// Registers a named "watch var" for monitoring.
    /// </summary>
    /// <param name="name">Name of the watch var to be shown in the console.</param>
    /// <param name="watchVar">The WatchVar instance you want to monitor.</param>
    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    public static void RegisterWatchVar(WatchVarBase watchVar)
    {
        DebugConsole.Instance.AddWatchVarToTable(watchVar);
    }

    /// <summary>
    /// Removes a previously-registered watch var.
    /// </summary>
    /// <param name="name">Name of the watch var you wish to remove.</param>
    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    public static void UnRegisterWatchVar(string name)
    {
        DebugConsole.Instance.RemoveWatchVarFromTable(name);
    }
    #endregion
    #region Console commands

    //==== Built-in example DebugCommand handlers ====

    object RoomDistanceCommand(params string[] args)
    {
        /* Room rm1 = MapMasterScript.GetRoomByID(int.Parse(args[1]));
        Room rm2 = MapMasterScript.GetRoomByID(int.Parse(args[2]));
        Vector2 pos1 = rm1.center;
        Vector2 pos2 = rm2.center;
        string text = "Distance is: " + Vector2.Distance(pos1, pos2); */
        return "Done.";
    }

    object CheckForAllActorsInMap(params string[] args)
    {
        foreach(Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            UnityEngine.Debug.Log(act.actorRefName + " " + act.displayName + " " + act.actorUniqueID + " is at " + act.GetPos());
        }

        return "Done!";
    }

    object CheckForDebugThing(params string[] args)
    {
        foreach(EffectScript eff in GameMasterScript.masterEffectList.Values)
        {
            if (eff.effectType == EffectType.ALTERBATTLEDATA)
            {
                AlterBattleDataEffect abd = eff as AlterBattleDataEffect;
                if (abd.tActorType != TargetActorType.SELF)
                {
                    UnityEngine.Debug.Log(abd.effectRefName + " does not target self");
                }
            }
        }

        return "Done!";
    }

    object CheckBubbleCooldown(params string[] args)
    {
        int turnCheck;
        GameMasterScript.heroPCActor.effectsInflictedOnTurn.TryGetValue("add_bubbleshield", out turnCheck);
        if (GameMasterScript.heroPCActor.effectsInflictedOnTurn.ContainsKey("add_bubbleshield"))
        {
            return "Turns since ability was triggered: " + turnCheck;
        }
        return "Don't have that status";
    }

    object PrintAllStatusBonus(params string[] args)
    {
        string builder = "";

        foreach(StatusEffect se in GameMasterScript.masterStatusList.Values)
        {
            if (!se.isPositive) continue;
            if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
            {
                continue;
            }
            if (se.CheckRunTriggerOn(StatusTrigger.ENTERTILE) || se.CheckRunTriggerOn(StatusTrigger.STARTTURNINTILE) || se.CheckRunTriggerOn(StatusTrigger.ENDTURNINTILE))
            {
                continue;
            }
            if (se.CheckAbilityTag(AbilityTags.GROUNDBASEDEFFECT))
            {
                continue;
            }

            bool skip = false;

            foreach(EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectRefName.Contains("generic"))
                {
                    skip = true;
                    break;
                }
            }

            if (skip) continue;

            string localBuilder = "<li><strong>" + se.refName + "</strong>";
            
            if (!string.IsNullOrEmpty(se.abilityName))
            {
                localBuilder += " (" + se.abilityName + ")";
            }

            localBuilder += ":";

            if (!se.CheckRunTriggerOn(StatusTrigger.PERMANENT))
            {
                bool first = true;
                for (int i = 0; i < se.runStatusTriggers.Length; i++)
                {
                    if (se.runStatusTriggers[i])
                    {
                        if (first)
                        {
                            localBuilder += " Runs On:";
                            first = false;
                        }
                        localBuilder += " " + (StatusTrigger)i + ",";
                    }
                }
                if (!first)
                {
                    localBuilder = localBuilder.Remove(localBuilder.Length - 1);
                }
            }


            if (!string.IsNullOrEmpty(se.description))
            {
                localBuilder += " " + se.description;
            }
            else
            {
                // Try to build what it does based on effect?
                bool first = true;
                foreach (EffectScript eff in se.listEffectScripts)
                {
                    if (first)
                    {
                        localBuilder += " Effects:";
                        first = false;
                    }
                    localBuilder += " " + eff.effectRefName + " (" + eff.effectType;
                    if (!string.IsNullOrEmpty(eff.effectName))
                    {
                        localBuilder += "/'" + eff.effectName + "'";
                    }
                    localBuilder += "),";
                }
                if (!first)
                {
                    localBuilder = localBuilder.Remove(localBuilder.Length - 1);
                }
                else
                {
                    localBuilder += " Hardcoded effect.";
                }
            }
            if (!string.IsNullOrEmpty(se.extraDescription))
            {
                localBuilder += " (" + se.extraDescription + ")";
            }
                        
            localBuilder = localBuilder.Replace("<color=yellow>", "");
            localBuilder = localBuilder.Replace("</color>", "");

            builder += localBuilder + "</li>\n";            
        }

        UnityEngine.Debug.Log(builder);

        return "Done.";
    }

    object PrintAllAbilities(params string[] args)
    {
        string builder = "";
        foreach(AbilityScript abil in GameMasterScript.masterAbilityList.Values)
        {
            if (string.IsNullOrEmpty(abil.abilityName)) continue; // Probably a monster ability            

            string localBuilder = "";
            localBuilder += "<li><strong>" + abil.refName + "</strong> (" + abil.abilityName + "): ";

            if (string.IsNullOrEmpty(abil.description))
            {
                bool probablyFood = false;
                if (!abil.passiveAbility)
                {
                    foreach (EffectScript eff in abil.listEffectScripts)
                    {
                        if (eff.effectRefName.Contains("foodfull"))
                        {
                            localBuilder += "No description. Some kind of food power.";
                            probablyFood = true;
                            break;
                        }
                    }
                    if (!probablyFood && abil.maxCooldownTurns == 0)
                    {
                        localBuilder += "No description. Probably a consumable power.";
                    }
                    else if (!probablyFood)
                    {
                        localBuilder += "No description. Probably a monster power.";
                    }
                }
                else
                {
                    localBuilder += "No description. Probably a monster power.";
                }

                
            }
            else
            {
                localBuilder += abil.description;
            }            
            if (!string.IsNullOrEmpty(abil.extraDescription))
            {
                localBuilder += " (<em>" + abil.GetExtraDescription() + "</em>)";
            }
            string costs = "";
            if (abil.energyCost > 0)
            {
                costs += " Energy: " + abil.energyCost;
            }
            if (abil.staminaCost > 0)
            {
                costs += " Stamina: " + abil.staminaCost;
            }
            if (abil.passiveAbility)
            {
                costs += " (PASSIVE)";
            }
            else
            {
                costs += " Cooldown: " + abil.maxCooldownTurns;
            }
            localBuilder += costs;
            localBuilder += "</li>";
            localBuilder = localBuilder.Replace("<color=yellow>", "");
            localBuilder = localBuilder.Replace("</color>", "");
            builder += localBuilder + "\n";
        }

        UnityEngine.Debug.Log(builder);

        return "Done!";
    }

    object DebugStairsAccessibility(params string[] args)
    {
        foreach(Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            bool accessible = MapMasterScript.activeMap.FloodFillToSeeIfGivenTileIsConnectedToBiggestCavern(MapMasterScript.activeMap.GetTile(st.GetPos()));
        }

        return "Done!";
    }

    object PrintAllSpriteEffects(params string[] args)
    {
        Dictionary<string, string> allEffects = new Dictionary<string, string>();

        foreach (Item itm in GameMasterScript.masterItemList.Values)
        {
            if (itm.itemType != ItemTypes.WEAPON) continue;
            Weapon w = itm as Weapon;
            if (!string.IsNullOrEmpty(w.impactEffect) && !allEffects.ContainsKey(w.impactEffect))
            {
                allEffects.Add(w.impactEffect, "Impact Effect");
            }
            if (!string.IsNullOrEmpty(w.swingEffect) && !allEffects.ContainsKey(w.swingEffect))
            {
                allEffects.Add(w.swingEffect, "Projectile");
            }
        }

        foreach (EffectScript eff in GameMasterScript.masterEffectList.Values)
        {
            if (!string.IsNullOrEmpty(eff.spriteEffectRef) && !allEffects.ContainsKey(eff.spriteEffectRef))
            {
                string localBuilder = "";
                if (!string.IsNullOrEmpty(eff.effectName))
                {
                    allEffects.Add(eff.spriteEffectRef, "Ability Sprite Effect (" + eff.effectName + ")");
                }
                else
                {
                    allEffects.Add(eff.spriteEffectRef, "Ability Sprite Effect");
                }                
            }
        }

        string finalBuilder = "";

        foreach (string key in allEffects.Keys)
        {
            finalBuilder += "<li>" + allEffects[key] + ": <em>" + key + "</em></li>\n";
        }

        UnityEngine.Debug.Log(finalBuilder);

        return "Done!";
    }

    object PrintAllObjectPrefabs(params string[] args)
    {
        Dictionary<string, string> dictRefsToDisplayNames = new Dictionary<string, string>();

        List<AbilityScript> nList = new List<AbilityScript>();
        foreach(AbilityScript abil in GameMasterScript.masterAbilityList.Values)
        {
            nList.Add(abil);
        }
        foreach (StatusEffect se in GameMasterScript.masterStatusList.Values)
        {
            nList.Add(se);
        }

        foreach (AbilityScript abil in nList)
        {
            foreach(EffectScript eff in abil.listEffectScripts)
            {
                if (eff.effectType == EffectType.SUMMONACTOR)
                {
                    SummonActorEffect sae = eff as SummonActorEffect;

                    if (dictRefsToDisplayNames.ContainsKey(sae.summonActorRef))
                    {
                        continue;
                    }

                    if (sae.summonActorType == ActorTypes.DESTRUCTIBLE)
                    {
                        if (GameMasterScript.masterMapObjectDict.ContainsKey(sae.summonActorRef))
                        {
                            Destructible dt = GameMasterScript.masterMapObjectDict[sae.summonActorRef];

                            if (dictRefsToDisplayNames.ContainsKey(dt.prefab)) continue;

                            string nameToUse = abil.abilityName;
                            if (string.IsNullOrEmpty(nameToUse))
                            {
                                nameToUse = eff.effectName;
                            }
                            if (string.IsNullOrEmpty(nameToUse))
                            {
                                nameToUse = "";
                            }

                            dictRefsToDisplayNames.Add(dt.prefab, dt.actorRefName + " (" + dt.displayName + "), Summoned by ability " + abil.abilityName);
                        }
                    }
                }
            }
        }

        foreach(Destructible dt in GameMasterScript.masterMapObjectDict.Values)
        {
            if (dictRefsToDisplayNames.ContainsKey(dt.prefab)) continue;
            string dName = " (" + dt.displayName + ")";
            if (string.IsNullOrEmpty(dt.displayName))
            {
                dName = "";
            }
            dictRefsToDisplayNames.Add(dt.prefab, dt.actorRefName + dName);
        }

        string builder = "";

        foreach(string str in dictRefsToDisplayNames.Keys)
        {
            builder += "<li><strong>" + str + "</strong>: " + dictRefsToDisplayNames[str] + "</li>\n";
        }

        UnityEngine.Debug.Log(builder);
        return "Done!";
    }

    object PrintAllMonsterPrefabs(params string[] args)
    {
        string finalBuilder = "";
        List<string> prefabs = new List<string>();
        foreach(MonsterTemplateData mtd in GameMasterScript.masterMonsterList.Values)
        {
            if (!prefabs.Contains(mtd.prefab))
            {
                prefabs.Add(mtd.prefab);
                finalBuilder += "<li><strong>" + mtd.prefab + "</strong> (Used by monster ref " + mtd.refName + " / " + mtd.monsterName + ")</li>\n";
            }
        }
        UnityEngine.Debug.Log(finalBuilder);
        return "Done!";
    }

    object ResetTutorials(params string[] args)
    {
        TutorialManagerScript.ResetTutorialData();
        return "Done!";
    }

    object PrintAllMagicMods(params string[] args)
    {
        string finalBuilder = "";
        foreach (MagicMod mm in GameMasterScript.masterMagicModList.Values)
        {
            if (mm.refName.Contains("mm_upgradeaccessory")) continue;
            if (mm.refName == "mm_") continue;
            if (mm.refName.Contains("mm_emblemwellrounded")) continue;
            string localBuilder = "<li><strong>";
            localBuilder += mm.refName + "</strong>:";
            if (string.IsNullOrEmpty(mm.modName))
            {
                localBuilder += " (No in-game name)";
            }
            else
            {
                localBuilder += " (" + mm.modName + ")";
            }
            localBuilder += " " + mm.GetDescription() + "</li>";
            localBuilder.Replace("<color=yellow>", "");
            localBuilder.Replace("</color>", "");
            finalBuilder += localBuilder += "\n";
        }

        UnityEngine.Debug.Log(finalBuilder);
        return "Done!";
    }

    object CheckLineOfSight(params string[] args)
    {
        foreach (Monster mn in MapMasterScript.activeMap.monstersInMap)
        {
            //UnityEngine.Debug.Log(mn.displayName + " at " + mn.GetPos() + " SBV? " + mn.myMovable.shouldBeVisible + " VIS? " + mn.myMovable.visible + " Dist from hero? " + MapMasterScript.GetGridDistance(mn.GetPos(), GameMasterScript.heroPCActor.GetPos()) + " SR enabled? " + mn.GetObject().GetComponent<SpriteRenderer>().enabled + " Tile visible? " + GameMasterScript.heroPCActor.visibleTilesArray[(int)mn.GetPos().x, (int)mn.GetPos().y]);
        }

        return "Done.";
    }

    object RunPokerTest(params string[] args)
    {
        int numTests = 0;
        Int32.TryParse(args[1], out numTests);
        GameMasterScript.heroPCActor.Test_DrawAndEvaluateHands(numTests);
        return "Done!";
    }
    object CheckTreeStates(params string[] args)
    {
        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.NPC)
            {
                MapTileData mtd = MapMasterScript.activeMap.GetTile(act.GetPos());
                UnityEngine.Debug.Log("NPC " + act.actorRefName + " is on tile " + mtd.pos + " NPC col states: " + act.monsterCollidable + " " + act.playerCollidable + " Tile states: " + mtd.playerCollidable + " " + mtd.monCollidable);
            }
        }

        return "Done!";
    }

    object CheckTileColorInfo(params string[] args)
    {
        int x = Int32.Parse(args[1]);
        int y = Int32.Parse(args[2]);

        //MapTileData mtd = MapMasterScript.activeMap.GetTile(new Vector2(x, y));

        //UnityEngine.Debug.Log(mtd.pos + " " + mtd.tileType + " " + mtd.CheckTag(LocationTags.EDGETILE) + MapMasterScript.activeMap.CheckMTDArea(mtd) + " " + mtd.caveColor);

        return "Done.";
    }

    object GetFloorInfo(params string[] args)
    {
        Map m = MapMasterScript.activeMap;

        UnityEngine.Debug.Log("True map floor: " + m.floor + " Effective floor: " + m.effectiveFloor + " " + m.monstersInMap.Count + " " + m.unfriendlyMonsterCount);

        return "Done!";
    }

    object SearchAllFilesForWords(params string[] args)
    {
        //string path = @"F:\ImpactRL\Impact7dayRL\Assets";

        //string textToSearch = System.IO.File.ReadAllText(@"F:\ImpactRL\Impact7dayRL\Assets\Resources\Localization\en_us_stringrefnames_TEMP.txt");        

        //string[] refsToSearch = textToSearch.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        //UnityEngine.Debug.Log("Count of refs: " + refsToSearch.Length);

        Dictionary<string, string> master = StringManager.dictStringsByLanguage[EGameLanguage.en_us];
        foreach (string key in master.Keys)
        {
            string value = master[key];
            int count = value.Length - value.Replace("^number1^", "").Length;
            //UnityEngine.Debug.Log(count);
            if (count > 9)
            {
                UnityEngine.Debug.Log("WARNING! " + key + " has duplicate number");
            }

        }

        return "Done!";

        /* List<string> filesToSearch = 
            filesToSearch = System.IO.Directory.GetFiles(path, "*.cs", System.IO.SearchOption.TopDirectoryOnly).ToList();

        path = @"F:\ImpactRL\Impact7dayRL\Assets\TDScripts";

        filesToSearch = filesToSearch.Union(
            System.IO.Directory.GetFiles(path, "*.cs", System.IO.SearchOption.AllDirectories)).ToList();

        path = @"F:\ImpactRL\Impact7dayRL\Assets\Resources";

        filesToSearch = filesToSearch.Union(
            System.IO.Directory.GetFiles(path, "*.xml", System.IO.SearchOption.AllDirectories)).ToList();

        List<string> refsNotFound = new List<string>();

        string theBigAssMasterString = "";

        foreach(string file in filesToSearch)
        {
            string textFromFile = System.IO.File.ReadAllText(file);
            theBigAssMasterString += textFromFile + " ";
        }

        UnityEngine.Debug.Log("The bigass string is " + theBigAssMasterString.Length + " characters long. Wow.");

        foreach (string str in refsToSearch)
        {
            if (!theBigAssMasterString.Contains(str))
            {
                refsNotFound.Add(str);
            }
        }

        foreach (string str in refsNotFound)
        {
            UnityEngine.Debug.Log("Ref not found anywhere: " + str);
        }

        return "Done!"; */
    }

    object PrintAllStairs(params string[] args)
    {
        Map m = MapMasterScript.activeMap;

        foreach (Stairs st in m.mapStairs)
        {
            UnityEngine.Debug.Log(st.actorUniqueID + " Stairs Up? " + st.stairsUp + " Destination? " + st.NewLocation.GetName() + " Floor: " + st.NewLocation.floor + "/" + st.newLocationID + " Position? " + st.GetPos() + " Enabled?" + st.actorEnabled);
        }

        return "Done!";
    }

    object CheckItemMods(params string[] args)
    {
        if (args.Length == 0)
        {
            return "Need to specify item ID.";
        }

        int ID;
        if (Int32.TryParse(args[1], out ID))
        {
            Actor find = GameMasterScript.gmsSingleton.TryLinkActorFromDict(ID);
            if (find == null)
            {
                return "Item doesn't exist.";
            }
            if (find.GetActorType() != ActorTypes.ITEM)
            {
                return "Not an item.";
            }
            Item itm = find as Item;
            if (!itm.IsEquipment())
            {
                return "Not equipment.";
            }

            Equipment eq = find as Equipment;
            eq.GetNonAutomodCount();

            return "Printed mods for " + eq.actorUniqueID;
        }
        else
        {
            return "Invalid ID.";
        }
    }

    object SpawnStairsUp(params string[] args)
    {
        Map m = MapMasterScript.activeMap;

        foreach (Stairs st in m.mapStairs)
        {
            if (!st.stairsUp)
            {
                Map newLoc = st.NewLocation;
                if (newLoc.IsMainPath())
                {
                    return "Already has main path stairs up!";
                }
            }
        }

        Stairs newStairs = m.SpawnStairs(false);
        MapMasterScript.singletonMMS.SpawnStairs(newStairs);
        Map pointToLocation = MapMasterScript.theDungeon.FindFloor(m.effectiveFloor + 1);
        newStairs.NewLocation = pointToLocation;
        newStairs.newLocationID = pointToLocation.mapAreaID;
        UnityEngine.Debug.Log("Spawning new stairs up on main path, located at " + newStairs.GetPos());

        return "Done!";
    }

    object CheckTileInfo(params string[] args)
    {
        Vector2 tile = GameMasterScript.heroPCActor.GetPos();
        MapTileData mtd = MapMasterScript.GetTile(tile);

        int decorIndex = -1;
        if (mtd.CheckTag(LocationTags.HASDECOR))
        {
            decorIndex = mtd.indexOfDecorSpriteInAtlas;
        }
        if (mtd.CheckTag(LocationTags.GRASS))
        {
            //add = "Grass " + mtd.indexOfGrassSpriteInAtlas;
        }

        //string txt = mtd.indexOfGrassSpriteInAtlas + " " + mtd.visualGrassTileType + " " + mtd.visualTileType + " " + mtd.CheckTag(LocationTags.GRASS) + " " + mtd.CheckTag(LocationTags.GRASS2) + " " + mtd.indexOfDecorSpriteInAtlas + " " + mtd.CheckTag(LocationTags.HASDECOR);

        //string txt = mtd.CheckTag(LocationTags.SECRET) + " " + MapMasterScript.activeMap.exploredTiles[(int)mtd.pos.x, (int)mtd.pos.y] + " " + MapMasterScript.GetAreaID(mtd.pos) + " " + mtd.tileVisualSet + " " + mtd.indexOfSpriteInAtlas + " " + mtd.visualTileType;

        //string txt = mtd.pos + " Area: " + mtd.roomID + " " + mtd.tileType + " " + mtd.tileVisualSet + " " + mtd.indexOfSpriteInAtlas + " Secret? " + mtd.CheckTag(LocationTags.SECRET) + " Edge tile? " + mtd.CheckTag(LocationTags.EDGETILE) + " Num actors? " + mtd.GetAllActors().Count + " DT? " + mtd.GetAllTargetable().Count + " Decor index " + decorIndex + " Grass " + add;

        //tile.x -= 1f;
        //mtd = MapMasterScript.GetTileType(tile);
        //string txt2 = " WEST: " + mtd.pos + " Area: " + mtd.roomID + " " + mtd.tileType + " Secret? " + mtd.CheckTag(LocationTags.SECRET) + " Edge tile? " + mtd.CheckTag(LocationTags.EDGETILE)";

        //string txt3 + txt + txt2;

        //UnityEngine.Debug.Log("Tile type is: " + mtd.tileType + " Solid tag? " + mtd.CheckTag(LocationTags.SOLIDTERRAIN) + " Water/Lava/Elec/Mud? " + mtd.CheckTag(LocationTags.WATER) + " " + mtd.CheckTag(LocationTags.LAVA) + " " + mtd.CheckTag(LocationTags.ELECTRIC) + " Mud: " + mtd.CheckTag(LocationTags.MUD) + " Water: " + mtd.tags[(int)LocationTags.WATER] + " SumMud: " + mtd.tags[(int)LocationTags.SUMMONEDMUD]);
        foreach (Actor act in mtd.GetAllActors())
        {
            UnityEngine.Debug.Log(act.actorRefName);
        }

        return "Done!";
    }

    object CheckTile(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Specify direction: east, west, south, north";
        }

        Vector2 dirToUse = Vector2.zero;

        switch(args[1].ToLowerInvariant())
        {
            case "north":
            default:
                dirToUse = MapMasterScript.xDirections[(int)Directions.NORTH];
                break;
            case "west":
                dirToUse = MapMasterScript.xDirections[(int)Directions.WEST];
                break;
            case "east":
                dirToUse = MapMasterScript.xDirections[(int)Directions.EAST];
                break;
            case "south":
                dirToUse = MapMasterScript.xDirections[(int)Directions.SOUTH];
                break;
        }
        Vector2 tile = GameMasterScript.heroPCActor.GetPos() + dirToUse;
        MapTileData mtd = MapMasterScript.GetTile(tile);
        UnityEngine.Debug.Log(mtd.tileType + " " + mtd.GetAllActors().Count + " Mon/Player collidable? " + mtd.monCollidable + " " + mtd.playerCollidable + " Grass? " + mtd.indexOfGrassSpriteInAtlas + " " + mtd.visualGrassTileType + " " + mtd.CheckTag(LocationTags.GRASS));
        UnityEngine.Debug.Log("Hole? " + mtd.CheckTag(LocationTags.HOLE) + " Has Blocker? " + mtd.CheckForSpecialMapObjectType(SpecialMapObject.BLOCKER));

        foreach (Actor act in mtd.GetAllActors())
        {
            UnityEngine.Debug.Log(act.actorRefName + " " + act.GetActorType() + " " + act.isInDeadQueue + " " + act.destroyed + " " + act.turnsToDisappear + " " + act.maxTurnsToDisappear);
        }
        return "Done!";
    }

    public static object SkipFloors(params string[] args)
    {
        if (args.Length <= 1)
        {
            return "Specify # of floors!";
        }
        int floor;
        if (Int32.TryParse(args[1], out floor))
        {
            GameMasterScript.mms.SwitchFloors(floor, true);
            return ("Done");
        }

        return ChangeFloorsViaName(args);
    }

    public static object ChangeFloorsViaName(params string[] args)
    {
        string strFloorData = "";
        int iFloor = 1;
        for (int t = 1; t < args.Length; t++)
        {
            //if this is a number, it's our floor
            int iTestVal;
            if (Int32.TryParse(args[t], out iTestVal))
            {
                iFloor = iTestVal;
            }

            //otherwise, it is part of the area name
            else
            {
                if (strFloorData == "")
                {
                    strFloorData = args[t];
                }
                else
                {
                    strFloorData += " " + args[t];
                }
            }
        }

        if (GameMasterScript.mms.TrySwitchFloor(strFloorData, iFloor))
        {
            return "Changed floors!";
        }

        return "No floor data found that matches '" + strFloorData + "'";
    }

    object RandomItems(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Specify count of new items.";
        }
        int number = 1;
        if (!int.TryParse(args[1], out number))
        {
            number = 1;
        }
        for (int i = 0; i < number; i++)
        {
            RandomItem(null);
        }

        return "Done!";
    }

    object RandomItem(params string[] args)
    {
        List<Item> allItems = GameMasterScript.masterItemList.Values.ToList();

        string iRef = LootGeneratorScript.GetLootTable("allitems").GetRandomActorRef();

        /* Item template = allItems[UnityEngine.Random.Range(0, allItems.Count)];
        while (string.IsNullOrEmpty(template.spriteRef) || template.challengeValue == 999f || string.IsNullOrEmpty(template.displayName))
        {
            template = allItems[UnityEngine.Random.Range(0, allItems.Count)];
        }
        Item newItem = null; */

        Item template = GameMasterScript.masterItemList[iRef];
        Item newItem = null;

        switch(template.itemType)
        {
            case ItemTypes.ARMOR:
                Armor armor = new Armor();
                newItem = armor;
                break;
            case ItemTypes.OFFHAND:
                Offhand oh = new Offhand();
                newItem = oh;
                break;
            case ItemTypes.WEAPON:
                Weapon w = new Weapon();
                newItem = w;
                break;
            case ItemTypes.ACCESSORY:
                Accessory acc = new Accessory();
                newItem = acc;
                break;
            case ItemTypes.CONSUMABLE:
                Consumable c = new Consumable();
                newItem = c;
                break;
            case ItemTypes.EMBLEM:
                Emblem e = new Emblem();
                newItem = e;
                break;
        }

        newItem.CopyFromItem(template);

        newItem.SetUniqueIDAndAddToDict();

        newItem.RebuildDisplayName();

        List<MapTileData> nearbyTilesToPlayer = new List<MapTileData>();
        nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
        foreach (MapTileData testTile in nearbyTilesToPlayer)
        {
            if (!testTile.IsCollidable(GameMasterScript.heroPCActor))
            {
                newItem.areaID = MapMasterScript.activeMap.CheckMTDArea(testTile);
                GameMasterScript.SpawnItemAtPosition(newItem, testTile.pos);
                return "Spawned an item!";
            }
        }
        return "Done!";
    }

    object MonsterBalance(params string[] args)
    {
        StringBuilder sb = new StringBuilder();

        List<MonsterTemplateData> monList = GameMasterScript.masterMonsterList.Values.ToList();

        monList.Sort((a, b) => (a.CompareTo(b)));

        float[] averageDamage = new float[15];
        float[] averageWeaponPower = new float[15];
        int[] countMonsters = new int[15];

        foreach (MonsterTemplateData mtd in monList)
        {
            if (!mtd.autoSpawn) continue;
            if (mtd.isBoss) continue;
            if (mtd.baseLevel >= countMonsters.Length)
            {
                UnityEngine.Debug.Log(mtd.refName + " out of level range.");
                continue;
            }
            countMonsters[mtd.baseLevel]++;
            Monster mn = MonsterManagerScript.CreateMonster(mtd.refName, false, false, false, 0f, 0f, false);
            float baseDamage = 0.0f;
            float withCrit = 0.0f;
            float withAccuracyAndCrit = 0.0f;
            float withAccCritCT = 0.0f;
            if (mn.myEquipment.IsWeaponRanged(mn.myEquipment.GetWeapon()))
            {
                baseDamage = mn.cachedBattleData.physicalWeaponDamage + 6;
                withCrit = baseDamage + (baseDamage * mn.cachedBattleData.critMeleeChance * mn.cachedBattleData.critMeleeDamageMult);
            }
            else
            {
                baseDamage = mn.cachedBattleData.physicalWeaponDamage + 6;
                withCrit = baseDamage + (baseDamage * mn.cachedBattleData.critRangedChance * mn.cachedBattleData.critRangedDamageMult);
            }

            withAccuracyAndCrit = withCrit * (mtd.accuracy / 100f);
            withAccCritCT = withAccuracyAndCrit * (mtd.chargetime / 100f);

            sb.Append(mtd.refName + " (" + mtd.baseLevel + "/" + mtd.challengeValue + ") Dmg per PC Turn: " + withAccCritCT + " WEapPower: " + mn.myEquipment.GetWeapon().power + "\n");
            //Base: " + baseDamage + " w/Crit: " + withCrit + " w/ Acc/Crit: " + withAccuracyAndCrit + "\n");
            averageDamage[mtd.baseLevel] += withAccCritCT;
            averageWeaponPower[mtd.baseLevel] += mn.myEquipment.GetWeapon().power;
        }

        for (int i = 0; i < averageDamage.Length; i++)
        {
            averageDamage[i] = averageDamage[i] / (countMonsters[i]);
            averageWeaponPower[i] = averageWeaponPower[i] / (countMonsters[i]);
            sb.Append("\n\nAverage Damage Level " + i + " is " + averageDamage[i] + " Weapon power is: " + averageWeaponPower[i]);
        }

        UnityEngine.Debug.Log(sb.ToString());
        return "Done.";
    }

    object TickGameTime(params string[] args)
    {
        bool timePassedAlready = false;
        if (args.Length > 1)
        {
            int outDayValue;
            if (Int32.TryParse(args[1], out outDayValue))
            {
                GameMasterScript.gmsSingleton.TickGameTime(outDayValue, true);
                timePassedAlready = true;
            }
        }

        if (!timePassedAlready)
        {
            GameMasterScript.gmsSingleton.TickGameTime(1, true);
        }


        return "Done!";
    }

    object SpawnLucidOrb(params string[] args)
    {
        if (args.Length != 2)
        {
            return "Specify a valid magic mod.";
        }

        string modName = args[1];

        if (!GameMasterScript.masterMagicModList.ContainsKey(modName))
        {
            var candidates = new List<string>();
            string strItMightBeThisThough = null;
            foreach (var kvp in GameMasterScript.masterMagicModList)
            {
                string strItemName = StringManager.GetLocalizedStringOrFallbackToEnglish(kvp.Value.modName);
                if (kvp.Key.ToLower().Contains(modName))
                {
                    candidates.Add(kvp.Key);
                    strItMightBeThisThough = kvp.Key;
                }
                else if (strItemName.ToLower().Contains(modName))
                {
                    candidates.Add("'" + strItemName + "'(" + kvp.Key + ")");
                    strItMightBeThisThough = kvp.Key;
                }
            }

            //if there is exactly one candidate, spawn that.
            if (candidates.Count == 1)
            {
                modName = strItMightBeThisThough;
            }
            //if there are no candidates, report back with great sadness.
            else if (candidates.Count == 0)
            {
                return "No magic mod found with ref '" + modName + "', and no close matches exist either.";
            }
            //otherwise, talk about all the candidates
            else
            {
                var sb = new StringBuilder();
                sb.Append("No items found with ref '" + modName + "', close matches: ");
                string strDelimiter = "";
                foreach (var s in candidates)
                {
                    sb.Append(strDelimiter);
                    sb.Append(s);
                    strDelimiter = ", ";
                }

                return sb.ToString();
            }

            if (string.IsNullOrEmpty(modName))
            {
                return "That is not a valid magic mod.";
            }
        }

        Item baseOrb = LootGeneratorScript.CreateItemFromTemplateRef("orb_itemworld", 1.0f, 0f, false);
        baseOrb.SetOrbMagicModRef(modName);        
        baseOrb.RebuildDisplayName();

        MapTileData tile = MapMasterScript.FindNearbyEmptyTileForItem(GameMasterScript.heroPCActor.GetPos());

        MapMasterScript.activeMap.PlaceActor(baseOrb, tile);
        MapMasterScript.singletonMMS.SpawnItem(baseOrb);

        return "Done!";
    }

    object SpawnSkillOrb(params string[] args)
    {
        Item baseOrb = LootGeneratorScript.CreateItemFromTemplateRef("orb_itemworld", 1.0f, 0f, false);

        List<MagicMod> possible = new List<MagicMod>();

        foreach (MagicMod mm in GameMasterScript.masterMagicModList.Values)
        {
            if (!mm.lucidOrbsOnly) continue;
            possible.Add(mm);
        }

        string modRef = possible[UnityEngine.Random.Range(0, possible.Count)].refName;
        Consumable c = baseOrb as Consumable;
        c.SetOrbMagicModRef(modRef);

        baseOrb.RebuildDisplayName();

        MapTileData tile = MapMasterScript.FindNearbyEmptyTileForItem(GameMasterScript.heroPCActor.GetPos());

        MapMasterScript.activeMap.PlaceActor(baseOrb, tile);
        MapMasterScript.singletonMMS.SpawnItem(baseOrb);

        return "Done!";
    }
    
    object SpawnItem(params string[] args)
    {
        if (args.Length <= 1)
        {
            return "Specify an item ref!";
        }

        string itemName = "";
        for (int i = 1; i < args.Length; i++)
        {
            itemName += args[i];
            if (i < args.Length - 1)
            {
                itemName += " ";
            }
        }

        //Look for a ref if it fits, if not start guessing at names.
        return SpawnItem(itemName);
    }

    public static object GenerateRelic(string[] args)
    {
        Item legRelic = LegendaryMaker.CreateNewLegendaryItem(UnityEngine.Random.Range(1f, 2.2f));
        MapTileData tileForRelic = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true, true, true);
        Item copy = LootGeneratorScript.CreateItemFromTemplateRef(legRelic.actorRefName, 2f, 0f, false, true);
        copy.SetActorData("grc", 1);
        MapMasterScript.activeMap.PlaceActor(copy, tileForRelic);
        MapMasterScript.singletonMMS.SpawnItem(copy);

        legRelic.RebuildDisplayName();
        string returnText = "Created: " + legRelic.displayName + " (Type: " + legRelic.itemType + ")";
        GameLogScript.GameLogWrite(returnText, GameMasterScript.heroPCActor);
        return "Done!";        
    }

    public static object GenerateMonster(string[] args)
    {
        MysteryDungeon testMD = new MysteryDungeon("test");
        testMD.monsterFamilies.AddToTable("frogs", 100);
        testMD.monsterFamilies.AddToTable("bandits", 100);
        testMD.monsterFamilies.AddToTable("hybrids", 100);
        testMD.monsterFamilies.AddToTable("robots", 100);
        testMD.monsterFamilies.AddToTable("snakes", 100);
        testMD.monsterFamilies.AddToTable("insects", 100);
        testMD.monsterFamilies.AddToTable("beasts", 100);
        testMD.monsterFamilies.AddToTable("spirits", 100);
        MonsterTemplateData mtd = MonsterMaker.CreateNewMonster(UnityEngine.Random.Range(1, 15), testMD);

        string returnText = "Created: " + mtd.monsterName + " (Family: " + mtd.monFamily + ")";
        GameLogScript.GameLogWrite(returnText, GameMasterScript.heroPCActor);
        return "Done!";        
    }

    /// <summary>
    /// Spawns an item nearby
    /// </summary>
    /// <param name="strInput">An item ref, or part of the item name if unique. Do your best.</param>
    /// <param name="iQuantity"></param>
    /// <returns></returns>
    public static object SpawnItem(string strInput, int iQuantity = 1)
    {
        strInput = strInput.ToLower();

        //check the input, or "item_" + input, just to see.
        bool bFoundARef = GameMasterScript.GetItemFromRef(strInput) != null;
        if (!bFoundARef && GameMasterScript.GetItemFromRef("item_" + strInput) != null)
        {
            bFoundARef = true;
            strInput = "item_" + strInput;
        }

        //if not, take a look at all the items we have, and see what's a close guess.
        if (bFoundARef == false)
        {
            var candidates = new List<string>();
            string strItMightBeThisThough = null;
            foreach (var kvp in GameMasterScript.masterItemList)
            {
                string strItemName = StringManager.GetLocalizedStringOrFallbackToEnglish(kvp.Value.displayName);
                if (kvp.Key.ToLower().Contains(strInput))
                {
                    candidates.Add(kvp.Key);
                    strItMightBeThisThough = kvp.Key;
                }
                else if (strItemName.ToLower().Contains(strInput))
                {
                    candidates.Add("'" + strItemName + "'(" + kvp.Key + ")");
                    strItMightBeThisThough = kvp.Key;
                }
            }

            //if there is exactly one candidate, spawn that.
            if (candidates.Count == 1)
            {
                strInput = strItMightBeThisThough;
            }
            //if there are no candidates, report back with great sadness.
            else if (candidates.Count == 0)
            {
                return "No items found with ref '" + strInput + "', and no close matches exist either.";
            }
            //otherwise, talk about all the candidates
            else
            {
                var sb = new StringBuilder();
                sb.Append("No items found with ref '" + strInput + "', close matches: ");
                string strDelimiter = "";
                foreach (var s in candidates)
                {
                    sb.Append(strDelimiter);
                    sb.Append(s);
                    strDelimiter = ", ";
                }

                return sb.ToString();
            }


        }

        //spawn them!
        for (int t = 0; t < iQuantity; t++)
        {
            Item newItem = LootGeneratorScript.CreateItemFromTemplateRef(strInput, 1.0f, 1.0f, false);

            // Spawn
            List<MapTileData> nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
            foreach (MapTileData testTile in nearbyTilesToPlayer)
            {
                if (!testTile.IsCollidable(GameMasterScript.heroPCActor))
                {
                    newItem.areaID = MapMasterScript.activeMap.CheckMTDArea(testTile);
                    GameMasterScript.SpawnItemAtPosition(newItem, testTile.pos);
                    break;
                }
            }
        }

        return "Spawned " + iQuantity + " " + strInput + "(s).";
    }

    object ScalePlayerToLevelOrCV(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Specify a level (1-15) or a CV (1.0 to 1.9)";
        }

        int pLevel = 1;

        float challengeValue = 1.0f;

        if (args[1].Contains('.'))
        {
            challengeValue = float.Parse(args[1]);
            foreach (int value in BalanceData.DICT_LEVEL_TO_CV.Keys)
            {
                if (CustomAlgorithms.CompareFloats(BalanceData.DICT_LEVEL_TO_CV[value], challengeValue))
                {
                    pLevel = value;
                    break;
                }
                if (Mathf.Abs(BalanceData.DICT_LEVEL_TO_CV[value] - challengeValue) <= 0.05f)
                {
                    // Backup value that is pretty close. Use this if all else fails.
                    pLevel = value;
                }
            }
        }
        else
        {
            pLevel = Int32.Parse(args[1]);
            challengeValue = BalanceData.DICT_LEVEL_TO_CV[pLevel];
        }

        int initialLevel = GameMasterScript.heroPCActor.myStats.GetLevel();

        // Adjust level, stats, give JP
        for (int i = 0; i < pLevel - initialLevel; i++)
        {
            GameMasterScript.heroPCActor.myStats.LevelUp();
        }
        
        GameMasterScript.heroPCActor.AddJP(pLevel * 500f);
        GameMasterScript.heroPCActor.ChangeMoney(pLevel * 600);

        int numConsumables = 4 + (pLevel * 2);
        int numFoodAndMeals = 3 + (pLevel * 3);
        int numLegendaries = 0;
        if (pLevel >= 7) numLegendaries++;
        if (pLevel >= 13) numLegendaries++;

        int numGuaranteedEquipmentMods = 0;
        if (pLevel >= 4) numGuaranteedEquipmentMods++;
        if (pLevel >= 8) numGuaranteedEquipmentMods++;
        if (pLevel >= 12) numGuaranteedEquipmentMods++;
        if (pLevel >= 15) numGuaranteedEquipmentMods++;


        // Spawn items of a close enough level, value.
        Dictionary<string, int> tablesToQuantities = new Dictionary<string, int>()
        {
            { "weapons", 7 },
            { "armor", 4 },
            { "accessories", 6 },
            { "offhands", 6 },
            { "consumables", numConsumables },
            { "legendary", numLegendaries },
            { "food_and_meals", numFoodAndMeals }
        };

        float magicChance = pLevel * 0.075f;

        foreach (string table in tablesToQuantities.Keys)
        {
            for (int i = 0; i < tablesToQuantities[table]; i++)
            {
                Item generatedItem = LootGeneratorScript.GenerateLootFromTable(challengeValue, magicChance, table);

                float maxVariance = 0.25f;
                if (table == "accessories" || table == "legendary") maxVariance = 0.5f;
                if (table == "armor") maxVariance = 0.3f;

                while (generatedItem.IsEquipment() && Mathf.Abs(generatedItem.challengeValue - challengeValue) > maxVariance)
                {
                    // Too stronk or too weak? Try again.
                    generatedItem = LootGeneratorScript.GenerateLootFromTable(challengeValue, magicChance, table);
                }
                if (generatedItem.IsEquipment())
                {
                    for (int x = generatedItem.GetNonAutomodCount(); x < numGuaranteedEquipmentMods; x++)
                    {
                        EquipmentBlock.MakeMagical(generatedItem, challengeValue, false);
                    }
                }
                GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(generatedItem, true);
            }
        }

        return "Done!";
    }

    object SpawnItemRMM(params string[] args)
    {
        if (args.Length <= 2)
        {
            return "Specify an item ref and mod count!";
        }
        else
        {
            string itemName = "";
            /*for (int i = 1; i < args.Length; i++)
            {
                itemName += args[i];
                if (i < args.Length - 1)
                {
                    itemName += " ";
                }
            }*/

            itemName = args[1];

            float cv = 1.0f;
            if (args.Length >= 4)
            {
                cv = float.Parse(args[3]);
            }

            UnityEngine.Debug.Log("Item name is " + itemName);
            Item newItem = LootGeneratorScript.CreateItemFromTemplateRef(itemName, cv, 1.0f, false);
            if (newItem == null)
            {
                return "Item template not found.";
            }

            int mods = Int32.Parse(args[2]);

            cv = newItem.challengeValue;
            if (args.Length >= 4)
            {
                cv = float.Parse(args[3]);
            }

            for (int i = 0; i < mods; i++)
            {
                EquipmentBlock.MakeMagical(newItem, cv, false);
            }
            // Spawn
            List<MapTileData> nearbyTilesToPlayer = new List<MapTileData>();
            nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
            foreach (MapTileData testTile in nearbyTilesToPlayer)
            {
                if (!testTile.IsCollidable(GameMasterScript.heroPCActor))
                {
                    newItem.areaID = MapMasterScript.activeMap.CheckMTDArea(testTile);
                    GameMasterScript.SpawnItemAtPosition(newItem, testTile.pos);
                    return "Spawned an item!";
                }
            }


        }
        return "Item not found.";
    }

    object SpawnItemMM(params string[] args)
    {
        if (args.Length <= 1)
        {
            return "Specify an item ref!";
        }
        else
        {
            string itemName = "";
            /*for (int i = 1; i < args.Length; i++)
            {
                itemName += args[i];
                if (i < args.Length - 1)
                {
                    itemName += " ";
                }
            }*/

            itemName = args[1];
            //UnityEngine.Debug.Log("Item name is " + itemName);
            Item newItem = LootGeneratorScript.CreateItemFromTemplateRef(itemName, 1.0f, 1.0f, false);
            if (newItem == null)
            {
                return "Item template not found.";
            }

            string mmName = args[2];

            MagicMod template = MagicMod.FindModFromName(mmName);
            if (template == null)
            {
                return "Magic Mod template not found";
            }

            EquipmentBlock.MakeMagicalFromMod(newItem, template, true, true, false);
            // Spawn
            List<MapTileData> nearbyTilesToPlayer = new List<MapTileData>();
            nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
            foreach (MapTileData testTile in nearbyTilesToPlayer)
            {
                if (!testTile.IsCollidable(GameMasterScript.heroPCActor))
                {
                    newItem.areaID = MapMasterScript.activeMap.CheckMTDArea(testTile);
                    GameMasterScript.SpawnItemAtPosition(newItem, testTile.pos);
                    return "Spawned an item!";
                }
            }


        }
        return "Item not found.";
    }

    object SpawnNPC(params string[] args)
    {
        if (args.Length <= 1)
        {
            return "Specify a monster ref!";
        }
        else
        {
            string monsterName = "";
            for (int i = 1; i < args.Length; i++)
            {
                monsterName += args[i];
                if (i < args.Length - 1)
                {
                    monsterName += " ";
                }
            }
            NPC newNPC = NPC.CreateNPC(monsterName);
            if (newNPC == null)
            {
                return "NPC not found";
            }

            // Spawn
            List<MapTileData> nearbyTilesToPlayer = new List<MapTileData>();
            nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
            foreach (MapTileData testTile in nearbyTilesToPlayer)
            {
                if (!testTile.IsCollidable(GameMasterScript.genericMonster))
                {
                    newNPC.SetSpawnPosXY((int)testTile.pos.x, (int)testTile.pos.y);
                    MapMasterScript.activeMap.AddActorToLocation(testTile.pos, newNPC);
                    MapMasterScript.activeMap.AddActorToMap(newNPC);
                    MapMasterScript.singletonMMS.SpawnNPC(newNPC);
                    newNPC.RefreshShopInventory(50);
                    return "Spawned an NPC!";
                }
            }


        }
        return "Done!";
    }

    object SpawnDestructible(params string[] args)
    {
        if (args.Length <= 1)
        {
            return "Specify a destructible ref!";
        }
        else
        {
            string dtName = "";
            for (int i = 1; i < args.Length; i++)
            {
                dtName += args[i];
                if (i < args.Length - 1)
                {
                    dtName += " ";
                }
            }

            if (!GameMasterScript.masterMapObjectDict.ContainsKey(dtName))
            {
                return "Object reference not found!";
            }

            MapTileData startTile = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true);
            Vector2 pos = startTile.pos;

            Destructible sd = GameMasterScript.SummonDestructible(GameMasterScript.heroPCActor, GameMasterScript.masterMapObjectDict[dtName], pos, 999);
            if (sd == null)
            {
                return "NPC not found";
            }

            //MapMasterScript.activeMap.PlaceActor(sd, startTile);
            //MapMasterScript.singletonMMS.SpawnDestructible(sd);


        }
        return "Done!";
    }

    object LearnSkill(params string[] args)
    {
        if (args.Length == 0) return "Specify skill ref!";

        AbilityScript abil;

        if (GameMasterScript.masterAbilityList.TryGetValue(args[1], out abil))
        {
            GameMasterScript.heroPCActor.LearnAbility(abil, true, true, false);
        }
        else
        {
            return "Ability ref not found.";
        }

        return "Done!";
    }
    object GenerateMonsterList(params string[] args)
    {
        string strPath = Path.Combine(CustomAlgorithms.GetPersistentDataPath(), "monstars_for_fervir.txt");
        using (StreamWriter sw = new StreamWriter(new FileStream(strPath, FileMode.Create)))
        {
            foreach (MonsterTemplateData montemplate in GameMasterScript.masterMonsterList.Values)
            {
                sw.WriteLine(montemplate.refName + ", " + montemplate.monsterName);
            }

            sw.Close();
        }

        return "List generated at " + strPath;
    }

    object ConvertMiscXMLToLocalizedText(params string[] args)
    {
        var dictWords = StringManager.Debug_GetDictionaryWithMiscAndLocalizationCombined();


        string strFileName = "en_us_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" +
                             DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" +
                             DateTime.Now.Second + ".txt";

        string strPath = Path.Combine(CustomAlgorithms.GetPersistentDataPath(), strFileName);
        using (StreamWriter sw = new StreamWriter(new FileStream(strPath, FileMode.Create)))
        {
            foreach (var kvp in dictWords)
            {
                sw.WriteLine(kvp.Key + '\t' + kvp.Value);
            }

            sw.Close();
        }
        string strRet = "Combined miscstrings.xml with en_us.txt.\n" +
                        "en_us is authoritative. Make changes there, miscstrings is deprecated!\n\n";

        return strRet + "New en_us generated at " + strPath;
    }


    public static object PrintAllMaps(params string[] args)
    {
        UnityEngine.Debug.Log("You are on floor " + MapMasterScript.activeMap.floor);

        for (int i = 0; i < MapMasterScript.theDungeon.maps.Count; i++)
        {
            UnityEngine.Debug.Log("Map " + MapMasterScript.theDungeon.maps[i].floor + " name " + MapMasterScript.theDungeon.maps[i].GetName());
        }

        return "Done!";
    }

    public static object SpawnMonster(params string[] args)
    {
        if (args.Length <= 1)
        {
            return "Specify a monster ref!";
        }
        else
        {
            string strAdditionalRetText = "";
            string monsterName = "";
            Vector2 vSpawnLocation = Vector2.zero;
            ChampionMod cm = null;
            bool bShouldChampify = false;
            for (int i = 1; i < args.Length; i++)
            {
                //Use style (x,y) to spawn the creature at some delta from your own location
                if (args[i].Contains("("))
                {
                    string strLoc = args[i];
                    strLoc = strLoc.Replace("(", "");
                    strLoc = strLoc.Replace(")", "");
                    string[] splitLoc = strLoc.Split(',');

                    int xDelta = 0;
                    int yDelta = 0;
                    Int32.TryParse(splitLoc[0], out xDelta);
                    Int32.TryParse(splitLoc[1], out yDelta);

                    vSpawnLocation = GameMasterScript.GetHeroActor().GetPos();
                    vSpawnLocation.x += xDelta;
                    vSpawnLocation.y += yDelta;
                }
                else if (args[i] == "champ")    //allow champ mod from this spawn
                {
                    bShouldChampify = true;
                    //roll forward to see if we are asking for a specific mod
                    i++;
                    if (i < args.Length)
                    {
                        cm = Monster.FindMod(args[i]);
                        if (cm == null)
                        {
                            //wellll
                            cm = Monster.FindMod("monmod_" + args[i]);
                        }

                        if (cm == null)
                        {
                            //if we didn't find a valid champ mod, it's possible we just said "champ" and sent in no mod.
                            //in that case we need to roll back one if the arg we're looking at is actually a location value
                            if (args[i].Contains("("))
                            {
                                i--;
                            }

                            strAdditionalRetText += " +Champion(Random)";

                        }
                        else
                        {
                            strAdditionalRetText += " +Champion(" + cm.displayName + ")";
                        }
                    }
                }
                else // this is just part of the name then
                {
                    monsterName += args[i];
                    if (i < args.Length - 1)
                    {
                        monsterName += " ";
                    }
                }
            }

            //trim trailing " " off monster name
            monsterName = monsterName.TrimEnd(' ');

            Monster newMon = MonsterManagerScript.CreateMonster(monsterName, true, true, false, 0f, 0f, false);
            if (newMon == null)
            {
                //We'll look for refnames that contain what we're asking for, and as a last resort we'll spawn
                //something that has the string in the ref or friendly name
                HashSet<string> strCandidates = new HashSet<string>();

                //Try the friendly name
                foreach (MonsterTemplateData montemplate in GameMasterScript.masterMonsterList.Values)
                {
                    if (montemplate.monsterName.ToLowerInvariant() == monsterName.ToLowerInvariant())
                    {
                        newMon = MonsterManagerScript.CreateMonster(montemplate.refName, true, true, false, 0f, 0f, false);
                        break;
                    }
                    //if what we asked for lives inside this friendly name, it is a candidate
                    if (montemplate.monsterName.ToLowerInvariant().Contains(monsterName.ToLowerInvariant()) ||
                        montemplate.refName.ToLowerInvariant().Contains(monsterName.ToLowerInvariant()))
                    {
                        strCandidates.Add(montemplate.refName.Replace("mon_", ""));
                    }
                }

                //Maybe we just forgot the prefix "mon_" ?
                if (newMon == null)
                {
                    newMon = MonsterManagerScript.CreateMonster("mon_" + monsterName, true, true, false, 0f, 0f, false);
                }

                if (newMon == null)
                {
                    //youtried.jpg
                    string strYouTried = "Monster '" + monsterName + "' not found.";
                    if (strCandidates.Count > 0)
                    {
                        string strTheseBros = string.Join(", ", strCandidates.ToArray());
                        strTheseBros = " Possible matches: " + strTheseBros;
                        strYouTried += strTheseBros;
                    }
                    return strYouTried;
                }
            }

            MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, newMon, false);

            //we has mon! Did we champion him up? 
            if (bShouldChampify)
            {
                if (cm != null)
                {
                    newMon.MakeChampionFromMod(cm);
                }
                else
                {
                    newMon.MakeChampion();
                }
            }

            // Spawn
            bool bFoundGoodSpawnLoc = vSpawnLocation != Vector2.zero;
            MapTileData spawnTile = MapMasterScript.GetTile(vSpawnLocation);
            if (!bFoundGoodSpawnLoc)
            {
                List<MapTileData> nearbyTilesToPlayer = new List<MapTileData>();
                nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
                foreach (MapTileData testTile in nearbyTilesToPlayer)
                {
                    if (!testTile.IsCollidable(GameMasterScript.genericMonster))
                    {
                        spawnTile = testTile;
                        vSpawnLocation = spawnTile.pos;
                        bFoundGoodSpawnLoc = true;
                    }
                }
            }

            //if there's no good spawnloc -- perhaps we're in a single empty space surrounded by solid stone? 
            //in this case just drop it on my face. Right on my face.
            if (!bFoundGoodSpawnLoc)
            {
                vSpawnLocation = GameMasterScript.GetHeroActor().GetPos();
                spawnTile = MapMasterScript.GetTile(vSpawnLocation);
                bFoundGoodSpawnLoc = true;
            }

            if (bFoundGoodSpawnLoc)
            {
                strAdditionalRetText += " @Loc(" + vSpawnLocation.ToString() + ")";
                newMon.startAreaID = MapMasterScript.activeMap.CheckMTDArea(spawnTile);
                newMon.SetSpawnPos(vSpawnLocation);
                MapMasterScript.activeMap.AddActorToLocation(spawnTile.pos, newMon);
                MapMasterScript.activeMap.AddActorToMap(newMon);
                MapMasterScript.singletonMMS.SpawnMonster(newMon);
                return "Spawned a " + newMon.actorRefName + strAdditionalRetText;
            }

            //did we get here somehow?
            if (newMon == null)
            {
                return "Monster '" + monsterName + "' not found.";
            }

            //weird failure case
            return "A monster was created but no valid location to spawn it was found.";
        }

    }

    object SpawnChampionMonster(params string[] args)
    {
        if (args.Length <= 1)
        {
            return "Specify a monster ref!";
        }
        else
        {
            string monsterName = args[1];
            Monster newMon = MonsterManagerScript.CreateMonster(monsterName, true, true, false, 0f, 0f, false);
            if (newMon == null)
            {
                return "Monster template not found.";
            }

            string champMod = args[2];
            ChampionMod cm = Monster.FindMod(champMod);
            newMon.MakeChampionFromMod(cm);

            // Spawn
            List<MapTileData> nearbyTilesToPlayer = new List<MapTileData>();
            nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
            foreach (MapTileData testTile in nearbyTilesToPlayer)
            {
                if (!testTile.IsCollidable(GameMasterScript.genericMonster))
                {
                    newMon.startAreaID = MapMasterScript.activeMap.CheckMTDArea(testTile);
                    newMon.SetSpawnPosXY((int)testTile.pos.x, (int)testTile.pos.y);
                    MapMasterScript.activeMap.AddActorToLocation(testTile.pos, newMon);
                    MapMasterScript.activeMap.AddActorToMap(newMon);
                    MapMasterScript.singletonMMS.SpawnMonster(newMon);
                    return "Spawned a monster!";
                }
            }


        }
        return "Monster not found.";
    }

    object Debug_TestAssetBundleLoading(params string[] args)
    {
        return GameMasterScript.Debug_TestAssetBundleLoading(args);
    }

    //adds a status effect to the hero or a given actor
    object Debug_AddStatusEffect(params string[] args)
    {
        if (args.Length < 2)
        {
            UIManagerScript.PlayCursorSound("Error");
            return ("AddStatus: needs an effect ref to add");
        }

        //the first arg should be the status effect to add
        string strStatusName = args[1];

        StatusEffect addMe = GameMasterScript.FindStatusTemplateByName(strStatusName);
        if (addMe == null)
        {
            UIManagerScript.PlayCursorSound("Error");
            return ("AddStatus: '" + strStatusName + "' isn't a valid status effect ref.");
        }

        int iUnitID = -1;
        Fighter targetFighter = GameMasterScript.heroPCActor;

        if (args.Length >= 3)
        {
            Int32.TryParse(args[2], out iUnitID);
        }
        if (iUnitID >= 0)
        {
            targetFighter = MapMasterScript.activeMap.FindActorByID(iUnitID) as Fighter;
            if (targetFighter == null)
            {
                UIManagerScript.PlayCursorSound("Error");
                return ("AddStatus: Unit ID '" + iUnitID + "' isn't a Fighter, or isn't an actor at all!");
            }
        }

        targetFighter.myStats.AddStatus(addMe, targetFighter);
        return ("AddStatus: Status '" + strStatusName + "' added to " + targetFighter.actorRefName + ": " + targetFighter.actorUniqueID);

    }

    object FindBoss(params string[] args)
    {
        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                if (mn.isItemBoss)
                {
                    GameLogScript.GameLogWrite("Memory King is " + mn.displayName + " in item world, dfloor " + mn.dungeonFloor + " ID " + mn.GetActorMap().mapAreaID + " Position: " + mn.GetPos(), GameMasterScript.heroPCActor);
                    return "Done!";
                }
            }
        }
        return "None found.";
    }

    object SetMyLevel(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Specify a level.";
        }

        GameMasterScript.heroPCActor.myStats.SetLevel(Int32.Parse(args[1]));
        return "Done!";
    }

    object AwardJP(params string[] args)
    {
        GameMasterScript.gmsSingleton.AwardJP(args.Length <= 1 ? 5000f : float.Parse(args[1]));
        return "Done!";
    }

    object AwardXP(params string[] args)
    {
        GameMasterScript.gmsSingleton.AwardXPFlat(args.Length <= 1 ? 5000f : float.Parse(args[1]), false);
        return "Done!";
    }

    object AwardMoney(params string[] args)
    {
        /*if (args.Length <= 1) {
    		return "Specify an amount!";
    	} */
        GameMasterScript.heroPCActor.ChangeMoney(5000);
        return "Done!";
    }

    object SharaModeInfo(params string[] args)
    {
        UnityEngine.Debug.Log("Hero job is: " + GameMasterScript.heroPCActor.myJob.jobEnum + ", game in shara mode: " + GameStartData.gameInSharaMode + " slot state: " + GameStartData.slotInSharaMode[GameStartData.saveGameSlot] + " GSD job: " + GameStartData.jobAsEnum);
        return "Done!";
    }

    object LevelUp(params string[] args)
    {
        GameMasterScript.heroPCActor.myStats.LevelUp();
        return "Done!";
    }

    object StartConversation(params string[] args)
    {
        Conversation c = GameMasterScript.FindConversation(args[1]);
        if (c == null)
        {
            return "No conversation named '" + args[1] + "' tho.";
        }

        UIManagerScript.StartConversationByRef(args[1], DialogType.STANDARD, null);
        return "beep";
    }

    object SpawnVFX(params string[] args)
    {
        if (args.Length < 2)
        {
            return "vfx [effectname] [(x,y)] where x,y is a delta from hero position.";
        }

        string strEffectName = args[1];

        int xDelta = 0;
        int yDelta = 0;

        int xOffset = 0;
        int yOffset = 0;

        if (args.Length > 2)
        {
            string strLoc = args[2];
            strLoc = strLoc.Replace("(", "");
            strLoc = strLoc.Replace(")", "");
            string[] splitLoc = strLoc.Split(',');

            Int32.TryParse(splitLoc[0], out xDelta);
            Int32.TryParse(splitLoc[1], out yDelta);
        }

        if (args.Length > 3)
        {
            string strLoc = args[3];
            strLoc = strLoc.Replace("(", "");
            strLoc = strLoc.Replace(")", "");
            string[] splitLoc = strLoc.Split(',');

            Int32.TryParse(splitLoc[0], out xOffset);
            Int32.TryParse(splitLoc[1], out yOffset);
        }

        var vSpawnLocation = GameMasterScript.GetHeroActor().GetPos();
        vSpawnLocation.x += xDelta;
        vSpawnLocation.y += yDelta;

        Vector2 vDelta = new Vector2(xOffset, yOffset);

        CombatManagerScript.GenerateDirectionalEffectAnimation(vSpawnLocation, vSpawnLocation + vDelta, strEffectName, true);

        return "pew pew";

    }

    /// <summary>
    /// Start a coroutine that bamfs the player to a new location
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    object TeleportPlayerToLocation(params string[] args)
    {
        if (args.Length < 3)
        {
            return "Send in an X and Y destination";
        }
        
        var dest = new Vector2( float.Parse(args[1]), float.Parse(args[2]));
        GameMasterScript.StartWatchedCoroutine(Cutscenes.GenericTeleportPlayer(dest, 2.0f, 0.1f));

        return "bamf!";
    }

    object FindThing(params string[] args)
    {
        if (args.Length != 2)
        {
            return "Specify an actor ID.";
        }
        int actorID = 0;
        if (!Int32.TryParse(args[1], out actorID))
        {
            UnityEngine.Debug.Log("Specified something other than number, searching...");
            foreach(Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act.actorRefName == args[1])
                {
                    actorID = act.actorUniqueID;
                    UnityEngine.Debug.Log("Found it! " + actorID + " is ID of " + args[1]);
                }
            }
            if (actorID == 0)
            {
                return "Couldn't find actor " + args[1] + " in this map.";
            }
        }
        Actor getAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(actorID);
        if (getAct == null)
        {
            return "ID " + actorID + " doesn't exist at all!";
        }
        if (!MapMasterScript.activeMap.actorsInMap.Contains(getAct))
        {
            return "Actor is not in this map. It's on floor " + getAct.dungeonFloor;
        }
        return "Actor's given pos: " + getAct.GetPos() + " Is it in that tile? " + MapMasterScript.GetTile(getAct.GetPos()).HasActor(getAct);

    }

    object WhereAmI(params string[] args)
    {
        return "Floor: " + MapMasterScript.activeMap.floor + " Area ID: " + MapMasterScript.activeMap.mapAreaID;
    }

    object RebuildRealmOfTheGods(params string[] args)
    {
        List<Map> mapsToRemove = new List<Map>();
        foreach(Map m in MapMasterScript.dictAllMaps.Values)
        {
            if (m.floor >= MapMasterScript.REALM_OF_GODS_START && m.floor <= MapMasterScript.REALM_OF_GODS_END)
            {
                mapsToRemove.Add(m);
            }
        }

        foreach(Map m in mapsToRemove)
        {
            MapMasterScript.theDungeon.RemoveMapByFloor(m.floor);
            MapMasterScript.dictAllMaps.Remove(m.mapAreaID);
            MapMasterScript.OnMapRemoved(m);
        }

        DLCManager.CreateAndConnectDLCMaps(MapMasterScript.REALM_OF_GODS_START, MapMasterScript.REALM_OF_GODS_END, true, MapMasterScript.FINAL_BOSS_FLOOR2);

        return "Done! Please save + quit.";
    }

    object GetLocalFlag(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Get Local Flag: Please specify flag";
        }

        return "Value of " + args[1] + " is: " + GameMasterScript.heroPCActor.ReadActorData(args[1]);
    }

    /// <summary>
    /// Unlocks a jorb
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    object UnlockJob(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Unlock Job: Please add a job to unlock";
        }

        string strJobbyJob = args[1].ToUpperInvariant();
        CharacterJobData cjd = CharacterJobData.GetJobData(strJobbyJob);
        if (cjd == null)
        {
            return "Unlock Job: It didn't work. Are you sure " + args[1] + " is a valid jorb?";
        }
        
        SharedBank.UnlockJob(cjd.jobEnum);

        return "Unlock Job: " + strJobbyJob + " is unlocked!";
    }
    
    // Prints a list of all the prefabs normally loaded AT runtime
    object FindListOfAbilityAndStatusPrefabs(params string[] args)
    {
        List<string> prefabs = new List<string>();

        foreach(AbilityScript abil in GameMasterScript.masterAbilityList.Values)
        {
            if (!string.IsNullOrEmpty(abil.instantDirectionalAnimationRef))
            {
                prefabs.Add(abil.instantDirectionalAnimationRef);
            }

            if (!string.IsNullOrEmpty(abil.stackProjectileFirstTile))
            {
                prefabs.Add(abil.stackProjectileFirstTile);
            }

            if (!string.IsNullOrEmpty(abil.sfxOverride))
            {
                prefabs.Add(abil.sfxOverride);
            }
        }

        foreach (StatusEffect abil in GameMasterScript.masterStatusList.Values)
        {
            if (!string.IsNullOrEmpty(abil.instantDirectionalAnimationRef))
            {
                prefabs.Add(abil.instantDirectionalAnimationRef);
            }

            if (!string.IsNullOrEmpty(abil.stackProjectileFirstTile))
            {
                prefabs.Add(abil.stackProjectileFirstTile);
            }

            if (!string.IsNullOrEmpty(abil.sfxOverride))
            {
                prefabs.Add(abil.sfxOverride);
            }
        }

        foreach (EffectScript eff in GameMasterScript.masterEffectList.Values)
        {
            if (!string.IsNullOrEmpty(eff.spriteEffectRef))
            {
                prefabs.Add(eff.spriteEffectRef);
            }
        }

        prefabs = prefabs.Distinct().ToList();
        StringBuilder sb = new StringBuilder();
        foreach (string str in prefabs)
        {
            sb.Append(str + "\n");
        }        

        return "Done!";
    }

    // This compares all strings in the game to our master mega dictionary downloaded from the internet
    // and builds a custom dictionary file of ONLY the kanji characters / sequences we actually use
    // The kanji are linked to kana (phonetic), needed for sorting.
    object BuildLocalJapaneseDictionary(params string[] args)
    {
        string path = CustomAlgorithms.GetPersistentDataPath() + "jpDict.txt";
        string[] originalText = File.ReadAllLines(path);

        string[] parsed = null;

        Dictionary<string, string> kanjiToKana = new Dictionary<string, string>();

        for (int i = 0; i < originalText.Length; i++)
        {
            parsed = originalText[i].Split('|');
            if (parsed.Length < 2) continue;
            if (kanjiToKana.ContainsKey(parsed[0])) continue;
            kanjiToKana.Add(parsed[0], parsed[1]);
        }

        // Now iterate through ALLLLLL our Japanese text to make our local dict.

        Dictionary<string, string> localKanjiToKana = new Dictionary<string, string>();

        // There are two particles we can use as dividers WITHIN a string, 
        // which should be excluded from the search
        List<string> dividers = StringManager.GetListOfCulturalDividers();

        foreach(string value in StringManager.dictStringsByLanguage[EGameLanguage.jp_japan].Values)
        {
            // here's a line of japanese text! 陰うつな武器
            // We need to check each kanji-containing chunk vs. our master dict

            char[] cArray = value.ToCharArray();

            string localSearchString = "";
            for (int i = 0; i < cArray.Length; i++)
            {
                string cToString = cArray[i].ToString();

                // We've hit a divider, therefore SEARCH for our text string thus far
                // in the master dict.
                if (dividers.Contains(cToString))
                {
                    // Let's check for the text, for example 陰うつ
                    // This function will add it to our local dict if it matches
                    SearchForKanjiInMasterDict(kanjiToKana, localKanjiToKana, localSearchString);
                    localSearchString = "";
                }
                else
                {
                    // Not a divider? ok
                    localSearchString += cArray[i];
                }

                // But wait, are we at the end of the text? Better search for our current string               
                if (i == cArray.Length - 1)
                {
                    SearchForKanjiInMasterDict(kanjiToKana, localKanjiToKana, localSearchString);
                }
                
            }
        }

        StringBuilder sb = new StringBuilder();

        foreach(string key in localKanjiToKana.Keys)
        {
            sb.Append(key);
            sb.Append('|');
            sb.Append(localKanjiToKana[key]);
            sb.Append('\n');
        }

        string nPath = Application.persistentDataPath + "kanjiToKanaDict.txt";
        File.WriteAllText(nPath, sb.ToString());

        return "Done!";
    }

    // Helper function to the dictionary builder that searches the master dictionary for the given kanji
    // Or maybe it's not kanji we're passing in! We don't know yet until we search
    void SearchForKanjiInMasterDict(Dictionary<string,string> masterDict, Dictionary<string,string> localDict, string localSearchString)
    {
        string kanaValue;
        if (masterDict.TryGetValue(localSearchString, out kanaValue))
        {
            // If it's in there, let's add an entry to our LOCAL dictionary.
            if (!localDict.ContainsKey(localSearchString))
            {
                localDict.Add(localSearchString, kanaValue);
            }
        }
        else
        {
            // Let's say we didn't find this chunk: 占いクッキー
            // But I KNOW that the master dict has this: 占い
            // So let's work backwards, removing one character at a time from the end
            // And maybe we'll find something.

            if (localSearchString.Length > 1)
            {
                string reducedLocalString = localSearchString.Substring(0, localSearchString.Length - 1);
                SearchForKanjiInMasterDict(masterDict, localDict, reducedLocalString);
            }
            
        }
    }

    // Simple helper function that takes the file I downloaded from the internet, which has bad formatting
    // and
    // gives it good formatting.
    object BuildUltimateJapaneseDictionary(params string[] args)
    {
        string originalText = File.ReadAllText("F:\\ImpactRL\\Impact7dayRL\\Assets\\Resources\\Localization\\JmdictFurigana.txt");

        string[] parsedByLine = originalText.Split('\n');
        UnityEngine.Debug.Log("Number of lines: " + parsedByLine.Length);

        StringBuilder sb = new StringBuilder();

        string[] parsed2 = null;

        for (int i = 0; i < parsedByLine.Length; i++)
        {
            parsed2 = parsedByLine[i].Split('|');
            if (parsed2.Length < 2) continue;
            sb.Append(parsed2[0]);
            sb.Append('|');
            sb.Append(parsed2[1]);
            sb.Append('\n');
        }
       
        string masterString = sb.ToString();
        string path = Application.persistentDataPath + "jpDict.txt";
        UnityEngine.Debug.Log(path);
        File.WriteAllText(path, masterString);

        return "Done!";
    }

    object PrintSuffixes(params string[] args)
    {
        string listOfMods = "";
        foreach(MagicMod mm in GameMasterScript.masterMagicModList.Values)
        {
            if (mm.noNameChange) continue;
            if (mm.prefix) continue;
            listOfMods += mm.refName + "/" + mm.modName + "\n";
        }

        UnityEngine.Debug.Log(listOfMods);
        return "Done";
    }

    object SkipToEnding(params string[] args)
    {
        MapMasterScript.singletonMMS.mapTileMesh.gameObject.SetActive(false);
        UIManagerScript.endingCanvas.gameObject.SetActive(true);
        UIManagerScript.singletonUIMS.myCanvas.gameObject.SetActive(false);
        UIManagerScript.singletonUIMS.endingCutscene.gameObject.SetActive(true);
        UIManagerScript.singletonUIMS.endingCutscene.BeginEndSequence(1f);
        return "Done";
    }

    object RunCutscene(params string[] args)
    {
        if (args.Length < 2)
        {
            return "But what Cutscene?";
        }

        MethodInfo scene = typeof(Cutscenes).GetMethod(args[1]);
        if (scene == null)
        {
            return "No static method in Cutscenes called '" + args[1] + "'";
        }

        scene.Invoke(null, null);

        return "Good luck!";
    }

    object ScreenShake(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Please send in a float value for seconds to shake the screen.";
        }

        float fShake = float.Parse(args[1]);
        GameMasterScript.cameraScript.AddScreenshake(fShake);

        return "shakealakin'";

    }

    object SpawnRandomLoots(params string[] args)
    {
        float fChallengeValue = 1.0f;
        float fMagicLootModifier = 1.0f;

        if (args.Length >= 2)
        {
            fChallengeValue = float.Parse(args[1]);
        }
        if (args.Length >= 3)
        {
            fMagicLootModifier = float.Parse(args[2]);
        }

        Item newItam = LootGeneratorScript.GenerateLoot(fChallengeValue, fMagicLootModifier);
        // Spawn
        List<MapTileData> nearbyTilesToPlayer = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2);
        foreach (MapTileData testTile in nearbyTilesToPlayer)
        {
            if (testTile.IsCollidable(GameMasterScript.heroPCActor))
            {
                continue;
            }

            newItam.areaID = MapMasterScript.activeMap.CheckMTDArea(testTile);
            GameMasterScript.SpawnItemAtPosition(newItam, testTile.pos);
            return "Created a " + newItam.displayName;
        }

        return "uh";
    }

    object TextIt(params string[] args)
    {
        int value = Int32.Parse(args[1]);

        UIManagerScript.typingText = true;
        UIManagerScript.idOfText = 999;

        return "Done!";
    }

    object CreateMapByID(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Requires a floor ID, found in <FLOOR> in the maps.xml files.";
        }        

        var floorIDOfMapFromXML = Int32.Parse(args[1]); // For example, floor 360

        bool skipToFloorImmediately = MapMasterScript.activeMap.floor == floorIDOfMapFromXML;

        MapMasterScript.CreateMap(floorIDOfMapFromXML, skipToFloorImmediately);

        return "Done!";
    }
    

    //Shep: Create an item world from parameters and leap into it!
    object CreateAndEnterItemWorld(params string[] args)
    {
        if (args.Length < 2)
        {
            return "ItemWorld: requires some arguments. Get angry.";
        }

        //loop through the args deciding what to do based on the data
        int iArgIndex;

        //info we might use
        // challenge value
        float fChallengeValue = 0f;
        // visual tileset
        TileSet useTheseTiles = TileSet.COUNT;
        // layout type
        DungeonFloorTypes useThisLayout = DungeonFloorTypes.COUNT;
        // dungeon level
        DungeonLevel useThisLevel = null;


        for (iArgIndex = 1; iArgIndex < args.Length; iArgIndex++)
        {
            string strArg = args[iArgIndex];
            string[] strSplitData = null;

            //strip out any closing ) cause those fuck with our parses
            strArg = strArg.Replace(")", "");

            //layout type
            //(layout,islands)
            if (strArg.Contains("(layout"))
            {
                strSplitData = strArg.Split(',');
                useThisLayout = (DungeonFloorTypes)Enum.Parse(typeof(DungeonFloorTypes), strSplitData[1].ToUpperInvariant());
            }

            //tile set
            //(tiles,future)
            if (strArg.Contains("(tileset"))
            {
                strSplitData = strArg.Split(',');
                useTheseTiles = (TileSet)Enum.Parse(typeof(TileSet), strSplitData[1].ToUpperInvariant());
            }

            //challenge value
            //(cv,9001)
            if (strArg.Contains("(cv"))
            {
                strSplitData = strArg.Split(',');
                float.TryParse(strSplitData[1], out fChallengeValue);

                List<DungeonLevel> possibleLevels = GameMasterScript.itemWorldMapDict[fChallengeValue];
                useThisLevel = possibleLevels[UnityEngine.Random.Range(0, possibleLevels.Count)];
            }
        }

        //if we don't a level yet, spawn one based on challenge value 1.0 babby
        if (useThisLevel == null)
        {
            List<DungeonLevel> possibleLevels = GameMasterScript.itemWorldMapDict[1.0f];
            useThisLevel = possibleLevels[UnityEngine.Random.Range(0, possibleLevels.Count)];
        }

        //mutate the level if need be
        if (useThisLayout != DungeonFloorTypes.COUNT)
        {
            useThisLevel.layoutType = useThisLayout;
        }

        if (useTheseTiles != TileSet.COUNT)
        {
            useThisLevel.tileVisualSet = useTheseTiles;
        }

        //that should be enough to crash the game wait I mean work perfectly
        MapMasterScript mms = MapMasterScript.singletonMMS;
        HeroPC hero = GameMasterScript.heroPCActor;

        Map[] itemWorld = mms.SpawnItemWorld(null, null, 1.0f, useThisLevel, null);
        hero.myStats.RemoveStatusByRef("status_itemworld");
        UIManagerScript.RefreshStatuses();
        TravelManager.TravelMaps(itemWorld[0], null, false);

        return "ItemWorld: Probably worked.";
    }



    //Shep: Take damage as normal but never go below 1HP
    object Undying(params string[] args)
    {
        GameMasterScript.debug_neverDie = !GameMasterScript.debug_neverDie;
        return "Undying! NeverDie==" + GameMasterScript.debug_neverDie;
    }

    //Shep: Deal enough damage to each monster to kill it
    object Detonate(params string[] args)
    {
        GameMasterScript.Debug_DetonateAllMonsters();
        return "FOOM!";
    }

    //Shep: Monsters take no actions, neither gaining nor spending CT
    object FreezeMonsters(params string[] args)
    {
        GameMasterScript.debug_freezeMonsters = !GameMasterScript.debug_freezeMonsters;
        string strAdditionalText = " ";
        if (args.Length >= 2)
        {
            Int32.TryParse(args[1], out GameMasterScript.debug_freezeAllButThisID);
            if (GameMasterScript.debug_freezeAllButThisID != 0)
            {
                Actor actar = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.debug_freezeAllButThisID);
                if (actar != null)
                {
                    strAdditionalText += "All monsters frozen except for " + actar.displayName + " ID " + GameMasterScript.debug_freezeAllButThisID;
                }
                else
                {
                    strAdditionalText += "ID " + GameMasterScript.debug_freezeAllButThisID + " isn't valid for any actor.";
                }
            }
        }
        else
        {
            GameMasterScript.debug_freezeAllButThisID = 0;
        }

        return "FreezeMonsters==" + GameMasterScript.debug_freezeMonsters + strAdditionalText;
    }

    //Shep: Poof, new jorb
    object ChangeJob(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Change Jobs: Please add a job to change to.";
        }

        string strJobbyJob = args[1].ToUpperInvariant();
        if (!GameMasterScript.heroPCActor.ChangeJobs(strJobbyJob, null))
        {
            return "Change Jobs: It didn't work. Are you sure " + args[1] + " is a valid jorb?";
        }

        return "Change Jobs: You are now a " + strJobbyJob;

    }

    object CheckDialogBoxState(params string[] args)
    {
        return MonsterCorralScript.corralInterfaceOpen + " " + UIManagerScript.dialogBoxOpen + " " + UIManagerScript.singletonUIMS.uiDialogMenuCursor.GetComponent<Image>().enabled + " " + UIManagerScript.singletonUIMS.uiDialogMenuCursor.activeSelf + " " + UIManagerScript.AnyInteractableWindowOpen() + " " + GameStartData.CurrentLoadState + " " + GameMasterScript.applicationQuittingOrChangingScenes;
    }

    //Shep: Add some cards to the gambler hand
    object DrawCard(params string[] args)
    {
        HeroPC hero = GameMasterScript.heroPCActor;
        if (hero == null || hero.gamblerHand == null)
        {
            return "DrawCard: You can't. I don't know why.";
        }

        int iNumToDraw = 1;
        if (args.Length >= 2)
        {
            Int32.TryParse(args[1], out iNumToDraw);
        }
        iNumToDraw = Math.Max(iNumToDraw, 1);

        for (int t = 0; t < iNumToDraw; t++)
        {
            GameMasterScript.heroPCActor.DrawWildCard();
        }

        return "DrawCard: Drew " + iNumToDraw + " card(s)";
    }

    //Shep: #todo expand this into full corral functionality
    object HappyFarm(params string[] args)
    {
        List<TamedCorralMonster> beests = MetaProgressScript.localTamedMonstersForThisSlot;

        for (int t = 0; t < beests.Count; t++)
        {
            TamedCorralMonster goodBoy = beests[t];
            goodBoy.ChangeHappiness(1);
        }

        UIManagerScript.PlayCursorSound("CasinoWin");

        return "HappyFarm: happy";
    }

    object PetLevelUp(params string[] args)
    {
        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();
        if (pet == null) return "Player Has No Pet :(";

        UnityEngine.Debug.Log("Health: " + pet.myStats.GetCurStat(StatTypes.HEALTH) +
            " Strength: " + pet.myStats.GetCurStat(StatTypes.STRENGTH) +
            " Weapon Power: " + pet.myEquipment.GetWeapon().power);
            
        pet.myStats.AdjustLevel(1);

        UnityEngine.Debug.Log("Lvl Up! Health: " + pet.myStats.GetCurStat(StatTypes.HEALTH) +
            " Strength: " + pet.myStats.GetCurStat(StatTypes.STRENGTH) +
            " Weapon Power: " + pet.myEquipment.GetWeapon().power);

        return "DONE!";
    }

    object SetLanguage(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Specify a language!";
        }

        EGameLanguage parsedLang = EGameLanguage.en_us;

        try {
            parsedLang = (EGameLanguage)Enum.Parse(typeof(EGameLanguage), args[1]);
        }
        catch(Exception e)
        {
            return "Failed to parse language enum due to " + e;
        }

        //TDPlayerPrefs.SetString("lang", parsedLang.ToString());

        return "Done!";
    }

    object AddStatusToSelf(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Specify a status ref!";
        }
        int dur = 99;
        if (args.Length >= 3)
        {
            Int32.TryParse(args[2], out dur);
        }
        GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog(args[1], GameMasterScript.heroPCActor, 99);
        return "Done!";
    }

    object SetMetaProgressFlag(params string[] args)
    {
        MetaProgressScript.SetMetaProgress(args[1], Int32.Parse(args[2]));
        return "Set value " + args[1] + " to " + args[2];
    }

    // Input an int (floor ID) or map name 
    object FindConnections(params string[] args)
    {
        if (args.Length < 1) return "Input a floor number or level name!";

        Map mapToCheck = null;

        int floorID;
        if (Int32.TryParse(args[1], out floorID))
        {
            mapToCheck = MapMasterScript.theDungeon.FindFloor(floorID);
        }
        else
        {
            string strFloorData = "";
            for (int t = 1; t < args.Length; t++)
            {                
                if (strFloorData == "")
                {
                    strFloorData = args[t];
                }
                else
                {
                    strFloorData += " " + args[t];
                }                
            }

            mapToCheck = MapMasterScript.theDungeon.FindFloorViaData(strFloorData, 0);
        }

        string infoStr = mapToCheck.GetName() + " Visible: " + !mapToCheck.GetMapVisibility();

        foreach(Stairs st in mapToCheck.mapStairs)
        {
            infoStr += " Has Stairs at " + st.GetPos() + " Enabled? " + st.actorEnabled;
            if (st.NewLocation != null)
            {
                infoStr += " Pointing at: " + st.NewLocation.GetName();
            }
            else
            {
                infoStr += " Pointing at NOTHING.";
            }
        }

        return "Done";
    }

    object SetHeroProgressFlag(params string[] args)
    {
        GameMasterScript.heroPCActor.SetActorData(args[1], Int32.Parse(args[2]));
        return "Set hero value " + args[1] + " to " + args[2];
    }


    object NeverDie(params string[] args)
    {
        GameMasterScript.debug_neverDie = !GameMasterScript.debug_neverDie;
        GameMasterScript.heroPCActor.myStats.BoostStatByPercent(StatTypes.HEALTH, 10f);
        GameMasterScript.heroPCActor.myStats.BoostStatByPercent(StatTypes.STRENGTH, 10f);
        GameMasterScript.heroPCActor.ChangeMoney(5000);
        return "Cheater! NeverDie==" + GameMasterScript.debug_neverDie;
    }

    object DebugMouse(params string[] args)
    {
        debuggingMouse = !debuggingMouse;
        return "Mouse debug state set to: " + debuggingMouse;
    }

    object SetPlayerPrefsInt(params string[] args)
    {
        if (args.Length < 3)
        {
            return "Specify key name and target value!";
        }
        //TDPlayerPrefs.SetInt(args[1], Int32.Parse(args[2]));
        return "Done!";
    }

    object CheckAllEffects(params string[] args)
    {
        foreach(StatusEffect se in GameMasterScript.masterStatusList.Values)
        {
            foreach (EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectType == EffectType.ADDSTATUS)
                {
                    AddStatusEffect ase = eff as AddStatusEffect;
                    if (!GameMasterScript.masterStatusList.ContainsKey(ase.statusRef) && !GameMasterScript.masterEffectList.ContainsKey(ase.statusRef))
                    {
                        UnityEngine.Debug.LogError(se.abilityName + " Missing status ref: " + ase.statusRef);
                    }
                }
                else if (eff.effectType == EffectType.SUMMONACTOR)
                {
                    SummonActorEffect sae = eff as SummonActorEffect;
                    if (sae.summonActorType == ActorTypes.DESTRUCTIBLE && !GameMasterScript.masterMapObjectDict.ContainsKey(sae.summonActorRef))
                    {
                        UnityEngine.Debug.LogError(se.abilityName + " Missing map obj ref: " + sae.summonActorRef);
                    }
                    if (sae.summonActorType == ActorTypes.MONSTER && !GameMasterScript.masterMonsterList.ContainsKey(sae.summonActorRef))
                    {
                        UnityEngine.Debug.LogError(se.abilityName + " Missing mon ref: " + sae.summonActorRef);
                    }
                }
            }
        }
        foreach(AbilityScript abil in GameMasterScript.masterAbilityList.Values)
        {
            foreach (EffectScript eff in abil.listEffectScripts)
            {
                if (eff.effectType == EffectType.ADDSTATUS)
                {
                    AddStatusEffect ase = eff as AddStatusEffect;
                    if (!GameMasterScript.masterStatusList.ContainsKey(ase.statusRef) && !GameMasterScript.masterEffectList.ContainsKey(ase.statusRef))
                    {
                        UnityEngine.Debug.LogError(abil.abilityName + " Missing status ref: " + ase.statusRef);
                    }
                }
                else if (eff.effectType == EffectType.SUMMONACTOR)
                {
                    SummonActorEffect sae = eff as SummonActorEffect;
                    if (sae.summonActorType == ActorTypes.DESTRUCTIBLE && !GameMasterScript.masterMapObjectDict.ContainsKey(sae.summonActorRef))
                    {
                        UnityEngine.Debug.LogError(abil.abilityName + " Missing map obj ref: " + sae.summonActorRef);
                    }
                    if (sae.summonActorType == ActorTypes.MONSTER && !GameMasterScript.masterMonsterList.ContainsKey(sae.summonActorRef))
                    {
                        UnityEngine.Debug.LogError(abil.abilityName + " Missing mon ref: " + sae.summonActorRef);
                    }
                }
            }
        }
        foreach(MonsterTemplateData mtd in GameMasterScript.masterMonsterList.Values)
        {
            if (!GameMasterScript.masterItemList.ContainsKey(mtd.weaponID))
            {
                UnityEngine.Debug.LogError("Missing weapon " + mtd.weaponID);
            }
            if (!string.IsNullOrEmpty(mtd.offhandArmorID) && !GameMasterScript.masterItemList.ContainsKey(mtd.offhandArmorID))
            {
                UnityEngine.Debug.LogError("Missing offhand armor " + mtd.offhandArmorID);
            }
            if (!string.IsNullOrEmpty(mtd.armorID) && !GameMasterScript.masterItemList.ContainsKey(mtd.armorID))
            {
                UnityEngine.Debug.LogError("Missing armor " + mtd.armorID);
            }
            if (!string.IsNullOrEmpty(mtd.offhandWeaponID) && !GameMasterScript.masterItemList.ContainsKey(mtd.offhandWeaponID))
            {
                UnityEngine.Debug.LogError("Missing offhand weapon " + mtd.offhandWeaponID);
            }
        }

        return "Done";
    }

    object SlimeDungeonMapWin(params string[] args)
    {
        GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.WonCurrentMap());
        return "Done";
    }

    object SlimeDungeonMapLose(params string[] args)
    {
        GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.FailedCurrentMap());
        return "Done";
    }

    object ChangePlayerName(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Must input a name for your character!";
        }
        string concatName = "";
        for (int i = 1; i < args.Length; i++)
        {
            if (i > 1)
            {
                concatName += " ";
            }
            concatName += args[i];
        }
        GameMasterScript.heroPCActor.displayName = concatName;
        return "Done! Your name is changed.";
    }

    object MonsterLove(params string[] args)
    {
        string builder = "";
        
        foreach(TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
        {
            foreach(int id in tcm.attractionToMonsters.Keys)
            {
                builder += tcm.sharedBankID + " attraction to " + id + " is " + tcm.attractionToMonsters[id];
            }
            builder += "\n";
        }

        UnityEngine.Debug.Log(builder);

        return "Done!";
    }

    object Reveal(params string[] args)
    {
        UIManagerScript.dbRevealMode = !UIManagerScript.dbRevealMode;
        return "Done!";
    }

    object CheckTileNorth(params string[] args)
    {
        Vector2 pos = GameMasterScript.heroPCActor.GetPos();
        UnityEngine.Debug.Log(GameMasterScript.heroPCActor.visibleTilesArray[(int)pos.x, (int)pos.y - 1]);

        return "Done!";
    }

    object CheckSteamStatsAndAchievements(params string[] args)
    {
        GameMasterScript.gmsSingleton.statsAndAchievements.UpdateSteamStatsAndAchievements(debug: true);
        return "Done!";
    }

    object CheckRevealStatus(params string[] args)
    {
        UnityEngine.Debug.Log(MapMasterScript.activeMap.dungeonLevelData.revealAll);
        return "Done";
    }

    object CheckTreeData(params string[] args)
    {
        return "Done!";
    }

    object RevealMapCommand(params string[] args)
    {
        UIManagerScript.SwitchRevealMode();

        return "Done!";
    }

    object CheckUpMonsterBehavior(params string[] args)
    {
        int posX = (int)GameMasterScript.heroPCActor.GetPos().x;
        int posY = (int)GameMasterScript.heroPCActor.GetPos().y + 1;

        Monster mon = (Monster)MapMasterScript.GetTile(new Vector2(posX, posY)).GetAllTargetable()[0] as Monster;

        DoMonText(mon);
        return "Done!";
    }

    private void DoMonText(Monster mon)
    {
        string extraText = "Target: ";

        if (mon.myTarget != null)
        {
            UnityEngine.Debug.Log("Target not null");
            extraText += mon.myTarget.displayName;
        }
        extraText += " TTile: ";
        if (mon.myTargetTile != null)
        {
            UnityEngine.Debug.Log("Tile not null");
            extraText += mon.myTargetTile;
        }
        extraText += " Interest: ";
        if (mon.myActorOfInterest != null)
        {
            UnityEngine.Debug.Log("Interest not null");
            extraText += mon.myActorOfInterest.displayName;
        }

        UnityEngine.Debug.Log(mon.displayName + ": Behavior state " + mon.myBehaviorState.ToString() + " " + extraText);
    }

    object CheckDownMonsterBehavior(params string[] args)
    {
        int posX = (int)GameMasterScript.heroPCActor.GetPos().x;
        int posY = (int)GameMasterScript.heroPCActor.GetPos().y - 1;

        Monster mon = (Monster)MapMasterScript.GetTile(new Vector2(posX, posY)).GetAllTargetable()[0] as Monster;

        DoMonText(mon);

        return "Done!";
    }

    object HurtSelf10(params string[] args)
    {
        GameMasterScript.GetHeroActor().TakeDamage(10f, DamageTypes.PHYSICAL);
        return "Done!";
    }

    object CheckLeftMonsterBehavior(params string[] args)
    {
        int posX = (int)GameMasterScript.heroPCActor.GetPos().x - 1;
        int posY = (int)GameMasterScript.heroPCActor.GetPos().y;

        Monster mon = (Monster)MapMasterScript.GetTile(new Vector2(posX, posY)).GetAllTargetable()[0] as Monster;

        DoMonText(mon);

        return "Done!";
    }

    object CheckRightMonsterBehavior(params string[] args)
    {
        int posX = (int)GameMasterScript.heroPCActor.GetPos().x + 1;
        int posY = (int)GameMasterScript.heroPCActor.GetPos().y;

        Monster mon = (Monster)MapMasterScript.GetTile(new Vector2(posX, posY)).GetAllTargetable()[0] as Monster;

        DoMonText(mon);
        return "Done!";
    }

    object CheckNEWSVisibility(params string[] args)
    {
        return "Done!";
    }

    object CMDClose(params string[] args)
    {
        _isOpen = false;

        return "closed";
    }

    object CMDClear(params string[] args)
    {
        this.ClearLog();

        return "clear";
    }

    object CMDHelp(params string[] args)
    {
        var output = new StringBuilder();

        output.AppendLine(":: Command List ::");

        foreach (string key in _cmdTable.Keys)
        {
            output.AppendLine(key);
        }

        output.AppendLine(" ");

        return output.ToString();
    }

    private void Update()
    {
        framesToUpdateCounter--;
        if (framesToUpdateCounter <= 0)
        {
            if (debuggingMouse)
            {
                float mouseAxis = Input.GetAxis("Mouse ScrollWheel");
                if (mouseAxis != 0)
                {
                    GameLogScript.GameLogWrite(mouseAxis.ToString(), GameMasterScript.heroPCActor);
                }                
            }
            framesToUpdateCounter = framesToUpdateMax;
        }

    }

    object CMDSystemInfo(params string[] args)
    {
        var info = new StringBuilder();

        info.AppendFormat("Unity Ver: {0}\n", Application.unityVersion);
        info.AppendFormat("Platform: {0} Language: {1}\n", Application.platform, Application.systemLanguage);
        info.AppendFormat("Screen:({0},{1}) DPI:{2} Target:{3}fps\n", Screen.width, Screen.height, Screen.dpi, Application.targetFrameRate);
        //info.AppendFormat("Level: {0} ({1} of {2})\n", Application.loadedLevelName, Application.loadedLevel, Application.levelCount);
        info.AppendFormat("Quality: {0}\n", QualitySettings.names[QualitySettings.GetQualityLevel()]);
        info.AppendLine();
#if !UNITY_SWITCH
        info.AppendFormat("Data Path: {0}\n", Application.dataPath);
        info.AppendFormat("Cache Path: {0}\n", Application.temporaryCachePath);
        info.AppendFormat("Persistent Path: {0}\n", Application.persistentDataPath);
        info.AppendFormat("Streaming Path: {0}\n", Application.streamingAssetsPath);
#endif

#if UNITY_WEBPLAYER
    info.AppendLine();
    info.AppendFormat("URL: {0}\n", Application.absoluteURL);
    info.AppendFormat("srcValue: {0}\n", Application.srcValue);
    info.AppendFormat("security URL: {0}\n", Application.webSecurityHostUrl);
#endif
#if MOBILE
    info.AppendLine();
    info.AppendFormat("Net Reachability: {0}\n", Application.internetReachability);
    info.AppendFormat("Multitouch: {0}\n", Input.multiTouchEnabled);
#endif
#if UNITY_EDITOR
        info.AppendLine();
        info.AppendFormat("editorApp: {0}\n", UnityEditor.EditorApplication.applicationPath);
        info.AppendFormat("editorAppContents: {0}\n", UnityEditor.EditorApplication.applicationContentsPath);
        //info.AppendFormat("scene: {0}\n", UnityEditor.EditorApplication.currentScene);
#endif
        info.AppendLine();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        var devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            info.AppendLine("Cameras: ");

            foreach (var device in devices)
            {
                info.AppendFormat("  {0} front:{1}\n", device.name, device.isFrontFacing);
            }
        }
#endif


        return info.ToString();
    }


    #endregion
    #region GUI Window Methods

    void DrawBottomControls()
    {
        GUI.SetNextControlName(ENTRYFIELD);
        _inputString = GUI.TextField(inputRect, _inputString);

        if (GUI.Button(enterRect, "Enter"))
        {
            EvalInputString(_inputString);
            _inputString = string.Empty;
        }

        var index = GUI.Toolbar(toolbarRect, toolbarIndex, tabs);

        if (index != toolbarIndex)
        {
            toolbarIndex = index;
        }

        if (_justOpened)
        {
            _justOpened = false;
            GUI.FocusControl(ENTRYFIELD);
        }
#if !MOBILE
        GUI.DragWindow();
#endif
    }

    void LogWindow(int windowID)
    {
        GUI.Box(scrollRect, string.Empty);

        innerRect.height = innerHeight < scrollRect.height ? scrollRect.height : innerHeight;

        _logScrollPos = GUI.BeginScrollView(scrollRect, _logScrollPos, innerRect, false, true);

        if (_messages != null || _messages.Count > 0)
        {
            Color oldColor = GUI.contentColor;

            messageLine.y = 0;

            foreach (Message m in _messages)
            {
                GUI.contentColor = m.color;

                guiContent.text = m.ToGUIString();

                messageLine.height = labelStyle.CalcHeight(guiContent, messageLine.width);

                GUI.Label(messageLine, guiContent);

                messageLine.y += (messageLine.height + lineOffset);

                innerHeight = messageLine.y > scrollRect.height ? (int)messageLine.y : (int)scrollRect.height;
            }
            GUI.contentColor = oldColor;
        }

        GUI.EndScrollView();

        DrawBottomControls();
    }

    string GetDisplayString()
    {
        if (_messages == null)
        {
            return string.Empty;
        }

        return _displayString.ToString();
    }

    void BuildDisplayString()
    {
        _displayString.Length = 0;

        foreach (Message m in _messages)
        {
            _displayString.AppendLine(m.ToString());
        }
    }

    void CopyLogWindow(int windowID)
    {

        guiContent.text = GetDisplayString();

        var calcHeight = GUI.skin.textArea.CalcHeight(guiContent, messageLine.width);

        innerRect.height = calcHeight < scrollRect.height ? scrollRect.height : calcHeight;

        _rawLogScrollPos = GUI.BeginScrollView(scrollRect, _rawLogScrollPos, innerRect, false, true);

        GUI.TextArea(innerRect, guiContent.text);

        GUI.EndScrollView();

        DrawBottomControls();
    }

    void WatchVarWindow(int windowID)
    {
        GUI.Box(scrollRect, string.Empty);

        innerRect.height = innerHeight < scrollRect.height ? scrollRect.height : innerHeight;

        _watchVarsScrollPos = GUI.BeginScrollView(scrollRect, _watchVarsScrollPos, innerRect, false, true);

        int line = 0;

        nameRect.y = valueRect.y = 0;

        nameRect.x = messageLine.x;

        float totalWidth = messageLine.width - messageLine.x;
        float nameMin;
        float nameMax;
        float valMin;
        float valMax;
        float stepHeight;

        var textAreaStyle = GUI.skin.textArea;

        foreach (var kvp in _watchVarTable)
        {
            var nameContent = new GUIContent(string.Format("{0}:", kvp.Value.Name));
            var valContent = new GUIContent(kvp.Value.ToString());

            labelStyle.CalcMinMaxWidth(nameContent, out nameMin, out nameMax);
            textAreaStyle.CalcMinMaxWidth(valContent, out valMin, out valMax);

            if (nameMax > totalWidth)
            {
                nameRect.width = totalWidth - valMin;
                valueRect.width = valMin;
            }
            else if (valMax + nameMax > totalWidth)
            {
                valueRect.width = totalWidth - nameMin;
                nameRect.width = nameMin;
            }
            else
            {
                valueRect.width = valMax;
                nameRect.width = nameMax;
            }

            nameRect.height = labelStyle.CalcHeight(nameContent, nameRect.width);
            valueRect.height = textAreaStyle.CalcHeight(valContent, valueRect.width);

            valueRect.x = totalWidth - valueRect.width + nameRect.x;

            GUI.Label(nameRect, nameContent);
            GUI.TextArea(valueRect, valContent.text);

            stepHeight = Mathf.Max(nameRect.height, valueRect.height) + 4;

            nameRect.y += stepHeight;
            valueRect.y += stepHeight;

            innerHeight = valueRect.y > scrollRect.height ? (int)valueRect.y : (int)scrollRect.height;

            line++;
        }

        GUI.EndScrollView();

        DrawBottomControls();
    }
    #endregion
    #region InternalFunctionality
    [Conditional("DEBUG_CONSOLE"),
     Conditional("UNITY_EDITOR")]
    void LogMessage(Message msg)
    {
        _messages.Add(msg);
        dirty = true;
    }

    //--- Local version. Use the static version above instead.
    void ClearLog()
    {
        _messages.Clear();
    }

    //--- Local version. Use the static version above instead.
    void RegisterCommandCallback(string commandString, DebugCommand commandCallback)
    {
#if !UNITY_FLASH
        _cmdTable[commandString.ToLower()] = new DebugCommand(commandCallback);
#endif
    }

    //--- Local version. Use the static version above instead.
    void UnRegisterCommandCallback(string commandString)
    {
        _cmdTable.Remove(commandString.ToLower());
    }

    //--- Local version. Use the static version above instead.
    void AddWatchVarToTable(WatchVarBase watchVar)
    {
        _watchVarTable[watchVar.Name] = watchVar;
    }

    //--- Local version. Use the static version above instead.
    void RemoveWatchVarFromTable(string name)
    {
        _watchVarTable.Remove(name);
    }

    void EvalInputString(string inputString)
    {
        inputString = inputString.Trim();

        if (string.IsNullOrEmpty(inputString))
        {
            LogMessage(Message.Input(string.Empty));
            return;
        }

        _history.Add(inputString);
        LogMessage(Message.Input(inputString));

        var input = new List<string>(inputString.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
        
        var cmd = input[0];

        if (_cmdTable.ContainsKey(cmd))
        {
            Log(_cmdTable[cmd](input.ToArray()), MessageType.OUTPUT);
        }
        else
        {
            input = input.ConvertAll<string>(low => {
                return low.ToLower();
            });
            if (_cmdTable.ContainsKey(cmd))
            {
                Log(_cmdTable[cmd](input.ToArray()), MessageType.OUTPUT);
                return;
            }
            LogMessage(Message.Output(string.Format("*** Unknown Command: {0} ***", cmd)));
        }
    }
    #endregion
}
#else
    public static class DebugConsole {
  public static bool IsOpen;
  public static KeyCode toggleKey;
  public delegate object DebugCommand(params string[] args);

  public static object Log(object message) {
    return message;
  }

  public static object LogFormat(string format, params object[] args) {
    return string.Format(format, args);
  }

  public static object LogWarning(object message) {
    return message;
  }

  public static object LogError(object message) {
    return message;
  }

  public static object Log(object message, object messageType) {
    return message;
  }

  public static object Log(object message, Color displayColor) {
    return message;
  }

  public static object Log(object message, object messageType, Color displayColor) {
    return message;
  }

  public static void Clear() {
  }

  public static void RegisterCommand(string commandString, DebugCommand commandCallback) {
  }

  public static void UnRegisterCommand(string commandString) {
  }

  public static void RegisterWatchVar(object watchVar) {
  }

  public static void UnRegisterWatchVar(string name) {
  }
}
#endif
/// <summary>
/// Base class for WatchVars. Provides base functionality.
/// </summary>
public abstract class WatchVarBase
{
    /// <summary>
    /// Name of the WatchVar.
    /// </summary>
    public string Name { get; private set; }

    protected FieldInfo _watchedStaticField;

    protected object _value;

    public WatchVarBase(string name, object val) : this(name)
    {
        _value = val;
    }

    public WatchVarBase(string name)
    {
        Name = name;
        Register();
    }

    public void Register()
    {
        DebugConsole.RegisterWatchVar(this);
    }

    public void UnRegister()
    {
        DebugConsole.UnRegisterWatchVar(Name);
    }

    public object ObjValue
    {
        get { return _value; }
    }

    public override string ToString()
    {
        if (_watchedStaticField != null)
        {
            _value = _watchedStaticField.GetValue(null);
        }
        
        if (_value == null)
        {
            return "<null>";
        }

        return _value.ToString();
    }

    public void SetStaticFieldInfo(Type classType, string fieldName)
    {
        _watchedStaticField = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
    }
}

/// <summary>
///
/// </summary>
public class WatchVar<T> : WatchVarBase
{
    public T Value
    {
        get { return (T)_value; }
        set { _value = value; }
    }

    public WatchVar(string name) : base(name)
    {

    }

    public WatchVar(string name, T val) : base(name, val)
    {

    }
}

public class FPSCounter
{
    public float current = 0.0f;
    public float updateInterval = 0.5f;
    // FPS accumulated over the interval
    float accum = 0;
    // Frames drawn over the interval
    int frames = 1;
    // Left time for current interval
    float timeleft;
    float delta;

    public FPSCounter()
    {
        timeleft = updateInterval;
    }

    public IEnumerator Update()
    {
        // skip the first frame where everything is initializing.
        yield return null;

        while (true)
        {
            delta = Time.deltaTime;

            timeleft -= delta;
            accum += Time.timeScale / delta;
            ++frames;

            // Interval ended - update GUI text and start new interval
            if (timeleft <= 0.0f)
            {
                current = accum / frames;
                timeleft = updateInterval;
                accum = 0.0f;
                frames = 0;
            }

            yield return null;
        }
    }
}

namespace UnityEngine
{
    public static class Assertion
    {
        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void Assert(bool condition)
        {
            Assert(condition, string.Empty, true);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void Assert(bool condition, string assertString)
        {
            Assert(condition, assertString, false);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void Assert(bool condition, string assertString, bool pauseOnFail)
        {
            if (condition)
            {
                return;
            }

            Debug.LogError(string.Format("Assertion failed!\n{0}", assertString));

            if (pauseOnFail)
            {
                Debug.Break();
            }
        }
    }
}

namespace UnityMock
{
    public static class Debug
    {
        // Methods
        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
        {
            UnityEngine.Debug.DrawLine(start, end, color, duration);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            UnityEngine.Debug.DrawLine(start, end, color);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end)
        {
            UnityEngine.Debug.DrawLine(start, end);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color)
        {
            UnityEngine.Debug.DrawRay(start, dir, color);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 start, Vector3 dir)
        {
            UnityEngine.Debug.DrawRay(start, dir);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration)
        {
            UnityEngine.Debug.DrawRay(start, dir, color);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("DEBUG_LEVEL_WARN"),
         Conditional("DEBUG_LEVEL_ERROR"),
         Conditional("UNITY_EDITOR")]
        public static void Break()
        {
            UnityEngine.Debug.Break();
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("DEBUG_LEVEL_WARN"),
         Conditional("DEBUG_LEVEL_ERROR"),
         Conditional("UNITY_EDITOR")]
        public static void DebugBreak()
        {
            UnityEngine.Debug.DebugBreak();
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("UNITY_EDITOR")]
        public static void Log(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("DEBUG_LEVEL_WARN"),
         Conditional("DEBUG_LEVEL_ERROR"),
         Conditional("UNITY_EDITOR")]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("DEBUG_LEVEL_WARN"),
         Conditional("DEBUG_LEVEL_ERROR"),
         Conditional("UNITY_EDITOR")]
        public static void LogError(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("DEBUG_LEVEL_WARN"),
         Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        [Conditional("DEBUG_LEVEL_LOG"),
         Conditional("DEBUG_LEVEL_WARN"),
         Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        // Properties
        public static bool isDebugBuild
        {
#if DEBUG
            get { return true; }
#else
      get { return false; }
#endif
        }
    }
}