// Copyright (c) 2015 Augie R. Maddox, Guavaman Enterprises. All rights reserved.
#pragma warning disable 0219
#pragma warning disable 0618
#pragma warning disable 0649

namespace Rewired.UI.ControlMapper
{

    using System;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Rewired;

    [Serializable]
    [CreateAssetMenu()]
    public class ChineseRewired : LanguageDataBase
    {
        [SerializeField]
        private string _yes = "是";
        [SerializeField]
        private string _no = "否";
        [SerializeField]
        private string _add = "增加";
        [SerializeField]
        private string _replace = "替换";
        [SerializeField]
        private string _remove = "删除";
        [SerializeField]
        private string _swap = "Swap";
        [SerializeField]
        private string _cancel = "取消";
        [SerializeField]
        private string _none = "未设置";
        [SerializeField]
        private string _okay = "好吧";
        [SerializeField]
        private string _done = "确定";
        [SerializeField]
        private string _default = "默认";
        [SerializeField]
        private string _assignControllerWindowTitle = "选择控制器";
        [SerializeField]
        private string _assignControllerWindowMessage = "按下任意键或拨动你想用的控制器摇杆。";
        [SerializeField]
        private string _controllerAssignmentConflictWindowTitle = "Controller Assignment";
        [SerializeField]
        [Tooltip("{0} = Joystick Name\n{1} = Other Player Name\n{2} = This Player Name")]
        private string _controllerAssignmentConflictWindowMessage = "{0} is already assigned to {1}. Do you want to assign this controller to {2} instead?";
        [SerializeField]
        private string _elementAssignmentPrePollingWindowMessage = "首先将所有摇杆居中，然后按下任意键或等待时间结束。";
        [SerializeField]
        [Tooltip("{0} = Action Name")]
        private string _joystickElementAssignmentPollingWindowMessage = "Now press a button or move an axis to assign it to {0}.";
        [SerializeField]
        [Tooltip("This text is only displayed when split-axis fields have been disabled and the user clicks on the full-axis field. Button/key/D-pad input cannot be assigned to a full-axis field.\n{0} = Action Name")]
        private string _joystickElementAssignmentPollingWindowMessage_fullAxisFieldOnly = "现在拨动一个摇杆将它设置为{0}。";
        [SerializeField]
        [Tooltip("{0} = Action Name")]
        private string _keyboardElementAssignmentPollingWindowMessage = "按一个键将它设为{0}。辅助按键也可以设置。要单独设置一个辅助按键，请按住1秒。";
        [SerializeField]
        [Tooltip("{0} = Action Name")]
        private string _mouseElementAssignmentPollingWindowMessage = "按一个鼠标键或拨动一个摇杆将它设为{0}。";
        [SerializeField]
        [Tooltip("This text is only displayed when split-axis fields have been disabled and the user clicks on the full-axis field. Button/key/D-pad input cannot be assigned to a full-axis field.\n{0} = Action Name")]
        private string _mouseElementAssignmentPollingWindowMessage_fullAxisFieldOnly = "现在拨动一个摇杆将它设置为{0}。";
        [SerializeField]
        private string _elementAssignmentConflictWindowMessage = "设置冲突";
        [SerializeField]
        [Tooltip("{0} = Element Name")]
        private string _elementAlreadyInUseBlocked = "{0}已被使用不可替换。";
        [SerializeField]
        [Tooltip("{0} = Element Name")]
        private string _elementAlreadyInUseCanReplace = "{0}已被使用。你想要替换吗？";
        [SerializeField]
        [Tooltip("{0} = Element Name")]
        private string _elementAlreadyInUseCanReplace_conflictAllowed = "{0}已被使用。你想要替换吗？你也可以选择添加。";
        [SerializeField]
        private string _mouseAssignmentConflictWindowTitle = "鼠标设置";
        [SerializeField]
        [Tooltip("{0} = Other Player Name\n{1} = This Player Name")]
        private string _mouseAssignmentConflictWindowMessage = "鼠标已被设置为{0}。你想要将鼠标替换为{1}吗？";
        [SerializeField]
        private string _calibrateControllerWindowTitle = "手柄校准";
        [SerializeField]
        private string _calibrateAxisStep1WindowTitle = "手柄归零";
        [SerializeField]
        [Tooltip("{0} = Axis Name")]
        private string _calibrateAxisStep1WindowMessage = "将摇杆居中或归零{0}并按任意键或等待时间结束。";
        [SerializeField]
        private string _calibrateAxisStep2WindowTitle = "校准范围";
        [SerializeField]
        [Tooltip("{0} = Axis Name")]
        private string _calibrateAxisStep2WindowMessage = "拨动{0}穿过整个范围然后按任意键或等待时间结束。";
        [SerializeField]
        private string _inputBehaviorSettingsWindowTitle = "灵敏度设置";
        [SerializeField]
        private string _restoreDefaultsWindowTitle = "恢复默认";
        [SerializeField]
        [Tooltip("Message for a single player game.")]
        private string _restoreDefaultsWindowMessage_onePlayer = "这将恢复默认的操作设置。你确定要这样做吗？";
        [SerializeField]
        [Tooltip("Message for a multi-player game.")]
        private string _restoreDefaultsWindowMessage_multiPlayer = "This will restore the default input configuration for all players. Are you sure you want to do this?";
        [SerializeField]
        private string _actionColumnLabel = "动作";
        [SerializeField]
        private string _keyboardColumnLabel = "键盘";
        [SerializeField]
        private string _mouseColumnLabel = "鼠标";
        [SerializeField]
        private string _controllerColumnLabel = "手柄";
        [SerializeField]
        private string _removeControllerButtonLabel = "删除";
        [SerializeField]
        private string _calibrateControllerButtonLabel = "校准";
        [SerializeField]
        private string _assignControllerButtonLabel = "分配控制器";
        [SerializeField]
        private string _inputBehaviorSettingsButtonLabel = "灵敏度";
        [SerializeField]
        private string _doneButtonLabel = "确定";
        [SerializeField]
        private string _restoreDefaultsButtonLabel = "恢复默认";
        [SerializeField]
        private string _playersGroupLabel = "Players:";
        [SerializeField]
        private string _controllerSettingsGroupLabel = "手柄:";
        [SerializeField]
        private string _assignedControllersGroupLabel = "Assigned Controllers:";
        [SerializeField]
        private string _settingsGroupLabel = "Settings:";
        [SerializeField]
        private string _mapCategoriesGroupLabel = "Categories:";
        [SerializeField]
        private string _calibrateWindow_deadZoneSliderLabel = "Dead Zone:";
        [SerializeField]
        private string _calibrateWindow_zeroSliderLabel = "Zero:";
        [SerializeField]
        private string _calibrateWindow_sensitivitySliderLabel = "灵敏度:";
        [SerializeField]
        private string _calibrateWindow_invertToggleLabel = "Invert";
        [SerializeField]
        private string _calibrateWindow_calibrateButtonLabel = "校准";
        [SerializeField]
        private ModifierKeys _modifierKeys;
        [SerializeField]
        private CustomEntry[] _customEntries;

