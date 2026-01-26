DND5E 系统 - 工程编码规范

严禁违反的核心规则

文档管理铁律
- 唯一技术文档: 本文档是项目唯一的技术规范文档.md文件
- 绝对禁止创建: 任何指南、对比文档、debug 记录、纠错文档等.md文件
- 所有技术描述: 必须直接更新本文档的相应章节
- 保持项目简洁: 违反此规则将被视为严重错误

AI 助手工作规范
- 正文对话绝对不要使用英语：除非代码变量名/方法名/类名/技术名等必须使用英语，否则所有对话必须使用中文
- 只修改现有文档: 永远不创建新的 markdown 文件
- 不要每开发新的功能或者一修改代码，就创建大量的总结/升级完成/汇总/操作说明/问题解决报告等等.md文件，对话框中阐述最重要的操作步骤流程和修改点即可
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

系统一致性原则
- 系统一致性： 所有系统组件必须保持一致的设计风格和实现方式
- 统一命名规范: 所有脚本、类、方法和变量必须遵守统一的命名规范
- 代码风格统一: 所有代码必须遵循统一的代码风格和格式
- 新增功能集成: 所有新功能必须无缝集成到现有系统中，保持整体一致性，不要破坏现有逻辑，如之前逻辑中已经又类似方法，但是
增加新功能没在原本架构上扩展，而是重写新的方法导致2套不同的运行逻辑导致编译错误


开发行为规范
- 单一设计原则: 实现功能只提供一种设计方案
不要多此一举在两个不同脚本上实现同样的功能留作预选，导致代码冗余和维护困难。所有功能只有一个实现路径和对应的脚本参数接口
- 禁止自动推送: 严禁自动执行 git push 操作到 GitHub 仓库
- 强制要求: 与用户的所有对话必须使用中文
- 最小改动原则: 开发新功能时必须在不破坏当前运行逻辑的基础上进行最小化修改
- 需求严格执行原则: 严格按照用户需求的字面含义开发，禁止自行延申解读和需求扩展，避免产生冗余逻辑和破坏基础架构

代码质量要求
- 强制错误清理: 严禁在 IDE 的 PROBLEMS 面板有任何报错的情况下声称功能完成
- 全项目编译检查: 必须解决项目中所有脚本的编译错误
- 弃用代码强制删除: 严禁保留任何标记为System.Obsolete的方法、类或属性
- 禁止使用过时API: 严禁使用 Unity 已弃用的 API 和方法
- 如程序集需要单独命名空间，必须使用 DND 命名空间前缀
- 变量和方法命名必须使用驼峰命名法 (camelCase)
- 类名和接口名必须使用帕斯卡命名法 (PascalCase)
- 常量命名必须使用全大写加下划线 (UPPER_SNAKE_CASE)
- 角色怪物等信息存储用ScriptableObject存储，禁止使用Json等其他方式存储
- 不要撰写任何测试脚本，所有调试必须在现有脚本中完成
- 不要撰写任何演示脚本，所有演示必须在现有脚本中完成
- 不要撰写任何设置向导脚本，所有设置必须在现有脚本中完成
- 不要创建任何修复版本文件，所有修复必须在现有脚本中完成
- 不要创建任何备份文件，所有备份必须在现有脚本中完成
- 不要创建任何旧版本文件，所有旧版本必须在现有脚本中完成
- 禁止创建空文件或仅包含注释的文件
- 禁止撰写图形图标的文字在注释或者日志中，导致编译产生问题
---

当前游戏核心架构

系统组件架构

IdleGameManager (挂机游戏主控制器)
├── HorizontalBattleFormationManager (横版阵型管理器)
├── AutoBattleAI (自动战斗AI系统)
├── ScrollLayer[] (背景滚动系统)
└── CharacterStats[] (角色数据管理)

核心功能逻辑(模块职责)
HorizontalBattleFormationManager-管理玩家和敌人的阵型生成与布局
AutoBattleAI-执行自动战斗决策和攻击行为
ScrollLayer-实现多层次背景滚动效果
CharacterStats-存储角色属性、技能和状态信息
DND_CharacterAdapter-统一管理角色动画播放和状态切换
BattlePositionComponent-标记角色在阵型中的位置（前排/后排）
HorizontalCombatRules-封装DND5E战斗规则和计算逻辑
HorizontalFormationTypes-定义阵型类型和位置映射
EventChannelManager-管理全局事件通道用于解耦系统间通信
DamageEventChannel-专门用于伤害事件的ScriptableObject事件通道
IdleGameManager 使用状态机管理游戏流程
InitiativeEntry-存储先攻检定结果和顺序
UI_HealthBar-角色血条UI组件
CharacterTemplate-存储角色和怪物的基础数据模板
GameEnums-定义游戏中使用的枚举类型

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
- 位置唯一真源：由 FormationContainer ScriptableObject 中的 6 个索引决定，并由 HorizontalBattleFormationManager 按相同索引实例化到对应 spawn 点。
- 索引与排位映射固定：
  - [0][1][2] → 前排（左/中/右）
  - [3][4][5] → 后排（左/中/右）
- spawn 点Transform 仅作为可视化与摆放载体，其逻辑含义与 FormationContainer 的索引严格一致。
- 强制禁止：任何基于 AC 或其它角色属性的“自动位置分配/推断”。HorizontalFormationTypes 不负责位置分配，仅提供位置枚举与通用判断工具。

战斗行为模式:
- 近战职业 → 移动到敌人面前攻击后返回原位,不能在对方有前排阵型时攻击后排
- 远程职业 → 原地攻击，支持跨位攻击后排
- 先攻系统 → 按DND5E规则进行先攻检定排序
- 回合制 → 每回合6秒，严格按先攻顺序执行

朝向一致性：
spine素材因为朝向问题，所有角色（玩家/中立/敌人）预制体必须面向右侧，战斗时仅需要将敌人阵型镜像翻转即可

动画使用技术选型:
- 协程动画会导致难以控制动画状态和同步问题，所有动画均使用DND_CharacterAdapter组件进行播放和状态管理，且使用SpineEvents事件回调处理攻击命中和伤害计算逻辑
- 问题记录：在攻击动画播放过程中，发现以下问题：
  1. ClearTrack(0) 未能成功清理轨道，导致攻击动画被其他动画覆盖。
  2. FindBestAttackAnimationName 方法未能正确匹配动画名称，导致动画播放失败。
  3. Spine事件触发存在延迟，备用计时器时间不足，可能导致事件未及时触发。
  4. PlayAnimation 方法中未能完全确保 SetAnimation 调用成功，导致动画状态未正确更新。
- 解决方案建议：
  1. 增加 ClearTrack(0) 的异常捕获和日志记录，确保轨道清理成功。
  2. 在 FindBestAttackAnimationName 方法中增加默认动画名称作为兜底逻辑。
  3. 调整备用计时器时间，确保 Spine 事件未触发时逻辑仍能正确执行。
  4. 在 PlayAnimation 方法中增加更严格的状态验证和刷新逻辑，确保动画状态正确更新。
---
当前动画已经做的优化点：
-SpineEvent监听与解绑逻辑集中管理，防止遗漏解绑导致内存泄漏
-DOTween动画完成后统一回调，避免多处分散回调逻辑
-预留状态机接口，方便后续扩展复杂动画状态
-动画名称映射配置，支持不同角色使用不同动画名称
- 动画映射通过SO配置，方便非程序人员调整

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
8. 区分近战和远程职业方式: 不通过配置的模板属性中描述的字符串来判断，而通过这个prefab在阵型什么位置来判断
   - 敌我双方在前排位置生成SpawnPoints[0]~[2]：判断其为近战职业
   - 敌我双方在后排位置生成SpawnPoints[3]~[5]：判断其为远程职业
9. 除非对方前排全灭之外，敌我双方前排职业只能攻击敌方前排，不能攻击敌方后排；且攻击行为是走到对方面前近战攻击，攻击完后走回原位
10. 后排职业可以攻击敌方前排和后排；攻击行为是原地远程攻击，不需要走到对方面前

CharacterStats 属性系统:
- 六大属性: Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma
- 调整值计算: (属性值-10)/2 标准DND5E公式
- 豁免熟练: 6种豁免投骰的熟练项配置
- 技能系统: 18种技能 + 熟练项配置
- 血量机制: MaxHP/CurrentHP + 死亡检测 (HP<=0)

HorizontalCombatRules 战斗规则:
- （力量武器）根据当前装备的近战力量武器时攻击检定: 1d20 + 力量调整值 + 熟练加值 vs AC
- （敏捷武器）根据当前装备的远程武器或者近战灵巧武器时攻击检定: 1d20 + 敏捷调整值 + 熟练加值 vs AC
- （法术攻击）根据 CharacterTemplate.defaultAttackType = Spell 时执行法术普通攻击：
  - 攻击检定: 1d20 + 主施法属性调整值(由 template.primarySpellAbility 配置，默认 intelligence) + 熟练加值 vs AC
  - 伤害计算: 仅取法术伤害骰(由 template.defaultCantrip.GetDamageDiceAtCasterLevel() 提供，随等级增长)，不叠加属性调整值
  - 暴击机制: 攻击检定=20 时触发暴击，伤害骰翻倍
  - 伤害类型: 由 template.defaultCantrip.damageType 决定（如 Force、Psychic 等）
