using System.Collections.Generic;
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

        private LinkedList<ICommand> _undoList = new LinkedList<ICommand>();
        private LinkedList<ICommand> _redoList = new LinkedList<ICommand>();
        
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
            // 处理撤销列表，移除最旧的命令
            while (_undoList.Count > _maxHistorySize)
            {
                var oldestCommandNode = _undoList.First;
                // 注意：达到历史限制时，只从列表中移除最旧命令，不销毁其资源，避免影响场景现状
                _undoList.RemoveFirst();
                Debug.Log($"已移除超出历史记录限制的命令(仅从历史移除，未销毁资源): {oldestCommandNode.Value.GetType().Name}");
            }
            
            // 处理重做列表，移除最旧的命令
            while (_redoList.Count > _maxHistorySize)
            {
                var oldestCommandNode = _redoList.First;
                // 仅移除历史记录，不销毁资源
                _redoList.RemoveFirst();
                Debug.Log($"已移除超出历史记录限制的重做命令(仅从历史移除): {oldestCommandNode.Value.GetType().Name}");
            }
        }
        
        /// <summary>
        /// 检查命令是否实现了IDisposableCommand，如果是，则调用其Dispose方法
        /// </summary>
        /// <param name="command">要处理的命令</param>
        private void DisposeCommand(ICommand command)
        {
            if (command is IDisposableCommand disposableCommand)
            {
                disposableCommand.Dispose();
            }
        }

        /// <summary>
        /// 执行一个新命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();          // 首先执行命令
            _undoList.AddLast(command); // 将命令添加到撤销列表的末尾
            _redoList.Clear();          // 一旦有新操作，清空重做列表
            
            // 检查历史记录限制
            EnforceHistoryLimit();
            
            Debug.Log($"执行命令: {command.GetType().Name}, 撤销列表数量: {_undoList.Count}");
        }

        /// <summary>
        /// 撤销上一个操作
        /// </summary>
        public void Undo()
        {
            if (_undoList.Count > 0)
            {
                var lastCommandNode = _undoList.Last; // 获取撤销列表的最后一个节点
                ICommand command = lastCommandNode.Value;
                command.Undo();                        // 执行撤销
                _undoList.RemoveLast();                // 从撤销列表中移除
                _redoList.AddLast(command);            // 将命令添加到重做列表的末尾
                
                Debug.Log($"撤销命令: {command.GetType().Name}, 撤销列表数量: {_undoList.Count}, 重做列表数量: {_redoList.Count}");
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
            if (_redoList.Count > 0)
            {
                var lastCommandNode = _redoList.Last; // 获取重做列表的最后一个节点
                ICommand command = lastCommandNode.Value;
                command.Execute();                     // 重新执行
                _redoList.RemoveLast();                // 从重做列表中移除
                _undoList.AddLast(command);            // 将命令添加回撤销列表的末尾
                
                Debug.Log($"重做命令: {command.GetType().Name}, 撤销列表数量: {_undoList.Count}, 重做列表数量: {_redoList.Count}");
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
            foreach (var command in _undoList)
            {
                DisposeCommand(command);
            }
            
            foreach (var command in _redoList)
            {
                DisposeCommand(command);
            }
            
            _undoList.Clear();
            _redoList.Clear();
            Debug.Log("命令历史已清空，所有相关对象已销毁");
        }
        
        /// <summary>
        /// 获取当前撤销列表中的命令数量
        /// </summary>
        public int UndoCount => _undoList.Count;
        
        /// <summary>
        /// 获取当前重做列表中的命令数量
        /// </summary>
        public int RedoCount => _redoList.Count;

        /// <summary>
        /// 获取是否可以撤销
        /// </summary>
        public bool CanUndo => _undoList.Count > 0;

        /// <summary>
        /// 获取是否可以重做
        /// </summary>
        public bool CanRedo => _redoList.Count > 0;
    }
}