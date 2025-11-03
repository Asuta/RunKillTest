# 过程加载系统使用说明

## 概述

过程加载系统将原本在一帧内完成的加载操作分散到多帧中执行，避免大量对象同时加载造成的卡顿。

## 主要特性

1. **分帧加载**: 根据设定的时长（默认3秒）和对象数量，自动计算每帧应加载的对象数量
2. **动态调整**: 根据实际帧率动态调整每帧加载的对象数量
3. **进度跟踪**: 提供实时的加载进度回调
4. **可配置**: 支持自定义加载时长、每帧最小/最大加载对象数量
5. **兼容性**: 保留原有的即时加载方式，可通过配置切换
·
## 配置参数

在 `SaveLoadManager` 的 Inspector 中可以配置以下参数：

- **useProgressiveLoading**: 是否启用过程加载（默认: true）
- **progressiveLoadingDuration**: 加载持续时间，单位秒（默认: 3.0秒）
- **minObjectsPerFrame**: 每帧最少加载对象数量（默认: 1）
- **maxObjectsPerFrame**: 每帧最多加载对象数量（默认: 50）

## 使用方法

### 1. 基本使用

```csharp
// 加载默认档位
SaveLoadManager.Instance.LoadSceneObjects();

// 加载指定档位
SaveLoadManager.Instance.LoadSceneObjects("Slot1");
```

### 2. 监听加载进度

```csharp
// 订阅进度事件
SaveLoadManager.Instance.OnLoadingProgress += OnLoadingProgress;
SaveLoadManager.Instance.OnLoadingComplete += OnLoadingComplete;
SaveLoadManager.Instance.OnLoadingError += OnLoadingError;

private void OnLoadingProgress(float progress, int currentCount, int totalCount)
{
    Debug.Log($"加载进度: {progress:P1} ({currentCount}/{totalCount})");
}

private void OnLoadingComplete()
{
    Debug.Log("加载完成!");
}

private void OnLoadingError(string errorMessage)
{
    Debug.LogError($"加载错误: {errorMessage}");
}
```

### 3. 控制加载

```csharp
// 检查是否正在加载
bool isLoading = SaveLoadManager.Instance.IsLoading();

// 停止当前加载
SaveLoadManager.Instance.StopLoading();
```

## 测试

项目包含一个测试脚本 `ProgressiveLoadingTest.cs`，可以用来测试过程加载功能：

1. 将 `ProgressiveLoadingTest` 脚本添加到场景中的 GameObject
2. 在 Inspector 中配置 UI 引用，或使用右键菜单的"创建测试UI"选项
3. 运行场景，使用按钮或快捷键测试：
   - **T键**: 开始加载
   - **ESC键**: 停止加载

## 工作原理

1. **计算加载速度**: 根据总对象数量和目标加载时长，计算每帧应加载的对象数量
2. **动态调整**: 考虑实际帧率，确保在目标时间内完成加载
3. **分帧执行**: 每帧加载指定数量的对象，避免单帧负载过高
4. **进度反馈**: 实时报告加载进度，便于显示加载界面

## 性能优化建议

1. **合理设置加载时长**: 根据对象数量和性能需求调整 `progressiveLoadingDuration`
2. **调整每帧加载量**: 根据设备性能调整 `minObjectsPerFrame` 和 `maxObjectsPerFrame`
3. **监控性能**: 使用 Unity Profiler 监控加载过程中的性能表现

## 注意事项

1. 过程加载会增加总加载时间，但能提供更流畅的用户体验
2. 如果对象数量很少（如少于10个），即时加载可能更合适
3. 确保在加载过程中不要进行其他可能影响性能的操作
4. 加载过程中场景已被清理，避免在加载完成前进行其他场景操作

## 示例场景

假设有300个对象需要加载，设置加载时长为3秒：

- 如果帧率为60fps，则总帧数为180帧
- 每帧加载对象数量 = 300 / 180 ≈ 1.67个
- 实际每帧会加载2个对象（向上取整）
- 总加载时间约为150帧（2.5秒）

这样可以确保在3秒内完成加载，同时避免单帧负载过高。