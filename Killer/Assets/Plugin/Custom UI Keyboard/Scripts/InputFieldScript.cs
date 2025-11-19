using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Script you need to add to your TMP_InputField if you want to use the UI keyboard
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class InputFieldScript : MonoBehaviour
{
    TMP_InputField inputField; // Current InputField
    public Transform target;
    public float createDistance = 8f;
    public float createScale = 1f;

    [HideInInspector]
    public KeyboardManager targetKeyboard; // Reference to keyboard manager

    /// <summary>
    /// Get current input field
    /// </summary>
    /// <returns></returns>
    public TMP_InputField GetInputField() {
        return inputField;
    }

    /// <summary>
    /// Pressed key button
    /// </summary>
    /// <param name="key">Key pressed</param>
    public void PressKey(string key) {
        switch (key) {
            case "Space":
                if (isSelectingText) {
                    inputField.ProcessEvent(Event.KeyboardEvent("Backspace"));
                }
                inputField.text = inputField.text.Insert(inputField.caretPosition, " ");
                inputField.caretPosition++;
                break;
            case "Backspace":
                inputField.ProcessEvent(Event.KeyboardEvent("Backspace"));
                break;
            case "Enter":
                if (inputField.lineType == TMP_InputField.LineType.SingleLine) {
                    targetKeyboard.canDeselect = true;
                    EventSystem.current.SetSelectedGameObject(null);
                } else {
                    inputField.text = inputField.text.Insert(inputField.caretPosition, "\n");
                    inputField.caretPosition++;
                }
                break;
            default:
                if (isSelectingText) {
                    inputField.ProcessEvent(Event.KeyboardEvent("Backspace"));
                }
                inputField.text = inputField.text.Insert(inputField.caretPosition, key);
                inputField.caretPosition += key.Length;
                break;
        }
        targetKeyboard.CheckShift();
    }

    bool isSelectingText;

    /// <summary>
    /// Start initialization
    /// </summary>
    private void Start() {
        inputField = GetComponent<TMP_InputField>();
        inputField.shouldHideMobileInput = true;
        inputField.shouldHideSoftKeyboard = true;
        inputField.onFocusSelectAll = false;
        inputField.resetOnDeActivation = false;
        inputField.onSelect.AddListener(OnSelected);
        inputField.onDeselect.AddListener(OnDeselected);
        inputField.onTextSelection.AddListener((string text, int pos, int pos2) => {
            isSelectingText = true;
        });
        inputField.onEndTextSelection.AddListener((string text, int pos, int pos2) => {
            isSelectingText = false;
        });
    }

    /// <summary>
    /// On selected input field event
    /// </summary>
    /// <param name="value"></param>
    public void OnSelected(string value) {
        // 延迟一帧执行键盘位置移动，确保targetKeyboard已经被赋值
        StartCoroutine(DelayedKeyboardPositioning());
    }

    /// <summary>
    /// 延迟执行键盘位置移动
    /// </summary>
    /// <returns></returns>
    IEnumerator DelayedKeyboardPositioning() {
        yield return new WaitForEndOfFrame();
        
        // 找到KeyboardManager并设置其父物体为target
        if (targetKeyboard != null && targetKeyboard.transform.parent != null)
        {
            target = targetKeyboard.transform.parent;
            
            // 只有当键盘未激活或者是不同的输入字段时才移动位置
            // 使用反射获取私有字段isSelectedInputField和targetInput
            var isSelectedInputFieldField = typeof(KeyboardManager).GetField("isSelectedInputField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetInputField = typeof(KeyboardManager).GetField("targetInput",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bool isSelectedInputField = (bool)isSelectedInputFieldField?.GetValue(targetKeyboard);
            InputFieldScript currentTargetInput = (InputFieldScript)targetInputField?.GetValue(targetKeyboard);
            
            // 如果键盘未激活或者是不同的输入字段，才移动位置
            if (!isSelectedInputField || currentTargetInput != this)
            {
                // 在激活键盘之前，移动target到指定位置
                if (target != null && GameManager.Instance.VrEditorCameraT != null)
                {
                    // 计算forward方向向下偏移80度的方向
                    // 使用本地坐标计算：先在本地坐标系中旋转，再转换到世界坐标
                    Vector3 localDirection = Quaternion.Euler(-60, 0, 0) * -Vector3.forward;
                    Vector3 direction = transform.TransformDirection(localDirection);
                    // 设置target位置为当前位置加上方向向量乘以距离8米
                    target.position = transform.position + direction * createDistance * GameManager.Instance.VrEditorScale;
                    target.localScale = Vector3.one * createScale * GameManager.Instance.VrEditorScale;
                    target.LookAt(GameManager.Instance.VrEditorCameraT.position);
                    // 在朝向摄像机后再旋转180度
                    target.rotation *= Quaternion.Euler(0, 180, 0);
                }
            }
        }
    }

    /// <summary>
    /// On deselected input field event
    /// </summary>
    /// <param name="value"></param>
    public void OnDeselected(string value) {
    }

    /// <summary>
    /// 当TMP_InputField组件被禁用时调用
    /// </summary>
    private void OnDisable() {
        // 如果键盘存在且处于激活状态，则隐藏键盘
        if (targetKeyboard != null && targetKeyboard.gameObject.activeInHierarchy) {
            // 设置canDeselect为true，允许键盘取消选择
            targetKeyboard.canDeselect = true;
            // 取消当前选中的游戏对象，这会触发键盘隐藏逻辑
            EventSystem.current.SetSelectedGameObject(null);    
        }
    }

    /// <summary>
    /// Deselect input field manually
    /// </summary>
    public void Deselect() {
        inputField.resetOnDeActivation = true;
        inputField.ReleaseSelection();
        inputField.resetOnDeActivation = false;
    }

    /// <summary>
    /// Select input field
    /// </summary>
    public void SelectInput() {
        inputField.Select();
    }
}
