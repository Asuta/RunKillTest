using UnityEngine;

namespace YouYouTest.CommandTest
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
}