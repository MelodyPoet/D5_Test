# DND5E 系统 - 工程编码规范

## 🔒 **严禁违反的核心规则**

### **文档管理铁律**
- **唯一技术文档**：本文档是项目唯一的技术规范文档
- **绝对禁止创建**：任何 markdown 指南、对比文档、debug 记录、纠错文档等
- **所有技术描述**：必须直接更新本文档的相应章节
- **保持项目简洁**：违反此规则将被视为严重错误

### **AI 助手工作规范**
- **只修改现有文档**：永远不创建新的 markdown 文件
- **整合而非分离**：新功能描述直接添加到现有章节
- **简洁优于详细**：记录核心要点，避免冗长描述

---

## 📋 **核心开发原则**

### **预制体配置优先原则**
- **强制要求**：所有角色预制体必须预先配置好 `DND_CharacterAdapter` 和 `SkeletonAnimation` 组件
- **禁止硬编码添加**：代码中严禁通过 `AddComponent<>()` 方式添加任何组件
- **错误处理原则**：如果预制体缺少必需组件，系统报错并拒绝创建角色

### **开发行为规范**
- **单一设计原则**：实现功能时只提供一种设计方案
- **禁止自动推送**：严禁自动执行 `git push` 操作到 GitHub 仓库
- **强制要求**：与用户的所有对话必须使用**中文**
- **最小改动原则**：开发新功能时必须在不破坏当前运行逻辑的基础上进行最小化修改
- **需求严格执行原则**：严格按照用户需求的字面含义开发，禁止自行延申解读和需求扩展，避免产生冗余逻辑和破坏基础架构

### **代码质量要求**
- **强制错误清理**：严禁在 IDE 的 PROBLEMS 面板有任何报错的情况下声称功能完成
- **全项目编译检查**：必须解决项目中所有脚本的编译错误
- **弃用代码强制删除**：严禁保留任何标记为[System.Obsolete]的方法、类或属性

---

## 🎭 **动画状态机与游戏玩法依赖关系**

### **系统架构依赖图**

```
IdleGameManager (挂机游戏主控制器)
├── HorizontalBattleFormationManager (横版阵型管理器)
│   ├── 调用 → CharacterStats.battleSide (识别阵营)
│   └── 管理 → 12个spawn点的Transform引用
├── AutoBattleAI (自动战斗AI系统)
│   ├── 调用 → CharacterAnimationStateMachine.PlayAttack()
│   ├── 调用 → CharacterAnimationStateMachine.PlayCast()
│   └── 依赖 → CharacterStats (角色属性数据)
├── ScrollLayer[] (背景滚动系统)
│   ├── 监听 → IdleGameManager.isInBattle (战斗状态)
│   └── 控制 → SpriteRenderer.material.mainTextureOffset
└── 角色实例 (运行时生成)
    ├── CharacterStats (角色数据核心)
    ├── DND_CharacterAdapter (传统动画管理器)
    │   ├── 引用 → SkeletonAnimation
    │   ├── 配置 → AnimationMapping (动画名称映射)
    │   └── 协程 → ReturnToIdle() (攻击后返回待机)
    └── CharacterAnimationStateMachine (新状态机，可选)
        ├── 引用 → SkeletonAnimation
        ├── 引用 → CharacterStats (血量检测)
        ├── 复用 → DND_CharacterAdapter.AnimationMapping
        └── 事件 → OnStateChanged、OnAnimationCompleted
```

### **动画管理双系统架构**

**当前项目存在两套并行的动画管理系统：**

#### **DND_CharacterAdapter (传统系统 - 当前生产使用)**
```csharp
// 核心方法调用链
IdleGameManager.SetPlayerPartyAnimation("walk")
  └── DND_CharacterAdapter.PlayAnimation(animationName, loop)
      └── SkeletonAnimation.AnimationState.SetAnimation(0, animationName, loop)
      └── StartCoroutine(ReturnToIdle(duration)) // 攻击动画完成后

// 战斗中的动画调用
AutoBattleAI.ExecuteAutoBattleTurn(character)
  └── DND_CharacterAdapter.PlayAttackAnimation()
      ├── PlayAnimation(attackAnimation, false)
      └── StartCoroutine(ReturnToIdle(attackDuration))
```

