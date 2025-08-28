# DND5E 系统 - 工程编码规范

## 📋 核心开发原则

### 🔒 **文档保护和管理规范**

- **绝对禁止删除本文档**：DEVELOPMENT_STANDARDS.md 是项目核心规范，严禁删除或移动
- **唯一文档原则**：所有业务逻辑规则、Unity 配置说明、手动设置指南均记录在本文档中
- **禁止创建其他文档**：不允许创建任何其他.md 技术### 🛠️ **Unity 手动挂载设置指南**

## ⚙️ **核心组件配置原则**

### 🎯 **预制体配置优先原则**

**📋 预制体必备组件配置规则：**

- **强制要求**：所有角色预制体必须预先配置好 `DND_CharacterAdapter` 和 `SkeletonAnimation` 组件
- **禁止硬编码添加**：代码中严禁通过 `AddComponent<>()` 方式添加任何组件
- **配置读取模式**：所有属性和引用通过预制体配置读取，保持灵活可调整性
- **错误处理原则**：如果预制体缺少必需组件，系统报错并拒绝创建角色，而不是自动添加组件

**🔧 角色预制体标准配置清单：**

```csharp
✅ 必需组件：
- CharacterStats           // 角色属性数据组件
- DND_CharacterAdapter     // 统一动画管理组件
- SkeletonAnimation        // Spine动画播放组件（来自Spine插件）

✅ DND_CharacterAdapter配置：
- characterStats: 自动获取同对象上的CharacterStats组件
- skeletonAnimation: 自动获取同对象上的SkeletonAnimation组件
- animationMapping: 在Inspector中配置角色专属的动画名映射

✅ SkeletonAnimation配置：
- Skeleton Data Asset: 必须配置角色的.asset骨骼数据文件
- Animation Name: 设置默认播放的动画名称
- Loop: 根据动画类型设置是否循环
- 其他Spine相关参数按角色需求在预制体中预设
```

**🚨 硬编码检查与修复规则：**

- **检查标准**：代码中不得出现 `gameObject.AddComponent<DND_CharacterAdapter>()` 等组件添加语句
- **报错处理**：当检测到预制体缺少组件时，必须抛出明确错误信息指导用户修复预制体配置
- **灵活配置**：所有动画名称、组件引用、参数设置均通过预制体 Inspector 面板配置
- **开发便利性**：通过预制体配置可以为不同角色设置不同的动画映射，无需修改代码

### 📁 **标准 Hierarchy 结构**设置指南

- **文档集中管理**：本文档作为项目唯一技术标准和操作手册

### 📝 **文档内容管理规范**

- **禁止记录临时内容**：不得在文档中记录逻辑更新过程、debug 调试记录等临时性内容
- **禁止过程日志**：不得记录开发过程中的修改历史、错误排查过程等流水账
- **仅记录核心规范**：只记录核心开发原则、配置标准、架构规范等永久性规则
- **保持文档简洁**：文档应聚焦于当前有效的技术标准，避免冗余信息

---

## 🎯 **开发行为规范**

### 🔧 **功能实现设计规范**

- **单一设计原则**：实现功能时只提供一种设计方案，不提供多套方案供选择
- **禁止方案选择**：严禁设计 2 套或多套方案让用户选择实现方式
- **直接实现**：基于最佳实践直接提供最优方案，一次性完成实现
- **迭代改进**：如果方案不够好，用户会明确要求设计新方案，此时才进行重新设计

### 🚫 **Git 操作限制规范**

- **禁止自动推送**：严禁自动执行 `git push` 操作到 GitHub 仓库
- **禁止主动推送**：不得主动进行代码推送到远程仓库
- **仅限本地操作**：只允许执行 `git add`、`git commit` 等本地 Git 操作
- **异常处理**：只有当用户手动推送遇到问题时，才协助解决推送相关技术问题

### 🗣️ **对话语言规范**

- **强制要求**：与用户的所有对话必须使用**中文**
- **代码注释**：所有代码注释必须使用中文
- **变量命名**：使用英文命名规范（驼峰命名法等）
- **文档内容**：所有.md 文档内容使用中文
- **严禁行为**：因为用户点击英文按钮选项而切换成英文回复
- **严禁行为**：在任何情况下使用英文与用户交流（除非是代码本身的关键字）