- 物理伤害计算: 武器伤害骰 + 不同武器对应的属性调整值（部分后续扩展职业特殊说明）
- 暴击机制: 攻击检定=20时触发暴击，伤害骰翻倍
- 距离判断: 近战/远程攻击距离限制
- 先攻检定: 1d20 + Dexterity调整值，决定回合顺序
- 回合制流程: 按先攻顺序依次行动，每回合6秒
- 状态效果: 简单的中毒、眩晕等状态效果框架（后续扩展）
- 战斗结束条件: 一方全灭或逃跑
- 战斗奖励: 经验值和金币（后续扩展）
- 战斗日志: 记录每次攻击和伤害结果（后续扩展）
- 怪物死亡：怪物血量扣到0以下，播放死亡动画并且执行消失流程
- 玩家和队友死亡：角色血量扣到0以下，播放昏迷动画并且执行昏迷流程，昏迷状态下无法行动，执行3回合的普通d20骰死豁免判断，3点失败则死亡，
3点成功则恢复1点血量并且脱离昏迷状态，恢复行动能力,DC=10，队友可以用治疗法术直接恢复该角色；此时怪物也有可能攻击该角色，此时受到伤害计一次死豁免失败,重击计两次死豁免失败，三次失败則死亡
- 玩家和队友倒地期间，如战斗结束进入探索模式，则该角色自动脱离昏迷状态，恢复1点血量並且恢复行动能力（探索期间不存在玩家方昏迷状态）

---使用ScriptableObject存储角色和怪物数据
---使用ScriptableObject实现战斗事件通道DamageEventChannel,用于解耦伤害计算和UI显示,动画播放等逻辑,使其更易于扩展
+ 使用ScriptableObject实现战斗事件通道DamageEventChannel,用于解耦伤害计算和UI显示,动画播放等逻辑,使其更易于扩展
+ 发布规范：仅由 `HorizontalCombatRules.ResolveAttack(attacker, target, ...)` 在“命中成立”后统一发布一次伤害事件；AI/动画回调/角色脚本等不得重复发布，避免 UI 重复刷新与日志重复。
+ 获取顺序：事件通道优先从参战者实例上的 `CharacterStats.damageEventChannel` 获取；如未配置，则回退到 `EventChannelManager` 的全局通道（`"DamageEventChannel"`）。若两者皆无，将打印警告日志以便排查。
+ 订阅建议：UI 使用 `CharacterStats.OnHealthChanged` 做最终显示刷新，`DamageEventChannel` 主要用于动画、特效、飘字与日志等跨系统联动，不直接驱动 HP 数值修改。
  - 事件通道与血条更新 — 关键技术点（简洁，供查阅）
   - 事件通道（`DamageEventChannel`）职责单一：用于把“谁受到伤害/谁造成伤害/伤害数值”等消息广播到所有关心该事件的系统（动画、伤害计算、视觉特效等），但不应直接由 UI 订阅来做最终显示更新。
   - UI 更新应依赖实例级的本地事件：`CharacterStats` 在受伤/治疗后应触发 `OnHealthChanged(int currentHp, int maxHp)`，UI（`UI_HealthBar`）在被绑定到具体 `CharacterStats` 实例时直接订阅该本地事件以保证目标明确与低时序窗。
   - 管理器做为保险：`HealthBarUIManager` 保存 `CharacterStats -> UI_HealthBar` 的映射表，提供 `RefreshBar(CharacterStats)` 接口供 `CharacterStats` 在处理伤害后主动调用，作为对本地事件的二次保障（同一职责的两条可靠路径）。
   - 避免 UI 直接订阅全局通道：当场景同时存在预制体（编辑器中放置）和运行时实例时，UI 若直接订阅全局通道容易订阅到错误目标或由于时序错过事件。
   - 单例与容器时序规则：确保 `HealthBarUIManager` 单例在 UI/角色创建前就存在（或在创建时立即同步容器与 prefab），避免在单例为 null 时触发回退销毁逻辑导致血条被误删。
   - 绝对避免启发式销毁：不得使用基于 Slider.value/maxValue 等启发式规则在不确定的情况下批量销毁血条；销毁条件应为 `owner == null` 且经确认（或超时/显式标记）后才执行。
   - 预制体配置优先：血条预制体必须在 Inspector 中预先绑定好 `Slider`/`Text` 等组件，减少运行时自动查找带来的不确定性。
   - 日志和调试接口：保留并使用 `HealthBarUIManager.DumpStatus` / `DumpMapDetails` 等调试方法，在复现问题时先采集映射与容器状态以便定位时序或引用不一致的问题。

受击时候头上冒字的伤害显示系统以及血条受击扣血系统
- UI_HealthBar更新血条
- DamageDisplayManager监听事件显示伤害数字
- 只保留一套基于预制体的逻辑
- 统一坐标转换方法，只使用标准的RectTransformUtility.ScreenPointToLocalPointInRectangle方法
 
配置为Canvas-PlayerHealthBars
           -EnemyHealthBars
这两者仅判断UI生成的区域，UI的实际位置通过代码动态计算

游戏运行时候偶发的HP归0后没播放敌人死亡动画的问题/玩家方角色昏迷流程动画问题根因(已修复，记录以便后续跟踪)：
1.终结状态被后续状态覆盖
DND_CharacterAdapter脚本终结状态缺少”护航“与”清理"，导致死亡/昏迷动画被后续Idle/Walk/Run/Attack或位移回调覆盖;
2.未清轨/未杀Tween让终结动画首帧不稳定。加上CharacterStats偶发找不到适配器，这些问题综合导致血量归0后未播放死亡动画或昏迷动画;

近战攻击链路:
AI决策:AutoBattleAI.ProcessCurrentTurn->DecideBestAction->ExecuteBattleActionEvent
流程：DND_CharacterAdapter.ExecuteMeleeAttack(target)
阶段1 移动：PlayWalkAnimation->transform.DOMove(attackPos)
阶段2 攻击:到达后ExecuteAttackAtPosition()
    首先ClearTrack(0)清理轨道，防止攻击动画被覆盖
    然后PlayAttackAnimation->PlayAnimation(attack,loop=false)
    事件/兜底:监听Spine Event触发OnAttackHit;并加DOVirtual.DelayedCall备用计时器防止事件未触发
阶段3 返回:攻击完成后DOMove(originalPosition)->OnComplete时若未死亡/未昏迷再PlayIdleAnimation

远程攻击链路:不移动，直接ExecuteAttackAtPosition+攻击事件逻辑

受击/Miss/脚步
命中后：若目标没倒地，target.PlayHitAnimation(短动画)
未命中:target.PlayDodgeAnimation+target.ShowMiss()
脚步/状态事件:通过Spine Event回调OnSpineEvent->OnStateChanged广播，CharacterStats收到后可做音效等

集成点:
- 保持战斗流程的一致性: 敌方也要遵循相同的动画和伤害规则

当前已经实现优先级和依赖关系

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

Step 4: UI系统集成
├── 添加回合指示器显示当前行动角色
├── 显示先攻顺序列表
└── 确保UI与战斗状态同步更新（角色/怪物血量、状态变化等）
且UI因做成预制件形式，动态加载并且通过ScriptableObject事件通道与战斗系统解耦


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

CharacterTemplate (SO 配置) 标准项：
- characterName / characterClass / level - 基础信息
- 六维属性 (strength/dexterity/constitution/intelligence/wisdom/charisma)
- hitDie - 生命骰
- baseArmorClass - 未着甲时的基础AC
- 【攻击方式】
  - defaultAttackType - 默认攻击方式（Physical=物理 / Spell=法术）
  - 物理模式：使用装备武器或徒手攻击
  - 法术模式：使用 defaultCantrip 进行法术普通攻击（戏法）
- 【法术配置】（仅在 defaultAttackType=Spell 时有效）
  - primarySpellAbility - 法术攻击检定使用的属性（intelligence/wisdom/charisma 等，默认 intelligence）
  - defaultCantrip - 法术普通攻击（戏法），必须拖入一个 SpellData SO
    - 该 SpellData 包含伤害骰、伤害类型、等级升级规则等
    - 通过菜单创建：右键 → Create → DND → Spell Data
- 【徒手参数】（未装备武器时）
  - unarmedDamageDice - 徒手伤害骰（如 1d4 / 1d6 / 1d8 等，支持自由配置）
  - unarmedDamageAbilityMode - 徒手能力模式（Strength / Dexterity / BestOfStrDex）
  - unarmedProficient - 徒手是否有熟练加值（bool，true=有，false=无）
  - unarmedDamageType - 徒手伤害类型（Bludgeoning/Slashing/Piercing 等，可按角色特性定制）
- proficientWeaponClasses / proficientWeaponTypes - 武器熟练
- proficientArmorTypes - 护甲熟练
- 抗性/免疫/易伤配置

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
-不要因为某个脚本debug时间需要而创建这些文件，所有调试必须在现有脚本中完成
-不要额外开发需求清单之外的一切附加任务
-如有逻辑问题，我会直接运行unity报错，并且告诉你报错信息，你需要根据报错信息来修复代码,不要创建上述测试脚本
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
- 强制手动挂载 - 所有组件引用必须手动设置，禁止自动查找
- 强关联关系 - IdleGameManager 必须引用 FormationManager 和 AutoBattleAI
- 单一配置路径 - 每个配置只有一种标准实现方式
- 质量第一 - 确保每个脚本都编译无误且功能完整
- 中文交流 - 所有对话、注释、文档均使用中文

此规范文档记录核心开发标准，指导所有后续开发工作。

所有角色生命值计算公式：
- 角色最大生命值（MaxHP）=第一级是职业的生命骰（如战士1d10，法师1d6）+角色的体质调整值*1（最低1点生命值）  
- 每升一级时，角色的最大生命值增加：职业生命骰的平均值（向上取整）+角色的体质调整值（最低1点生命值）