#### **CharacterAnimationStateMachine (新状态机 - 实验性)**
```csharp
// 状态机调用链
CharacterAnimationStateMachine.PlayAttack()
  └── ChangeState(AnimationState.Attacking)
      ├── PlayStateAnimation() // 播放攻击动画
      ├── trackEntry.Complete += OnAnimationComplete // 设置回调
      └── OnAnimationComplete() → ChangeState(AnimationState.Idle) // 自动返回

// 血量监控自动状态切换
Update() → UpdateState()
  └── if (characterStats.currentHitPoints <= 0)
      └── ChangeState(AnimationState.Death) // 自动死亡状态
```

### **游戏玩法机制调用关系**

#### **挂机循环核心调用链**
```csharp
IdleGameManager.Start()
  └── InitializeIdleSystem()
      ├── GenerateInitialTeams() // 生成玩家队伍
      │   └── HorizontalBattleFormationManager.GeneratePlayerFormation()
      │       └── 实例化角色预制体 → spawn点位置
      └── StartExploreMode()
          ├── SetPlayerPartyAnimation("walk") // 开始走路
          ├── StartBackgroundScrolling() // 启动背景滚动
          └── StartCoroutine(IdleGameLoop()) // 启动主循环

IdleGameLoop() (每秒执行)
  ├── ExploreStage()
  │   ├── SetPlayerPartyAnimation("walk") // 确保持续走路
  │   ├── EnsureBackgroundScrolling() // 确保背景滚动
  │   └── stageProgressPercent += Time.deltaTime * 10f // 进度推进
  └── if (Time.time >= nextEncounterTime)
      └── StartRandomEncounter() // 触发战斗
```

#### **战斗状态转换调用链**
```csharp
StartRandomEncounter()
  ├── isInBattle = true // 设置战斗状态
  ├── GenerateEnemyParty()
  │   └── HorizontalBattleFormationManager.GenerateEnemyFormation()
  ├── EnemyEntranceAnimation(enemyParty) // 敌人进场
  ├── SetPlayerPartyAnimation("idle") // 玩家切换待机
  ├── StopBackgroundScrolling() // 停止背景滚动
  └── AutoBattleSequence()
      ├── SetPlayerPartyAnimation("idle")
      ├── SetEnemyPartyAnimation(enemies, "idle")
      └── foreach character → AutoBattleAI.ExecuteAutoBattleTurn()
          └── DND_CharacterAdapter.PlayAttackAnimation()
              └── PlayAnimation() + ReturnToIdle()

// 战斗结束
if (HasLivingMembers(playerParty))
  ├── isInBattle = false
  ├── SetPlayerPartyAnimation("walk") // 恢复走路
  └── ResumeBackgroundScrolling() // 恢复背景滚动
```

### **关键依赖关系**

#### **强制手动引用依赖**
- `IdleGameManager.formationManager` → 必须手动拖入 `HorizontalBattleFormationManager`
- `IdleGameManager.autoBattleAI` → 必须手动拖入 `AutoBattleAI`
- `HorizontalBattleFormationManager.spawn点` → 必须手动拖入12个Transform引用

#### **组件自动获取依赖**
- `DND_CharacterAdapter.characterStats` → 自动获取同对象的 `CharacterStats`
- `DND_CharacterAdapter.skeletonAnimation` → 自动获取同对象的 `SkeletonAnimation`
- `CharacterAnimationStateMachine.characterStats` → 自动获取同对象的 `CharacterStats`

#### **运行时动态依赖**
- `ScrollLayer.IsScrolling()` ← 被 `IdleGameManager.EnsureBackgroundScrolling()` 检查
- `CharacterStats.currentHitPoints` ← 被 `CharacterAnimationStateMachine` 监控自动死亡
- `CharacterStats.battleSide` ← 被 `HorizontalBattleFormationManager` 用于阵营识别

### **动画系统兼容性说明**

#### **当前生产状态**
- **DND_CharacterAdapter**: 当前主要使用的动画管理器，与 `IdleGameManager` 完全集成
- **CharacterAnimationStateMachine**: 新开发的状态机，功能更完善但尚未集成到主游戏循环

