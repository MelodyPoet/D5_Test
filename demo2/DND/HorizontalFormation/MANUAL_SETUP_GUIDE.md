# Unity Hierarchy 手动设置指南

## 🎯 目标

在 Unity 场景中手动创建和配置横版线性战斗系统所需的 GameObject 和组件。

## � 使用规则

**与 AI 助手对话时的统一语言规则：**

- ✅ **统一使用中文** - 所有对话、提问、回答都使用中文
- ❌ **避免使用英语** - 确保交流清晰，避免中英混用
- 💡 **技术术语保持英文** - Unity 组件名称、方法名等技术名词保持原文（如：GameObject、Transform、Inspector 等）
- 🎯 **确保理解准确** - 中文交流有助于减少误解，提高开发效率

**文档内容规则：**

- ✅ **只记录正确的操作步骤** - 文档专注于标准配置流程
- ❌ **不记录故障排除过程** - 调试、错误分析等内容不在此文档中记录
- 🎯 **保持简洁明确** - 确保用户能直接按步骤操作，避免混淆

## �📐 纯 2D 横版游戏坐标系说明

⚠️ **重要声明：本系统专为纯 2D 横版游戏设计，角色不会在 Z 轴上实际移动！**

坐标系统约定：

- **X 轴（线性布局）**：从左到右的战术排列
  - 玩家后排：X 轴最左侧（远程支援位置）
  - 玩家前排：X 轴中左（近战防守位置）
  - 敌人前排：X 轴中右（近战攻击位置）
  - 敌人后排：X 轴最右侧（远程支援位置）
- **Y 轴（上下排列）**：控制左中右的位置排列
  - 上方：Y 值较大（右侧位置）
  - 中央：Y 值为 0（中央位置）
  - 下方：Y 值较小（左侧位置）
- **Z 轴（渲染层）**：固定为 0，所有角色在同一 2D 平面

**战场布局示意图：**

```
     Y轴 ↑ (上方，右侧位置)
         |
 玩家后排 | 玩家前排 | 敌人前排 | 敌人后排
---------(0,0)-------------------------------- → X轴
 (远程)  | (近战)   | (近战)   | (远程)
         | ↓ (下方，左侧位置)
```

**战术机制支持：**

- **近战限制**：前排近战只能攻击相邻的敌方前排
- **远程削弱**：后排远程攻击对方后排时攻击力被削弱
- **位置战术**：前排保护后排，后排提供火力支援

**关键参数说明：**

- `battlefieldDepth`：控制相邻排之间的 X 轴间距
- `battlefieldWidth`：控制玩家方与敌人方的 X 轴总间距
- `positionSpacing`：控制 Y 轴上左中右的间距

---

## 📋 第一步：创建管理器对象

### 1. 创建 BattleFormationManager

```
右键Hierarchy → Create Empty
命名为: BattleFormationManager
添加脚本: BattleFormationManager.cs
```

**Inspector 配置:**

- Formation Type: `Balanced`
- All Positions: 设置 Size 为 12（稍后配置）

### 2. 创建 HorizontalBattleIntegration

```
右键Hierarchy → Create Empty
命名为: HorizontalBattleIntegration
添加脚本: HorizontalBattleIntegration.cs
```

**Inspector 配置:**

- Use Horizontal Formation: ✅ true
- Enable Cover System: ✅ true
- Enable Stealth System: ✅ true
- Formation Manager: 拖拽 BattleFormationManager 对象
- Combat Manager: 留空（自动查找）

### 3. 创建 AutoBattleAI（可选）

```
右键Hierarchy → Create Empty
命名为: AutoBattleAI
添加脚本: AutoBattleAI.cs
```

**Inspector 配置:**

- Enable Auto Battle: ✅ true
- Decision Delay: 1.0
- Show AI Thoughts: ✅ true
- Healing Priority: 0.8
- Positioning Priority: 0.6
- Offensive Priority: 0.7
- Defensive Priority: 0.5

---

## 📍 第二步：配置 Spawn 点

### 🤖 方式一：自动生成（推荐）

BattleFormationManager 默认会自动生成所有 spawn 点位置：

**优势：**

- ✅ 自动基于摄像机视野计算位置
- ✅ 考虑战场宽度和深度
- ✅ 智能间距分布
- ✅ 零配置，开箱即用

**配置参数：**

- `Auto Generate Spawn Points`: ✅ true （默认启用）
- `Battlefield Width`: 20 （水平距离：玩家方到敌人方）
- `Battlefield Depth`: 4 （纵向距离：前排到后排的间距）
- `Position Spacing`: 2 （角色间距：同排内角色之间的距离）

**📌 纯 2D 系统中的"战场深度"说明：**