        // Working vars
        private bool _initialized;
        private Dictionary<string, string> customDict;

        // Public Methods / Properties

        public override void Initialize()
        {
            if (_initialized) return;
            customDict = CustomEntry.ToDictionary(_customEntries);
            _initialized = true;
        }

        public override string GetCustomEntry(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string value;
            if (!customDict.TryGetValue(key, out value)) return string.Empty;
            return value;
        }

        public override bool ContainsCustomEntryKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return customDict.ContainsKey(key);
        }

        public override string yes { get { return _yes; } }
        public override string no { get { return _no; } }
        public override string add { get { return _add; } }
        public override string replace { get { return _replace; } }
        public override string remove { get { return _remove; } }
        public override string swap { get { return _swap; } }
        public override string cancel { get { return _cancel; } }
        public override string none { get { return _none; } }
        public override string okay { get { return _okay; } }
        public override string done { get { return _done; } }
        public override string default_ { get { return _default; } }
        public override string assignControllerWindowTitle { get { return _assignControllerWindowTitle; } }
        public override string assignControllerWindowMessage { get { return _assignControllerWindowMessage; } }
        public override string controllerAssignmentConflictWindowTitle { get { return _controllerAssignmentConflictWindowTitle; } }
        public override string elementAssignmentPrePollingWindowMessage { get { return _elementAssignmentPrePollingWindowMessage; } }
        public override string elementAssignmentConflictWindowMessage { get { return _elementAssignmentConflictWindowMessage; } }
        public override string mouseAssignmentConflictWindowTitle { get { return _mouseAssignmentConflictWindowTitle; } }
        public override string calibrateControllerWindowTitle { get { return _calibrateControllerWindowTitle; } }
        public override string calibrateAxisStep1WindowTitle { get { return _calibrateAxisStep1WindowTitle; } }
        public override string calibrateAxisStep2WindowTitle { get { return _calibrateAxisStep2WindowTitle; } }
        public override string inputBehaviorSettingsWindowTitle { get { return _inputBehaviorSettingsWindowTitle; } }
        public override string restoreDefaultsWindowTitle { get { return _restoreDefaultsWindowTitle; } }
        public override string actionColumnLabel { get { return _actionColumnLabel; } }
        public override string keyboardColumnLabel { get { return _keyboardColumnLabel; } }
        public override string mouseColumnLabel { get { return _mouseColumnLabel; } }
        public override string controllerColumnLabel { get { return _controllerColumnLabel; } }
        public override string removeControllerButtonLabel { get { return _removeControllerButtonLabel; } }
        public override string calibrateControllerButtonLabel { get { return _calibrateControllerButtonLabel; } }
        public override string assignControllerButtonLabel { get { return _assignControllerButtonLabel; } }
        public override string inputBehaviorSettingsButtonLabel { get { return _inputBehaviorSettingsButtonLabel; } }
        public override string doneButtonLabel { get { return _doneButtonLabel; } }
        public override string restoreDefaultsButtonLabel { get { return _restoreDefaultsButtonLabel; } }
        public override string controllerSettingsGroupLabel { get { return _controllerSettingsGroupLabel; } }
        public override string playersGroupLabel { get { return _playersGroupLabel; } }
        public override string assignedControllersGroupLabel { get { return _assignedControllersGroupLabel; } }
        public override string settingsGroupLabel { get { return _settingsGroupLabel; } }
        public override string mapCategoriesGroupLabel { get { return _mapCategoriesGroupLabel; } }
        public override string restoreDefaultsWindowMessage
        {
            get
            {
                if (Rewired.ReInput.players.playerCount > 1) return _restoreDefaultsWindowMessage_multiPlayer;
                else return _restoreDefaultsWindowMessage_onePlayer;
            }
        }
        public override string calibrateWindow_deadZoneSliderLabel { get { return _calibrateWindow_deadZoneSliderLabel; } }
        public override string calibrateWindow_zeroSliderLabel { get { return _calibrateWindow_zeroSliderLabel; } }
        public override string calibrateWindow_sensitivitySliderLabel { get { return _calibrateWindow_sensitivitySliderLabel; } }
        public override string calibrateWindow_invertToggleLabel { get { return _calibrateWindow_invertToggleLabel; } }
        public override string calibrateWindow_calibrateButtonLabel { get { return _calibrateWindow_calibrateButtonLabel; } }