---

## ⚠️ **严格禁止项**

### 🚨 **禁止创建的文件类型**

```
❌ XXXTester.cs         - 测试脚本
❌ XXXDemo.cs           - 演示脚本
❌ XXXSetupWizard.cs    - 设置向导
❌ XXXChecker.cs        - 检查器
❌ XXXValidator.cs      - 验证器
❌ QuickSetupXXX.cs     - 快速设置
❌ AutoXXX.cs           - 自动化工具
❌ XXXTestManager.cs    - 测试管理器
❌ XXX_Fixed.cs         - 修复版本文件
❌ XXX_Backup.cs        - 备份文件
❌ XXX_Old.cs           - 旧版本文件
❌ 空文件或仅包含注释的文件
```

### 🛠️ **代码质量要求**

- **强制错误清理**：严禁在 IDE 的 PROBLEMS 面板有任何报错的情况下声称功能完成
- **全项目编译检查**：必须解决项目中所有脚本的编译错误，不论是否与当前任务直接相关
- **配置方法单一化**：每个配置项仅提供一种最直接、最可靠的配置方法
- **弃用代码强制删除**：严禁保留任何标记为[System.Obsolete]的方法、类或属性

---

## 🎮 **游戏系统架构**

### 🏗️ **横版 2D DND5E 战斗系统**

本项目是一个 Unity 横版 2D 战斗游戏，实现了完整的 DND5E 规则系统和自动挂机模式。

#### **核心游戏模式**

- **挂机模式**: 全自动探索、遭遇、战斗循环
- **横版布局**: 玩家在左侧，敌人在右侧的经典对战布局
- **DND5E 规则**: 完整的角色属性、技能、法术系统
- **动画系统**: 基于 Spine 的角色动画，支持走路、空闲、攻击等状态

#### **🎮 核心玩法逻辑规则**

##### **探索与遇敌机制**

- **探索状态**: 玩家队伍播放走路动画，背景水平滚动，模拟前进探索
- **遇敌触发**: 基于时间间隔随机触发敌人遭遇事件
- **敌人生成**: 敌人队伍在右侧阵型位置生成，与玩家阵型形成镜像对称
- **进场动画**: 敌人从屏幕右侧外进入，滑动到预定阵型位置

##### **阵型镜像布局规则**

- **玩家阵型(左侧)**: 固定在屏幕左侧，6 个位置分前后排、左中右翼
- **敌人阵型(右侧)**: 与玩家阵型完全镜像对称，同样 6 个对应位置
- **位置对应关系**:
  ```
  玩家前排左翼 ←→ 敌人前排右翼
  玩家前排中锋 ←→ 敌人前排中锋
  玩家前排右翼 ←→ 敌人前排左翼
  玩家后排左翼 ←→ 敌人后排右翼
  玩家后排中路 ←→ 敌人后排中路
  玩家后排右翼 ←→ 敌人后排左翼
  ```
- **镜像原则**: 敌人阵型不得重新排列到屏幕中央，必须保持与玩家的对称布局

##### **战斗状态转换规则**

- **遇敌前**: 玩家走路动画 + 背景滚动
- **敌人进场**:
  - 玩家立即切换到待机动画
  - 背景停止滚动
  - 敌人播放走路动画进场
- **阵型就位**:
  - 敌人到达阵型位置后切换到待机动画
  - 双方都保持待机状态准备战斗
- **战斗进行**: 双方都播放待机动画，背景保持静止
- **战斗结束**:
  - 玩家恢复走路动画
  - 背景恢复滚动
  - 继续探索循环

##### **动画状态管理规则**

- **探索期间**: 只有玩家播放走路动画，敌人不存在
- **敌人进场**: 敌人播放走路动画，玩家切换到待机
- **战斗期间**: 双方都必须播放待机动画，严禁走路动画
- **战斗结束**: 玩家恢复走路动画，敌人被清理
- **动画冲突处理**: 使用 `isInBattle` 标志防止探索循环覆盖战斗动画状态

##### **自动战斗系统规则**

###### **先攻与回合制机制**

- **先攻判定**: 双方阵型就位后，按照 DND5E 标准规则进行先攻检定决定行动顺序
- **回合时长**: 遵循 DND5E 规则，每回合代表 6 秒的游戏时间
- **行动顺序**: 按先攻值从高到低依次执行角色行动
- **回合行动**: 每个角色在自己的回合可执行攻击、施法、喝药等标准行动和移动