在本纯 2D 横版游戏系统中，`Battlefield Depth` 控制前后排的逻辑间距：

- **用途**：仅用于视觉和战术，不涉及角色移动

  - **视觉层次**：前排角色显示在后排前面（渲染顺序）
  - **攻击优先级**：前排角色优先受到攻击
  - **战术分类**：区分前排（坦克）和后排（法师/射手）
  - **特效层次**：技能动画的深度感

- **重要限制**：

  - ❌ 角色不会在 Z 轴上实际移动
  - ❌ 不是真实的 3D 移动空间
  - ✅ 仅用于位置分类和视觉排序

- **推荐值**：
  - **手机游戏**：2-3（屏幕小，需要紧凑布局）
  - **PC 游戏**：4-5（标准显示效果）
  - **战术游戏**：5-6（强调前后排区分）

> 💡 **详细说明**：关于"战场深度"的完整解释，请参考 [2D_BATTLEFIELD_DEPTH_GUIDE.md](./2D_BATTLEFIELD_DEPTH_GUIDE.md)

**手动触发：**
如需重新生成，在 Inspector 中右键 BattleFormationManager 组件 → "Auto Generate Spawn Points"

### 🔧 方式二：手动创建（自定义需求）

如果您需要完全自定义位置，可以：

1. 在 Inspector 中设置 `Auto Generate Spawn Points` = false
2. 手动创建 spawn 点

#### 创建 PlayerSpawnPoints 父对象

```
右键Hierarchy → Create Empty
命名为: PlayerSpawnPoints
```

#### 创建 EnemySpawnPoints 父对象

```
右键Hierarchy → Create Empty
命名为: EnemySpawnPoints
```

#### 手动设置位置

| Spawn 点名称      | Transform Position | 描述       | 数组索引 |
| ----------------- | ------------------ | ---------- | -------- |
| **玩家 Spawn 点** |                    |            |          |
| PlayerSpawn_0     | (-10, 0, -1)       | 玩家后排左 | [0]      |
| PlayerSpawn_1     | (-10, 0, 0)        | 玩家后排中 | [1]      |
| PlayerSpawn_2     | (-10, 0, 1)        | 玩家后排右 | [2]      |
| PlayerSpawn_3     | (-6, 0, -1)        | 玩家前排左 | [3]      |
| PlayerSpawn_4     | (-6, 0, 0)         | 玩家前排中 | [4]      |
| PlayerSpawn_5     | (-6, 0, 1)         | 玩家前排右 | [5]      |
| **敌人 Spawn 点** |                    |            |          |
| EnemySpawn_0      | (6, 0, -1)         | 敌人前排左 | [0]      |
| EnemySpawn_1      | (6, 0, 0)          | 敌人前排中 | [1]      |
| EnemySpawn_2      | (6, 0, 1)          | 敌人前排右 | [2]      |
| EnemySpawn_3      | (10, 0, -1)        | 敌人后排左 | [3]      |
| EnemySpawn_4      | (10, 0, 0)         | 敌人后排中 | [4]      |
| EnemySpawn_5      | (10, 0, 1)         | 敌人后排右 | [5]      |

---

## 🎭 第三步：准备角色对象

### 角色必需组件

每个参与战斗的角色 GameObject 需要：

- ✅ `CharacterStats` 组件（DND5E 核心）
- 其他组件会自动添加

### 角色命名规则

为了系统能正确识别：

- **玩家角色**: 名称包含"Player"、"Hero"、"Ally"
- **敌人角色**: 名称包含"Enemy"、"Monster"、"Orc"

或者设置正确的 Tag：

- **玩家角色**: Tag 设为"Player"
- **敌人角色**: Tag 设为"Enemy"

---

## ⚙️ 第四步：配置组件引用和预制体

### 1. 配置 BattleFormationManager

选中 BattleFormationManager 对象：

#### **配置 Spawn 点数组：**

1. **Player Spawn Points** - 设置 Size 为 6，按顺序拖拽：

   - [0] PlayerSpawn_0 (后排左)
   - [1] PlayerSpawn_1 (后排中)
   - [2] PlayerSpawn_2 (后排右)
   - [3] PlayerSpawn_3 (前排左)
   - [4] PlayerSpawn_4 (前排中)
   - [5] PlayerSpawn_5 (前排右)

2. **Enemy Spawn Points** - 设置 Size 为 6，按顺序拖拽：
   - [0] EnemySpawn_0 (前排左)
   - [1] EnemySpawn_1 (前排中)
   - [2] EnemySpawn_2 (前排右)
   - [3] EnemySpawn_3 (后排左)
   - [4] EnemySpawn_4 (后排中)
   - [5] EnemySpawn_5 (后排右)

#### **配置角色预制体：**