装备与命中规则（装备驱动 — 设计要点）
- 单一来源：命中/伤害只能由“装备/法术源”驱动，禁止硬编码能力选择。
- 运行时组件：角色挂载 CharacterInventory（背包）+ CharacterEquipment（装备栏），
严格手动挂载；仅作为数据来源。
- 统一接口：所有攻击都通过 ICombatSource（武器/法术两种实现）提供命中与伤害参数；HorizontalCombatRules 
只接收 ICombatSource，保持单一路径。
- 攻击类别与属性选择：
  - 近战武器：默认使用力量；若武器含 Finesse（灵巧），可使用敏捷（或按模板 allowMeleeFinesse 开关，默认关闭）。
  - 远程武器：使用敏捷。
  - 法术攻击（含默认戏法普通攻击）：只要角色有法术，命中就使用职业主属性（primarySpellAbility），且伤害仅取法术伤害骰（不叠加主属性）。
  - 默认普通攻击策略：基于 CharacterTemplate.defaultAttackType 决定。Physical → 走装备/物理；Spell → 使用 defaultCantrip
  （若无则按兜底策略）。
- 熟练与加值：
  - 物理命中 = 1d20（含优/劣势）+ 当前武器类型对应的属性调整值（如无武器则参考下面的拳头判定） + 熟练加值（若熟练）。
  - 法术命中 = 1d20（含优/劣势）+ 当前template设置的职业主施法属性调整值 + 熟练加值（若熟练）。
  - 物理伤害 = 武器给的伤害骰 + 当前武器类型对应的属性调整值 ；暴击仅翻倍骰，不翻倍修正。
  - 法术伤害 = 法术给的伤害骰 ；暴击仅翻倍骰，不翻倍修正。
  - 熟练判定来自模板：proficientWeaponClasses / proficientWeaponTypes 或职业施法熟练。
- 阵位与动画不变：
  - 前排 = 近战位移攻击；后排 = 远程/施法原地攻击。AI/动画链路保持不变，仅更换 ICombatSource。
- 远程弹药与兜底：
  - 默认不需要弹药，但预留后面设计某些怪物需要特定弹药才能穿透其抗性，（Ammunition/Thrown）的武器在弹药不足时自动回退：优先默认戏法
  （SpellAttack）
- 距离规则：使用源的 RangeMin/RangeMax 判定可攻击距离；近战短距离，远程/法术按配置。
- 兼容与回退：
  - 若未挂装备系统或无可用源（视作用双拳攻击）：此时所有角色无论站位在前、后排，命中和伤害取力量调整值,伤害骰按1d4；施法攻击
（前提是施法职业且拥有对应法术）无限制（空手也可以使用攻击型戏法，使用职业主属性）。
  
- 数据与枚举约束：
  - 新增枚举统一追加到 GameEnums.cs：AbilityScore / AttackCategory / EquipmentSlot / WeaponClass / WeaponType /
  WeaponTag / AmmoType；伤害骰用 DiceSpec（个数/面数/修正）。
  - 物品数据统一使用 ScriptableObject：ItemBase_SO / WeaponItem_SO / ArmorItem_SO / AccessoryItem_SO / AmmoItem_SO；
  禁止使用 Json 等其他持久化方式。
- 严格禁止：
  - 不得引入第二套命中/伤害流程；所有路径必须收敛到 HorizontalCombatRules.ResolveAttack(attacker, target, ICombatSource, 
  advantageFlag)。

装备与背包系统 — 契约与集成（简版）
- 核心玩法：（参考steam上很火的背包乱斗类游戏）
  - 玩家通过构筑职业流派的装备组合来提升战斗力。
  - 装备物品提供命中/伤害/防御等属性加成。
  - 不同装备类别（武器/护甲/饰品）有不同的槽位限制。
  - 装备物品可有特殊标签（如 TwoHanded/Shield），影响装备规则。
  - 不同装备之间存在共生和关联关系（举例如弓箭与箭矢放在临近位置激活增强远程攻击属性）。考验玩家利用有限的背包空间进行优化配置的能力/不同装备组合
  带来的战斗策略差异。
  - 玩家等级/种族/专长的构筑影响可装备物品的种类和数量。这样给玩家一定的成长指引。
  - 不同装备所占背包内空间的基础单元格不同，从而让玩家在有限的背包空间内进行取舍和优化。包括手动旋转，组合，摆放整理物品，同时要考虑物品之间的关联
  和共生关系。  
  - 物品的共生和关联关系受到自身职业构筑的流派指导和限制，(如板甲骑士的构筑必定会关联重甲类别的护甲，如战斗中获得更好的重甲，可以替换当前装备而不
  破坏自身职业构筑带来的共生关系的加成，但此时如果通过战斗随机获得一件强力的轻甲，其属性足以颠覆原本的职业构筑)从而让玩家在构筑职业流派时有更多的思
  考和选择空间。也让玩家在战斗中根据随机获得的强力装备来不断构思和调整自身的职业构筑和战术策略。（这种设计思路类似于Roguelike游戏中通过随机获得的
  装备来调整自身的构筑和策略）同时洗点的成本适中，可以让玩家有一定的沉没成本支付能力。
  - 局内战斗时背包锁定不可操作，通过探索阶段遇到的“篝火”类checkpoint进入装备调整界面。
  - 局外的资源分配和角色成长运营让玩家策略性构思进入局内战斗时的默认装备和战术消耗道具，如在局内通过战斗获得自己职业构筑所需的更优质装备，则可更新
  当前装备。
  但战斗死亡就会失去所有收获的装备物品，回到初始装备状态。从而建立2D横版简约搜打撤机制。
 
-背包中的物品占格子规则：
  - 参考经典背包类游戏《背包乱斗》和经典传统玩法的俄罗斯方块的拼接方法。
    -每个物品应该有最低1x1格子大小。
    -物品可以是1x1,1x2,2x1,2x2,3x2,2x3,1x4等不同形状大小的。
    - 物品可以有不同的形状，如L形，T形等，但必须是矩形的组合。
    - 物品在背包里可以根据形状凹凸进行拼接，但不能重叠。
    - （这些规则为后续预留玩法设计空间）
    - 物品之间特定的拼接条件可以触发额外的共生关系的特定加成效果。（如弓箭和箭矢拼接在一起触发远程攻击加成效果）
    - 并且多件物品拼接形成的格子如果占满背包的行和列也触发对应不同的属性加成效果。(俄罗斯方块)
    
-统一形状对应格子规则：(俄罗斯方块规则的多形状物品占格子规则)
    - ItemBaseSO的SlotWidth/SlotHeight属性接口应该是以下方式：
        - 物品的形状是矩形的组合，所以一律从左上角开始计算宽度和高度。      
        - 比如一个L形物品，那么首先有个接口来输入确认该物品一共会在形状上占据几行(SlotWidth)，再有一个接口来确认它的第一行占据几个格子
        (SlotHeight)，然后再有一个接口来确认它的第二行占据几个格子(SlotHeight)，以此类推，直到该物品的形状完全描述完毕。
        - 角色默认背包的总网格大小上限是8行*16列。后续结合营地经营玩法，可以通过升级营地设置来制作高级背包来提升总行列数上限。
    
  规则细节：
  - 每个物品根据其类型和大小占用不同数量的格子（slotWidth/slotHeight）。
  - 鼠标左键点击物品即从背包中单独选择该物品，此时该物品脱离吸附格子，物品图标稍微变大一些且往右偏移2个像素距离，
而跟随鼠标的位置，且此时才可以右键打开对物品操作菜单。
  - 旋转必须要在物品被单选中状态下，脱离吸附格子的情况下，才可以右键菜单中选择“旋转”选项后，物品顺时针旋转90度-SlotWidth/SlotHeight互换。
  - 此时仍旧保持在单选状态，且此时物品脱离吸附背包，不能被右键菜单的装备/卸下这两个按钮操作。
  - 单选中状态下，物品仍然对背包格子产生一个自身大小的投影（透明灰色），提示当前物品可放置的位置。
  - 再次左键点击放下物品时，若投影位置合法（无重叠且在网格内），则物品吸附到该位置；否则回退原位。
  
  - 背包网格有固定的行列数（rows/cols），决定总容量。
  - 物品摆放时需考虑格子间距和边距（spacing/padding）。

- UI 布局与交互（概要）
  - 背包/角色属性 Hub 采用“锚点优先”的布局原则：不使用 LayoutGroup/ContentSizeFitter；背包网格（InventoryGridView）采用手动定位
  （按cellSize/spacing/padding 计算），不使用 LayoutGroup。

[路径A落地决策与约束]
- 决策：采用“在角色预制体/实例的 CharacterInventory.initialItems 直接配置初始物品”的路径A。启动时由该列表生成 ItemInstance 并触发刷新。
- 绑定位置（强制）：CharacterInventory 必须挂在每个“玩家/盟友”角色预制体/实例上，禁止挂在场景空节点（如 PlayerData）。
  - PlayerData 空节点不再承担背包数据容器的职责；仅当未来需要“队伍共享仓库/共用背包”时，另行定义独立 PartyInventory（不影响本规范的“角色个人
  背包”）。
- 网格行列 rows/cols 的意义（保留且重要）：
  - rows/cols 仍然是“容量/版面”（可用格子总数），用于判定是否存在可容纳物品占位的空区、物品旋转后的适配、碰撞/覆盖检测与自动排布；而物品占多少
  格由 ItemBaseSO 的 slotWidth/slotHeight 决定，二者职责不同、同时生效。
