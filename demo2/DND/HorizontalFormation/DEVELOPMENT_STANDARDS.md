DND5E 系统 - 工程编码规范

严禁违反的核心规则

文档管理铁律
- 唯一技术文档: 本文档是项目唯一的技术规范文档
- 绝对禁止创建: 任何 markdown 指南、对比文档、debug 记录、纠错文档等
- 所有技术描述: 必须直接更新本文档的相应章节
- 保持项目简洁: 违反此规则将被视为严重错误

AI 助手工作规范
- 只修改现有文档: 永远不创建新的 markdown 文件
- 整合而非分离: 新功能描述直接添加到现有章节
- 简洁优于详细: 记录核心要点，避免冗长描述
- 文档核心用于和用户需求对齐：永远不要更改当前文档字体或者添加注释，导致用户无法编辑当前文档
- 需求严格执行原则: 严格按照用户需求的字面含义开发，禁止自行延申解读和需求扩展，避免产生冗余逻辑和破坏基础架构
- 需求清单原则：要开发的下一步需求会罗列在当前文档中并且随时更新，需要时刻对齐当前文档需求清单
---

核心开发原则

预制体配置优先原则
- 强制要求: 所有角色预制体必须预先配置好 DND_CharacterAdapter 和 SkeletonAnimation 组件
- 禁止硬编码添加: 代码中严禁通过 AddComponent 方式添加任何组件
- 错误处理原则: 如果预制体缺少必需组件，系统报错并拒绝创建角色

开发行为规范
- 单一设计原则: 实现功能��只提供一种设计方案
- 禁止自动推送: 严禁自动执行 git push 操作到 GitHub 仓库
- 强制要求: 与用户的所有对话必须使用中文
- 最小改动原则: 开发新功能时必须在不破坏当前运行逻辑的基础上进行最小化修改
- 需求严格执行原则: 严格按照用户需求的字面含义开发，禁止自行延申解读和需求扩展，避免产生冗余逻辑和破坏基础架构

代码质量要求
- 强制错误清理: 严禁在 IDE 的 PROBLEMS 面板有任何报错的情况下声称功能完成
- 全项目编译检查: 必须解决项目中所有脚本的编译错误
- 弃用代码强制删除: 严禁保留任何标记为System.Obsolete的方法、类或属性

---

当前游戏核心架构

系统组件架构

IdleGameManager (挂机游戏主控制器)
├── HorizontalBattleFormationManager (横版阵型管理器)
├── AutoBattleAI (自动战斗AI系统)
├── ScrollLayer[] (背景滚动系统)
└── CharacterStats[] (角色数据管理)

挂机游戏循环逻辑

探索阶段:
1. 玩家队伍生成 → 使用 HorizontalBattleFormationManager.GeneratePlayerFormation()
2. 探索动画启动 → 玩家队伍播放走路动画，背景开始滚动
3. 进度推进 → stageProgressPercent 每秒增加10%
4. 遭遇触发 → 基于 encounterInterval 时间间隔随机触发敌人遭遇

战斗状态转换:
1. 遇敌检测 → Time.time >= nextEncounterTime 触发战斗
2. 敌人生成 → formationManager.GenerateEnemyFormation() 创建敌人队伍
3. 动画切换 → 玩家切换到待机动画，背景停止滚动
4. 敌人进场 → 敌人从右侧进场到阵型位置，播放走路→待机动画
5. 自动战斗 → AutoBattleAI.ExecuteAutoBattleTurn() 执行回合制战斗
6. 战斗结束 → 玩家恢复走路动画，背景恢复滚动

横版阵型战斗系统

阵型布局设计:
实际阵型位置由HorizontalBattleFormationManager中配置的spawn点Transform决定
前排/后排判断基于实际spawn点的X坐标，而非固定数值

战斗行为模式:
- 近战职业 → 移动到敌人面前攻击后返回原位,不能在对方有前排阵型时攻击后排
- 远程职业 → 原地攻击，支持跨位攻击后排
- 先攻系统 → 按DND5E规则进行先攻检定排序
- 回合制 → 每回合6秒，严格按先攻顺序执行

---

当前实现状态总览

已完成的核心系统

1. 挂机游戏主控制循环

IdleGameManager 状态机:
探索模式 → 遭遇触发 → 战斗准备 → 自动战斗 → 战斗结束 → 恢复探索