#### **集成注意事项**
- 两套系统不能同时使用在同一角色上，会产生动画冲突
- 如需切换到状态机，需要修改 `IdleGameManager` 中的动画调用接口
- 状态机提供更好的状态管理和事件回调，适合复杂战斗逻辑

#### **推荐集成路径**
```csharp
// 当前调用: IdleGameManager → DND_CharacterAdapter
SetPlayerPartyAnimation("walk") → adapter.PlayAnimation("walk", true)

// 状态机调用: IdleGameManager → CharacterAnimationStateMachine  
SetPlayerPartyAnimation("walk") → stateMachine.StartWalking()
```

---

## 🎮 **当前游戏核心架构**

### **系统组件架构**
```
IdleGameManager (挂机游戏主控制器)
├── HorizontalBattleFormationManager (横版阵型管理器)
├── AutoBattleAI (自动战斗AI系统)
├── ScrollLayer[] (背景滚动系统)
└── CharacterStats[] (角色数据管理)
```

### **挂机游戏循环逻辑**
**探索阶段：**
1. 玩家队伍生成 → 使用 `HorizontalBattleFormationManager.GeneratePlayerFormation()`
2. 探索动画启动 → 玩家队伍播放走路动画，背景开始滚动
3. 进度推进 → `stageProgressPercent` 每秒增加10%
4. 遭遇触发 → 基于 `encounterInterval` 时间间隔随机触发敌人遭遇

**战斗状态转换：**
1. 遇敌检测 → `Time.time >= nextEncounterTime` 触发战斗
2. 敌人生成 → `formationManager.GenerateEnemyFormation()` 创建敌人队伍
3. 动画切换 → 玩家切换到待机动画，背景停止滚动
4. 敌人进场 → 敌人从右侧进场到阵型位置，播放走路→待机动画
5. 自动战斗 → `AutoBattleAI.ExecuteAutoBattleTurn()` 执行回合制战斗
6. 战斗结束 → 玩家恢复走路动画，背景恢复滚动

### **横版阵型战斗系统**
**阵型布局设计：**
```
实际阵型位置由HorizontalBattleFormationManager中配置的spawn点Transform决定
前排/后排判断基于实际spawn点的X坐标，而非固定数值
```

**战斗行为模式：**
- **近战职业** → 移动到敌人面前攻击后返回原位
- **远程职业** → 原地攻击，支持跨位攻击后排
- **先攻系统** → 按DND5E规则进行先攻检定排序
- **回合制** → 每回合6秒，严格按先攻顺序执行

**位置判断规则：**
1. **优先使用BattlePositionComponent** - 如果角色有位置组件，直接使用枚举值判断
2. **备用spawn点坐标判断** - 比较角色X坐标与前排/后排spawn点的距离
3. **前排优先攻击原则** - 近战角色必须优先攻击敌方前排，前排全灭后才能攻击后排

---

## 🛠️ **Unity 配置指南**

### **标准 Hierarchy 结构**
```
├── IdleGameSystem (空GameObject)
│   ├── IdleGameManager (手动添加IdleGameManager脚本)
│   ├── HorizontalBattleFormationManager (手动添加HorizontalBattleFormationManager脚本)
│   └── AutoBattleAI (手动添加AutoBattleAI脚本)
└── Environment (空GameObject，静态背景组织容器)
    ├── Background_Layer1 (SpriteRenderer + ScrollLayer，远景背景)
    ├── Background_Layer2 (SpriteRenderer + ScrollLayer，中景背景)
    └── Background_Layer3 (SpriteRenderer + ScrollLayer，近景背景)
```

### **组件配置设置**

#### **IdleGameManager 配置**
```csharp
[挂机模式设置]
✅ idleModeEnabled = false (启动时自动开启探索模式)
✅ encounterInterval = 5.0f (遭遇间隔时间)
✅ battleSpeed = 1.0f (战斗速度倍率)

[系统组件 - 强制手动引用]
✅ formationManager: 手动拖入HorizontalBattleFormationManager组件
✅ autoBattleAI: 手动拖入AutoBattleAI组件
```

