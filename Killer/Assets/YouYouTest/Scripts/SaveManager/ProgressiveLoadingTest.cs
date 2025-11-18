using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 过程加载测试脚本
/// </summary>
public class ProgressiveLoadingTest : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private Text loadingText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Text statusText;
    
    [Header("测试设置")]
    [SerializeField] private string testSlotName = "Default";
    [SerializeField] private bool showDebugUI = true;
    
    private void Start()
    {
        // 初始化UI
        InitializeUI();
        
        // 订阅加载事件
        SaveLoadManager.Instance.OnLoadingProgress += OnLoadingProgress;
        SaveLoadManager.Instance.OnLoadingComplete += OnLoadingComplete;
        SaveLoadManager.Instance.OnLoadingError += OnLoadingError;
        
        UpdateStatus("准备就绪");
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.OnLoadingProgress -= OnLoadingProgress;
            SaveLoadManager.Instance.OnLoadingComplete -= OnLoadingComplete;
            SaveLoadManager.Instance.OnLoadingError -= OnLoadingError;
        }
    }
    
    private void InitializeUI()
    {
        if (!showDebugUI)
        {
            // 如果不需要显示UI，隐藏所有UI元素
            if (loadingSlider != null) loadingSlider.gameObject.SetActive(false);
            if (loadingText != null) loadingText.gameObject.SetActive(false);
            if (loadButton != null) loadButton.gameObject.SetActive(false);
            if (stopButton != null) stopButton.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);
            return;
        }
        
        // 设置按钮事件
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(StartLoading);
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(StopLoading);
        }
        
        // 初始化进度条
        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = 0f;
        }
        
        // 初始化文本
        if (loadingText != null)
        {
            loadingText.text = "0%";
        }
        
        // 初始状态
        UpdateButtonStates(true);
    }
    
    private void Update()
    {
        // 按T键测试加载
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartLoading();
        }
        
        // 按ESC键停止加载
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopLoading();
        }
    }
    
    public void StartLoading()
    {
        if (SaveLoadManager.Instance.IsLoading())
        {
            Debug.LogWarning("已有加载任务在进行中");
            return;
        }
        
        Debug.Log($"开始测试过程加载，档位: {testSlotName}");
        UpdateStatus("正在加载...");
        UpdateButtonStates(false);
        
        // 开始加载
        SaveLoadManager.Instance.LoadSceneObjects(testSlotName);
    }
    
    public void StopLoading()
    {
        if (!SaveLoadManager.Instance.IsLoading())
        {
            Debug.LogWarning("当前没有加载任务");
            return;
        }
        
        Debug.Log("停止加载");
        SaveLoadManager.Instance.StopLoading();
        
        UpdateStatus("加载已停止");
        UpdateButtonStates(true);
        
        // 重置UI
        if (loadingSlider != null) loadingSlider.value = 0f;
        if (loadingText != null) loadingText.text = "0%";
    }
    
    private void OnLoadingProgress(float progress, int currentCount, int totalCount)
    {
        // 更新进度条
        if (loadingSlider != null)
        {
            loadingSlider.value = progress;
        }
        
        // 更新文本
        if (loadingText != null)
        {
            loadingText.text = $"{(progress * 100):F1}%";
        }
        
        // 更新状态
        UpdateStatus($"加载中: {currentCount}/{totalCount} ({(progress * 100):F1}%)");
        
        // 每10%输出一次日志
        if (currentCount % Mathf.Max(1, totalCount / 10) == 0)
        {
            Debug.Log($"加载进度: {currentCount}/{totalCount} ({(progress * 100):F1}%)");
        }
    }
    
    private void OnLoadingComplete()
    {
        Debug.Log("加载完成!");
        UpdateStatus("加载完成");
        UpdateButtonStates(true);
        
        // 完成后隐藏进度条
        if (loadingSlider != null)
        {
            loadingSlider.value = 1f;
        }
        
        if (loadingText != null)
        {
            loadingText.text = "100%";
        }
    }
    
    private void OnLoadingError(string errorMessage)
    {
        Debug.LogError($"加载错误: {errorMessage}");
        UpdateStatus($"加载错误: {errorMessage}");
        UpdateButtonStates(true);
    }
    
    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }
    
    private void UpdateButtonStates(bool canLoad)
    {
        if (loadButton != null)
        {
            loadButton.interactable = canLoad;
        }
        
        if (stopButton != null)
        {
            stopButton.interactable = !canLoad;
        }
    }
    
    /// <summary>
    /// 创建测试UI（如果没有在Inspector中设置）
    /// </summary>
    [ContextMenu("创建测试UI")]
    public void CreateTestUI()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("ProgressiveLoadingTest_UI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 创建背景面板
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // 创建进度条
        GameObject sliderObj = new GameObject("LoadingSlider");
        sliderObj.transform.SetParent(panel.transform, false);
        loadingSlider = sliderObj.AddComponent<Slider>();
        
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.3f, 0.6f);
        sliderRect.anchorMax = new Vector2(0.7f, 0.65f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;
        
        // 创建进度文本
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(panel.transform, false);
        loadingText = textObj.AddComponent<Text>();
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.3f, 0.5f);
        textRect.anchorMax = new Vector2(0.7f, 0.55f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // 创建状态文本
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(panel.transform, false);
        statusText = statusObj.AddComponent<Text>();
        
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.3f, 0.4f);
        statusRect.anchorMax = new Vector2(0.7f, 0.45f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        
        // 创建加载按钮
        GameObject loadBtnObj = new GameObject("LoadButton");
        loadBtnObj.transform.SetParent(panel.transform, false);
        loadButton = loadBtnObj.AddComponent<Button>();
        
        RectTransform loadBtnRect = loadBtnObj.GetComponent<RectTransform>();
        loadBtnRect.anchorMin = new Vector2(0.3f, 0.2f);
        loadBtnRect.anchorMax = new Vector2(0.45f, 0.3f);
        loadBtnRect.offsetMin = Vector2.zero;
        loadBtnRect.offsetMax = Vector2.zero;
        
        // 创建停止按钮
        GameObject stopBtnObj = new GameObject("StopButton");
        stopBtnObj.transform.SetParent(panel.transform, false);
        stopButton = stopBtnObj.AddComponent<Button>();
        
        RectTransform stopBtnRect = stopBtnObj.GetComponent<RectTransform>();
        stopBtnRect.anchorMin = new Vector2(0.55f, 0.2f);
        stopBtnRect.anchorMax = new Vector2(0.7f, 0.3f);
        stopBtnRect.offsetMin = Vector2.zero;
        stopBtnRect.offsetMax = Vector2.zero;
        
        Debug.Log("测试UI已创建");
    }
}