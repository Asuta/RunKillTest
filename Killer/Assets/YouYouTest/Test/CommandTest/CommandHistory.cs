using System.Collections.Generic;
using UnityEngine;

namespace YouYouTest.CommandTest
{
    /// <summary>
    /// 命令历史管理器，用于处理命令的执行、撤销和重做
    /// </summary>
    public class CommandHistory
    {
        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();

        /// <summary>
        /// 执行一个新命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();          // 首先执行命令
            _undoStack.Push(command);   // 将命令压入撤销栈
            _redoStack.Clear();         // 一旦有新操作，清空重做栈
            
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
            _undoStack.Clear();
            _redoStack.Clear();
            Debug.Log("命令历史已清空");
        }

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