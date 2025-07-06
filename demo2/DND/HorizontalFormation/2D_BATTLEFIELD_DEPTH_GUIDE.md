# 2D 横版游戏中的"战场深度"详解

## 🎯 核心概念

在 2D 横版游戏中，"战场深度"（Battlefield Depth）是一个重要但容易混淆的概念。本文档详细解释其含义和用途。

---

## 📐 坐标系统

### Unity 3D 坐标系在 2D 游戏中的应用

即使是 2D 游戏，Unity 仍使用 3D 坐标系：

- **X 轴**：水平方向（左右移动）
- **Y 轴**：垂直方向（上下移动，或在 2D 侧视图中控制高度）
- **Z 轴**：深度方向（前后层次，控制渲染顺序）

### 本系统的坐标约定

```
玩家方 ←—————————— 战场宽度 ——————————→ 敌人方
   |                                        |
后排|    前排              前排    |后排
   |     ↑                  ↑       |
   |  战场深度           战场深度     |
   |     ↓                  ↓       |
   +————————————————————————————————+
```

---

## 🔍 "战场深度"的实际含义

### ⚠️ 重要说明：本系统为纯 2D 设计

**本战斗系统专为纯 2D 横版游戏设计，角色不会在 Z 轴上实际移动！**

### 在纯 2D 横版游戏中

**战场深度 = 前排与后排之间的逻辑间距**

- **用途**：

  - **视觉层次**：前排角色显示在后排前面（渲染顺序）
  - **战术定位**：区分前排（坦克/近战）和后排（法师/射手）
  - **攻击优先级**：前排角色优先受到攻击
  - **碰撞检测**：优化碰撞体积和检测精度
  - **动画效果**：技能特效的层次感

- **角色移动说明**：

  - ✅ 角色可以在同一排内左右移动（X 轴）
  - ✅ 角色可以在前排后排之间切换（战术转换）
  - ❌ 角色不会在 Z 轴上连续移动
  - ❌ 没有真实的 3D 空间移动

- **典型值**：
  - `battlefieldDepth = 2`：紧凑型战斗，前后排贴近
  - `battlefieldDepth = 4`：标准间距，平衡视觉和战术
  - `battlefieldDepth = 6`：宽松布局，更明显的前后排区分

---

## ⚙️ 参数详解

### BattleFormationManager 中的关键参数

```csharp
[Tooltip("战场深度 - 前排到后排的纵向距离")]
public float battlefieldDepth = 4f;

[Tooltip("角色间的间距 - 同一排内角色之间的距离")]
public float positionSpacing = 2f;
```

### 参数关系图（纯 2D 系统）

```
    玩家后排                玩家前排           敌人前排                敌人后排
 [角色] - [角色] - [角色]   [角色] - [角色] - [角色]   [角色] - [角色] - [角色]   [角色] - [角色] - [角色]
   |         |         |     |         |         |     |         |         |     |         |         |
   |<- positionSpacing ->|     |<- positionSpacing ->|     |<- positionSpacing ->|     |<- positionSpacing ->|
   |                           |                           |                           |
   |<-------- battlefieldDepth ------->|<-------- battlefieldDepth ------->|
   |                                                                               |
   |<------------------------ battlefieldWidth -------------------------->|

注意：角色固定在指定位置，不会在Z轴上自由移动
```

---

## 🎮 实际应用示例（纯 2D 系统）

### 示例 1：经典日式 RPG 风格

```csharp
battlefieldDepth = 3f;     // 较小的前后排间距
positionSpacing = 1.5f;    // 紧凑的角色排列
```

**效果**：角色站位紧密，更注重队形整齐，适合传统回合制 RPG

### 示例 2：战术策略游戏风格

```csharp
battlefieldDepth = 6f;     // 较大的前后排间距
positionSpacing = 2.5f;    // 宽松的角色间距
```

**效果**：更明显的前后排区分，视觉层次清晰，适合战术重点的游戏

### 示例 3：动作游戏风格

```csharp
battlefieldDepth = 2f;     // 最小间距
positionSpacing = 1f;      // 密集排列
```

**效果**：快节奏战斗，角色贴近，适合连击和组合技能

### 示例 4：移动端休闲游戏

```csharp
battlefieldDepth = 4f;     // 标准间距
positionSpacing = 2f;      // 标准间距
```

**效果**：平衡的视觉效果，适合小屏幕显示

---

## 🛠️ 调试和可视化

### 在 Scene 视图中查看

1. 选中 `BattleFormationManager` 对象
2. 在 Scene 视图中可以看到自动生成的 spawn 点
3. 观察前后排之间的 Z 轴距离

### 运行时调整

```csharp
// 动态调整战场深度
BattleFormationManager.Instance.battlefieldDepth = newDepth;
BattleFormationManager.Instance.AutoGenerateSpawnPoints();
```

---

## ❓ 常见问题

### Q: 我的游戏是纯 2D 的，还需要设置深度吗？

**A**: 是的！即使是纯 2D 游戏，Z 轴深度仍然用于：

- **渲染层次**：哪个角色显示在前面（Sprite Renderer 的 Order in Layer）
- **攻击判定**：前排优先受到攻击的逻辑
- **战术定位**：区分前排（坦克）和后排（法师/射手）角色
- **视觉效果**：技能特效的层次和深度感

**注意**：角色不会在 Z 轴上自由移动，这只是位置分类和视觉排序！

### Q: 角色可以在前后排之间移动吗？

**A**: 可以进行战术转换，但不是连续移动：

- ✅ **可以**：通过技能或战术指令从前排切换到后排（瞬移到新位置）
- ✅ **可以**：在同一排内左右移动（X 轴位置调整）
- ❌ **不可以**：在 Z 轴上连续移动或自由漫游
- ❌ **不可以**：像 3D 游戏那样在深度方向走动

### Q: 如何选择合适的深度值？

**A**: 建议根据游戏风格和屏幕显示选择：

- **手机游戏**：`battlefieldDepth = 2-3`（屏幕小，需要紧凑）
- **PC 休闲游戏**：`battlefieldDepth = 4-5`（标准显示效果）
- **战术重点游戏**：`battlefieldDepth = 5-6`（强调前后排区分）
- **动作快节奏**：`battlefieldDepth = 1-2`（快速识别，减少视觉干扰）

### Q: 深度设置会影响性能吗？

**A**: 不会。这只是位置计算，不会影响渲染或计算性能。

---

## 🔗 相关文档

- [MANUAL_SETUP_GUIDE.md](./MANUAL_SETUP_GUIDE.md) - 完整设置指南
- [DEVELOPMENT_STANDARDS.md](./DEVELOPMENT_STANDARDS.md) - 开发标准
- [HorizontalFormationTypes.cs](./HorizontalFormationTypes.cs) - 位置类型定义
