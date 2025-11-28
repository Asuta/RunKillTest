using UnityEngine;

public class UserData
{
    public string UserName { get; set; } = "Player1";
    public int Level { get; set; } = 1;

    public void LogUserData()
    {
        Debug.Log($"[UserData] User: {UserName}, Level: {Level}");
    }
}