###### **职业战斗行为模式**

- **近战职业行为规则**:

  - 从自己阵型位置出发，走向目标敌人位置
  - 移动到敌人面前半个身位距离进行攻击
  - 攻击完成后立即返回自己的原始阵型位置
  - **禁止跨位攻击**: 不能越过敌方前排攻击后排
  - **直接面对原则**: 只有当直接面对敌方后排时才能攻击后排目标

- **远程职业行为规则**:
  - 始终保持在自己的阵型位置，不进行移动
  - 在原位进行施法或远程攻击
  - **允许跨位攻击**: 可以越过敌方前排攻击后排目标
  - 具备全战场攻击能力，不受位置限制

###### **战斗动画与移动规则**

- **近战攻击流程**:

  1. 播放走路动画移动到敌人面前
  2. 到达位置后切换到攻击动画
  3. 攻击完成后播放走路动画返回原位
  4. 返回原位后恢复待机动画

- **远程攻击流程**:

  1. 保持在原位，切换到攻击/施法动画
  2. 攻击/施法完成后立即恢复待机动画
  3. 整个过程不涉及位置移动

- **目标选择优先级**:
  - 近战职业优先攻击距离最近的可达敌人
  - 远程职业按威胁度或血量选择最优目标
  - 遵循 DND5E 标准的攻击距离和视线规则

#### **核心系统组件（强关联架构）**

```
✅ IdleGameManager.cs                    - 挂机游戏主管理器（核心）
✅ HorizontalBattleFormationManager.cs  - 横版阵型管理器（核心）
✅ AutoBattleAI.cs                      - 自动战斗AI系统（核心）
✅ CharacterStats.cs                    - 角色属性系统
✅ Role.cs                              - 角色动画控制组件
```

**强关联关系**:

- IdleGameManager 必须引用 HorizontalBattleFormationManager
- IdleGameManager 必须引用 AutoBattleAI
- 三个核心组件必须在同一场景中手动挂载
- 不使用任何自动创建或查找逻辑

---

## 🛠️ **Unity 手动挂载设置指南**

### 📁 **标准 Hierarchy 结构**

```
Hierarchy结构:
├── IdleGameSystem (空GameObject)
│   ├── IdleGameManager (手动添加IdleGameManager脚本)
│   ├── HorizontalBattleFormationManager (手动添加HorizontalBattleFormationManager脚本)
│   └── AutoBattleAI (手动添加AutoBattleAI脚本)
└── Environment (空GameObject，静态背景组织容器)
    ├── Background_Layer1 (SpriteRenderer + ScrollLayer，远景背景)
    ├── Background_Layer2 (SpriteRenderer + ScrollLayer，中景背景)
    └── Background_Layer3 (SpriteRenderer + ScrollLayer，近景背景)
```

**⚠️ 背景滚动配置规则:**

- **✅ 多层视差背景**: 每层背景使用独立的 GameObject，配置不同滚动速度
- **✅ ScrollLayer 组件**: 每个背景层必须添加 ScrollLayer 脚本
- **✅ SpriteRenderer 兼容**: 自动检测 SpriteRenderer 并使用适配的滚动方式
- **✅ 自动启动**: 游戏开始时自动启动背景滚动，无需手动触发

**⚠️ 角色容器简化规则:**

- **❌ 不需要手动创建 Characters 容器**: HorizontalBattleFormationManager 会自动管理角色实例化
- **❌ 不需要 PlayerParty/EnemyParty 容器**: 角色直接实例化到场景根节点，通过位置坐标管理
- **✅ 直接使用预制体配置**: 在 HorizontalBattleFormationManager 的 Inspector 中配置预制体即可
- **✅ 自动 spawn 点管理**: 系统自动生成和管理角色 spawn 位置，无需手动容器

### ⚙️ **组件配置设置**

#### **1. IdleGameManager 配置**

