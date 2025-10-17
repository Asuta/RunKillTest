using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 命令历史管理器，用于处理命令的执行、撤销和重做
    /// </summary>
    public class CommandHistory
    {
        // 单例实例
        private static CommandHistory _instance;
        public static CommandHistory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CommandHistory();
                }
                return _instance;
            }
        }

        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();
        
        // 历史记录数量限制
        [SerializeField] private int _maxHistorySize = 50; // 默认限制为50条记录
        
        // 属性用于外部访问和修改最大历史记录数量
        public int MaxHistorySize
        {
            get => _maxHistorySize;
            set
            {
                _maxHistorySize = Mathf.Max(1, value); // 确保至少为1
                EnforceHistoryLimit(); // 应用新限制
            }
        }
        
        // 私有构造函数，防止外部实例化
        private CommandHistory()
        {
        }
        
        /// <summary>
        /// 强制执行历史记录限制，当超过限制时销毁最旧的命令
        /// </summary>
        private void EnforceHistoryLimit()
        {
            // 处理撤销栈
            while (_undoStack.Count > _maxHistorySize)
            {
                // 获取最底部的命令（最早的命令）
                ICommand oldestCommand = _undoStack.ElementAt(_undoStack.Count - 1);
                
                // 从栈中移除该命令
                Stack<ICommand> tempStack = new Stack<ICommand>();
                while (_undoStack.Count > 0)
                {
                    ICommand cmd = _undoStack.Pop();
                    if (cmd != oldestCommand)
                    {
                        tempStack.Push(cmd);
                    }
                }
                
                // 重新构建栈
                while (tempStack.Count > 0)
                {
                    _undoStack.Push(tempStack.Pop());
                }
                
                // 销毁该命令引用的对象
                DestroyCommandObjects(oldestCommand);
                Debug.Log($"已销毁超出历史记录限制的命令: {oldestCommand.GetType().Name}");
            }
            
            // 处理重做栈
            while (_redoStack.Count > _maxHistorySize)
            {
                // 获取最底部的命令（最早的命令）
                ICommand oldestCommand = _redoStack.ElementAt(_redoStack.Count - 1);
                
                // 从栈中移除该命令
                Stack<ICommand> tempStack = new Stack<ICommand>();
                while (_redoStack.Count > 0)
                {
                    ICommand cmd = _redoStack.Pop();
                    if (cmd != oldestCommand)
                    {
                        tempStack.Push(cmd);
                    }
                }
                
                // 重新构建栈
                while (tempStack.Count > 0)
                {
                    _redoStack.Push(tempStack.Pop());
                }
                
                // 销毁该命令引用的对象
                DestroyCommandObjects(oldestCommand);
                Debug.Log($"已销毁超出历史记录限制的重做命令: {oldestCommand.GetType().Name}");
            }
        }
        
        /// <summary>
        /// 销毁命令引用的对象，真正地销毁而不是禁用
        /// </summary>
        /// <param name="command">要销毁对象的命令</param>
        private void DestroyCommandObjects(ICommand command)
        {
            if (command is CreateObjectCommand createCommand)
            {
                GameObject createdObject = createCommand.GetCreatedObject();
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                    Debug.Log($"已销毁对象: {createdObject.name}");
                }
            }
            else if (command is DeleteObjectCommand deleteCommand)
            {
                // 获取DeleteObjectCommand的目标对象并真正销毁它
                GameObject targetObject = deleteCommand.GetTargetObject();
                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                    Debug.Log($"已销毁DeleteObjectCommand的目标对象: {targetObject.name}");
                }
            }
            // MoveCommand和GrabCommand通常不需要销毁对象，因为它们只是移动现有对象
        }

        /// <summary>
        /// 执行一个新命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();          // 首先执行命令
            _undoStack.Push(command);   // 将命令压入撤销栈
            _redoStack.Clear();         // 一旦有新操作，清空重做栈
            
            // 检查历史记录限制
            EnforceHistoryLimit();
            
            Debug.Log($"执行命令: {command.GetType().Name}, 撤销栈数量: {_undoStack.Count}");
        }

        /// <summary>
        /// 撤销上一个操作
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                ICommand command = _undoStack.Pop(); // 从撤销栈中取出命令
                command.Undo();                      // 执行撤销
                _redoStack.Push(command);            // 将命令压入重做栈
                
                // 检查历史记录限制
                EnforceHistoryLimit();
                
                Debug.Log($"撤销命令: {command.GetType().Name}, 撤销栈数量: {_undoStack.Count}, 重做栈数量: {_redoStack.Count}");
            }
            else
            {
                Debug.Log("没有可撤销的操作");
            }
        }

        /// <summary>
        /// 重做上一个被撤销的操作
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                ICommand command = _redoStack.Pop(); // 从重做栈中取出命令
                command.Execute();                   // 重新执行
                _undoStack.Push(command);            // 将命令压回撤销栈
                
                // 检查历史记录限制
                EnforceHistoryLimit();
                
                Debug.Log($"重做命令: {command.GetType().Name}, 撤销栈数量: {_undoStack.Count}, 重做栈数量: {_redoStack.Count}");
            }
            else
            {
                Debug.Log("没有可重做的操作");
            }
        }
      
        /// <summary>
        /// 清空历史记录 (例如：加载新关卡时)
        /// </summary>
        public void Clear()
        {
            // 销毁所有命令引用的对象
            foreach (var command in _undoStack)
            {
                DestroyCommandObjects(command);
            }
            
            foreach (var command in _redoStack)
            {
                DestroyCommandObjects(command);
            }
            
            _undoStack.Clear();
            _redoStack.Clear();
            Debug.Log("命令历史已清空，所有相关对象已销毁");
        }
        
        /// <summary>
        /// 获取当前撤销栈中的命令数量
        /// </summary>
        public int UndoCount => _undoStack.Count;
        
        /// <summary>
        /// 获取当前重做栈中的命令数量
        /// </summary>
        public int RedoCount => _redoStack.Count;

        /// <summary>
        /// 获取是否可以撤销
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// 获取是否可以重做
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;
    }
}