- UI 绑定（切换合同）：
  - InventoryUIBinder.sourceInventory 始终指向“当前选中角色”的 CharacterInventory；左右切换仅重绑引用→GridView 清空并重建→Binder 
  刷新显示。
  - 强制手动挂载：FormationContainer 实例化后的角色对象，其 CharacterInventory 引用必须手动填入“角色选择器（SelectionHub）”的目标列
  表中，禁止自动查找。
  - 多来源支持（新增）：InventoryUIBinder 允许挂载多个来源（sourceInventories），通过 activeSourceIndex 或 
  SetActiveSourceIndex(i) 在玩家/盟友等不同角色之间切换；旧字段 sourceInventory 仍兼容单一来源模式（UseSingleSource(inv)）。
  提供 AddSource/RemoveSource 以维护列表。切换时 Binder 会自动解绑旧源事件、按来源 rows/cols 重配 GridView 并重建。
  - 约束：多来源列表中的每一项必须是对应角色预制体上的 CharacterInventory 引用；禁止将场景空节点（如 PlayerData）作为背包数据源使用。
  玩家与盟友的背包与装备分离时，分别作为不同来源挂入列表，以活动索引进行切换。
- 运行时交互策略：
  - 战斗中禁编辑：通过交互遮罩或交互开关屏蔽拖拽/旋转；篝火/休整界面再开放编辑与整理。
  - AutoFitToContainer=启用；keepSquareCells 视美术风格选择；includeSpacingInItemSize 按是否要“覆盖格间隙”选择。
- 物品来源：
  - 初始物品：仅由各角色的 CharacterInventory.initialItems 决定（本规范）。
  - 运行时新增：通过 InventoryUIBinder.TryAddNew(ItemBaseSO) 尝试落地→成功后再加入数据源；失败不改变数据源。

角色默认属性+物品以及职业专长/技能等加成规则

战斗时角色数据源中属性是根据Base层+Permanent层+Equipment层+Effects层+Situational层来计算的,严禁在代码中硬编码固定战斗属性值

CharacterTemplate（角色模板）的定位与职责
- 单一职责：ScriptableObject 仅作为“配置与规则”的权威来源，绝不直接充当运行时数值容器。
- 初始化来源：角色实例（CharacterStats）在 Awake/InitializeFromTemplate 阶段，从模板拷贝基础信息（名称/职业/等级/六维/基础AC），
并按本规范的生命值公式计算 MaxHP/CurrentHP。
- 规则查询：战斗/系统仅从模板读取“静态规则与表项”，包括但不限于：
  - primarySpellAbility / defaultAttackType / defaultCantrip（默认戏法与施法主属性）
  - 熟练项与熟练加值（按等级段（Lv1-4:+2，5-8:+3，9-12:+4，…））
  - baseArmorClass / immunities / resistances / vulnerabilities（基础AC与免疫/抗性/弱点）
- 运行时权威：一切“当前值/可变值”（如 currentHitPoints、temporaryHitPoints、armorClass、六维、状态效果）一律以实例 CharacterStats 
为唯一权威；模板不参与当下数值运算。
- 修改影响：运行中修改模板不会回溯已生成角色实例；如需生效必须在实例侧显式重算（如 RecalculateMaxHitPointsFromLevel / 
UpdateArmorClass 等）。
- 挂载约束：每个角色预制体必须手动挂载 CharacterStats，并在其 template 字段手动引用对应 CharacterTemplate；禁止在代码中 AddComponent 
或自动查找。
- 战斗读取约定：HorizontalCombatRules 使用 CharacterStats 读取即时数值（命中检定所需的能力调整、目标 AC、当前 HP 等），同时仅用模板
提供的“规则项”（主施法属性、熟练、默认戏法、抗性/弱点等）。
- 与装备系统对齐：当 ICombatSource（武器/法术）路径落地后，命中与伤害参数由“装备/法术源”单一路径提供；模板继续提供熟练与主施法等规则项，
不得新增第二套并行路径。
- 角色模板上应该有对应的护甲，武器等熟练与否配置项
  如无武器熟练则无法装备对应武器/如无护甲熟练则无法穿戴对应护甲
- Base层角色天生有物理攻击手段（双拳攻击，命中和伤害加值用力量调整值，伤害骰1d4） 
无护甲时（盾也算护甲)天生AC取基础AC10+敏捷调整值

属性修正与叠加 — 统一规则参考（实现前置设计）
- 单一权威：运行时一切“当前值/可变值”仅以实例 `CharacterStats` 为准；模板/种族/职业/专长/装备/BUFF/DEBUFF 都作为“修正源”，统一汇总到 `CharacterStats` 后输出给战斗系统与UI。

一）来源层级（自上而下应用）
1. Base 基础出生值（来自模板，初始化到实例）
   - 名称/职业/等级/六维/基础AC/抗性集合等。
2. Permanent 永久成长/被动（实例长期生效）
   - 等级带来的 Proficiency Bonus、职业特性（Class Features）、ASI、已选择的专长（Feats）、种族天赋（Racial Traits）。
3. Equipment 装备（WhileEquipped）
   - 武器/护甲/盾牌/饰品等的静态与条件修正；护甲可覆盖 AC 计算公式；武器/法术作为 ICombatSource 提供命中/伤害参数。
4. Effects 临时效果（TimedSeconds/TimedRounds/UntilRest/WhileConcentrating）
   - BUFF/DEBUFF/光环等；支持持续时间、栈策略、条件（如“穿重甲时才生效”）。
5. Situational 情境瞬时（本轮或本动作）
   - 优/劣势、距离/姿态判定、专注检定结果等。

二）修正器模型（StatModifier，统一数据契约）
- 字段建议：
  - stat: StatType（STR/DEX/CON/INT/WIS/CHA/AC/MaxHP/AttackBonus/DamageBonus/SavingThrowX/Speed/Resist/Immune/Vulnerable/AdvantageX/DisadvantageX…）
  - op: ModifierOp（Add/Multiply/Override/Flag）
  - value: 数值或枚举（按 stat 类型解释）
  - source: 来源对象（Trait/Feat/Item/Effect/Skill 等）
  - stackKey: string（同源/同类唯一键；用于“相同效果不叠加”或“取最大/最新”）
  - policy: 冲突策略（Max/Min/Sum/Replace）
  - duration: {type: TimedSeconds/TimedRounds/WhileEquipped/WhileConcentrating, amount}
  - conditions: 可选条件（例：穿重甲、持盾、距离≤X、目标在光环内等）

三）叠加与优先级
- 应用顺序：Base → Permanent → Equipment → Effects → Situational（后者可覆盖前者）。
- 运算规则：
  - Add：相加；Multiply：连乘（少用）；Override：替换（如护甲AC公式）；Flag：优势/劣势/免疫/抗性/易伤等布尔或集合。
  - 同 stackKey 的效果遵循 policy；不同来源不同 stackKey 可叠加。
- 特例约定：
  - 优势/劣势不叠加：多个优势仍视为1个优势；优势与劣势相互抵消。
  - 护甲 AC 覆盖优先于基础AC；盾牌等 Additive 加在覆盖结果上。
  - 光环（Aura）按来源去重或取最大（由 stackKey/policy 控制）。

四）计算顺序（一次重算，全局生效）
1. 六维（STR~CHA）
2. 熟练加值（Proficiency，按等级段）
3. MaxHP 按既定公式增长
4. AC（基础/护甲覆盖/盾牌/姿态/临时）
5. 抗性/免疫/易伤集合合并
6. 命中/伤害参数：从 ICombatSource 取（能力类型/熟练/伤害骰/标签），结合最终六维/熟练得出命中加值与伤害修正
7. 旗标：Advantage/Disadvantage/不可行动/隐形/集中等布尔值

五）实时更新（事件驱动）
- 触发重算：LevelUp、Feat 选择、ASI、装备变化、效果增减/到期、姿态切换、专注失败等。
- 执行：标记 Dirty → RecalculateAll → 触发 `OnStatsChanged(finalSnapshot)`；已有 `OnHealthChanged` 继续用于HP。
- 联动：
  - UI 面板/血条订阅事件刷新
  - 战斗系统读取“最终快照”（或直接读 `CharacterStats` 的已汇总字段）

六）典型规则清单（参考实现）
- 人类（Racial Traits）
  - 普通人类：六维 +1（Add）
  - 变体人类：任意两项 +1（Add）+ 赠送 1 个专长（Feat）
- 职业与熟练
  - 主施法属性：来自模板 primarySpellAbility，仅用于确定命中能力类型；数值来自实例最终六维
  - 熟练加值：按等级段（Lv1-4:+2，5-8:+3，9-12:+4，…）
  - 职业特性：如重甲熟练（允许护甲覆盖AC）、额外攻击（规则侧消费，不做数值；或通过 ICombatSource 提供多次打击）
- 装备
  - 武器：
    - Finesse：命中可用DEX替代STR（取更优或按规则固定）
    - 远程：命中用DEX
    - 伤害：由武器伤害骰 +（近战默认加 STR 调整）
    - 熟练：若武器类别/类型熟练，叠加 Proficiency
  - 法术：默认戏法 `defaultCantrip` 提供伤害骰；命中用主施法属性调整 + 熟练（若熟练）
  - 护甲：覆盖 AC 公式（Override），盾牌：AC +2（Add）
- BUFF（Effects）
  - Bless：命中/豁免 +1d4（可简化为 +2 命中/+2 豁免期望值）
  - Dodge（防御姿态）：AC +2 或对方命中劣势（二选一，遵循当前项目既有定义）