```csharp
[挂机模式设置]
✅ idleModeEnabled = false (启动时自动开启探索模式)
✅ encounterInterval = 5.0f (遭遇间隔时间，5秒便于测试)
✅ battleSpeed = 1.0f (战斗速度倍率)

[探索设置]
✅ currentStage = 1 (当前关卡)
✅ currentWave = 1 (当前波次)
✅ stageProgressPercent = 0f (关卡进度百分比)

[队伍生成设置]
✅ useFormationManager = true (使用阵型管理器生成队伍)
✅ playerPartySize = 3 (玩家队伍人数上限，推荐1-6人)

[战斗状态管理]
✅ isInBattle = false (战斗状态标志，控制动画切换)

[系统组件 - 强制手动引用]
✅ formationManager: 手动拖入同场景中的HorizontalBattleFormationManager组件
✅ autoBattleAI: 手动拖入同场景中的AutoBattleAI组件
```

**⚠️ 核心玩法实现规则:**

- **阵型位置保持**: 敌人生成后不得调用 `ArrangeExistingTeam` 重新排列位置
- **动画状态控制**: 必须使用 `isInBattle` 标志防止探索循环覆盖战斗动画
- **镜像布局维护**: 敌人进场动画必须保存并恢复原始阵型位置，不得移动到屏幕中央
- **状态转换时机**:
  - 遇敌时立即设置 `isInBattle = true`
  - 敌人进场完成后玩家切换到待机动画
  - 战斗结束后设置 `isInBattle = false` 并恢复玩家走路动画

#### **2. HorizontalBattleFormationManager 配置**

```csharp
[🔵 玩家阵型配置 (左侧)]
前排:
✅ 玩家前排左翼: 拖入玩家前排左翼角色预制体
✅ 玩家前排中锋: 拖入玩家前排中锋角色预制体
✅ 玩家前排右翼: 拖入玩家前排右翼角色预制体

后排:
✅ 玩家后排左翼: 拖入玩家后排左翼角色预制体
✅ 玩家后排中路: 拖入玩家后排中路角色预制体
✅ 玩家后排右翼: 拖入玩家后排右翼角色预制体

[🔴 敌人阵型配置 (右侧)]
前排:
✅ 敌人前排左翼: 拖入敌人前排左翼角色预制体
✅ 敌人前排中锋: 拖入敌人前排中锋角色预制体
✅ 敌人前排右翼: 拖入敌人前排右翼角色预制体

后排:
✅ 敌人后排左翼: 拖入敌人后排左翼角色预制体
✅ 敌人后排中路: 拖入敌人后排中路角色预制体
✅ 敌人后排右翼: 拖入敌人后排右翼角色预制体

[⚙️ 阵型参数设置]
✅ battlefieldWidth = 20f
✅ battlefieldDepth = 4f
✅ positionSpacing = 2f
```

#### **3. AutoBattleAI 配置**

```csharp
[基础战斗设置]
✅ enableAutoBattle = true
✅ attackDamage = 15
✅ attackDelay = 1.0f

[DND5E 回合制设置]
✅ roundDuration = 6.0f (每回合6秒，遵循DND5E标准)
✅ initiativeRollEnabled = true (启用先攻检定)
✅ turnOrderByInitiative = true (按先攻值排序行动顺序)

[职业行为模式设置]
✅ meleeMovementEnabled = true (近战职业移动攻击)
✅ rangedStaticAttack = true (远程职业原地攻击)
✅ crossPositionAttackForRanged = true (远程可跨位攻击)
✅ meleePositionRestriction = true (近战禁止跨位攻击)
```

**⚠️ 自动战斗系统实现规则:**

- **先攻检定实现**: 必须在战斗开始时为所有角色进行先攻检定，按 DND5E 规则计算
- **回合制执行**: 严格按照 6 秒/回合的时间标准和先攻顺序执行角色行动
- **近战移动逻辑**:
  - 必须实现角色从原位移动到目标位置的路径计算
  - 攻击距离控制在半个身位以内
  - 攻击完成后必须返回原始阵型位置
  - 禁止跨越敌方前排攻击后排的逻辑检查
- **远程攻击逻辑**:
  - 确保远程角色始终保持在原位不移动
  - 实现跨位攻击的目标选择算法
  - 支持全战场范围的目标选择
- **动画状态同步**: 战斗行动必须与对应的动画状态(攻击、移动、待机)正确同步

#### **4. ScrollLayer 背景滚动配置**

