DND5E 系统 - 工程编码规范

严禁违反的核心规则

文档管理铁律
- 唯一技术文档: 本文档是项目唯一的技术规范文档
- 绝对禁止创建: 任何 markdown 指南、对比文档、debug 记录、纠错文档等
- 所有技术描述: 必须直接更新本文档的相应章节
- 保持项目简洁: 违反此规则将被视为严重错误

AI 助手工作规范
- 正文对话绝对不要使用英语：除非代码变量名/方法名/类名/技术名等必须使用英语，否则所有对话必须使用中文
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

系统一致性原则
- 系统一致性： 所有系统组件必须保持一致的设计风格和实现方式
- 统一命名规范: 所有脚本、类、方法和变量必须遵守统一的命名规范
- 代码风格统一: 所有代码必须遵循统一的代码风格和格式
- 新增功能集成: 所有新功能必须无缝集成到现有系统中，保持整体一致性，不要破坏现有逻辑，如之前逻辑中已经又类似方法，但是
增加新功能没在原本架构上扩展，而是重写新的方法导致2套不同的运行逻辑导致编译错误


开发行为规范
- 单一设计原则: 实现功能只提供一种设计方案
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
实际阵型位置由HorizontalBattleFormationManager中配置的spawn点Transform决定
前排/后排判断基于实际spawn点的X坐标，而非固定数值

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
-动画映射通过SO配置，方便非程序人员调整

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
- (远程法术攻击包括默认的戏法普通攻击)法术攻击检定: 1d20 + 职业主属性调整值 + 熟练加值 vs AC
- 伤害计算: 武器伤害骰 + 属性调整值（部分后续扩展职业特殊说明）
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
3点成功则恢复1点血量并且脱离昏迷状态，恢复行动能力,DC=10，队友可以用治疗法术直接恢复该角色；此时怪物也有可能攻击该角色，此时受到伤害计一次死豁免失败,重击计两次死豁免失败，三次失败则死亡
- 玩家和队友倒地期间，如战斗结束进入探索模式，则该角色自动脱离昏迷状态，恢复1点血量并且恢复行动能力（探索期间不存在玩家方昏迷状态）

---使用ScriptableObject存储角色和怪物数据
---使用ScriptableObject实现战斗事件通道DamageEventChannel,用于解耦伤害计算和UI显示,动画播放等逻辑,使其更易于扩展
+ 事件通道与血条更新 — 关键技术点（简洁，供查阅）
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
  - 法术攻击（含默认戏法）：只要角色有法术，命中就使用职业主属性（primarySpellAbility），且伤害仅取法术伤害骰（不叠加主属性）。
  - 默认普通攻击策略：基于 CharacterTemplate.defaultAttackType 决定。Physical → 走装备/物理；Spell → 使用 defaultCantrip（若无则按兜底策略）。
- 熟练与加值：
  - 物理命中 = 1d20（含优/劣势）+ 当前武器类型对应的属性调整值（如无武器则参考下面的拳头判定） + 熟练加值（若熟练）。
  - 法术命中 = 1d20（含优/劣势）+ 当前template设置的职业主施法属性调整值 + 熟练加值（若熟练）。
  - 物理伤害 = 武器给的伤害骰 + 能力调整值 ；暴击仅翻倍骰，不翻倍修正。
  - 法术伤害 = 法术给的伤害骰 ；暴击仅翻倍骰，不翻倍修正。
  - 熟练判定来自模板：proficientWeaponClasses / proficientWeaponTypes 或职业施法熟练。
- 阵位与动画不变：
  - 前排 = 近战位移攻击；后排 = 远程/施法原地攻击。AI/动画链路保持不变，仅更换 ICombatSource。
- 远程弹药与兜底：
  - 默认不需要弹药，但预留后面设计某些怪物需要特定弹药才能穿透其抗性，（Ammunition/Thrown）的武器在弹药不足时自动回退：优先默认戏法（SpellAttack）
