// Interactable.cs
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    // C# Action/Event 是实现事件驱动的推荐方式
    public event Action<InteractableState> OnStateChanged;

    private InteractableState _currentState = InteractableState.None;

    public InteractableState CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState != value)
            {
                _currentState = value;
                // 当状态改变时，广播事件，并把新状态传递出去
                OnStateChanged?.Invoke(_currentState);
                Debug.Log($"{gameObject.name} state changed to: {_currentState}");
            }
        }
    }

    // 提供一个公共方法给外部调用，用来改变状态
    public void SetState(InteractableState newState)
    {
        CurrentState = newState;
    }
}
