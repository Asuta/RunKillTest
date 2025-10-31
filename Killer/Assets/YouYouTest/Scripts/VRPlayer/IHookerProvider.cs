using System;
public interface IWallSlidingProvider
{

    event Action OnEnterWallSliding;
    event Action OnExitWallSliding;
    // event Action<int> OnValueChanged;


}