        public override string GetControllerAssignmentConflictWindowMessage(string joystickName, string otherPlayerName, string currentPlayerName)
        {
            return string.Format(_controllerAssignmentConflictWindowMessage, joystickName, otherPlayerName, currentPlayerName);
        }
        public override string GetJoystickElementAssignmentPollingWindowMessage(string actionName)
        {
            return string.Format(_joystickElementAssignmentPollingWindowMessage, actionName);
        }
        public override string GetJoystickElementAssignmentPollingWindowMessage_FullAxisFieldOnly(string actionName)
        {
            return string.Format(_joystickElementAssignmentPollingWindowMessage_fullAxisFieldOnly, actionName);
        }
        public override string GetKeyboardElementAssignmentPollingWindowMessage(string actionName)
        {
            return string.Format(_keyboardElementAssignmentPollingWindowMessage, actionName);
        }
        public override string GetMouseElementAssignmentPollingWindowMessage(string actionName)
        {
            return string.Format(_mouseElementAssignmentPollingWindowMessage, actionName);
        }
        public override string GetMouseElementAssignmentPollingWindowMessage_FullAxisFieldOnly(string actionName)
        {
            return string.Format(_mouseElementAssignmentPollingWindowMessage_fullAxisFieldOnly, actionName);
        }
        public override string GetElementAlreadyInUseBlocked(string elementName)
        {
            return string.Format(_elementAlreadyInUseBlocked, elementName);
        }
        public override string GetElementAlreadyInUseCanReplace(string elementName, bool allowConflicts)
        {
            if (!allowConflicts) return string.Format(_elementAlreadyInUseCanReplace, elementName);
            return string.Format(_elementAlreadyInUseCanReplace_conflictAllowed, elementName);
        }
        public override string GetMouseAssignmentConflictWindowMessage(string otherPlayerName, string thisPlayerName)
        {
            return string.Format(_mouseAssignmentConflictWindowMessage, otherPlayerName, thisPlayerName);
        }
        public override string GetCalibrateAxisStep1WindowMessage(string axisName)
        {
            return string.Format(_calibrateAxisStep1WindowMessage, axisName);
        }
        public override string GetCalibrateAxisStep2WindowMessage(string axisName)
        {
            return string.Format(_calibrateAxisStep2WindowMessage, axisName);
        }

