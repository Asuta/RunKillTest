using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 关卡编辑器管理器，编辑器的主控制器
    /// </summary>
    public class LevelEditorManager : MonoBehaviour
    {
        [Header("编辑器设置")]
        [SerializeField] private GameObject _cubePrefab;
        [SerializeField] private GameObject _spherePrefab;
        [SerializeField] private Camera _editorCamera;
        
        [Header("调试信息")]
        [SerializeField] private bool _showDebugInfo = true;
        
        private CommandHistory _commandHistory = new CommandHistory();
        private Transform _selectedObject; // 当前选中的物体
        private Vector3 _dragStartPosition; // 拖拽物体的起始位置
        private Quaternion _dragStartRotation; // 拖拽物体的起始角度
        private bool _isDragging = false;

        void Start()
        {
            if (_editorCamera == null)
                _editorCamera = Camera.main;
                
            Debug.Log("关卡编辑器已初始化");
        }

        void Update()
        {
            // 监听键盘输入，用于撤销和重做
            // Windows: Ctrl+Z, Ctrl+Y
            // macOS: Command+Z, Command+Shift+Z (这里用Ctrl+Y代替)
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    _commandHistory.Undo();
                }
                else if (Input.GetKeyDown(KeyCode.Y))
                {
                    _commandHistory.Redo();
                }
            }

            // 处理用户输入，例如鼠标点击选择、拖拽物体等
            HandleMouseInput();
            
            // 处理键盘快捷键
            HandleKeyboardInput();
        }
      
        private void HandleMouseInput()
        {
            // --- 示例：拖拽移动物体 ---
            if (Input.GetMouseButtonDown(0))
            {
                // 通过射线检测等方式选中物体
                SelectObjectAtMousePosition();
                
                if (_selectedObject != null)
                {
                    _dragStartPosition = _selectedObject.position;
                    _dragStartRotation = _selectedObject.rotation;
                    _isDragging = true;
                }
            }
      
            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                if (_selectedObject != null)
                {
                    Vector3 endPosition = GetMouseWorldPosition();
              
                    // 如果位置发生了变化，创建一个MoveCommand
                    if (Vector3.Distance(_dragStartPosition, endPosition) > 0.01f) // 加一个阈值避免误操作
                    {
                        ICommand moveCommand = new MoveCommand(_selectedObject, _dragStartPosition, endPosition, _dragStartRotation, _selectedObject.rotation);
                        // 注意：这里不要直接执行，而是通过CommandHistory来执行
                        // 我们需要将移动操作本身也放入CommandHistory的记录中
                        // 所以正确的做法是：在拖拽结束后，把物体位置先复原，再由ExecuteCommand来设定最终位置
                        _selectedObject.position = _dragStartPosition; // 复原位置
                        _selectedObject.rotation = _dragStartRotation; // 复原角度
                        _commandHistory.ExecuteCommand(moveCommand); // 执行并记录
                    }
                }
                _selectedObject = null;
                _isDragging = false;
            }
            
            // 实时更新拖拽物体的位置（用于视觉反馈，但不记录到命令历史）
            if (_isDragging && _selectedObject != null)
            {
                Vector3 currentMousePos = GetMouseWorldPosition();
                _selectedObject.position = currentMousePos;
            }
        }
        
        private void HandleKeyboardInput()
        {
            // 删除选中的物体
            if (Input.GetKeyDown(KeyCode.Delete) && _selectedObject != null)
            {
                ICommand deleteCommand = new DeleteObjectCommand(_selectedObject.gameObject);
                _commandHistory.ExecuteCommand(deleteCommand);
                _selectedObject = null;
            }
            
            // 创建立方体
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                OnCreateObjectButtonPressed(_cubePrefab);
            }
            
            // 创建球体
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                OnCreateObjectButtonPressed(_spherePrefab);
            }
        }

        // --- 示例：创建物体的按钮回调 ---
        public void OnCreateObjectButtonPressed(GameObject prefabToCreate)
        {
            if (prefabToCreate == null)
            {
                Debug.LogWarning("预制体为空，无法创建物体");
                return;
            }
                
            // 假设在鼠标位置创建
            Vector3 createPosition = GetMouseWorldPosition(); 
            ICommand createCommand = new CreateObjectCommand(prefabToCreate, createPosition);
            _commandHistory.ExecuteCommand(createCommand);
        }

        // --- 示例：删除物体的按钮回调 ---
        public void OnDeleteObjectButtonPressed(GameObject objectToDelete)
        {
            if (objectToDelete == null)
            {
                Debug.LogWarning("要删除的物体为空");
                return;
            }
                
            ICommand deleteCommand = new DeleteObjectCommand(objectToDelete);
            _commandHistory.ExecuteCommand(deleteCommand);
        }
      
        // 加载新关卡时清除历史记录
        public void LoadLevel()
        {
           // ... 加载关卡逻辑 ...
           _commandHistory.Clear();
        }

        /// <summary>
        /// 获取鼠标在世界坐标中的位置
        /// </summary>
        /// <returns>世界坐标位置</returns>
        private Vector3 GetMouseWorldPosition()
        {
            if (_editorCamera == null)
                return Vector3.zero;
                
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f; // 设置一个固定的Z距离
            
            return _editorCamera.ScreenToWorldPoint(mousePosition);
        }
        
        /// <summary>
        /// 在鼠标位置选择物体
        /// </summary>
        private void SelectObjectAtMousePosition()
        {
            if (_editorCamera == null)
                return;
                
            Ray ray = _editorCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                _selectedObject = hit.transform;
                Debug.Log($"选中物体: {_selectedObject.name}");
            }
            else
            {
                _selectedObject = null;
            }
        }
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        void OnGUI()
        {
            if (!_showDebugInfo)
                return;
                
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("关卡编辑器控制说明:", GUI.skin.box);
            GUILayout.Label("左键拖拽: 移动物体");
            GUILayout.Label("1键: 创建立方体");
            GUILayout.Label("2键: 创建球体");
            GUILayout.Label("Delete键: 删除选中物体");
            GUILayout.Label("Ctrl+Z: 撤销");
            GUILayout.Label("Ctrl+Y: 重做");
            GUILayout.Label($"可撤销: {_commandHistory.CanUndo}");
            GUILayout.Label($"可重做: {_commandHistory.CanRedo}");
            GUILayout.EndArea();
        }
    }
}