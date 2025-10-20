using UnityEngine;

namespace YouYouTest.CommandFramework
{
    /// <summary>
    /// 命令接口，所有具体的操作都将实现这个接口
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 执行这个操作
        /// </summary>
        void Execute();
        
        /// <summary>
        /// 撤销这个操作
        /// </summary>
        void Undo();
    }

    /// <summary>
    /// 用于定义需要释放资源的命令接口
    /// 当命令从历史记录中被永久移除时，CommandHistory会调用此接口来清理相关资源
    /// </summary>
    public interface IDisposableCommand : ICommand
    {
        /// <summary>
        /// 释放此命令所持有的资源
        /// </summary>
        void Dispose();
    }
}