        // Translation of Rewired core items

        public override string GetPlayerName(int playerId)
        {
            Player player = ReInput.players.GetPlayer(playerId);
            if (player == null) throw new ArgumentException("Invalid player id: " + playerId);
            return player.descriptiveName;
        }

        public override string GetControllerName(Controller controller)
        {
            if (controller == null) throw new ArgumentNullException("controller");
            return controller.name;
        }

        public override string GetElementIdentifierName(ActionElementMap actionElementMap)
        {
            if (actionElementMap == null) throw new ArgumentNullException("actionElementMap");
            if (actionElementMap.controllerMap.controllerType == ControllerType.Keyboard) return GetElementIdentifierName(actionElementMap.keyCode, actionElementMap.modifierKeyFlags);
            return GetElementIdentifierName(actionElementMap.controllerMap.controller, actionElementMap.elementIdentifierId, actionElementMap.axisRange);
        }
        public override string GetElementIdentifierName(Controller controller, int elementIdentifierId, AxisRange axisRange)
        {
            if (controller == null) throw new ArgumentNullException("controller");
            ControllerElementIdentifier eid = controller.GetElementIdentifierById(elementIdentifierId);
            if (eid == null) throw new ArgumentException("Invalid element identifier id: " + elementIdentifierId);
            Controller.Element element = controller.GetElementById(elementIdentifierId);
            if (element == null) return string.Empty;
            switch (element.type)
            {
                case ControllerElementType.Axis:
                    return eid.GetDisplayName(element.type, axisRange);
                case ControllerElementType.Button:
                    return eid.name;
                default:
                    return eid.name;
            }
        }
        public override string GetElementIdentifierName(KeyCode keyCode, ModifierKeyFlags modifierKeyFlags)
        {
            if (modifierKeyFlags != ModifierKeyFlags.None)
            {
                return string.Format(
                    "{0}{1}{2}",
                    ModifierKeyFlagsToString(modifierKeyFlags),
                    _modifierKeys.separator,
                    Keyboard.GetKeyName(keyCode)
                );
            }
            else
            {
                return Keyboard.GetKeyName(keyCode);
            }
        }

        public override string GetActionName(int actionId)
        {
            InputAction action = ReInput.mapping.GetAction(actionId);
            if (action == null) throw new ArgumentException("Invalid action id: " + actionId);

            return GetLocalizedDescriptiveName(action);
        }

        string GetLocalizedDescriptiveName(InputAction action)
        {
            if (StringManager.gameLanguage == EGameLanguage.zh_cn)
            {
                return StringManager.GetString(action.descriptiveName);
            }

            return action.descriptiveName;
        }