- DEBUFF（Effects）
  - Poisoned：攻击与检定劣势（Flag）
  - Weakened：近战伤害减半（Multiply 0.5）或固定 -X（Add，简化模型）
  - Restrained/Slowed：移动/命中/AC/豁免的综合不利（按需要拆解为多个修正器）
- 抗性/免疫/易伤
  - 基础来自模板；效果或装备可临时增减；结算在 `ApplyDamageToSelf` 前判定并调整伤害
- 升级（LevelUp）
  - MaxHP 按既定公式增长
  - 指定等级节点触发 ASI 或 Feat 选择
  - 解锁职业特性与更高戏法伤害骰

七）边界策略
- MaxHP 下降：仅夹住 currentHP≤newMax，不做额外扣血
- CON 改变：MaxHP 统一按“全等级重算并夹住”或“差额法”二选一，保持一致性（建议：全等级重算并夹住）
- 优/劣势：不叠加；优势与劣势互相抵消为“正常”
- 专注（Concentration）：以 `Concentrating` 标志表示；受伤时进行专注检定，失败移除对应效果
- 持续时间：
  - TimedRounds 由 AutoBattleAI 的回合推进减少
  - TimedSeconds 由计时器减少

八）实现落地路线图（建议）
1. 在 `CharacterStats` 内引入修正聚合器（ModifierAggregator）与 `ActiveEffects` 容器
2. 定义 SO：RaceTraitSO / ClassFeatureSO / FeatSO / EffectSO / ItemBaseSO（含修正器列表）
3. 确立 ICombatSource（武器/法术），`HorizontalCombatRules` 仅用该接口提供的命中/伤害参数
4. 提供 RecalculateAll 与 `OnStatsChanged` 事件；UI与AI订阅刷新
5. 逐步用修正器替换零散“直接改字段”的逻辑，保留最小向后兼容

- 绝对约束（与本文其他章节保持一致）
  - 只保留一条命中/伤害流程：ICombatSource → ResolveAttack
  - 运行时权威仅在 `CharacterStats`；模板/物品/专长/效果仅作为修正源
  - 强制手动挂载；禁止自动 AddComponent；禁止在文档之外另起新规范

---

更新：装备与徒手结算规范（2025-10-26)

为统一战斗数值计算与项目配置，新增并确认如下规范：

- 计算命中/伤害/AC 时，仅读取 `CharacterEquipment` 的装备槽：
  - 武器：主手 `mainHand`或者双手 `twoHanded`，视物品标签而定。
  如果主手装备双手武器，则副手槽位无效。如果主手装备单手武器，则副手槽位可以装备盾牌或另一把单手武器（若熟练则可双持攻击）。
  - 护甲：`armor`
  - 盾牌：`shield`
- 若对应槽位未装备，则：
  - 攻击按“徒手”处理（见下文可配置），不再从背包挑选任何物品。
  - AC 按“未着甲公式”处理：`baseArmorClass + DexMod (+ 盾牌加值，若装备盾)`。
- 背包（`CharacterInventory`）中的未装备物品不参与任何战斗数值结算（不再有“背包即装备”的回退逻辑）。

## 徒手伤害可配置

在 `CharacterTemplate` 上配置徒手相关参数，用于当未装备武器时的伤害骰、能力修正、熟练与伤害类型：

- `unarmedDamageDice: DiceFormula` - 徒手攻击伤害骰（如 1d4、1d6 等）
- `unarmedDamageAbilityMode: PhysicalHitAbilityMode` - 徒手伤害与命中使用的能力类型
  - `Strength`（默认）- 仅使用力量
  - `Dexterity` - 仅使用敏捷
  - `BestOfStrDex` - 在 STR/DEX 中取较优
- `unarmedProficient: bool` - 徒手攻击是否具有熟练加值（true 时命中和伤害均加熟练加值，false 时无加值）
- `unarmedDamageType: DamageType` - 徒手攻击的伤害类型（Bludgeoning/Slashing/Piercing/等，支持灵活配置）
  - `Bludgeoning`（默认）- 钝击（普通拳击）
  - `Slashing` - 挥砍（爪子、利刃）
  - `Piercing` - 穿刺（尖刺、獠牙）
  - 其他类型 - 如需特殊伤害类型（Force/Psychic/等）

说明：
- 该配置对所有角色（玩家、队友、敌人）均适用，支持不同角色有不同的徒手规则
- 爪子怪物可配置 `unarmedDamageType = Slashing`；獠牙怪物可配置 `Piercing`；标准拳击保持 `Bludgeoning`
- 法术普通攻击依旧按模板 `defaultCantrip` 与主法术属性（`primarySpellAbility`）处理，与徒手配置无关
- 物理普通攻击（有武器）按武器的 `isFinesse / weaponHitAbilityMode / weaponDamageDice / weaponDamageType` 与模板熟练计算，不受徒手配置影响


## 法术与骰子系统


### SpellData - 法术数据 SO（Assets/demo2/DND/SpellData.cs）

**用途**：定义普通法术/戏法的伤害骰、伤害类型、等级升级规则等，在 CharacterTemplate.defaultCantrip 中引用

**关键字段**：
- `spellName` - 法术名称（如 "Fire Bolt"）
- `baseDamageDice` - 基础伤害骰（diceCount/diceSize，如 1d10）
- `damageType` - 伤害类型（Force/Fire/Cold/Psychic 等）
- `upgradeAtCantriplevel` - 是否按施法者等级升级伤害骰（勾选）
- `upgradeEntries[]` - 升级规则数组：
  - [0] characterLevel=1, upgradedDice=1d10
  - [1] characterLevel=5, upgradedDice=2d10
  - [2] characterLevel=11, upgradedDice=3d10
  - [3] characterLevel=17, upgradedDice=4d10

**配置步骤**：
1. 右键 Create → DND → Spell Data
2. 配置基本信息（spellName、baseDamageDice、damageType）
3. 勾选 upgradeAtCantriplevel，填充升级规则

### DiceFormula - 骰子公式（Assets/demo2/DND/DiceFormula.cs）

为补完现有代码中被广泛使用但之前缺失的定义：ItemBaseSO 中 `weaponDamageDice`、CharacterTemplate 中 `unarmedDamageDice`、HorizontalCombatRules 中伤害骰计算均使用此类型。

Serializable class，用于表示 DND5E 中的伤害骰数（如 1d6、2d8+3）。支持在 Inspector 中编辑。

---

## 相关实现位置

- 普通攻击/伤害结算：`HorizontalCombatRules.cs`
  - `ResolveAttack()` - 主攻击流程，调用 GetAttackBonus 和 CalculateDamageUnified；伤害类型分配逻辑：
    ```csharp
    r.damageType = isSpell
        ? ((attacker.template != null && attacker.template.defaultCantrip != null) ? attacker.template.defaultCantrip.damageType : DamageType.Force)
        : (weapon != null ? weapon.weaponDamageType : (attacker.template != null ? attacker.template.unarmedDamageType : DamageType.Bludgeoning));
    // 徒手时：读取 template.unarmedDamageType，若无模板则默认 Bludgeoning
    ```
  - `GetAttackBonus()` - 计算命中加值，徒手情况下根据 `unarmedDamageAbilityMode` 选择能力：
    ```csharp
    var mode = c.template.unarmedDamageAbilityMode;  // Strength / Dexterity / BestOfStrDex
    switch (mode)
    {
        case Dexterity: abilityNameForHit = "dexterity"; break;
        case BestOfStrDex: abilityNameForHit = (dex > str) ? "dexterity" : "strength"; break;
        default: abilityNameForHit = "strength"; break;
    }
    int mod = (abilityNameForHit == "dexterity") ? dex : str;
    int prof = c.template.unarmedProficient ? 熟练加值 : 0;
    return mod + prof;
    ```
  - `CalculateDamageUnified()` - 计算伤害，徒手时读取 `unarmedDamageDice` 骰数和 `unarmedDamageAbilityMode` 能力修正
  - 特点：仅读取装备槽；未装备时完全由 `CharacterTemplate` 四个参数驱动（unarmedDamageDice / unarmedDamageAbilityMode / unarmedProficient / unarmedDamageType）
  - 确保：命中、伤害、伤害类型逻辑完全一致，不同角色可配置不同徒手规则
- AC 计算：`ModifierAggregator.cs`
  - 仅读取装备槽中的护甲/盾牌
  - 未穿甲按未着甲规则：AC = baseArmorClass(来自模板) + DexMod

- 法术攻击系统：`HorizontalCombatRules.cs` - `GetAttackBonus()` 和 `CalculateDamageUnified()`
  - 攻击检定：读取 `template.primarySpellAbility` 决定使用哪个属性修正（默认 intelligence）
    ```csharp
    abilityNameForHit = NormalizeAbilityName(c.template != null ? c.template.primarySpellAbility : "intelligence");
    int mod = GetAbilityModifierFromSnapshot(snap, c, abilityNameForHit);
    ```
  - 伤害计算：读取 `template.defaultCantrip` 获取伤害骰，按等级返回升级后的骰数；伤害**仅为纯骰子结果**（不加属性修正）
    ```csharp
    DiceFormula dice = c.template.defaultCantrip.GetDamageDiceAtCasterLevel(c.Level);
    // 返回 rolledTotal，不做任何属性修正
    ```
  - 伤害类型：由 `template.defaultCantrip.damageType` 决定
  - 熟练：由 `template.IsProficientForAttack(true, isMelee)` 决定是否加熟练加值