具体实现:
- 探索阶段: 玩家队伍走路动画 + 背景滚动 + 进度推进(10%/秒)
- 遭遇检测: 基于时间间隔(encounterInterval)触发敌人生成
- 战斗转换: 停止滚动 + 切换待机动画 + 敌人进场动画
- 胜利处理: 销毁敌人 + 恢复探索状态 + 奖励结算

2. 横版阵型战斗系统

HorizontalBattleFormationManager 阵型布局:
玩家阵型（左侧）            敌人阵型（右侧）
后排：左翼  前排：左翼  ←→  前排: 左翼  后排:左翼
后排：中路  前排：中路  ←→  前排: 中路  后排:中路
后排：右翼  前排：右翼  ←→  前排: 右翼  后排:右翼    

实现特点:
- 12个固定spawn点Transform手动配置
- 预制体实例化到指定位置
- 阵营识别: CharacterStats.battleSide (Player/Enemy)
- 镜像对称布局: 敌人阵型完全镜像玩家布局

3. 玩家攻击行为系统

AutoBattleAI.ExecuteAutoBattleTurn(playerCharacter) 完整流程:
1. 决策阶段: AI分析可攻击目标列表
2. 目标选择: 优先攻击敌方前排，前排全灭后攻击后排
3. 攻击执行: 近战角色移动攻击，远程角色原地攻击
4. 动画播放: DND_CharacterAdapter.PlayAttackAnimation()
5. 伤害计算: 基于DND5E攻击检定和伤害公式
6. 状态更新: 目标血量扣减，死亡检测和尸体处理
7. 动画复位: 攻击动画完成后自动返回待机状态

4. DND5E规则引擎

CharacterStats 属性系统:
- 六大属性: Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma
- 调整值计算: (属性值-10)/2 标准DND5E公式
- 豁免熟练: 6种豁免投骰的熟练项配置
- 技能系统: 18种技能 + 熟练项配置
- 血量机制: MaxHP/CurrentHP + 死亡检测 (HP<=0)

HorizontalCombatRules 战斗规则:
- 攻击检定: 1d20 + 属性调整值 + 熟练加值 vs AC
- 伤害计算: 武器伤害骰 + 属性调整值（部分后续扩展职业特殊说明）
- 暴击机制: 攻击检定=20时触发暴击，伤害骰翻倍
- 距离判断: 近战/远程攻击距离限制

---

当前开发任务: 敌方攻击行为 + DND5E先攻系统

任务1: 实现DND5E先攻检定系统

需要开发的功能:
1. 先攻检定计算: 1d20 + 敏捷调整值 
2. 先攻顺序排序: 所有参战角色按先攻值从高到低排序
3. 回合制循环: 严格按先攻顺序执行每个角色的回合
4. 先攻值相同处理: 敏捷属性高者优先，再相同则随机决定

集成点:
- AutoBattleAI.StartBattleSequence() 战斗开始时执行先攻检定
- 修改当前的简单轮流攻击逻辑为基于先攻顺序的回合制
- 保持与当前动画系统(DND_CharacterAdapter)的兼容性

任务2: 敌方攻击行为AI系统

需要开发的功能:
1. 敌方目标选择: AI选择玩家阵型中的最优攻击目标
2. 攻击决策逻辑: 近战优先攻击前排，远程可跨位攻击后排
3. 攻击动画执行: 复用现有DND_CharacterAdapter.PlayAttackAnimation()
4. 伤害计算和应用: 使用相同的DND5E攻击检定和伤害公式

集成点:
- 扩展AutoBattleAI.ExecuteAutoBattleTurn()支持敌方角色
- 修改目标选择逻辑: 敌方攻击玩家，玩家攻击敌方
- 保持战斗流程的一致性: 敌方也要遵循相同的动画和伤害规则

实现优先级和依赖关系

Step 1: 先攻系统基础框架
├── 在HorizontalCombatRules中添加先攻检定方法
├── 在AutoBattleAI中添加先攻顺序管理
└── 修改战斗流程为回合制循环

Step 2: 敌方攻击逻辑
├── 扩展AutoBattleAI的目标选择算法
├── 添加敌方专用的攻击决策树
└── 确保动画和伤害计算的一致性

