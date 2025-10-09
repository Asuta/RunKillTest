using UnityEngine;

public class HeadCameraController : MonoBehaviour
{
    [Header("鼠标灵敏度设置")]
    [Tooltip("控制水平（左右）鼠标移动的灵敏度")]
    public float mouseSensitivity = 100f;
    
    [Tooltip("控制垂直（上下）鼠标移动的灵敏度")]
    public float verticalSensitivity = 100f;
    
    [Header("旋转限制")]
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;
    
    private float xRotation = 0f;
    private float yRotation = 0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 锁定鼠标到屏幕中心并隐藏鼠标光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMouseLook();
    }
    
    void HandleMouseLook()
    {
        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;
        
        // 计算旋转角度
        yRotation += mouseX;
        xRotation -= mouseY;
        
        // 限制垂直旋转角度
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);
        
        // 应用旋转
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
    
    // 可选：添加方法以便在其他地方控制鼠标锁定状态
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