#### **HorizontalBattleFormationManager 配置**
```csharp
[🔵 玩家阵型配置 (左侧)]
✅ 玩家前排左翼/中锋/右翼: 拖入对应角色预制体
✅ 玩家后排左翼/中路/右翼: 拖入对应角色预制体

[🔴 敌人阵型配置 (右侧)]
✅ 敌人前排左翼/中锋/右翼: 拖入对应角色预制体
✅ 敌人后排左翼/中路/右翼: 拖入对应角色预制体
```

#### **角色预制体标准配置**
```csharp
✅ 必需组件：
- CharacterStats           // 角色属性数据组件
- DND_CharacterAdapter     // 统一动画管理组件
- SkeletonAnimation        // Spine动画播放组件

✅ DND_CharacterAdapter配置：
- characterStats: 自动获取同对象上的CharacterStats组件
- skeletonAnimation: 自动获取同对象上的SkeletonAnimation组件
- animationMapping: 在Inspector中配置角色专属的动画名映射
```

---

## ⚠️ **严格禁止项**

### **禁止创建的文件类型**
```
❌ XXXTester.cs         - 测试脚本
❌ XXXDemo.cs           - 演示脚本
❌ XXXSetupWizard.cs    - 设置向导
❌ XXX_Fixed.cs         - 修复版本文件
❌ XXX_Backup.cs        - 备份文件
❌ XXX_Old.cs           - 旧版本文件
❌ 空文件或仅包含注释的文件
```

---

## 🚨 **违规检查清单**

### **功能完成前必须检查**
- [ ] 当前修改的脚本是否有编译错误？
- [ ] 整个项目的 PROBLEMS 面板是否显示 0 个错误？
- [ ] 敌人生成后是否保持与玩家阵型的镜像对称布局？
- [ ] 战斗开始时双方是否都切换到待机动画状态？
- [ ] 背景滚动是否与战斗状态正确联动？
- [ ] 是否使用 `isInBattle` 标志正确控制动画状态转换？

---

## 📝 **总结**

**核心原则：严格手动挂载、强关联、单一路径、中文**

- 🎯 **强制手动挂载** - 所有组件引用必须手动设置，禁止自动查找
- 💪 **强关联关系** - IdleGameManager 必须引用 FormationManager 和 AutoBattleAI
- 📋 **单一配置路径** - 每个配置只有一种标准实现方式
- ✅ **质量第一** - 确保每个脚本都编译无误且功能完整
- 🗣️ **中文交流** - 所有对话、注释、文档均使用中文

_此规范文档记录核心开发标准，指导所有后续开发工作。_

---

## **自定义动画状态机 vs Unity Timeline 对比分析**

#### **当前项目状态**
- **已实现**: 自定义 `CharacterAnimationStateMachine` 和传统 `DND_CharacterAdapter`
- **未使用**: Unity Timeline系统（仅发现Spine插件相关的Timeline扩展）
- **架构特点**: 基于状态机的事件驱动动画管理

#### **自定义动画状态机优势**

**✅ 实时响应能力**
```csharp
// 立即响应血量变化，自动切换死亡状态
Update() → UpdateState()
  └── if (characterStats.currentHitPoints <= 0)
      └── ChangeState(AnimationState.Death) // 0延迟状态切换
```

**✅ 游戏逻辑深度集成**
- 与 `CharacterStats` 血量系统完全同步
- 与挂机循环 `IdleGameManager.isInBattle` 状态联动
- 支持战斗AI直接调用动画接口（`PlayAttack()`, `PlayCast()`）

**✅ 内存效率**
- 轻量级状态机，运行时内存占用极小
- 无需预加载大量动画序列数据
- 适合大量角色同时存在的挂机游戏场景

**✅ 程序化控制精度**
```csharp
// 精确的动画完成检测和自动回调
trackEntry.Complete += OnAnimationComplete
  └── OnAnimationComplete() → ChangeState(AnimationState.Idle)
```

**✅ 扩展性强**
- 可随时添加新的 `AnimationState` 枚举值
- 事件系统支持复杂的动画完成回调
- 适合程序化生成的角色和动画序列

#### **自定义动画状态机劣势**