Step 3: 系统集成测试
├── 验证先攻顺序的正确性
├── 测试双方攻击行为的平衡性
└── 确保战斗流程的流畅性

---

后续扩展路线图

短期目标（当前迭代完成后）

1. 法术系统基础框架
   - 法术释放动画集成
   - 法术目标选择（单体/群体）
   - 法术伤害和效果计算

2. 装备系统
   - 武器攻击力加成
   - 护甲防御力计算
   - 装备属性加成应用

3. 经验和升级系统
   - 战斗胜利经验获得
   - 属性成长和技能点分配
   - 等级对战斗力的影响

中期目标

1. 多阶段地图系统
   - 不同地图的敌人配置
   - 地图推进和解锁机制
   - Boss战特殊规则

2. 阵容优化系统
   - 角色职业系统（战士/法师/盗贼等）
   - 阵型效果和协同BUFF
   - 角色替换和阵容调整

3. 挂机收益优化
   - 离线挂机收益计算
   - 挂机效率提升道具
   - 自动升级和装备更换

---

Unity 配置指南

标准 Hierarchy 结构

├── IdleGameSystem (空GameObject)
│   ├── IdleGameManager (手动添加IdleGameManager脚本)
│   ├── HorizontalBattleFormationManager (手动添加HorizontalBattleFormationManager脚本)
│   └── AutoBattleAI (手动添加AutoBattleAI脚本)
└── Environment (空GameObject，静态背景组织容器)
    ├── Background_Layer1 (SpriteRenderer + ScrollLayer，远景背景)
    ├── Background_Layer2 (SpriteRenderer + ScrollLayer，中景背景)
    └── Background_Layer3 (SpriteRenderer + ScrollLayer，近景背景)

组件配置设置

IdleGameManager 配置

[挂机模式设置]
idleModeEnabled = false (启动时自动开启探索模式)
encounterInterval = 5.0f (遭遇间隔时间)
battleSpeed = 1.0f (战斗速度倍率)

[系统组件 - 强制手动引用]
formationManager: 手动拖入HorizontalBattleFormationManager组件
autoBattleAI: 手动拖入AutoBattleAI组件

HorizontalBattleFormationManager 配置

[玩家阵型配置 (左侧)]
玩家前排左翼/中锋/右翼: 拖入对应角色预制体
玩家后排左翼/中路/右翼: 拖入对应角色预制体

[敌人阵型配置 (右侧)]
敌人前排左翼/中锋/右翼: 拖入对应角色预制体
敌人后排左翼/中路/右翼: 拖入对应角色预制体

角色预制体标准配置

必需组件:
- CharacterStats           // 角色属性数据组件
- DND_CharacterAdapter     // 统一动画管理组件
- SkeletonAnimation        // Spine动画播放组件

DND_CharacterAdapter配置:
- characterStats: 自动获取同对象上的CharacterStats组件
- skeletonAnimation: 自动获取同对象上的SkeletonAnimation组件
- animationMapping: 在Inspector中配置角色专属的动画名映射

---

严格禁止项

禁止创建的文件类型

XXXTester.cs         - 测试脚本
XXXDemo.cs           - 演示脚本
XXXSetupWizard.cs    - 设置向导
XXX_Fixed.cs         - 修复版本文件
XXX_Backup.cs        - 备份文件
XXX_Old.cs           - 旧版本文件
空文件或仅包含注释的文件

---

违规检查清单

功能完成前必须检查

- 当前修改的脚本是否有编译错误？
- 整个项目的 PROBLEMS 面板是否显示 0 个错误？
- 敌人生成后是否保持与玩家阵型的镜像对称布局？
- 战斗开始时双方是否都切换到待机动画状态？
- 背景滚动是否与战斗状态正确联动？
- 是否使用 isInBattle 标志正确控制动画状态转换？

---

总结

核心原则: 严格手动挂载、强关联、单一路径、中文

- 强制手动挂载 - 所有组件引用必须手动设置，禁止自动查找
- 强关联关系 - IdleGameManager 必须引用 FormationManager 和 AutoBattleAI
- 单一配置路径 - 每个配置只有一种标准实现方式
- 质量第一 - 确保每个脚本都编译无误且功能完整
- 中文交流 - 所有对话、注释、文档均使用中文

此规范文档记录核心开发标准，指导所有后续开发工作。