3. **Player Character Prefabs** - 设置 Size 为 6：

   - 从 Project 窗口拖拽你的玩家角色预制体到对应槽位
   - 不想使用的位置留空即可
   - 例如：[0] Hero_Warrior, [1] Hero_Mage, [2] 留空...

4. **Enemy Character Prefabs** - 设置 Size 为 6：
   - 从 Project 窗口拖拽你的敌人角色预制体到对应槽位
   - 不想使用的位置留空即可
   - 例如：[0] Orc_Warrior, [1] Orc_Archer, [2] 留空...

**💡 灵活配置优势：**

- ✅ **支持部分配置**：只配置你需要的角色位置，其他位置留空
- ✅ 可以随时更换不同的预制体
- ✅ 支持不同的战斗阵容组合（1v1, 2v3, 任意组合）
- ✅ 空位置会被自动忽略，不影响生成

**🔧 配置要求：**

- 预制体必须包含 `CharacterStats` 组件
- 对应的 spawn 点必须存在且不为 null
- 数组索引必须对应（例如：prefabs[0] 对应 spawnPoints[0]）

### 2. 配置 HorizontalBattleIntegration

选中 HorizontalBattleIntegration 对象：

- Formation Manager: 拖拽 BattleFormationManager 对象

---

## 🎮 第五步：启动战斗

### 方法一：通过预制体自动生成（推荐）

```csharp
// 获取阵型管理器
BattleFormationManager formationManager = FindObjectOfType<BattleFormationManager>();

// 直接初始化战斗（会自动根据预制体生成角色）
formationManager.InitializeBattle();
```

### 方法二：使用现有角色列表

```csharp
// 获取阵型管理器
BattleFormationManager formationManager = FindObjectOfType<BattleFormationManager>();

// 准备现有角色列表
List<CharacterStats> playerTeam = new List<CharacterStats>();
List<CharacterStats> enemyTeam = new List<CharacterStats>();
// ... 添加角色到列表 ...

// 使用现有角色初始化战斗
formationManager.InitializeBattleWithExistingCharacters(playerTeam, enemyTeam);
```

### 方法三：通过 HorizontalBattleIntegration 启动

```csharp
// 获取集成系统
HorizontalBattleIntegration battleSystem = FindObjectOfType<HorizontalBattleIntegration>();

// 启动横版战斗（新的无参数版本）
battleSystem.StartHorizontalBattle();
```

### 通过 Inspector 一键启动

1. 选中 BattleFormationManager 对象
2. 在 Scene 视图中点击播放按钮
3. 在 Inspector 中调用对应的方法
4. 角色会自动在对应的 spawn 点生成或移动

**🎯 新系统优势：**

- ✅ 无需手动准备角色列表
- ✅ 直接通过预制体自动生成
- ✅ 支持灵活的阵容配置
- ✅ 空位置自动跳过

---

## 📊 完整 Hierarchy 结构示例

### 🤖 自动生成模式（推荐）

```
Hierarchy
├── Main Camera
├── Directional Light
├── BattleFormationManager (BattleFormationManager.cs)
│   ├── PlayerSpawnPoints
│   │   ├── PlayerSpawn_0_BackLeft (Position: 自动计算)
│   │   ├── PlayerSpawn_1_BackCenter (Position: 自动计算)
│   │   ├── PlayerSpawn_2_BackRight (Position: 自动计算)
│   │   ├── PlayerSpawn_3_FrontLeft (Position: 自动计算)
│   │   ├── PlayerSpawn_4_FrontCenter (Position: 自动计算)
│   │   └── PlayerSpawn_5_FrontRight (Position: 自动计算)
│   └── EnemySpawnPoints
│       ├── EnemySpawn_0_FrontLeft (Position: 自动计算)
│       ├── EnemySpawn_1_FrontCenter (Position: 自动计算)
│       ├── EnemySpawn_2_FrontRight (Position: 自动计算)
│       ├── EnemySpawn_3_BackLeft (Position: 自动计算)
│       ├── EnemySpawn_4_BackCenter (Position: 自动计算)
│       └── EnemySpawn_5_BackRight (Position: 自动计算)
├── HorizontalBattleIntegration (HorizontalBattleIntegration.cs)
├── AutoBattleAI (AutoBattleAI.cs) [可选]
└── [运行时生成的角色会自动出现在这里]
```

### 🔧 手动创建模式