- 背包与装备：
  - `CharacterEquipment.cs`：仅"已装备"才生效，负责给 `CharacterStats` 添加/移除装备来源的修饰
  - `CharacterInventory.cs`：背包变更只会驱动装备槽校正与重新应用，未装备物品不参与战斗数值

---

## 架构重构计划：背包与UI系统 (事件总线驱动)

| 文档 | 位置 | 内容 | 何时查看 |
|------|------|------|---------|
| **法术SO配置流程** | `法术SO配置流程.md` | 详细的6步配置指南 + 检查清单 | 首次创建法术SO |
| **法术系统可视化流程** | `法术系统可视化流程.md` | 系统架构图、数据流追踪、错误排查 | 深入理解系统 / 调试问题 |
| **本文（规范）** | `DEVELOPMENT_STANDARDS.md` | 快速配置 + 技术实现细节 | 日常参考 |

---

## 🔗 快速导航

### 我想要...

#### 快速上手法术系统（3分钟）
→ 查看本文的"快速配置流程（3分钟上手）"部分

#### 完整、逐步的配置教程
→ 打开 `法术SO配置流程.md`

#### 理解法术系统的数据流
→ 打开 `法术系统可视化流程.md` → 查看"数据流向跟踪"部分

#### 排查法术攻击不生效的问题
→ 打开 `法术系统可视化流程.md` → 查看"常见错误和排查"部分

#### 知道某个字段的具体作用
→ 本文的"法术与骰子系统"部分有详细字段说明

**目标**：为支持未来的营地建造与策略玩法，将现有紧耦合的 `InventoryUIBinder` 拆分为基于“事件总线”的高度解耦架构。

**核心思想**：用 `ScriptableObject` 作为事件通道，让不同系统（UI、背包逻辑、角色管理）通过发布/订阅事件进行通信，而不是直接引用。

### 实施步骤

#### 第 0 步：搭建事件总线基础设施
1.  **创建通用事件通道脚本**:
    - `EventChannelSO.cs` (无参数)
    - `EventChannelSO<T>.cs` (带泛型参数)
2.  **创建具体事件资产**:
    - 在项目中创建一系列事件通道资产，如 `RequestEquipItemChannel.asset`, `InventoryChangedChannel.asset`, `ActiveCharacterChangedChannel.asset` 等。

#### 第 1 步：剥离纯粹的“视图”层 (`InventoryView`)
- **职责**: 严格限定 `InventoryGridView` (可更名为 `InventoryView`) 只负责UI渲染和捕获用户输入。
- **行为**:
  - **发布**: 当用户点击UI时，发布“请求”类事件 (如 `RequestEquipItem`)。
  - **订阅**: 监听“数据变更”类事件 (如 `InventoryChanged`) 来刷新界面。

#### 第 2 步：创建独立的“控制器”层 (`InventoryController`)
- **职责**: 创建新脚本 `InventoryController.cs`，用于处理所有背包相关的业务逻辑。
- **行为**:
  - **订阅**: 监听所有“请求”类事件。
  - **发布**: 在处理完逻辑并修改数据后，发布“数据变更”类事件。
- **挂载位置**: 作为全局逻辑处理器，挂载在场景的核心管理器对象上 (如 `IdleGameSystem`)。

#### 第 3 步：创建“当前角色管理器” (`ActiveCharacterManager`)
- **职责**: 创建新脚本 `ActiveCharacterManager.cs`，作为全局单例，唯一职责是维护当前玩家选中的角色。
- **行为**:
  - **发布**: 当玩家切换角色时，发布 `ActiveCharacterChanged` 事件。
  - **订阅者**: `InventoryController` 和 `InventoryView` 等关心当前角色的系统都将订阅此事件，以切换其操作/显示目标。
- **挂载位置**: 挂载在核心管理器对象上 (如 `IdleGameSystem`)。

#### 第 4 步：`InventoryUIBinder` 的退役
- **最终归宿**: 在上述职责被完全分离后，原 `InventoryUIBinder` 脚本将被重构、简化或彻底删除，其功能由新的专用脚本各司其职。

### 事件总线实施进度（背包/角色选择体系）
- 第0步（基础事件通道脚本）: 已完成。`EventChannelSO` / 泛型版本 已投入使用。
- 第1步（视图层职责剥离）: 基本完成（2025-11-13）。`InventoryUIBinder` 已接入事件通道：
  - 订阅 `InventoryChangedChannel_SO`（当前激活来源变更时刷新网格与属性UI）。
  - 订阅 `ActiveCharacterChangedChannel_SO`（当前角色切换时自动切换至其 `CharacterInventory`）。
  - 默认启用 `enableEventChannels=true`；兼容保留对 `CharacterInventory.OnInventoryChanged` 的直接订阅作为兜底，避免丢刷新。
- 第2步（控制器层建立）: 已创建 `InventoryController`，订阅 `RequestEquipItemChannel_SO`，发布 `InventoryChangedChannel_SO`。
- 第3步（当前角色管理器）: 已创建 `ActiveCharacterManager`，发布 `ActiveCharacterChangedChannel_SO`。
- 第4步（旧 Binder 退役）: 准备中。待完全迁移到“请求/变更”双向事件模型后，逐步移除 Binder 对数据源的直连订阅与遗留职责。

【进度确认（2025-11-13）】
- 代码侧闭环已打通：视图（UI）→ 请求通道（装备/卸下）→ 控制器执行业务 → 变更通道广播 → 视图刷新；当前 UI 还保留直连兜底订阅以确保稳定。
- 需要在 Inspector 中完成资产引用（见下方“快速配置核对”），否则事件化刷新不会生效。

### 下一阶段迁移动作（计划）
1) Binder 去耦与收敛
- 逐步取消 `InventoryUIBinder` 对 `CharacterInventory.OnInventoryChanged` 的直接订阅，改为仅依赖 `InventoryChangedChannel` + `ActiveCharacterChangedChannel` 驱动刷新（保留短期开关用于回滚）。
- 梳理 Binder 的导航/状态职责，确保翻页/显隐统一由 `UITabSwitcher` 持有；Binder 专注“绑定与刷新”。

2) UI 只发布请求，不越权执行业务
- `InventoryItemView` 的右键菜单/按钮交互统一通过 `RequestEquipItemChannel_SO` 发送 `Equip/Unequip/Toggle`，禁止直接触达 `CharacterEquipment`。

3) 刷新路径统一与抖动优化
- Grid 刷新触发仅来自 `InventoryChangedChannel` 与 `ActiveCharacterChanged`；减少 `Start()/SetActiveSourceIndex()` 中的强制重建，优先差量更新（必要时保留 `ClearAndRebuild` 作为开关）。

4) 验收用例（必须通过）
- 切换当前角色 → 背包面板自动切换到该角色的 `CharacterInventory` 且属性面板同步。
- 装备/卸下任意物品 → 控制器处理 → 广播变更 → 背包网格与“已装备”标签/属性 UI 一致更新（无重复/遗漏）。
- 在未配置事件资产时，至少不报错且直连兜底路径仍可刷新（打印警告日志提醒配置缺失）。

5) 故障排查最小清单
- 确认三项资产是否正确引用：`RequestEquipItemChannel.asset` / `InventoryChangedChannel.asset` / `ActiveCharacterChangedChannel.asset`。
- 确认 `InventoryUIBinder.enableEventChannels=true` 且面板中 Binder 已指向上述两个通道资产。
- 确认场景存在唯一 `InventoryController` 与 `ActiveCharacterManager`，且其字段已拖拽到对应资产。

【快速配置核对（事件总线）】
- `InventoryUIBinder`（每个背包UI实例）：勾选 `enableEventChannels`，拖入 `InventoryChangedChannel.asset` 与 `ActiveCharacterChangedChannel.asset`。
- `InventoryController`（全局）：拖入 `RequestEquipItemChannel.asset` 与 `InventoryChangedChannel.asset`。
- `ActiveCharacterManager`（全局）：拖入 `ActiveCharacterChangedChannel.asset`。
- 角色预制体：继续手动挂载 `CharacterStats` / `CharacterEquipment` / `CharacterInventory`。

【追加 - 2025-11-13 完成事项】
- 已执行旧路径退役：`InventoryUIBinder` 取消直接订阅 `CharacterInventory.OnInventoryChanged`，刷新现由事件通道驱动（`InventoryChangedChannel_SO` / `ActiveCharacterChangedChannel_SO`）。
- Binder 内部新增在 TryAddNew/Remove 成功后主动 RaiseEvent 逻辑，确保外部不依赖旧订阅即可获得刷新。
- 下一阶段：观察是否仍需保留 fallback 日志；若运行一段时间未出现遗漏刷新，再移除相关警告与临时变量。

---

物品掉落业务逻辑（精炼）

目标
- 击败怪物 → 评估掉落 → 通过事件添加到玩家背包 → UI 刷新（单一路径，杜绝重复）。

数据对象
- ItemDropTableSO（掉落表）
  - DropEntry: item(ItemBaseSO), dropChance(0-1), minAmount, maxAmount, guaranteed
  - 评估：guaranteed=true 则必掉；否则按 dropChance 判定；数量为闭区间 [minAmount, maxAmount] 的整数。min=max=1 时只产生 1 件。
- EnemyDropSource（挂在敌人）
  - 字段：dropTable, autoEvaluateOnDeath, delegateToManager, requestAddItemChannel(直发), overrideTargetInventory(可空)
  - 触发：HP≤0 且未发放过 →
    - 管理器分发（推荐）：调用 LootDropManager.RegisterDeath(this)
    - 直接分发：逐条 RaiseEvent(InventoryAddItemRequest)