- 距离规则：使用源的 RangeMin/RangeMax 判定可攻击距离；近战短距离，远程/法术按配置。
- 兼容与回退：
  - 若未挂装备系统或无可用源（视作用双拳攻击）：物理攻击所有角色无论前、后排命中取力量和敏捷调整值中大的一方，伤害按1d6+力量调整值；施法攻击（前提是施法职业且拥有对应法术）
  无限制（空手也可以使用攻击型戏法，使用职业主属性）。
  - 拳头攻击视作未装备武器的近战武器，命中判定为力量/敏捷调整值二者取大。伤害判定为1d6+力量调整值。
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
  - 不同装备之间存在共生和关联关系（举例如弓箭与箭矢放在临近位置激活增强远程攻击属性）。考验玩家利用有限的背包空间进行优化配置的能力/不同装备组合带来的战斗策略差异。
  - 玩家等级/种族/专长的构筑影响可装备物品的种类和数量。这样给玩家一定的成长指引。
  - 不同装备所占背包内空间的基础单元格不同，从而让玩家在有限的背包空间内进行取舍和优化。包括手动旋转，组合，摆放整理物品，同时要考虑物品之间的关联和共生关系。  
  - 物品的共生和关联关系受到自身职业构筑的流派指导和限制，(如板甲骑士的构筑必定会关联重甲类别的护甲，如战斗中获得更好的重甲，可以替换当前装备而不破坏自身
  职业构筑带来的共生关系的加成，但此时如果通过战斗随机获得一件强力的轻甲，其属性足以颠覆原本的职业构筑)从而让玩家在构筑职业流派时有更多的思考和选择空间。也让玩家在战斗中根据随机获得的强力装备来不断构思
  和调整自身的职业构筑和战术策略。（这种设计思路类似于Roguelike游戏中通过随机获得的装备来调整自身的构筑和策略）同时洗点的成本适中，可以让玩家有一定的沉没成本支付能力。
  - 局内战斗时背包锁定不可操作，通过探索阶段遇到的“篝火”类checkpoint进入装备调整界面。
  - 局外的资源分配和角色成长运营让玩家策略性构思进入局内战斗时的默认装备和战术消耗道具，如在局内通过战斗获得自己职业构筑所需的更优质装备，则可更新当前装备。
  但战斗死亡就会失去所有收获的装备物品，回到初始装备状态。从而建立2D横版简约搜打撤机制。
 
- UI 布局与交互（概要）
  - 背包/角色属性 Hub 采用“锚点优先”的布局原则：不使用 LayoutGroup/ContentSizeFitter；背包网格（InventoryGridView）采用手动定位（按 cellSize/spacing/padding 计算），
  不使用 LayoutGroup。

[路径A落地决策与约束]
- 决策：采用“在角色预制体/实例的 CharacterInventory.initialItems 直接配置初始物品”的路径A。启动时由该列表生成 ItemInstance 并触发刷新。
- 绑定位置（强制）：CharacterInventory 必须挂在每个“玩家/盟友”角色预制体/实例上，禁止挂在场景空节点（如 PlayerData）。
  - PlayerData 空节点不再承担背包数据容器的职责；仅当未来需要“队伍共享仓库/共用背包”时，另行定义独立 PartyInventory（不影响本规范的“角色个人背包”）。
- 网格行列 rows/cols 的意义（保留且重要）：
  - rows/cols 仍然是“容量/版面”（可用格子总数），用于判定是否存在可容纳物品占位的空区、物品旋转后的适配、碰撞/覆盖检测与自动排布；而物品占多少格由 ItemBaseSO 的 slotWidth/slotHeight 决定，二者职责不同、同时生效。
- UI 绑定（切换合同）：
  - InventoryUIBinder.sourceInventory 始终指向“当前选中角色”的 CharacterInventory；左右切换仅重绑引用→GridView 清空并重建→Binder 刷新显示。
  - 强制手动挂载：FormationContainer 实例化后的角色对象，其 CharacterInventory 引用必须手动填入“角色选择器（SelectionHub）”的目标列表中，禁止自动查找。
  - 多来源支持（新增）：InventoryUIBinder 允许挂载多个来源（sourceInventories），通过 activeSourceIndex 或 SetActiveSourceIndex(i) 在玩家/盟友等不同角色之间切换；旧字段 sourceInventory 仍兼容单一来源模式（UseSingleSource(inv)）。提供 AddSource/RemoveSource 以维护列表。切换时 Binder 会自动解绑旧源事件、按来源 rows/cols 重配 GridView 并重建。
  - 约束：多来源列表中的每一项必须是对应角色预制体上的 CharacterInventory 引用；禁止将场景空节点（如 PlayerData）作为背包数据源使用。玩家与盟友的背包与装备分离时，分别作为不同来源挂入列表，以活动索引进行切换。
- 运行时交互策略：
  - 战斗中禁编辑：通过交互遮罩或交互开关屏蔽拖拽/旋转；篝火/休整界面再开放编辑与整理。
  - AutoFitToContainer=启用；keepSquareCells 视美术风格选择；includeSpacingInItemSize 按是否要“覆盖格间隙”选择。
- 物品来源：
  - 初始物品：仅由各角色的 CharacterInventory.initialItems 决定（本规范）。
  - 运行时新增：通过 InventoryUIBinder.TryAddNew(ItemBaseSO) 尝试落地→成功后再加入数据源；失败不改变数据源。

角色默认属性+物品以及职业专长/技能等加成规则
