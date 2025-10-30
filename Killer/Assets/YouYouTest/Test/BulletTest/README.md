# 动态子弹移动系统

这个系统实现了子弹从A点移动到B点的功能，特别之处在于A点和B点的位置可以在移动过程中动态变化，而子弹会始终保持在两个点的直接相连的直线上。

## 核心特性

### 1. 动态追踪算法
- 实时检测A点和B点的位置变化
- 自动重新计算路径和距离
- 确保子弹始终在A点和B点的直线上移动

### 2. 智能路径重计算
- 当A点变化时：调整子弹位置并更新路径
- 当B点变化时：重新计算目标距离
- 当两点都变化时：按比例调整已移动距离

### 3. 可视化调试
- Scene视图中显示A点、B点和子弹位置
- 实时绘制连接线
- 可自定义调试颜色

## 使用方法

### 基本设置
1. 将 `BulletMoveTest.cs` 脚本附加到子弹游戏对象上
2. 在Inspector中设置 `pointA` 和 `pointB` 的Transform引用
3. 调整 `speed` 参数控制子弹移动速度
4. 启用 `useDynamicTracking` 来使用动态追踪功能

### 测试控制器
1. 将 `BulletMoveTestController.cs` 脚本附加到场景中的空游戏对象上
2. 设置 `bulletTest`、`pointA` 和 `pointB` 的引用
3. 调整移动参数来测试不同的动态场景

### 控制说明
- **空格键** - 手动发射子弹
- **M键** - 切换A点和B点的移动状态
- **F键** - 切换自动发射
- **R键** - 重置所有位置

## 技术实现

### 核心算法
```csharp
// 动态追踪移动的核心逻辑
private void UpdateDynamicMovement()
{
    // 检测A点和B点位置变化
    bool pointAChanged = !pointA.position.Equals(lastPointAPosition);
    bool pointBChanged = !pointB.position.Equals(lastPointBPosition);
    
    if (pointAChanged || pointBChanged)
    {
        RecalculatePath(pointAChanged, pointBChanged);
    }
    
    // 计算当前应该在直线上的位置
    Vector3 currentTargetPosition = CalculatePositionOnLine(traveledDistance);
    
    // 移动子弹
    Vector3 direction = (currentTargetPosition - transform.position).normalized;
    transform.position += direction * speed * Time.deltaTime;
    
    traveledDistance += speed * Time.deltaTime;
}
```

### 路径重计算
系统会根据不同的变化情况采用不同的重计算策略：
- **A点变化**：调整子弹位置，保持移动进度
- **B点变化**：重新计算总距离
- **两点都变化**：按比例调整所有参数

## 应用场景

这个系统适用于以下情况：
- 追踪移动目标的导弹
- 连接两个移动物体的能量束
- 动态路径的投射物
- 需要实时调整轨迹的子弹系统

## 参数说明

### BulletMoveTest 参数
- `speed`: 子弹移动速度
- `useDynamicTracking`: 是否启用动态追踪
- `pointA`: 起始点Transform
- `pointB`: 目标点Transform
- `showDebugLine`: 是否显示调试线
- `debugLineColor`: 调试线颜色

### BulletMoveTestController 参数
- `moveSpeed`: A点和B点的移动速度
- `moveRadius`: 移动半径
- `enableMovement`: 是否启用移动
- `autoFire`: 是否自动发射
- `fireInterval`: 自动发射间隔

## 扩展建议

1. **添加曲线运动**：可以扩展为支持贝塞尔曲线等复杂路径
2. **多目标追踪**：支持多个目标点的动态切换
3. **物理集成**：与Rigidbody和物理系统结合
4. **网络同步**：添加网络同步支持用于多人游戏