        public override string GetActionName(int actionId, AxisRange axisRange)
        {
            InputAction action = ReInput.mapping.GetAction(actionId);
            if (action == null) throw new ArgumentException("Invalid action id: " + actionId);

            string baseValue = action.descriptiveName;
            string posName = action.positiveDescriptiveName;
            string negName = action.negativeDescriptiveName;

            if (StringManager.gameLanguage == EGameLanguage.zh_cn)
            {
                baseValue = StringManager.GetString(baseValue);
                if (!string.IsNullOrEmpty(posName)) posName = StringManager.GetString(posName);
                if (!string.IsNullOrEmpty(negName)) negName = StringManager.GetString(negName);
            }

            switch (axisRange)
            {
                case AxisRange.Full:
                    return baseValue;
                case AxisRange.Positive:
                    return !string.IsNullOrEmpty(posName) ? posName : baseValue + " +";
                case AxisRange.Negative:
                    return !string.IsNullOrEmpty(negName) ? negName : baseValue + " -";
                default:
                    throw new NotImplementedException();
            }
        }

        public override string GetMapCategoryName(int id)
        {
            InputMapCategory category = ReInput.mapping.GetMapCategory(id);
            if (category == null) throw new ArgumentException("Invalid map category id: " + id);
            return category.descriptiveName;
        }

        public override string GetActionCategoryName(int id)
        {
            InputCategory category = ReInput.mapping.GetActionCategory(id);
            if (category == null) throw new ArgumentException("Invalid action category id: " + id);
            return category.descriptiveName;
        }

        public override string GetLayoutName(ControllerType controllerType, int id)
        {
            InputLayout layout = ReInput.mapping.GetLayout(controllerType, id);
            if (layout == null) throw new ArgumentException("Invalid " + controllerType + " layout id: " + id);
            return layout.descriptiveName;
        }

        public override string ModifierKeyFlagsToString(ModifierKeyFlags flags)
        {
            int count = 0;
            string label = string.Empty;

            // TODO: Make this use the HardwareMap so languages can be supported

            if (Keyboard.ModifierKeyFlagsContain(flags, ModifierKey.Control))
            {
                label += _modifierKeys.control;
                count++;
            }

            if (Keyboard.ModifierKeyFlagsContain(flags, ModifierKey.Command))
            {
                if (count > 0 && !string.IsNullOrEmpty(_modifierKeys.separator)) label += _modifierKeys.separator;
                label += _modifierKeys.command;
                count++;
            }

            if (Keyboard.ModifierKeyFlagsContain(flags, ModifierKey.Alt))
            {
                if (count > 0 && !string.IsNullOrEmpty(_modifierKeys.separator)) label += _modifierKeys.separator;
                label += _modifierKeys.alt;
                count++;
            }
            if (count >= 3) return label; // hit the limit of 3 modifiers

            if (Keyboard.ModifierKeyFlagsContain(flags, ModifierKey.Shift))
            {
                if (count > 0 && !string.IsNullOrEmpty(_modifierKeys.separator)) label += _modifierKeys.separator;
                label += _modifierKeys.shift;
                count++;
            }

            return label;
        }

        // Classes

        [System.Serializable]
        protected class CustomEntry
        {
            public string key;
            public string value;

            public CustomEntry()
            {
            }

            public CustomEntry(string key, string value)
            {
                this.key = key;
                this.value = value;
            }

            public static Dictionary<string, string> ToDictionary(CustomEntry[] array)
            {
                if (array == null) return new Dictionary<string, string>();
                Dictionary<string, string> dict = new Dictionary<string, string>();
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] == null) continue;
                    if (string.IsNullOrEmpty(array[i].key) || string.IsNullOrEmpty(array[i].value)) continue;
                    if (dict.ContainsKey(array[i].key))
                    {
                        Debug.LogError("Key \"" + array[i].key + "\" is already in dictionary!");
                        continue;
                    }
                    dict.Add(array[i].key, array[i].value);
                }
                return dict;
            }
        }

        [System.Serializable]
        protected class ModifierKeys
        {
            public string control = "Control";
            public string alt = "Alt";
            public string shift = "Shift";
            public string command = "Command";
            public string separator = " + ";
        }
    }
}