```csharp
[滚动模式选择]
✅ scrollMode = AutoDetect (自动检测最佳滚动方式)

[滚动参数]
✅ scrollSpeed = 2f (远景层)
✅ scrollSpeed = 4f (中景层)
✅ scrollSpeed = 6f (近景层)
✅ scrollDirection = 1 (向右滚动，模拟前进)

[视差效果配置]
✅ 远景背景: scrollSpeed = 1-2f (移动最慢)
✅ 中景背景: scrollSpeed = 3-4f (中等速度)
✅ 近景背景: scrollSpeed = 5-6f (移动最快)
```

**⚠️ 背景滚动与战斗状态联动规则:**

- **探索时自动滚动**: 游戏开始时背景自动启动滚动，配合玩家走路动画
- **遇敌时停止滚动**: 敌人出现时调用 `StopBackgroundScrolling()` 停止所有背景层滚动
- **战斗结束恢复滚动**: 战斗完成后调用 `ResumeBackgroundScrolling()` 恢复背景滚动
- **滚动状态监控**: 使用 `EnsureBackgroundScrolling()` 在探索循环中持续监控滚动状态
- **UV 滚动兼容**: 系统自动检测 SpriteRenderer 材质，如果 UV 滚动失效则自动切换到 Transform 移动模式

### 🎭 **动画系统管理**

#### **DND_CharacterAdapter 动画系统**

**统一动画管理器**：所有角色（玩家和敌人）必须使用 `DND_CharacterAdapter` 组件

```csharp
// 动画映射配置
public class AnimationMapping {
    public string idleAnimation = "idle";           // 待机动画
    public string walkAnimation = "walk";           // 走路动画
    public string moveToIdleAnimation = "m_to_i";   // 移动到待机过渡
    public string attackAnimation = "attack";       // 攻击动画
    public string hitAnimation = "hit";             // 受击动画
    public string deathAnimation = "death";         // 死亡动画
    public string castAnimation = "cast";           // 施法动画
}
```

**核心动画方法**：

```csharp
// 基础动画播放
adapter.PlayAnimation(animationName, loop);

// 专用动画方法
adapter.PlayWalkAnimation();          // 走路动画
adapter.StopWalkWithTransition();     // 停止走路+过渡到待机
adapter.PlayAttackAnimation();        // 攻击动画
adapter.PlayHitAnimation();           // 受击动画
adapter.PlayDeathAnimation();         // 死亡动画
adapter.PlayCastAnimation();          // 施法动画
```

#### **战斗动画控制规则**

**近战职业动画序列:**

```csharp
// 近战攻击完整序列
1. adapter.PlayWalkAnimation();              // 移动到敌人位置
2. adapter.PlayAttackAnimation();            // 执行攻击动画
3. adapter.PlayWalkAnimation();              // 返回原位
4. adapter.StopWalkWithTransition();         // 恢复待机状态
```

**远程职业动画序列:**

```csharp
// 远程攻击完整序列
1. adapter.PlayCastAnimation();              // 原地攻击/施法动画
2. // 自动返回待机状态（通过协程）
```

**战斗状态动画管理:**

- **待机状态**: 战斗中默认状态，所有角色在非行动回合保持待机动画
- **行动状态**: 角色回合时根据职业类型播放对应的行动动画序列
- **受击状态**: 被攻击时播放受击动画，然后恢复待机状态
- **死亡状态**: 角色死亡时播放死亡动画并保持不变
- **动画优先级**: 受击 > 攻击 > 移动 > 待机，高优先级动画可以打断低优先级动画

### 📐 **横版 2D 布局规范**

#### **坐标系统说明**

- **X 轴**: 前后排纵深方向 (负值=玩家方后排，正值=敌人方后排)
- **Y 轴**: 左中右翼分布 (正值=左翼，0=中路，负值=右翼)
- **Z 轴**: 固定为 0 (2D 游戏)

#### **标准阵型位置**

**玩家方(左侧)坐标:**

```
前排左翼: (-1.0, +2.0, 0)  // 前排，左翼 (更接近敌人)
前排中锋: (-1.0, 0.0, 0)   // 前排，中路
前排右翼: (-1.0, -2.0, 0)  // 前排，右翼

后排左翼: (-2.0, +2.0, 0)  // 后排，左翼 (远离敌人)
后排中路: (-2.0, 0.0, 0)   // 后排，中路
后排右翼: (-2.0, -2.0, 0)  // 后排，右翼
```

