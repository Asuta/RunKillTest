using UnityEngine;
using YouYouTest.CommandFramework;

namespace YouYouTest.Test
{
    /// <summary>
    /// 缩放命令测试脚本
    /// </summary>
    public class ScaleCommandTest : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private GameObject testObject;
        [SerializeField] private Vector3 testScale = Vector3.one * 2f;
        
        [Header("快捷键设置")]
        [SerializeField] private KeyCode executeKey = KeyCode.E;
        [SerializeField] private KeyCode undoKey = KeyCode.Z;
        [SerializeField] private KeyCode redoKey = KeyCode.Y;
        
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        
        private void Start()
        {
            if (testObject == null)
            {
                Debug.LogError("请设置测试物体！");
                return;
            }
            
            // 保存原始状态
            originalScale = testObject.transform.localScale;
            originalPosition = testObject.transform.position;
            originalRotation = testObject.transform.rotation;
            
            Debug.Log($"缩放命令测试初始化完成。测试物体: {testObject.name}");
            Debug.Log($"原始缩放: {originalScale}");
            Debug.Log($"快捷键: {executeKey} - 执行缩放, {undoKey} - 撤销, {redoKey} - 重做");
        }
        
        private void Update()
        {
            if (testObject == null) return;
            
            // 执行缩放命令
            if (Input.GetKeyDown(executeKey))
            {
                ExecuteScaleTest();
            }
            
            // 撤销
            if (Input.GetKeyDown(undoKey))
            {
                UndoScaleTest();
            }
            
            // 重做
            if (Input.GetKeyDown(redoKey))
            {
                RedoScaleTest();
            }
        }
        
        /// <summary>
        /// 执行缩放测试
        /// </summary>
        private void ExecuteScaleTest()
        {
            // 创建并执行缩放命令
            var scaleCommand = new ScaleCommand(
                testObject.transform,
                originalScale,
                originalPosition,
                originalRotation
            );
            
            // 设置最终状态
            scaleCommand.SetEndTransform(
                testScale,
                testObject.transform.position,
                testObject.transform.rotation
            );
            
            // 执行命令
            CommandHistory.Instance.ExecuteCommand(scaleCommand);
            
            Debug.Log($"执行缩放命令: {originalScale} -> {testScale}");
        }
        
        /// <summary>
        /// 撤销缩放测试
        /// </summary>
        private void UndoScaleTest()
        {
            CommandHistory.Instance.Undo();
            Debug.Log("撤销缩放命令");
        }
        
        /// <summary>
        /// 重做缩放测试
        /// </summary>
        private void RedoScaleTest()
        {
            CommandHistory.Instance.Redo();
            Debug.Log("重做缩放命令");
        }
        
        /// <summary>
        /// 在GUI中显示当前状态和操作提示
        /// </summary>
        private void OnGUI()
        {
            if (testObject == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"测试物体: {testObject.name}");
            GUILayout.Label($"当前缩放: {testObject.transform.localScale}");
            GUILayout.Label($"原始缩放: {originalScale}");
            GUILayout.Label($"目标缩放: {testScale}");
            GUILayout.Space(10);
            GUILayout.Label($"历史记录 - 可撤销: {CommandHistory.Instance.CanUndo}");
            GUILayout.Label($"历史记录 - 可重做: {CommandHistory.Instance.CanRedo}");
            GUILayout.Space(10);
            GUILayout.Label($"按 {executeKey} 执行缩放");
            GUILayout.Label($"按 {undoKey} 撤销操作");
            GUILayout.Label($"按 {redoKey} 重做操作");
            GUILayout.EndArea();
        }
    }
}