```
Hierarchy
├── Main Camera
├── Directional Light
├── BattleFormationManager (BattleFormationManager.cs)
├── HorizontalBattleIntegration (HorizontalBattleIntegration.cs)
├── AutoBattleAI (AutoBattleAI.cs) [可选]
├── PlayerSpawnPoints
│   ├── PlayerSpawn_0 (Position: -10, 0, -1) [后排左]
│   ├── PlayerSpawn_1 (Position: -10, 0, 0) [后排中]
│   ├── PlayerSpawn_2 (Position: -10, 0, 1) [后排右]
│   ├── PlayerSpawn_3 (Position: -6, 0, -1) [前排左]
│   ├── PlayerSpawn_4 (Position: -6, 0, 0) [前排中]
│   └── PlayerSpawn_5 (Position: -6, 0, 1) [前排右]
├── EnemySpawnPoints
│   ├── EnemySpawn_0 (Position: 6, 0, -1) [前排左]
│   ├── EnemySpawn_1 (Position: 6, 0, 0) [前排中]
│   ├── EnemySpawn_2 (Position: 6, 0, 1) [前排右]
│   ├── EnemySpawn_3 (Position: 10, 0, -1) [后排左]
│   ├── EnemySpawn_4 (Position: 10, 0, 0) [后排中]
│   └── EnemySpawn_5 (Position: 10, 0, 1) [后排右]
└── [运行时生成的角色会自动出现在这里]
```

---

## ✅ 验证设置

### 基本验证

运行游戏后，Console 应该显示：

```
=== 开始初始化战斗 ===
开始生成 Player 方角色...
✅ [角色名] 成功生成到位置 PlayerBackLeft
开始生成 Enemy 方角色...
✅ [角色名] 成功生成到位置 EnemyFrontCenter
=== 战斗初始化完成 ===
```

角色应该根据你配置的预制体自动生成到对应的 spawn 点位置。

### 角色可见性诊断

**如果角色在 Scene 视图可见但 Game 视图不可见，请按以下步骤诊断：**

#### 🔧 步骤 1：一键修复（推荐）

- 选中 BattleFormationManager
- 右键组件 → "Force Characters Visible"
- 此方法会自动修复常见的可见性问题

#### 📷 步骤 2：检查摄像机设置

确认 Main Camera 的设置：

- **Projection**: Orthographic
- **Size**: 8 或更大
- **Position**: (0, 0, -10)
- **Culling Mask**: Everything
- **Clear Flags**: Solid Color

#### 🎭 步骤 3：检查角色设置

- 角色的 **Layer** 为 Default 或被摄像机 Culling Mask 包含
- 角色位置的 **Z 坐标** 为 0（2D 游戏标准）
- 角色的 **Renderer** 已启用
- 角色的 **Materials** 不为空

#### ⚙️ 步骤 4：使用调试工具

- 右键 BattleFormationManager → "Adjust Camera View"（自动调整摄像机）
- 右键 BattleFormationManager → "Debug Configuration"（查看配置信息）
- 右键 BattleFormationManager → "Debug Initialize Battle"（详细初始化日志）

#### 📋 步骤 5：手动检查清单

如果以上方法仍无效，请检查：

1. **摄像机视野范围**

   - 正交尺寸是否足够大以包含角色位置
   - 摄像机位置是否正确

2. **角色渲染设置**

   - Renderer 组件是否存在且启用
   - Material 是否正确分配
   - Layer 是否在摄像机的 CullingMask 中

3. **位置关系**
   - 角色 Z 位置是否在摄像机的近远裁剪平面之间
   - 角色是否在摄像机的视锥体内

**💡 常见问题解决方案：**

- 摄像机正交尺寸太小 → 增大 Orthographic Size
- 角色 Layer 错误 → 设置为 Default 层
- 角色 Z 位置错误 → 设置为 0
- 摄像机位置错误 → 设置为 (0, 0, -10)

---

## 🎯 关键要点

1. **Spawn 点灵活配置** - 只创建你需要的 spawn 点
2. **预制体拖拽设置** - 直接拖拽预制体到 Inspector 数组中
3. **空位置自动忽略** - 不使用的位置留空即可
4. **自动生成角色** - 无需手动准备角色列表
5. **支持任意阵容** - 1v1, 3v3, 6v6 都支持

**💡 新系统的人性化优势：**

- 🎮 **用户友好** - 拖拽预制体比填写复杂数组简单
- 🔄 **灵活配置** - 随时更换不同的角色组合
- ⚡ **快速测试** - 一键初始化，无需复杂代码
- 🎯 **自由设计** - 玩家可以完全自定义站位和阵容

**📋 配置要求总结：**

1. **必需配置**：BattleFormationManager 的所有数组（预制体和 spawn 点）
2. **预制体要求**：必须包含 CharacterStats 组件
3. **数组对应**：保持索引对应关系（prefabs[0] ↔ spawnPoints[0]）
4. **配置顺序**：先配置所有数组，再运行游戏测试

这样设置后，你的横版战斗系统就变得非常灵活和用户友好了！