**敌人方(右侧)坐标:**

```
前排左翼: (+1.0, +2.0, 0)  // 前排，左翼 (更接近玩家)
前排中锋: (+1.0, 0.0, 0)   // 前排，中路
前排右翼: (+1.0, -2.0, 0)  // 前排，右翼

后排左翼: (+2.0, +2.0, 0)  // 后排，左翼 (远离玩家)
后排中路: (+2.0, 0.0, 0)   // 后排，中路
后排右翼: (+2.0, -2.0, 0)  // 后排，右翼
```

### 🎯 **手动 Spawn 点配置标准流程**

#### **Unity 层级视图中创建 Spawn 点**

1. **创建阵型管理器父对象**

   - 在 Hierarchy 中创建空 GameObject，命名为`FormationManager`
   - 添加`HorizontalBattleFormationManager`组件

2. **创建玩家方 Spawn 点** (在 FormationManager 下)

   ```
   └── FormationManager
       ├── PlayerSpawnPoints
       │   ├── PlayerFrontLeftSpawn     // 玩家前排左翼位置
       │   ├── PlayerFrontCenterSpawn   // 玩家前排中锋位置
       │   ├── PlayerFrontRightSpawn    // 玩家前排右翼位置
       │   ├── PlayerBackLeftSpawn      // 玩家后排左翼位置
       │   ├── PlayerBackCenterSpawn    // 玩家后排中路位置
       │   └── PlayerBackRightSpawn     // 玩家后排右翼位置
   ```

3. **创建敌人方 Spawn 点** (在 FormationManager 下)
   ```
   └── FormationManager
       └── EnemySpawnPoints
           ├── EnemyFrontLeftSpawn      // 敌人前排左翼位置
           ├── EnemyFrontCenterSpawn    // 敌人前排中锋位置
           ├── EnemyFrontRightSpawn     // 敌人前排右翼位置
           ├── EnemyBackLeftSpawn       // 敌人后排左翼位置
           ├── EnemyBackCenterSpawn     // 敌人后排中路位置
           └── EnemyBackRightSpawn      // 敌人后排右翼位置
   ```

#### **Inspector 中配置 Spawn 点引用**

1. **选中 FormationManager 对象**
2. **在 HorizontalBattleFormationManager 组件中**:
   - 先配置角色预制体 (玩家前排左翼、玩家前排中锋等 12 个位置)
   - 然后在"🎯 Spawn 点位置配置"区域拖拽对应的 Transform:
     - `playerFrontLeftSpawn` → 拖入 PlayerFrontLeftSpawn 对象
     - `playerFrontCenterSpawn` → 拖入 PlayerFrontCenterSpawn 对象
     - 依此类推，配置全部 12 个 spawn 点

#### **手动调整 Spawn 点位置**

1. **Scene 视图调整**: 在 Scene 视图中直接拖拽每个 spawn 点 GameObject 到期望位置
2. **Transform 组件调整**: 在 Inspector 中精确设置每个 spawn 点的 Transform 坐标
3. **镜像对称原则**: 确保玩家方和敌人方的 spawn 点呈镜像对称布局
4. **阵型层次**: 前排更接近战场中心，后排远离战场中心
5. **实时预览**: 调整位置时可在 Scene 视图实时预览阵型效果

#### **验证配置完整性**

1. **右键组件菜单**: 选择"验证 Spawn 点配置"
2. **查看 Console 输出**: 确保显示"✅ 所有 spawn 点配置完整"
3. **强制要求**: 所有 12 个 spawn 点必须完整配置，缺一不可

#### **配置标准与限制**

- **禁止自动计算**: 移除所有位置自动计算逻辑，只使用手动配置的 Transform 位置
- **唯一配置路径**: 只提供手动配置一种方式，不支持自动生成回退
- **强制完整性**: 未配置完整时系统将报错并拒绝工作
- **精确位置控制**: 每个 spawn 点位置可在 Scene 视图中精确调整
- **层级组织**: 建议在 FormationManager 下用 PlayerSpawnPoints 和 EnemySpawnPoints 分组管理

---

## 🚨 **违规检查清单**

### **功能完成前必须检查**