- LootDropManager（全局唯一）
  - 聚合：可批量合并时间窗（batchDispatch + batchWindowSeconds）后统一分发
  - 目标背包解析：preferFormationFirstAlive 优先阵型首个存活角色 → 否则扫描第一个 battleSide=Player 的 CharacterInventory
  - 输出：对每条掉落调用 RequestAddItemChannel.RaiseEvent(InventoryAddItemRequest)
- InventoryController（全局唯一）
  - 订阅 RequestAddItemChannel，实际向数据源添加 ItemInstance，并广播 InventoryChangedChannel 供 UI 刷新
- InventoryUIBinder（UI）
  - 默认不处理拾取事件：handleAddItemEvents=false，仅监听 InventoryChangedChannel 做显示刷新；若必须由 UI 直接落地，谨慎开启且确保控制器不重复处理

事件通道
- RequestAddItemChannel_SO：载荷 InventoryAddItemRequest(inventory, item, amount)
- InventoryChangedChannel_SO：载荷 CharacterInventory（通知 UI 刷新）
- ActiveCharacterChangedChannel_SO：可选，用于 UI 切换显示来源背包

配置约束（单一路径，禁止重复）
- 仅允许：EnemyDropSource →（可选 LootDropManager）→ RequestAddItemChannel → InventoryController
- 禁止 UI 与控制器同时处理拾取：确保 InventoryUIBinder.handleAddItemEvents 关闭（默认关闭）
- 场景中 InventoryController、LootDropManager 各仅一份；EnemyDropSource 推荐 delegateToManager=true

故障排查
- 掉落 1 件却入包 2 件：多订阅同一拾取事件（常见：UI 与控制器同时处理，或“直发+代管”叠加）。处理：关闭 UI handleAddItemEvents，检查是否重复挂载/双路径并用。
- 未入包：检查 RequestAddItemChannel 是否注入管理器/控制器；确认能解析到目标背包（battleSide=Player）。

验收清单
- min=max=1 的掉落只产生 1 条 InventoryAddItemRequest
- 击败敌人时日志仅出现一次“已分发/已添加”记录
- UI 通过 InventoryChangedChannel 刷新，无重复生成

背包和槽位系统 — 战斗集成规范（简版）
- 角色槽位定义：每个槽位只能被一件装备占用，且每个装备只能占用特定槽位
  - 主手（MainHand）：单手武器/双手武器
  物品标签 TwoHanded 标记为双手武器，此时将占用主手和副手槽位
  - 副手（OffHand）：单手武器/盾牌
  物品标签 Shield 标记为盾牌，只能装备在副手槽位且与副手单手武器互斥，如果主手装备双手武器，则副手槽位无效，
  如主手装备单手武器，则副手槽位可以装备另一把单手武器（若熟练则可双持攻击）。
  - 护甲（Armor）：轻甲/中甲/重甲
  - 头盔（Helmet）
  - 护手（Gauntlets）
  - 靴子（Boots）
  - 项链（Necklace）
  - 戒指（Ring）
  - 腰带（Belt）
  - 披风（Cloak）

### 装备槽位编码范式重构方案

#### 修改目标
- 统一装备槽位的管理方式，提升代码的可维护性和扩展性。
- 避免硬编码装备槽位，减少潜在的逻辑错误。

#### 修改步骤

1. **新增枚举类型 `EquipmentSlot`**
   - 在 `GameEnums` 中新增 `EquipmentSlot` 枚举，定义所有标准装备槽位，例如：
     ```csharp
     // Centralized enum (authoritative) — defined in Assets/demo2/DND/GameEnums.cs
     public enum EquipmentSlot {
         MainHand,   // Weapon (primary hand)
         OffHand,    // Secondary hand / Shield
         Armor,      // Chest
         Helmet,     // Head
         Gauntlets,  // Hands
         Boots,      // Feet
         Necklace,
         Ring,
         Belt,
         Cloak
     }
     ```
目前武器/盾牌/护甲等装备本身自带基础属性加成（默认的武器伤害骰和护甲/盾牌的AC等），角色装备后这些属性会被自动应用到角色属性计算中。
其他槽位的装备无默认的伤害/AC值，但可以预留未来扩展使用（根据对应的ItemSO上实际的填入的额外属性数值增加属性加成等）。

装备槽位的定义必须和战斗结算逻辑保持一致，确保装备的武器/护甲/盾牌和其他带有额外属性的角色其他槽位等能正确影响角色的战斗属性。
接口`CharacterEquipment`负责管理角色的装备槽位和装备物品。

装备槽位只是逻辑上定义，在UI上依旧使用之前的背包UI,不另外开辟一个UI界面专门显示不同身体装备槽位。

2. **修改角色装备管理逻辑**
     - 在角色管理类（如 `CharacterStats` 或 `InventoryManager`）中：
         - 替换原有的装备槽字段（如 `headItem`, `chestItem` 等）为 `Dictionary<EquipmentSlot, ItemInstance>`。
         - 示例：
           ```csharp
           private Dictionary<EquipmentSlot, ItemInstance> equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();
           ```

3. **实现装备槽操作方法**
   - 在角色管理类中新增以下方法：
     - 装备物品：`EquipItem(EquipmentSlot slot, ItemInstance item)`
     - 卸下物品：`UnequipItem(EquipmentSlot slot)`
     - 获取当前装备：`GetEquippedItem(EquipmentSlot slot)`

4. **更新装备判定规则**
   - 修改所有依赖装备槽位的逻辑，确保使用 `EquipmentSlot` 枚举进行判断。
   - 示例：
     ```csharp
     if (equippedItems.ContainsKey(EquipmentSlot.Weapon)) {
         // 执行武器相关逻辑
     }
     ```
 

#### 注意事项
- **最小改动原则**：所有修改必须在不破坏当前逻辑的基础上进行。
- **系统一致性**：确保新增功能与现有系统风格一致。
- **代码质量**：所有改动必须通过全项目编译检查，确保无任何报错。
- **需求对齐**：严格按照用户需求执行，避免自行扩展或修改需求。

#### 需求清单
- 修改 `DND_CharacterAdapter` 和 `SkeletonAnimationController`，支持新功能。
- 调整 `GameManager` 的业务调用链，集成新功能。
- 编写单元测试并验证功能。

角色换装系统设计思路:
目的:实现游戏开始时候的角色自定义换装功能的UI界面,玩家通过选择对应的部位装备的icon来拼接成一个完整的角色形象.
并且后续可以通过战斗中掉落的服装部件来更换角色的外观.
需求：1.一个定制角色(有选择服装/头发/配饰/颜色等的UI操作界面),可以通过选择不同的部件来更换角色的外观.
     2.一套符合Spine里使用的皮肤和attachment素材导入unity生成的SkeletonAnimation的对应属性使用的换装系统
(素材使用的是Spine示例工程中的Mix-and-match角色,素材做好了一套骨架和多个皮肤部件)
Spine骨骼动画系统支持动态替换骨骼的附件（Attachment），可以利用这一特性实现角色的换装功能。
当前为根据Spine素材样式测试，所以先设置以下的部位槽位(和游戏最终要实现的DND规则战斗机制所需的物品槽位有区别)
先参考下面临时槽位实现，之后确认跑通后再替换成实际游戏机制所需

具体设计代码实现思路如下：
--Spine中每一个皮肤代表一个部件,每个皮肤都会对应在unity中的SkeletonData一个Skin
--然后通过SkeletonAnimation上的Initial Skin来切换，但无法同时存在2个不同的皮肤显示配置来组合成一个搭配
--unity中通过配置SkinConfig(自定义SO)来进行创建
    定义2个空的列表：第一个是皮肤的槽位skinSlots
        皮肤skinID(唯一)
        类型(定义SkinBodyPartType枚举,根据Spine素材有几种不同的皮肤槽位来区分)
            暂时设置以下几种部件:
            accessory(配饰)
            clothes(衣服)
            eyelids(眼皮)
            eyes(眼睛)
            full-skins(全身)
            hair(头发)
            legs(腿)
            nose(鼻子)
        皮肤的名字(用来在unity的UI上展示给玩家)
        叠加的颜色(用来给皮肤和服装定制化染色)
    第二个是动画的集合skinAnimations
        动画的名字
        类型
        此处要结合工程中专门负责读取动画的DND_CharacterAdapter.cs来进行对应调整，因为目前游戏的状态机是自己定义的。
目标是希望换装界面完成后可以兼容现有的角色动画系统,并且可以通过换装界面来更换角色的外观.游戏中获得新的武器服装配饰等也可以通过这个系统来更换角色外观.

---

## 具体代码修改步骤

### 第1步：在GameEnums.cs中新增枚举

**文件位置**：`Assets/demo2/DND/GameEnums.cs`

**操作**：在现有枚举定义之后添加以下代码

```csharp
// ...existing enums...

/// <summary>
/// 角色换装系统 - 身体部件类型枚举
/// </summary>
public enum SkinBodyPartType {
    Hair,      // 头发
    Clothes,   // 衣服
    Legs,      // 腿部
    Eyes,      // 眼睛
    Eyelids,   // 眼皮
    Nose,      // 鼻子
    Accessory, // 配饰
    FullSkin   // 全身套装（整套替换，无需组合）
}
```

**编译检查**：保存后检查IDE是否有语法错误。

---

### 第2步：创建SkinConfig.cs（新文件）

**文件位置**：`Assets/demo2/DND/Character/SkinConfig.cs`

**职责**：定义皮肤部件的配置表SO，包含SkinPartEntry序列化类和查询接口