**❌ 复杂动画序列制作困难**
- 无法可视化编辑复杂的动画时间线
- 多角色协同动画需要大量程序化同步代码
- 技能连击、合体技等复杂序列实现复杂

**❌ 美术工作流程限制**
- 动画师无法直接预览和调整动画时机
- 特效、音效与动画的精确同步需要硬编码
- 动画混合和过渡时机难以可视化调试

**❌ 时间控制精度限制**
- 基于 `Update()` 的帧率依赖性
- 难以实现精确到毫秒的动画同步
- 动画速度调整需要修改代码参数

---

#### **Unity Timeline 优势**

**✅ 可视化编辑能力**
- Timeline窗口直观展示动画序列
- 美术人员可直接调整动画时机和效果
- 支持多轨道同时编辑（动画+特效+音效+镜头）

**✅ 精确时间控制**
- 基于时间轴的精确控制，不依赖帧率
- 支持动画速度缩放和时间重映射
- 关键帧精确到毫秒级别

**✅ 复杂序列制作能力**
```csharp
// Timeline可以组合的元素
- Animation Track: Spine动画播放
- Audio Track: 技能音效同步
- Control Track: 特效GameObject激活/停用
- Signal Track: 代码事件触发点
- Cinemachine Track: 镜头切换和震动
```

**✅ 团队协作友好**
- 程序员提供Timeline接口，美术直接制作内容
- 版本控制友好，Timeline资产可独立管理
- 支持预制体级别的Timeline复用

**✅ 播放控制灵活**
```csharp
// Timeline播放控制示例
playableDirector.Play(); // 播放
playableDirector.Pause(); // 暂停
playableDirector.time = 2.5f; // 跳转到指定时间
playableDirector.playableAsset = skillTimeline; // 切换Timeline
```

#### **Unity Timeline 劣势**

**❌ 实时响应能力有限**
- Timeline播放中难以立即响应外部状态变化
- 血量归零等紧急状态切换延迟较高
- 不适合需要立即中断的游戏逻辑

**❌ 内存和性能开销**
- Timeline资产需要预加载到内存
- PlayableGraph构建有运行时开销
- 大量角色同时播放Timeline性能压力大

**❌ 程序化控制复杂**
- 动态生成Timeline序列较为复杂
- 与挂机系统的自动化逻辑集成困难
- 需要额外的Timeline资产管理系统

**❌ 挂机游戏适配性差**
- 挂机游戏需要大量重复的简单动画循环
- Timeline更适合剧情演出等复杂但一次性的序列
- 自动战斗的简单动画使用Timeline过于繁重

---

#### **DND5E挂机游戏的最佳选择分析**

**推荐使用自定义状态机的场景：**
- ✅ **日常挂机循环**: 走路、待机、简单攻击动画
- ✅ **自动战斗系统**: AI驱动的快速动画切换
- ✅ **状态响应**: 血量变化、死亡检测等实时反馈
- ✅ **大量角色管理**: 玩家队伍+敌人队伍同时动画

**推荐使用Timeline的场景：**
- ✅ **技能演出**: 复杂的多段攻击、魔法释放序列
- ✅ **剧情演出**: 角色对话、过场动画、镜头切换
- ✅ **Boss战特殊阶段**: 需要精确时机控制的复杂战斗序列
- ✅ **胜利/失败演出**: 战斗结束后的庆祝或失败动画

#### **混合架构推荐方案**

**基础层：自定义状态机**
```csharp
// 处理基础游戏状态
CharacterAnimationStateMachine.ChangeState(AnimationState.Idle);
CharacterAnimationStateMachine.StartWalking();
CharacterAnimationStateMachine.PlayAttack(); // 简单攻击
```

**表现层：Timeline增强**
```csharp
// 处理复杂演出序列
if (isSkillSequence) {
    playableDirector.playableAsset = skillTimeline;
    playableDirector.Play();
} else {
    animationStateMachine.PlayAttack(); // 普通攻击
}
```

**集成接口设计：**
```csharp
// Timeline完成后自动返回状态机控制
timeline.stopped += (playableDirector) => {
    animationStateMachine.ChangeState(AnimationState.Idle);
};
```