- [ ] 当前修改的脚本是否有编译错误？
- [ ] 整个项目的 PROBLEMS 面板是否显示 0 个错误？
- [ ] 是否存在跨脚本的引用错误或依赖问题？
- [ ] 所有相关脚本是否都能正常编译？
- [ ] 是否检查了命名冲突和类型定义问题？

### **核心玩法逻辑检查**

- [ ] 敌人生成后是否保持与玩家阵型的镜像对称布局？
- [ ] 是否避免了敌人进场后重新排列到屏幕中央？
- [ ] 战斗开始时双方是否都切换到待机动画状态？
- [ ] 探索时是否只有玩家播放走路动画？
- [ ] 敌人进场动画是否正确保存并恢复原始阵型位置？
- [ ] 背景滚动是否与战斗状态正确联动（探索时滚动，战斗时停止）？
- [ ] 是否使用 `isInBattle` 标志正确控制动画状态转换？
- [ ] 战斗结束后玩家是否正确恢复走路动画和背景滚动？

### **自动战斗系统检查**

- [ ] 是否在战斗开始时进行先攻检定并按 DND5E 规则排序？
- [ ] 每回合是否严格按照 6 秒时长执行？
- [ ] 近战职业是否正确实现移动到敌人面前攻击后返回原位？
- [ ] 远程职业是否始终保持在原阵型位置进行攻击？
- [ ] 是否正确实现近战职业的跨位攻击限制（不能越过前排攻击后排）？
- [ ] 是否正确实现远程职业的跨位攻击能力？
- [ ] 近战攻击时是否正确播放移动-攻击-返回的动画序列？
- [ ] 远程攻击时是否正确播放原地攻击/施法动画？
- [ ] 所有战斗行动是否与对应动画状态正确同步？
- [ ] 目标选择是否遵循距离和视线规则？

### **配置设计前必须检查**

- [ ] 是否为每个配置项选择了唯一的实现方法？
- [ ] 是否避免了多重优先级的复杂配置逻辑？
- [ ] 是否选择了最不容易出错的配置方式？
- [ ] 配置路径是否足够简洁明确？

### **手动 Spawn 点配置检查**

- [ ] 是否在 Unity 层级视图中创建了完整的 12 个 spawn 点 GameObject？
- [ ] 是否正确组织了 PlayerSpawnPoints 和 EnemySpawnPoints 分组？
- [ ] 是否在 Inspector 中完整配置了所有 12 个 spawn 点 Transform 引用？
- [ ] 是否运行了"验证 Spawn 点配置"并确认所有配置完整？
- [ ] 是否在 Scene 视图中手动调整了每个 spawn 点到合适的位置？
- [ ] 是否移除了所有自动计算位置的代码和逻辑？
- [ ] spawn 点布局是否符合横版 2D 的镜像对称设计？
- [ ] 是否确保玩家方在左侧，敌人方在右侧的相对位置？
- [ ] 前排 spawn 点是否更接近战场中心，后排是否远离中心？

### **代码提交前必须检查**

- [ ] 是否清理了所有空文件和无效文件？
- [ ] 是否删除了所有测试、演示、验证类型的脚本？
- [ ] 是否移除了以\_Fixed、\_Backup、\_Old 等结尾的文件？
- [ ] 项目中是否只保留核心业务功能文件？
- [ ] 是否删除了所有[System.Obsolete]标记的代码？
- [ ] 是否清理了所有包含"弃用"、"已弃用"字样的注释？

---

## 📝 **总结**

**核心原则：严格手动挂载、强关联、单一路径、中文**

- 🎯 **强制手动挂载** - 所有组件引用必须手动设置，禁止自动查找
- 💪 **强关联关系** - IdleGameManager 必须引用 FormationManager 和 AutoBattleAI
- 🗑️ **删除弃用逻辑** - 移除 CombatManager、FindObjectOfType 等过时代码
- 📋 **单一配置路径** - 每个配置只有一种标准实现方式
- ✅ **质量第一** - 确保每个脚本都编译无误且功能完整
- 🗣️ **中文交流** - 所有对话、注释、文档均使用中文

**记住：统一手动挂载，强关联设计，删除弃用逻辑。**

---

_此规范文档记录核心开发标准，指导所有后续开发工作。_