**设计要点**：
- SkinPartEntry包含：skinID、partType、displayName、overlayColor、previewIcon
- SkinConfig作为ScriptableObject存储所有部件列表
- 提供按ID和按类型的查询接口

**关键接口**：
- GetPartBySkinID(skinID) - 按ID查找部件
- GetPartsByType(partType) - 按类型查询所有部件
- GetAllParts() - 获取全部部件列表
- GetSkinParts() - 编辑器用接口（用于Inspector编辑）

---

### 第3步：创建CharacterAppearance.cs（新文件）

**文件位置**：`Assets/demo2/DND/Character/CharacterAppearance.cs`

**职责**：管理角色的部件组合状态，维护当前皮肤配置

**设计要点**：
- 维护Dictionary<SkinBodyPartType, skinID>存储当前部件组合
- 监听SetPart请求，动态更新Spine皮肤
- 不清理Animation轨道（保持动画连贯）
- 仅修改皮肤，不涉及动画状态
- baseSkin作为打底（包含非遮挡区域的人体）
- combinedSkin动态创建（baseSkin + 各部件皮肤）

**关键方法**：
- SetPart(partType, skinID) - 修改指定部位皮肤，发布OnAppearanceChanged事件
- GetCurrentPart(partType) - 获取当前部位皮肤ID
- ApplyAppearanceToSkeleton() - 应用外观配置到Spine

**事件**：
- OnAppearanceChanged - 外观改变事件

---

### 第4步：修改DND_CharacterAdapter.cs（极小改动）

**文件位置**：`Assets/demo2/DND/DND_CharacterAdapter.cs`

**修改点**：在`PlayAnimation`方法的开始处添加皮肤检查钩子

**目的**：确保在播放动画前，Spine骨架与当前皮肤保持一致，防止皮肤与骨架错位

**修改逻辑**：
- 获取同对象的SkeletonAppearanceManager组件
- 若存在，调用EnsureSkinApplied()确保皮肤已应用
- 然后继续原有的动画播放逻辑

---

### 第5步：创建CharacterCustomizationPanel.cs（新文件）

**文件位置**：`Assets/demo2/DND/UI/CharacterCustomizationPanel.cs`

**职责**：角色换装UI面板 - 显示部件选项，处理用户交互

**设计要点**：
- 作为UI层，仅负责显示和输入
- 遍历SkinConfig中的所有部件，按部件类型（SkinBodyPartType）分类显示
- 为每个部件创建UI按钮，显示previewIcon和displayName
- 按类型分组显示UI（Hair、Clothes、Legs等不同区域）

**主要功能**：
1. PopulateUI() - 遍历SkinConfig，生成分类UI部件列表
2. CreatePartButton(SkinPartEntry) - 为单个部件创建UI按钮
3. OnPartSelected(SkinPartEntry) - 用户点击部件时，调用targetAppearance.SetPart()驱动换装
4. OnConfirmClicked() / OnCancelClicked() - 确认/取消按钮回调

**关键字段**：
- skinConfig: SkinConfig - 皮肤配置资产（需手动拖入）
- targetAppearance: CharacterAppearance - 目标角色外观组件（需手动拖入）
- partsContainer: Transform - UI部件容器（存放生成的按钮）
- confirmButton / cancelButton: Button - 确认/取消按钮

**事件**：
- OnConfirm - 确认换装
- OnCancel - 取消换装

---

### 第6步：创建SkinConfig资产并配置部件列表

**操作步骤**：

1. 在Project窗口右键 → Create → DND → Skin Config
2. 命名为 `SkinConfig`（或其他名称）
3. 在Inspector中展开Skin Parts列表，添加部件条目
4. 对每个部件条目填入以下信息：
   - **Skin ID**：对应Spine皮肤名称（必须与Spine工程中的皮肤名完全一致，如 `hair_001`）
   - **Part Type**：选择该部件的类型（如 `Hair`、`Clothes`、`Eyes`等，来自SkinBodyPartType枚举）
   - **Display Name**：该部件在UI上的显示名称（如 `红色长发`、`蓝色衣服`）
   - **Overlay Color**：可选的叠加染色颜色（默认白色表示不染色，可用于动态调整皮肤颜色）
   - **Preview Icon**：可选的UI预览图标（在换装面板中显示该部件的缩略图）
5. 完成配置后保存资产

**部件命名规范**：
- Skin ID应与Spine导出的皮肤名保持一致
- Display Name用于玩家可读性，支持中文
- 同一Part Type下可有多个不同的Skin ID（如多种头发样式都属于Hair）

---

### 第7步：在角色预制体上挂载换装系统组件

**操作步骤**：

1. 打开角色预制体
2. 在层级中创建一个UI Canvas用于放置换装面板（或使用现有Canvas）
3. 在Canvas中创建一个Panel作为换装面板容器
4. 在该Panel上添加CharacterCustomizationPanel脚本
5. 在Inspector中配置以下字段：
   - **Skin Config**：拖入第6步创建的SkinConfig资产
   - **Target Appearance**：拖入角色身上的CharacterAppearance组件
   - **Parts Container**：拖入Panel中用于放置部件按钮的Transform容器
   - **Confirm Button**：拖入确认按钮
   - **Cancel Button**：拖入取消按钮
6. 确保角色身上挂载了CharacterAppearance组件（需在CharacterStats同级）

---

### 第8步：编译与功能测试

**编译检查**：
- 保存所有脚本
- 在Unity编辑器中查看Console和PROBLEMS面板
- 使用get_errors检查是否有编译错误
- 确保无编译错误（Error数 = 0）

**功能测试流程**：
1. 在场景中放置配置好的角色预制体
2. 运行游戏
3. 验证项目：
   - ✅ 角色外观是否正确加载（初始皮肤配置是否显示）
   - ✅ 换装面板是否正常显示（所有部件分类是否正确）
   - ✅ 点击部件按钮时角色外观是否改变
   - ✅ 换装时动画是否继续播放（不被打断）
   - ✅ 确认/取消按钮是否正常工作
4. 在运行时Inspector观察CharacterAppearance的部件字典是否正确更新
5. 检查Console中是否有警告日志（如皮肤未找到等）

由于当前Spine素材的制作习惯且通过Spine官方的插件脚本SkeletonAnimation来统一识别，所以需有一套指定的映射规则来确保Spine导出的皮肤和附件
能正确应用到Unity中的SkeletonAnimation组件上。
后续会灵活有所调整
Spine Skin Importer映射规则确认:
最终目的是玩家在角色捏人换装定制化的Customization UI界面选择不同的部件来更换角色外观时,Spine的SkeletonAnimation组件能正确识别并应用
对应的皮肤和附件.因此需要确认Spine导出的皮肤和附件名称与Unity中SkeletonData的Skins和Attachments的映射关系.
以当前示例的Spine骨骼和皮肤文件具体规则如下:
由于换装的主要目的是有机的组合Spine素材中设置的不同部件的皮肤,因此需要确保每个部件的皮肤名称在Spine和Unity中是一一对应的.
玩家在UI界面选择时，角色应该保持原骨骼的所有动画正常播放前提下，可以适配所有服装样式
玩家选择不同部件组合/整套套装时，会激活不同的对应条件(UI界面应该设置不同标签区分开整套和部件组合的选择)
因此，需要确保以下两者情况:
1.第一类是玩家使用UI界面时候，角色实际上有一个SkinBase(只有人脸和裸手臂)，其他部件都是通过附加的皮肤来覆盖显示.
 如这些类别(注意前缀):hair/legs/clothes/eyes/eyelids/nose/accessory通过这些附加导基础SkinBase上,强调玩家自定义组装。
2.第二类是玩家选择整套FullSkin(全身套装),这种情况下就不需要自定义组装，而是会用素材中默认配置好的一套服装直接替换掉基础SkinBase,不再叠加
其他部件.   

所以Spine Skin Importer映射规则应该根据这些规则来设置

UI换装界面需求细则:
界面左边有一个展示角色正常播放动画的人物界面，且界面下方有不同button可以切换几段素材里的动画，界面右边是不同的标签页，
不同的标签展示不同的部位分类(如头发/衣服/腿/眼睛/眼皮/鼻子/配饰/整套)，每个单独分类标签下则是不同物品服装的icon，玩家点击icon后，
角色的对应部位会更换成该部件的外观.

蒙皮与UI交互的规则:
CharacterAppearance组件负责管理角色的皮肤部件组合状态，并通过事件驱动与UI面板交互。
ApplyAppearanceToSkeleton() 方法确保每次部件更改后，Spine骨架的皮肤正确更新。
1.运行UI界面一开始，默认采取素材SkeletonData中Skin列表下full-skins内第一套皮肤作为初始皮肤,full-skins代表预设的整套服装.
使用full-skins类别下的皮肤会覆盖掉其他部件的显示,无需组合.
2.玩家点击其他分类标签(如hair/clothes/legs等)时,会切换到该分类下的部件icon列表.此时应该默认加载除了full-skins类别外的基础皮肤,默认
搭配每个部件类别下的第一个部件组合成为当前服装显示再结合当前玩家点击的散件更新到骨架,严禁缺失部件显示.
3.如玩家点击过full-skins后再点击散装的某个部件icon时,此时应清空之前组合的整套full-skins皮肤(如果之前选择过的话),
以确保当前是自定义组合模式.
4.如果玩家当前时自定义组合模式(非整套full-skins),则记录当前的组合，因为如果玩家再次点击full-skins后再次点击散装部件时,应该恢复之前记录
的散装部件的自定义组合状态+当下玩家选择的新部件进行显示.
