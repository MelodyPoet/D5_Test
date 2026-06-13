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

游戏类型宗旨原则
- 2D横版回合制RPG挂机游戏：最终目标需要呈现在用户PC上的是一款结合了Steam上《你的老婆》这样的桌面可交互伴侣，允许用户用鼠标点击角色进行互动
（如摸头、调情等等），同时可以在PC屏幕右边显示一个占全屏25%宽度的竖屏挂机游戏窗口（参考Steam上《巴尔的遗产》的显示方式），用户可以在这个窗口中
看到角色们在进行自动战斗，升级，换装备等挂机游戏核心玩法的内容；同时具备战斗局外养成的营地建造要素，角色非战斗时间在营地可以进行日常烹饪食物/制作
武器装备/升级营地各种家具、设备/休息回复探险精力/消耗食物来进行次级属性锻炼提升微属性加成等（参考手游《重返家园》）,在营地时候用户可以通过点击
角色来触发一些特殊事件（如角色视线跟随用户鼠标，用户鼠标摸头、调情触发角色特殊动画等等），这些特殊事件会有一定的概率提升角色好感度，从而激活一些
特殊剧情和恋爱事件；总之这款游戏的核心宗旨是让用户能够在PC上养成一个虚拟的2D横版回合制RPG角色，并且通过点击和观察来享受养成和互动的乐趣，同时
也能通过挂机游戏的玩法来体验角色成长和战斗的乐趣(战斗机制基于DND5E跑团规则上根据2D横版回合制)，最终达到用户在PC上有一个可交互的虚拟伴侣的体验。

- 游戏需要的各大主要模块
  1.一个在PC桌面上显示的竖屏游戏窗口，最上面的战斗窗口中角色们在进行自动战斗，升级，等挂机游戏核心玩法的内容，中间是背包和属性栏，提供物品拼接
和行列属性加成（参考背包乱斗),下面窗口是营地界面，角色可以在营地进行上个段落文章里说的各个养成活动。

  2.战斗系统：基于DND5E跑团规则上根据2D横版回合制的战斗系统，包含角色属性、技能、状态效果、先攻系统、近战远程攻击机制、暴击机制等核心玩法
且融合了《背包乱斗》类的物品整理玩法，玩家可以将不同种类物品通过链接标记进行关联以激活共生关系属性加成和职业专长机制加成（具体加成配置表都在后文）
同时融合轻度俄罗斯方块的背包栏的行列填满可以激活微属性加成或者技能加成。战斗中这些加成的机制来触发特殊效果（如临时增加攻击力、防御力、回复生命值等，
增加额外的攻击和防御手段等），同时装备本身也有默认的属性(如5e规则下鳞甲的AC/短剑的1d6伤害等)来提升角色属性和战斗力。
  决定角色战斗强度的，由以下2个主要核心子系统:
  - 角色养成系统：角色通过战斗获得经验升级，升级后可以提升属性点和技能点，属性点可以用来提升力量、敏捷、体质、智力、感知、魅力等基本属性，技能点
可以用来提升各种技能的熟练度（如运动、隐匿、察觉等），同时角色还可以通过战斗获得金币和各类素材资源，资源可以用来购买装备和消耗品，或者在营地进行
建造和升级各种设施来提升角色的养成效率和战斗力。 
  - 背包物品拼接系统：如上文战斗和养成系统所描述,物品具备互相之间根据链接标记进行关联以激活共生关系属性加成和职业专长机制加成的玩法。
    - 所有背包内物品存在两种状态：装箱/拆箱状态，装箱状态所有形状物品全部变成1个背包单元格大小的视觉显示元素，且失去所有属性加成，同样物品装箱后
  可以在背包栏里堆叠10个，拆箱状态物品会根据其形状占用背包栏里不同数量的单元格，且根据物品属性提供不同的属性加成；玩家可以通过右键物品激活操作
  菜单来切换装箱/拆箱状态，但每次只能操作同类型一件物品。
    - 拆箱状态下物品分为可装备和不可装备物品两个分类，可装备物品可以装备在角色身上对应部位槽位里提供装备自身的属性加成和战斗力提升，不可装备物品
  则只能在背包里。但两者均在背包里根据其形状和相互链接激活的加成机制来提供属性加成和战斗力提升；同时无论装箱/拆箱状态下的物品在背包栏的行列都可以
  正常填满背包行列单元格激活微属性加成或技能加成。（具体逻辑分类下文有详细描述）
    
  3.角色定制界面：开始游戏时允许玩家自定义自己的角色的属性/职业/外观等，角色外观可以通过预制体和Spine动画来实现，角色属性和职业会影响战斗系统中
的属性点分配、技能熟练度、可用装备等，完成定制后玩法引导进入演示关卡战斗体验物品整理玩法和首次获得掉落，引导玩家拼接玩法。
  
  4.营地系统：角色非战斗时间在营地可以进行日常烹饪食物/制作武器装备/升级营地各种家具、设备/休息回复探险精力/消耗食物来进行次级属性锻炼提升微属性
加成等（参考手游《重返家园》），同时在营地时候用户可以通过点击角色来触发一些特殊事件（如角色视线跟随用户鼠标，用户鼠标摸头、调情触发角色特殊动画
等等），这些特殊事件会有一定的概率提升角色好感度，从而激活一些特殊剧情和恋爱事件。

  5.桌面虚拟偶像系统：计划预留后期角色可以在桌面上单独显示为屏幕上的小人（虚拟桌面偶像），角色和玩家之间可以通过鼠标操作交互（如角色视线跟随玩家
鼠标/玩家鼠标可以触发摸头、不同身体部位调情/角色跳宅舞等/桌面闹钟虚拟角色提醒用户时间小功能/桌面情侣笔记本共同学习记忆等。

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
- 位置唯一真源：由 FormationContainer ScriptableObject 中的 6 个索引决定，并由 HorizontalBattleFormationManager 按相同索引实例化
到对应 spawn 点。
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
- 协程动画会导致难以控制动画状态和同步问题，所有动画均使用DND_CharacterAdapter组件进行播放和状态管理，且使用SpineEvents事件回调处理攻击命中
和伤害计算逻辑
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

3. 玩家和敌人攻击行为系统以及玩法底层规则

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
11. 角色预制体下方显示当前受到的状态(如燃烧，中毒等)图标，状态持续时间，以及当前的buff/debuff状态图标，从左到右排列，右键图标会弹出文字提示框
描述具体状态效果，持续回合数等信息
12. 角色预制体下方显示目前受到什么敌人的近战还是远程威胁标志图标；前排角色会受到对方近战威胁和远程威胁，后排角色只受到对方远程威胁；
无论站位在哪，只要手持远程武器且受到敌人近战威胁，你的攻击都会被强加劣势（除非特定专长的修正），需花一回合切换近战武器攻击才解除劣势
13. 优势/劣势机制：当角色处于优势状态时，攻击检定时掷2d20取高；处于劣势状态时，掷2d20取低；当同时处于优势和劣势时，互相抵消，正常掷1d20

CharacterStats 属性系统:（底层核心运行规则，基于DND5E规则）
所有的生物——无论玩家角色还是怪物——都有六项属性用于衡量其身体和精神特征，如属性描述表所示。

属性描述Ability Descriptions 
属性               数值衡量……
力量Strength       力气
敏捷Dexterity      灵活性、反应能力和平衡感
体质Constitution   健康程度和耐力
智力Intelligence   推理能力和记忆力
感知Wisdom         洞察力和精神坚韧程度
魅力Charisma       自信、仪态以及吸引力

属性值 Ability Scores
每项属性都有从1到20的数值，不过有些怪物的数值最高能达到30。这个数值代表属性的强弱，属性值表概述了这些数值的含义。

属性值Ability Scores 
数值       含义
1         这是通常情况下属性数值可以降到的最低值。如果一个效应将数值降低到0，那么这种效应自身就会解释发生了什么。
2-9       这代表能力较弱。
10-11     这代表人类的平均值。
12-19     这代表能力较强。
20        除非某特性另有说明，否则这已经是冒险者的属性数值所能达到的最高值。
21-29     这代表才能非凡
30        这是属性数值能达到的极限

- 调整值计算: (属性值-10)/2 标准DND5E公式
- 豁免熟练: 6种豁免投骰的熟练项配置
- 技能系统: 18种技能 + 熟练项配置
- 血量机制: MaxHP/CurrentHP + 死亡检测 (HP<=0)
- 动作/附赠动作和反应：
  - 动作(action):是角色的基础行动单位，可以执行下列一系列行为；
    1. 攻击(Attack):使用近战和远程武器进行攻击，或者使用徒手攻击
    2. 施法(Cast a Spell):施展一道法术（包括使用法术位施法和戏法）、使用一个魔法物品或者其他专长/种族/职业特性允许你使用的法术
    3. 撤离(Disengage):使用这个动作后，你的移动不会引起敌人的借机攻击
    4. 游荡者可以躲藏(Hide):使用这个动作后，你可以尝试进行隐匿检定，从而让自己潜行起来，躲避敌人的视线并且发动偷袭攻击
    
  - 附赠动作(BonusActions):有很多职业特性、法术或其他能力允许你在你的回合中执行一种额外的动作，这种动作叫作附赠动作。例如，灵巧动作特性就能够
让游荡者执行附赠动作。只有在某个特殊能力、法术或游戏中其他明确了你能以附赠动作来做某事的特性的允许下，你才能执行附赠动作。否则，你没有可以执行的
附赠动作。你只能在自己的回合执行一个附赠动作，因此在你拥有多个可执行的附赠动作项时，你必须选择你要执行哪一个。除非该附赠动作项的发生时点有明确
规定，否则你可以自行选择在你的回合内何时执行附赠动作。任何能够让你无法执行动作的效应，同样会使你无法执行附赠动作。(如游荡者使用附赠动作进行潜行，
或者你副手持有另一把武器或者具有盾牌大师时候，花费附赠动作追加一次副手的攻击，这次追加的攻击不叠加熟练加值，伤害只有武器伤害骰，无属性调整值加成)；
双持客专长允许副手武器的追加攻击可以叠加熟练加值，另外双武器战斗风格专长可以允许你的副武器伤害增加你的属性调整值；

  - 反应(reaction)是特定条件触发的即时行动，每轮只能使用一次(可以根据特定专长使用一些针对敌人和队友的特殊攻击或防御机制)；
  - 休息机制：一次地城关卡探险2-3次短休（根据关卡长度决定)，短休允许角色消耗背包内装箱的食物资源1个，恢复一个生命骰的生命值，长休只在营地阶段的
床上休息才可以进行，根据实际生活中的时间流逝按分钟回复生命值和其他职业资源（如法术位、技能点等），为后续营地建造玩法预留


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
3点成功则恢复1点血量并且脱离昏迷状态，恢复行动能力,DC=10，队友可以用治疗法术直接恢复该角色；此时怪物也有可能攻击该角色，此时受到伤害计一次死豁免
失败,重击计两次死豁免失败，三次失败則死亡
- 玩家和队友倒地期间，如战斗胜利进入探索模式，则该角色自动脱离昏迷状态，恢复1点血量並且恢复行动能力（探索期间不存在玩家方昏迷状态）

---使用ScriptableObject存储角色和怪物数据
---使用ScriptableObject实现战斗事件通道DamageEventChannel,用于解耦伤害计算和UI显示,动画播放等逻辑,使其更易于扩展
+ 使用ScriptableObject实现战斗事件通道DamageEventChannel,用于解耦伤害计算和UI显示,动画播放等逻辑,使其更易于扩展
+ 发布规范：仅由 `HorizontalCombatRules.ResolveAttack(attacker, target, ...)` 在“命中成立”后统一发布一次伤害事件；AI/动画回调/角色
脚本等不得重复发布，避免 UI 重复刷新与日志重复。
+ 获取顺序：事件通道优先从参战者实例上的 `CharacterStats.damageEventChannel` 获取；如未配置，则回退到 `EventChannelManager` 的全局
通道（`"DamageEventChannel"`）。若两者皆无，将打印警告日志以便排查。
+ 订阅建议：UI 使用 `CharacterStats.OnHealthChanged` 做最终显示刷新，`DamageEventChannel` 主要用于动画、特效、飘字与日志等跨系统联动，
不直接驱动 HP 数值修改。
  - 事件通道与血条更新 — 关键技术点（简洁，供查阅）
   - 事件通道（`DamageEventChannel`）职责单一：用于把“谁受到伤害/谁造成伤害/伤害数值”等消息广播到所有关心该事件的系统（动画、伤害计算、视觉
   特效等），但不应直接由 UI 订阅来做最终显示更新。
   - UI 更新应依赖实例级的本地事件：`CharacterStats` 在受伤/治疗后应触发 `OnHealthChanged(int currentHp, int maxHp)`，
   UI（`UI_HealthBar`）在被绑定到具体 `CharacterStats` 实例时直接订阅该本地事件以保证目标明确与低时序窗。
   - 管理器做为保险：`HealthBarUIManager` 保存 `CharacterStats -> UI_HealthBar` 的映射表，提供 `RefreshBar(CharacterStats)` 
   接口供 `CharacterStats` 在处理伤害后主动调用，作为对本地事件的二次保障（同一职责的两条可靠路径）。
   - 避免 UI 直接订阅全局通道：当场景同时存在预制体（编辑器中放置）和运行时实例时，UI 若直接订阅全局通道容易订阅到错误目标或由于时序错过事件。
   - 单例与容器时序规则：确保 `HealthBarUIManager` 单例在 UI/角色创建前就存在（或在创建时立即同步容器与 prefab），避免在单例为 null 时
   触发回退销毁逻辑导致血条被误删。
   - 绝对避免启发式销毁：不得使用基于 Slider.value/maxValue 等启发式规则在不确定的情况下批量销毁血条；销毁条件应为 `owner == null` 且
   经确认（或超时/显式标记）后才执行。
   - 预制体配置优先：血条预制体必须在 Inspector 中预先绑定好 `Slider`/`Text` 等组件，减少运行时自动查找带来的不确定性。
   - 日志和调试接口：保留并使用 `HealthBarUIManager.DumpStatus` / `DumpMapDetails` 等调试方法，在复现问题时先采集映射与容器状态以便
   定位时序或引用不一致的问题。

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
- 27点购点：
- 属性值点数花费Ability Score Point Costs
Score 8 9 10 11 12 13 14 15
Cost  0 1 2  3  4  5  7  9 

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
  - 若未挂装备系统或无可用源（视作用双拳攻击）：此时所有角色无论站位在前、后排，命中和伤害取力量调整值,伤害骰按1d1；施法攻击
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
  - 物品有2种大类：
    - 1.可装备物品：在角色身上对应槽位的，槽位只能填充一件类型的可装备物品(如躯干只能穿一件护甲),背包里其余护甲均无任何属性/拼接加成/行列加成
  效果可以给到角色，必须装箱成一个单元格的视觉/数量标识物品icon。
    - 2.普通物品: 不可装备只能在背包里，但是所有物品都统一存在于背包格子系统中。
      (无专门角色身上的装备栏的概念)为后续背包乱斗like玩法结合DND5E战斗系统的职业构筑融合做铺垫。
  - 物品的装箱/拆箱:
    - 所有根据ItemBaseSO创建的有形状的物品都可以通过右键激活一个操作菜单
      菜单有2个操作button:
      - 1.旋转-》负责将物品顺时针90度旋转，改变物品占用的格子形状
      - 2.装箱/拆箱-》如果当前物品在背包里，此时这个button处于激活可以操作状态--》将当前鼠标选择的物品（无论形状大小）统一装箱成一个背包单位
    格子的icon图标；如果当前物品已经装备在角色身上装备槽位里，则必须先执行双击卸下装备后才可以激活装箱/拆箱操作。
    - 装箱后的单格物品图标不再显示物品的形状大小信息，而是一个单元格的图标，相同物品的装箱后图标可以在背包里堆叠，上限10个。也无法再对角色属性
    产生加成效果（包括物品链接加成和行列加成等),此时物品变成了在背包里只计算数量存放和视觉标识的含义，直到被拆箱放回背包格子系统中。
  - 装箱后的物品图标只能左键点击拾起/移动/放下，右键旋转禁用，右键装箱/拆箱可以再次操作拆箱回复物品初始形状大小和属性。一次操作只能把当前堆叠的
  装箱后物品图标拆箱一个单位的该物品。
  - 玩家通过构筑职业流派的装备组合来提升战斗力。
  - 装备物品提供命中/伤害/防御等属性加成。
  - 不同装备类别（武器/护甲/饰品）有不同的槽位限制。
  - 物品有特殊词条标签（如 TwoHanded/Shield），影响装备规则。
  - 不同装备之间存在共生和关联关系（举例如弓箭与箭矢放在临近位置激活增强远程攻击属性）。考验玩家利用有限的背包空间进行优化配置的能力/不同装备组合
  带来的战斗策略差异。
  - 玩家等级/种族/专长的构筑影响可装备物品的种类和数量。这样给玩家一定的成长指引。
  - 不同装备所占背包内空间的基础单元格不同，从而让玩家在有限的背包空间内进行取舍和优化。包括手动旋转，组合，摆放整理物品，同时要考虑物品之间的关联
  和共生关系。  
  - 物品的共生和关联关系受到物品特定套装加成的影响和牵制，(如板甲骑士的构筑必定会关联重甲类别的护甲，如战斗中获得更好的重甲，可以替换当前装备而不破坏自身职业构筑带来的加成，但此时如果通过战斗随机获得一套强力套装的其中一个部件轻甲，其属性以及之后有可能搭配同套装的属性加成足以颠覆原本的职业构筑)从而让玩家在构筑职业流派时有更多的思考和选择空间。也让玩家在战斗中根据随机获得的强力装备来不断构思和调整自身的职业构筑和战术策略。（这种设计思路类似于Roguelike游戏中通过随机获得的装备来调整自身的构筑和策略）同时洗点的成本适中，可以让玩家有一定的沉没成本支付能力。
  - 局内战斗时背包锁定不可操作，通过探索阶段遇到的“篝火”类checkpoint进入装备调整界面。
  - 局外的资源分配和角色成长运营让玩家策略性构思进入局内战斗时的默认装备和战术消耗道具，如在局内通过战斗获得自己职业构筑所需的更优质装备，则可更新
  当前装备。
  但战斗死亡就会失去所有收获的装备物品，回到初始装备状态。从而建立2D横版简约搜打撤机制。
 
-背包中的物品占格子规则：
  - 参考经典背包类游戏《背包乱斗》和经典传统玩法的俄罗斯方块的拼接方法。      
    - 物品可以是1x1,1x2,2x1,2x2,3x2,2x3,1x4等不同形状大小的。
    - 物品可以有不同的形状，如L形，T形等，但必须是矩形的组合。
    - 物品在背包里可以根据形状凹凸进行拼接，但不能重叠。
    - （这些规则为后续预留玩法设计空间）
    - 物品之间特定的拼接条件可以触发额外的共生关系的特定加成效果。（如弓箭和箭矢拼接在一起触发远程攻击加成效果）
    - 并且多件物品拼接形成的格子如果占满背包的行和列也触发对应不同的属性加成效果。(俄罗斯方块)
    - 鼠标左键物品单选中后，物品此时被视为"拿起"状态，脱离背包格子吸附，且右键可以顺时针旋转90度，物品形状和占格子规则随之变化。
    - 此时物品在背包内会有一个透明灰色的投影，提示当前物品可放置的位置。
    - 鼠标左键再次点击放下物品时，若投影位置合法（无重叠且在网格内），则物品吸附到该位置；否则回退原位。
    - 双击物品可以快速装备/卸下该物品（若该物品可装备）。
    - 无论是否已经装备的物品，都必须放在背包格子系统内。但已经装备到对应角色身上槽位的物品，格子显示为灰色不可单击操作状态。
    - 卸下的装备格子显示为白色，可以被左键单击选中并进行旋转和放置操作。
    - 物品的旋转和放置操作必须遵守物品形状对应的格子占用规则。
    - 可装备的物品只有在装备状态下，才会对角色属性产生加成效果。
    
-统一形状对应格子规则：(俄罗斯方块规则的多形状物品占格子规则)
    - ItemBaseSO的SlotWidth/SlotHeight属性接口应该是以下方式：
        - 物品的形状是矩形的组合，所以一律从左上角开始计算宽度和高度。      
        - 比如一个L形物品，那么首先有个接口来输入确认该物品一共会在形状上占据几行(SlotWidth)，再有一个接口来确认它的第一行占据几个格子
        元素（填充对应坐标的元素），然后再有一个接口来确认它的第二行占据几个格子元素，以此类推，直到该物品的形状完全描述完毕。
        - 角色默认背包的总网格大小上限是8行*16列。后续结合营地经营玩法，可以通过升级营地设置来制作高级背包来提升总行列数上限。
        
  物品格子填充形状和物品拼接规则细节：
  - 每个物品根据其类型和大小占用不同数量的格子（slotWidth/slotHeight）。
  - 鼠标左键点击物品即从背包中单独选择该物品，此时该物品脱离吸附格子，物品图标稍微变大一些且往右偏移2个像素距离，
而跟随鼠标的位置，且此时才可以右键打开对物品操作菜单。
  - 旋转必须要在物品被单选中状态下，脱离吸附格子的情况下，才可以右键菜单中选择“旋转”选项后，物品顺时针旋转90度-SlotWidth/SlotHeight互换。
  - 此时仍旧保持在单选状态，且此时物品脱离吸附背包，不能被右键菜单的装备/卸下这两个按钮操作。
  - 单选中状态下，物品仍然对背包格子产生一个自身大小的投影（透明灰色），提示当前物品可放置的位置。
  - 再次左键点击放下物品时，若投影位置合法（无重叠且在网格内），则物品吸附到该位置；否则回退原位。  
  - 背包网格有固定的行列数（rows/cols），决定总容量。
  - 物品摆放时需考虑格子间距和边距（spacing/padding）。
  - 生成的物品格子实例的图标具备以下功能，从而实现不同形状物品互相拼接触发战斗机制和属性调整:（参考背包乱斗)
    - 物品实例图标上要显示2种拼接图标，分别是“共生关系图标”和“专长图标”，当物品满足特定拼接条件时，这些图标会被激活显示。
    - 基本共生关系图标：星星样式，当两个物品满足特定的拼接条件（如弓箭和箭矢拼接在一起）时，高亮星星，且激活对应的共生关系加成效果（如远程攻击
  加成，具体文档后面定义）。
    - 专长先决条件图标：菱型样式，当某个物品满足特定的拼接条件（如近战武器和重甲护甲拼接在一起）时，高亮菱型图标，且激活对应的重甲大师专长
  加成效果。
    - 两种图标要求在物品实例图标上的四周根据格子填充的尺寸扩展出去的自定义位置显示（比如一个2*2的物品实例图标，2个星星图标显示在物品图标上下
  扩充出去的区域，菱型图标显示在左右扩充出去的区域），且当满足条件时高亮显示，不满足条件时保持灰色暗淡状态。
    - 这些拼接提示图标需要在当前ItemBaseSo的inspector上扩展设计一个接口来配置这些上文说到的需要显示的位置，且必须保证这些图标的位置的坐标
  是相对于物品实例图标的中心点的，这样无论物品实例图标怎么旋转，这些图标都能保持在正确的位置。且这些图标的位置是符合无论该物品怎么移动/旋转，总能
  对应填充进背包里网格，因为它们都是根据组成物品的基础格子尺寸来往自定义外配置的。
    - 拼接图标可以看作是物品的一部分，目前ItemBaseSO上配置物品形状大小是通过ShapeCoords来填充不同位置的格子元素来组合形成，拼接图标一样遵循
  这个规则来配置：
      物品形状（ShapeCoords）和拼接图标位置遵循同一套坐标系统，可以：
      物品实际形状格子 → 标记为 "Shape" 类型
      拼接图标位置格子 → 标记为 "Icon" 类型（Synergy 或 Feature）
      两者都使用相同的 ShapeCoords 结构定义
      其实拼接图标和生成物品形状的1单元格子一样是符合这个坐标条件，因此完全可以遵循这个规则改进这个接口（分类出填充的哪个格子元素是拼接图标，
  哪个格子元素是物品实际形状)这样方便自定义出更多样形状的物品和拼接图标位置。物品的八个方向都可以配置拼接图标位置（含对角线),从而满足物品的形状
  和拼接图标位置的多样化需求。
    - 物品实例图标的旋转和位置调整必须遵守上述规则，并且在旋转时保持拼接图标相对于物品图标的正确位置。
    - 拼接图标在背包里的位置和其他物品重叠时，如果其他物品满足拼接条件，图标则高亮显示（其余时间保持暗淡状态），同时激活物品拼接的机制或属性加成。

- UI 布局与交互（概要）
  - 背包/角色属性 Hub 采用“锚点优先”的布局原则：不使用 LayoutGroup/ContentSizeFitter；背包网格（InventoryGridView）采用手动定位
  

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
- Base层角色天生有物理攻击手段
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

5. 已执行旧路径退役：`InventoryUIBinder` 取消直接订阅 `CharacterInventory.OnInventoryChanged`，刷新现由事件通道驱动（`InventoryChangedChannel_SO` / `ActiveCharacterChangedChannel_SO`）。
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
        内层--默认内层基础服装和皮肤五官等（为外观/妆容/时装等定制化服务）
          类型(定义SkinBodyPartType枚举,根据Spine素材有几种不同的皮肤槽位来区分)被外层图片所覆盖
            暂时设置以下几种部件:
            hair(头发)         
            eyes(眼睛))
            mouth(嘴)
            body(躯干和四肢)
            皮肤的名字(用来在unity的UI上展示给玩家)
            叠加的颜色(用来给皮肤和服装定制化染色)
        外层--盔甲武器等实际跟玩法逻辑机制强关联的装备（物品共生链接+物品拼接专长等）          
            暂时设置以下几种部件:
            helm(头盔)--分头盔和头环/王冠两个大类，前者覆盖内层的头发，后者不覆盖
            glove(护腕)
            boots（靴子）
            belt(腰带)--包括腰封和大腿的装界
            cloak(披风)
            weapon(武器)--分单手和双手武器，后者会覆盖副手武器槽位
            shield(盾牌)--同时也是副手武器，可以被双手武器覆盖，也可以再额外单持另一把轻型武器/特殊专长可以单持另一把常规武器
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
以当前示例的Spine骨骼和皮肤文件具体规则如下：
由于换装的主要目的是有机的组合Spine素材中设置的不同部件的皮肤，因此需要确保每个部件的皮肤名称在Spine和Unity中是一一对应的.
玩家在UI界面选择时，角色应该保持原骨骼的所有动画正常播放前提下，可以适配所有服装样式
玩家选择不同部件组合/整套套装时，会激活不同的对应条件(UI界面应该设置不同标签区分开整套和部件组合的选择)
因此，需要确保以下两者情况:
1.第一类是玩家使用UI界面时候，角色实际上有一个SkinBase(只有人脸和裸手臂)，其他部件都是通过附加的皮肤来覆盖显示.
 如这些类别(注意前缀):hair/legs/clothes/eyes/eyelids/nose/accessory通过这些附加导基础SkinBase上,强调玩家自定义组装。
2.第二类是玩家选择整套FullSkin(全身套装),这种情况下就不需要自定义组装，而是会用素材中默认配置好的一套服装直接替换掉基础SkinBase,不再叠加
其他部件.

### UI换装界面需求细则:
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


---

## 附录：存档与数据驱动设计策略（单机RPG ⇨ 联网扩展预留）

本项目规划为**先完成可上 Steam 的 PVE 单机 RPG**，后续在不推翻现有架构的前提下，预留向**多人联网（含 PVP/MMO 要素）**扩展的空间。
以下约定仅作为技术选型与数据设计的总则，不改变现有业务逻辑实现。

### 1. 单机阶段的存档策略（本地真源）

1.1 真源位置
- 单机版本中，**本地存档文件是运行时状态的真源**，Unity 场景和组件只是当前快照。
- 静态内容（如 `ItemBaseSO`、法术 SO、角色模板 SO、Spine 换装配置 SO）仍作为**只读模板**存在工程中，不直接写入存档，只用 ID/路径引用。

1.2 存档范围建议
- **角色基础信息**：角色 ID、名称、等级、经验、可用属性点/专长点等。
- **数值状态**：基础属性、已习得专长/技能列表、当前生命/法力（视是否需要战斗中读档而定）。
- **装备状态**：`CharacterEquipment` 每个槽位绑定的 `ItemInstance.instanceId`。
- **背包状态**：`CharacterInventory.Items` 列表中每个 `ItemInstance` 的：
  - `instanceId`（唯一标识）
  - 对应模板 ID（映射到 `ItemBaseSO`：GUID 或资源路径）
  - `gridPosition`（格子坐标，已在运行时维护）
  - `rotation`（物品旋转状态）
  - 未来扩展字段（耐久、强化等级、随机词条等）。
- **任务 / 世界进度**：当前主线/支线阶段、已完成任务 ID、关键世界开关（如传送点解锁、Boss 击杀标记等）。

1.3 推荐实现方式
- 定义统一的存档数据结构，例如：
  - `GameSaveData`（包含多个 `CharacterSaveData` 与全局世界状态）。
  - `CharacterSaveData`：包含等级/经验、BaseStats、Feat 列表、`ItemInstanceSaveData` 列表、`EquipmentSaveData` 等。
- 使用 JSON 作为首选存储格式：
  - 优点：可读易调试、版本演进友好、跨平台一致。
  - 存储目录：`Application.persistentDataPath` 下以 `save_slotX.json` 形式保存。
- 提供集中式 API（避免到处散写 IO）：
  - `SaveGameManager.SaveGame(slotId)`：遍历当前 `CharacterInventory`、`CharacterEquipment`、`CharacterStats` 组装 `GameSaveData` 并写入文件。
  - `SaveGameManager.LoadGame(slotId)`：从文件还原 `GameSaveData`，重建 `ItemInstance` 列表、恢复装备槽绑定，最后调用 UI 刷新接口（例如 `InventoryUIBinder.RefreshFromInventory()`）。

1.4 与当前背包实现的对接
- 当前已将物品的格子坐标写入 `ItemInstance.gridPosition`，且 `InventoryGridView.SpawnInstance` 优先使用该位置落地。
- 存档只需将 `gridPosition` 一并序列化记录，在读档阶段重建 `ItemInstance` 时按存档覆盖 `rotation` 与 `gridPosition`，即可保持玩家手动调整的背包布局不变。


### 2. 联网 / MMO 阶段的存档与数据真源迁移

2.1 真源迁移原则
- 多人联网（含 PVP/MMO）模式下，涉及**角色实力、经济、进度**的所有关键数据，其真源必须迁移到服务器。
- 客户端的数据结构（`ItemInstance`、`CharacterInventory`、`CharacterEquipment`、`CharacterStats` 等）继续作为**缓存 + 表现层**使用，但不再拥有最终裁决权。

2.2 客户端与服务器的角色
- **服务器端**：
  - 保存并裁决：角色属性、背包物品、装备状态、金币/货币、任务进度、世界状态、PVP 战果等。
  - 通常使用关系型 DB（如 MySQL/PostgreSQL）或文档型 DB（如 MongoDB）储存。
  - 内部也可使用与客户端类似的领域模型（如 Server 版 `ItemInstance`、`CharacterInventory`），但与 Unity 解耦。
- **客户端**：
  - 持有一份从服务器获取的**快照**，重建 Unity 组件树以驱动表现（Spine 捏人、背包 UI、战斗动画等）。
  - 存本地的仅是**最近一次快照缓存**，真正存档仍以服务器为准。

2.3 可复用的存档结构
- 建议客户端的 `GameSaveData` 结构尽量与服务器端使用的角色数据结构保持相似：
  - 相当于客户端的 JSON Save 就是“服务器角色数据的一种序列化快照”。
- 这样单机阶段的 `GameSaveData` / `CharacterSaveData` / `ItemInstanceSaveData` 将来可以直接用于：
  - 与服务器的 HTTP / gRPC / 自定义协议的数据交换，逐步替换 `LocalGameLogicService` 为远程实现。
  - 本地缓存最近一次从服务器拉取的数据。

2.4 逻辑裁决位置的预留
- 当前单机阶段，装备/卸下、背包移动、经验结算等逻辑可以完全在客户端实现。
- 为未来 MMO 预留扩展空间：为“改变角色状态的操作”准备一层服务接口，例如：
  - `IGameLogicService.TryEquipItem(...)`
  - `IGameLogicService.TryMoveItem(...)`
  - `IGameLogicService.TryApplyDamage(...)`
- 单机版使用 `LocalGameLogicService`（直接修改本地对象），
- 联网版使用 `RemoteGameLogicService`（将请求发往服务器，由服务器返回新状态再更新本地）。


### 3. 使用 Excel 表驱动静态数据（掉落表/刷怪/经验表等）

为避免大量“表格型数据”全部使用 ScriptableObject 逐条手填，统一采用：

> Excel/CSV 作为**源数据** → Editor 工具导入 → 生成 ScriptableObject 容器 → 运行时只读取容器，不直接读 Excel。

3.1 适合从 Excel 驱动的配置类型
- 怪物刷新表（MonsterSpawn）：怪物 ID、等级区间、刷新区域 ID、刷新权重/时间间隔等。
- 掉落表（DropTables）：掉落表 ID、物品 ID、权重、数量范围、等级条件等。
- 升级经验表（LevelExp）：等级、所需经验、获得属性点/专长点等。
- 技能/专长表（Feats/Skills）：SkillId、等级、前置条件、数值加成等。

3.2 Editor 导入管线（建议流程）
1. 在项目中约定一个原始表目录，例如：`Assets/GameData/Raw/*.xlsx` 或 `*.csv`。
2. 每张表定义清晰的列：
   - 第一行：字段名（如 `DropTableId`, `ItemId`, `Weight`, `MinQty`, `MaxQty`）。
   - 第二行：字段类型（可选：`int`/`float`/`string`/`enum` 等），方便导入工具做类型解析。
3. 编写 Editor 菜单脚本（例如：`Tools/GameData/Import Excel`）：
   - 使用 CSV 或 xlsx 解析库（`#if UNITY_EDITOR` 下引用），读取每行数据。
   - 将每行映射为 C# Row 结构体或类，如：
     ```csharp
     [Serializable]
     public class DropTableRow {
         public string dropTableId;
         public string itemId;   // 对应 ItemBaseSO 的逻辑 ID
         public float weight;
         public int minQty;
         public int maxQty;
         public int minLevel;
         public int maxLevel;
     }
     ```
   - 将行集合填入 ScriptableObject 容器中，例如：
     ```csharp
     [CreateAssetMenu(menuName = "GameData/DropTable")]
     public class DropTable : ScriptableObject {
         public List<DropTableRow> rows;
         // 可选：构建按 dropTableId 分组的字典索引
     }
     ```
   - 导入器负责在 `Assets/GameData/Generated/` 等目录下创建/更新对应 `.asset` 文件。

3.3 运行时代码的使用原则
- 运行时代码**只依赖 SO 容器**（例如 `DropTable`, `MonsterSpawnTable`, `LevelExpTable`），
  不直接访问 Excel/CSV 文件：
  - 掉落逻辑根据怪物配置上的 `DropTableId`，从 `DropTable` 容器中筛选行并做权重随机，最后再根据 `itemId` 映射到 `ItemBaseSO` 生成 `ItemInstance`。
  - 刷怪逻辑根据地点 ID 和 `MonsterSpawnTable` 的规则生成敌人。
  - 升级逻辑根据 `LevelExpTable` 查经验阈值，发放属性点/专长点。
- 这样在未来 MMO 模式下：
  - 客户端与服务器都可以共用同一份 Excel 源数据，分别生成各自语言/平台的配置对象；
  - 真正的掉落/升级裁决可以逐步迁移到服务器实现，而客户端仍使用同样的数据表进行 UI 显示或本地预测。


### 4. 技术选型小结（供后续开发快速回顾）

- **单机 PVE 阶段**：
  - 真源：本地 JSON 存档（`GameSaveData`），静态内容用 SO + 表导入生成容器。
  - 运行时：`CharacterInventory` / `ItemInstance` / `CharacterEquipment` / `CharacterStats` 驱动所有战斗与 UI。
  - 存档：通过集中式 `SaveGameManager` 收集并写入文件；启动/读档时重建运行时对象。

- **联网 PVP/MMO 扩展阶段**：
  - 真源：服务器数据库；客户端存档作为“快照缓存”，不再拥有裁决权。
  - 协议：以当前 `GameSaveData` 结构为蓝本定义网络消息格式，逐步替换 `LocalGameLogicService` 为远程实现。
  - 静态数据：继续使用 Excel → （服务器/客户端各自的）容器对象的模式，前后端共用一份源表。

- **Excel 表驱动**：
  - 大量同构、易表格化的数据（掉落表、刷怪表、经验表、技能表）统一由 Excel/CSV 驱动；
  - Editor 工具负责生成 SO 容器；
  - 运行时代码只依赖容器，不直接读原始表。

---

## 5. 背包行列逐级激活规则（补充说明）

- **逐级激活起点**：
  - 行：从“第1行填满”开始激活第1级。
  - 列：从“第1列填满”开始激活第1级。

- **逐级激活递进条件**：
  - 行第2级起，需要在已完成基础上，连续填满更多行（例：第2级需再填满2行，第3级需再填满3行，依次递进）。
  - 列同理：后续等级需要连续填满更多列。

- **奖励递增但保持微幅**：
  - 每一级的提升只比上一级“略高”，保持“微增幅+清晰正反馈”的强度。

- **奖励分工**：
  - 行激活：倾向技能点/机制微增幅（技能点用于投资到职业构筑物品链接激活的技能树上）。
  - 列激活：倾向属性微增幅（HP、先攻、命中、豁免等错开轮换）。

- **星标提示**：
  - 行/列每达到一个新等级即点亮对应星标，用于提示当前激活等级与下一阶段目标。

- **策略权衡目标**：
  - 玩家需在“形状连接触发机制”与“行列逐级填满”之间做权衡，以最大化当前构筑收益。

- **物品连接机制**：

  - 分2种，分别在物品上对应两种不同外观的链接标记区分：（对应的机制不同）

    - 物品共生关系：特定套装组成的特别机制加成，参考《暗黑2》经典的几件套装组合机制，2件+某种机制，3件再额外增加机制以此类推，同时与职业构筑流派专长机制的物品链接机制保持既有相互扶持又存在排斥的情况。如一套"收割者"套装要求盗贼必须收集：收割者匕首，收割者兜帽（头部）和收割者披风3件套装装备才能激活对应机制，但同时不影响下面的专长构筑中巫师杀手Mage Slayer:(需要等级4+)物品链接:任何近战武器+任何披风的激活条件。套装即享受套装机制，又享受专长机制，同时又不影响其他套装的激活。但专长机制必须满足物品的类别激活前提（背包栏里必须具备披风类装备和近战武器类装备）。

    - 物品共生关系不会激活任何技能系统，纯粹是一种机制加成，如特定物品替换就失去了当前套装共生关系给的加成。比如在背包里拥有2件收割者不同部件情况下替换另外一件普通披风，就失去了“收割者”套装的机制加成。但另一件普通披风和“收割者”匕首仍然可以激活职业构筑流派专长的机制（因为满足了专长激活前提）。

    - 职业构筑流派专长机制：基于职业背景的特殊构筑需求，提供与特定职业相关的装备联动效果，强调职业特色与装备搭配的结合。必须满足物品的类别激活前提（背包栏里必须具备对应类别的装备）。比如上面提到的巫师杀手，必须满足装备栏里必须具备近战武器类装备和披风类装备。近战武器和披风可以是任意近战武器和披风，但必须满足是这个物品的类别才能激活，不能是腰带和头盔（那会激活另外的专长）

    - 职业构筑同时会激活技能树，前文提到的背包行填满奖励的技能点可以投资在技能树学习速率上，从而激活职业的专长机制。

### 6. 盗贼潜行机制（横版回合制 - 5e对抗检定）

- **动作占用**：潜行是本回合的“动作”，当前版本不启用附赠动作。
- **失败惩罚**：无额外惩罚，仅消耗本回合动作。
- **检定规则**：按 5e 标准“潜行 vs 被动察觉”对抗。
  - 潜行检定：`d20 + 敏捷调整值 + 潜行熟练（若有） + 其他加成`
  - 被动察觉：`10 + 感知调整值 + 察觉熟练（若有）`
  - 先按标准规则实现，后续按测试结果再调整难度与加成。
- **成功效果**：进入潜行状态（不可被选中 + 视觉特效）。
- **状态解除**：无论下一回合是否命中，潜行状态都会在下一回合行动后解除。
- **偷袭触发**：潜行成功后，下一回合首击附加偷袭骰伤害（按 5e 等级阶梯）。

### 7. 专长与职业-装备联动（横版2D回合制，5e参考）

- **设计目标**：
  - 专长遵循 5e 规则精神，但以“战术机制触发”为主，不以数值堆叠为主。
  - 装备联动优先于等级纯数值，强调构筑与摆放策略。
  - 横版回合制约束：前排位移近战、后排原地远程/施法、先攻与回合节奏重要。

- **联动结构**：
  - 职业专长提供“触发条件/触发窗口/触发上限”。
  - 装备（武器/护甲/饰品/法器）提供“机制来源”。
  - 行/列激活只做“微幅强化”，不改变核心机制；用于后续扩展。

- **战士（Fighter）**：
  - 盾反系：装备盾牌时，受击后下一次近战攻击追加小额反击伤害（每回合上限1次）。
  - 重甲推进：穿重甲的近战位移攻击后获得短暂减伤或AC微增幅（1回合）。

- **盗贼（Rogue）**：
  - 轻甲+灵巧武器：先攻优势或首击偷袭强化。
  - **潜行机制（统一纳入本规则块）**：
    - 动作占用：潜行占用本回合动作（当前不启用附赠动作）。
    - 失败惩罚：无额外惩罚，仅消耗本回合动作。
    - 对抗检定：按 5e 标准“潜行 vs 被动察觉”。
      - 潜行检定：`d20 + 敏捷调整值 + 潜行熟练（若有） + 其他加成`
      - 被动察觉：`10 + 感知调整值 + 察觉熟练（若有）`
    - 成功效果：进入潜行状态（不可被选中 + 视觉特效）。
    - 状态解除：无论下一回合是否命中，潜行状态在下一回合行动后解除。
    - 偷袭触发：潜行成功后，下一回合首击附加偷袭骰伤害（按 5e 等级阶梯）,注意必须是灵巧或远程武器且对目标具有优势。
    - 优势解释：如果潜行成功，下一回合使用符合条件的武器攻击该目标时，此时你的盟友也对该目标产生威胁（如敌方目标被你战士队友的攻击范围所能威胁
  到，则视为你对该目标具有优势），如果没有任何盟友能威胁到该目标，则此次你的攻击没有优势就无法偷袭，只有正常攻击伤害骰。

- **游侠/弓手（Ranger）**：
  - 远程专注：弓类武器连续命中同一目标时提供小幅命中/伤害提升（有限叠加）。
  - 弹药联动：特定箭矢与弓连接触发穿刺/破甲等机制。

- **法师（Wizard）**：
  - 法器共鸣：装备法器后法术普通攻击命中时获得下次法术微增幅（不叠属性）。
  - 专注守恒：法袍/法器联动降低被打断的概率（以豁免加成或小额优势表现）。

- **牧师（Cleric）**：
  - 圣徽守护：装备圣徽/盾时首个受击获得微弱减伤或短暂AC提升。
  - 祈福回响：法术命中后触发轻度回复或战斗内一次性微增益。

- **数值边界与上限**：
  - 专长触发遵循“每回合/每战斗上限”。
  - 装备机制优先，专长仅作为触发与微增强，不取代装备。
  - 行/列增强仅提供微幅数值或触发频率调整，严禁改变核心机制。

- **对接扩展**：
  - 行激活：用于提升“机制触发频率/触发窗口”或提供轻度技能点微增幅。
  - 列激活：用于HP/先攻/命中/豁免等微属性错开轮换。

### 8. 1-4级专长规则（通用专长，中文本地化）
专长分为2种大类：通用专长和战斗风格，所有职业的构筑均由玩家在这些分类中DIY搭配来组合

- **专长数量**：
  - Lv1：自选 1 个专长。
  - Lv2-Lv3：不新增专长。
  - Lv4：获得第 2 个专长。

- **通用专长定义**：
  - 根据先决等级和属性条件，以及特定物品链接激活。不区分职业专属；战士/盗贼/法师/牧师/游侠均可自由选择搭配。
  - 专长以“战术机制触发”为主，不以纯数值堆叠为主。

- **与装备拼接/行列加成的联动**：
  - 装备拼接触发“机制来源”有2种：
    - 普通拼接加成的物品共生关系机制(无需专长激活)，如战士的盾反、游侠的远程专注等。
    - 专长拼接加成的特殊战斗机制(需要专长激活)，如下面通用专长等。
  - 行激活：填满激活次级技能点（不改变核心机制，用来加速次级技能学习增幅物品共生关系机制）。v
  - 列激活：用于微属性加成（HP/AC/先攻/属性等）。

- **通用专长清单（整合版，5e通用专长本地化）：*注意以下所有需求均需满足才能选择该专长，且不计算装备增加的属性值，专长提供的属性提升可以计算在内*
  - 属性值提升(Ability Score Improvement)：(需要等级4+)选择一个属性提升2点，或两个属性各提升1点（不超过20）。可重复选择本专长。
  无物品链接激活条件，选择后直接提升属性值。
  
  - 物品链接必须满足完全词条条件才激活对应专长，从而和普通物品链接的共生关系加成机制区分开，从而增加策略构筑深度：如任何近战武器=所有近战武器，
单手近战轻型武器=必须是单手持握，同时是近战武器，且是轻型武器;同样，任何护甲=所有护甲，重甲=护甲同时必须是重甲，双手重型武器=是双手武器同时必须
是重型武器，盾牌=必须是盾牌，法器=必须是法器等。
  
  - 远动员Athlete：(需要等级4+,力量或敏捷13+)物品链接激活条件:鞋子+任何护手
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 攀爬速度Climb Speed:特技Acrobatics和运动Athletics相关检定获得优势（如攀爬、跳跃等）。
    - 鲤鱼打挺Hop Up：处于倒地状态时，当前回合如还有动作可以在本角色行动时使用动作起身，无该专长的角色需在下一回合才能使用动作起身。

  - 冲锋手Charger：(需要等级4+,力量或敏捷13+)物品链接激活条件:鞋子+任何近战武器
    - 力量提升1，最多提升到20(Ability Score Increse)。
    - 进阶疾走Improved Dash:使用疾走时候，下一回合先攻+4,无此专长的角色使用疾走时下一回合先攻+2,持续1回合。
    - 冲锋攻击Charge Attack:在攻击前如果使用了疾走，则下一回合攻击获得额外伤害加成，伤害值为角色等级的一半（向下取整），每回合只能触发一次。

  - 强弩专家CrossBow Expert：(需要等级4+,力量或敏捷13+)物品链接激活条件:两把手弩
    - 敏捷提升1，最多提升到20(Ability Score Increse)。
    - 近战射击：使用弩类武器时，可以在近战范围内攻击（敌人近战威胁不会对角色持有远程武器产生劣势），无此专长角色如果手持远程武器（包括弩）
  时被敌人近战单位威胁（前排队友阵亡，后排直接面对敌人近战单位）时，武器攻击会被强加劣势。
    - 双持射击：当你发动由轻型词条的手弩使用附赠动作发动副手追加攻击时，则此次伤害可以加入属性调整值；无此专长角色双持轻型手弩使用附赠动作追加
  副手攻击时不计算属性调整值。

  - 防御式决斗Defensive Duelist:(需要等级4+,敏捷13+)物品链接激活条件:近战灵巧武器+鞋子
    - 敏捷提升1，最多提升到20(Ability Score Increse)。
    - 招架Parry：当你使用灵巧武器被近战攻击命中时，可以使用反应将你当前等级的熟练加值加入到你的AC中。

  - 双持客Dual Wielder:(需要等级4+,力量或敏捷13+)物品链接激活条件:两把单手任何近战武器
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 强化双持Enhanced Dual Wielding：允许你副手也持有非轻型武器，并且当你使用双持攻击时，主手和副手的攻击都可以加入属性调整值。

  - 耐性Durable:(需要等级4+)物品链接激活条件:项链/戒指+任何护甲
    - 体质提升1，最多提升到20(Ability Score Increse)。
    - 悍不畏死Defy Death: 你进行死亡豁免具有优势
    - 高速恢复Speedy Recovery: 以一个附赠动作，你可以消耗并投掷一枚生命骰，恢复投掷结果的生命值

  - 粉碎者Crusher:(需要等级4+)物品链接激活条件:钝击类型近战武器+任何护甲
    - 力量提升1，最多提升到20(Ability Score Increse)。
    - 推击Push：每回合一次，当你使用钝击伤害的近战武器攻击命中时，只要目标的体型不比你大一级，既可以对其施加先攻-1的debuff。
    - 暴击强化Enhanced Critical：当你使用钝击伤害的近战武器攻击暴击时，直到你的下一回合，目标受到一个任何对其的攻击都视作优势的debuff。

  - 元素掌控Elemental Adept:(需要等级4+,施法或契约魔法特性)物品链接激活条件：法袍/长棍+法器,本专长可复选，但每次需更换
  不同类型元素
    - 智力/感知/魅力择一提升1，最多提升到20(Ability Score Increse)。
    - 能量掌控Energy Mastery：选择一种元素伤害类型（火/冰/电/酸/声波），你使用该类型的法术攻击时，忽略目标对该类型伤害的抗性，
  并且当你掷伤害骰时，任何结果为1的骰子都视作2。

  - 妖精触碰Fey Touched:(需要等级4+)物品链接激活条件:项链/戒指+任何护手/法器
    - 智力/感知/魅力择一提升1，最多提升到20(Ability Score Increse)。
    - 妖精魔法Fey Magic：选择一道预言或惑控学派的一环法术。你始终准备着你选择的这道法术和迷踪步，一次长休前可以无需法术位施展它们。
  长休后重获施展这2个法术的能力。同时你也能使用合适环阶的法术位来施展它们。

  - 擒抱者Grappler:(需要等级4+,力量或敏捷13+)物品链接激活条件：任何护手+腰带
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 连擒带打Punch and Grab:当你在攻击中用徒手打击命中一个目标，且目标体型不大于你，你可以对目标同时造成伤害和擒抱状态，
  每回合只能使用一次。
    - 优势攻击Attack Advantage:当你对一个被你擒抱的目标进行攻击时，你的攻击具有优势。

  - 巨武器大师Great Weapon Master:(需要等级4+,力量13+)物品链接激活条件：双手重型近战武器+任何护甲
    - 力量提升1，最多提升到20(Ability Score Increse)。
    - 重武器掌握Heavy Weapon Master:当你使用双手重型近战武器攻击时，攻击造成的伤害可以加入你当前等级的熟练加值
    - 顺势斩Hew:当你使用双手重型近战武器重击或将一个目标生命降至0时，你可以使用一个附赠动作再进行一次近战武器攻击。

  - 重甲大师Heavy Armor Master:(需要等级4+,重甲熟练)物品链接激活条件：重甲+任何头盔/项链
    - 力量或体质提升1，最多提升到20(Ability Score Increse)。
    - 伤害减免Damage Reduction:当你穿着重甲时，任何对你造成的钝击/穿刺/挥砍伤害均减去你当前等级的熟练加值。

  - 领袖之证Inspiring Leader:(需要等级4+,魅力13+)物品链接激活条件：项链/戒指+任何头盔
    - 感知或魅力提升1，最多提升到20(Ability Score Increse)。
    - 激励演出Bolsering Performance: 每当你的短休/长休结束时，你可以做一场激励人心的演讲:你和你的队友均获得一个临时生命值，
  数值等于你的角色等级+你通过这个专长选择提升的属性总调整值（如你选17感知+1=18则4调整值，魅力亦然)

  - 巫师杀手Mage Slayer:(需要等级4+)物品链接激活条件:任何近战武器+任何披风
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 专注中断手Concentration Breaker:当你对一名正处于专注中的目标造成伤害时，该目标为维持本次专注所作的豁免检定具有劣势。
    - 审慎护心Guarded Mind:当你的智力/感知/魅力豁免失败时，你可以将其改为成功一次。短休/长休恢复此能力。

  - 中甲大师Medium Armor Master:(需要等级4+,中甲熟练)物品链接激活条件:中甲+腰带
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 灵敏着装Dexterous Wearer:穿着中甲期间，若你的敏捷在16或更高，你可以在AC计算中将敏捷调整值的上限提高到3。
  
  - 轻甲大师Light Armor Master:(需要等级4+,轻甲熟练)物品链接激活条件:轻甲+披风
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 轻甲韧性Light Armor Toughness:当你穿着轻甲时，当你受某效应影响而需用敏捷豁免来判定”只受一半伤害"时，你的敏捷豁免具有优势。
    
  - 穿刺者Piercer:(需要等级4+)物品链接激活条件:穿刺类型武器(含远程)+任何护手
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 穿刺伤Puncture:每回合一次，当你造成穿刺伤害时，可以重骰这次攻击伤害的一个伤害骰取大值。
    - 强化重击Enhanced Critical:当你使用穿刺伤害的武器重击时，多加一个当前武器的伤害骰。但增加的伤害骰不计算重击翻倍和属性调整值。

  - 毒师Poisoner:(需要等级4+)物品链接激活条件:近战灵巧武器+法袍
    - 敏捷或智力提升1，最多提升到20(Ability Score Increse)。
    - 强效毒素Potent Poison:你造成毒素伤害的伤害骰无视对方毒素伤害抗性。
    - 涂毒BrewPoison：你可以使用一个附赠动作为一把近战或远程武器涂毒，持续1次攻击。被涂毒武器命中的目标，需通过一次体质豁免
  (DC=8+你的熟练加值+你通过这个专长选择提升的属性总调整值)来抵抗中毒状态，否则将受到2d8毒素伤害并且在接下来的一回合处于中毒状态。

  - 长柄武器大师Polearm Master:(需要等级4+,力量或敏捷13+)物品激活条件:长触及武器+任何护甲
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 长柄打击Pole Strike:当你使用长触及武器攻击后，可以立即使用附赠动作，使用该武器的另一端发动一次1d4+属性调整值的钝击伤害。

  - 强健身心Resilient:(需要等级4+)物品激活条件:任何护手/头盔+任何披风/腰带
    - 选择一个你不具有其豁免熟练的属性提升1点，最多提升到20(Ability Score Increse)。
    - 熟练豁免Saving Throw Proficiency:你获得所选属性的豁免熟练。

  - 仪式施法者Ritual Caster:(需要等级4+,智力/感知/魅力中一项13+)物品激活条件:鞋子+长棍/法器
    - 智力/感知/魅力选择一项提升1，最多提升到20(Ability Score Increse)。
    - 仪式法术Ritual Spells:你选择数道具有仪式标签的一环法术，数量等于你的熟练加值。你始终准备着这些法术，并且可以在没有法术位的
  情况下以仪式方式施展它们各一次，施法属性为你用此专长提升的属性，使用后直到下一次长休才能恢复这几道法术。随着你的熟练加值提升，
  你可以选择更多的仪式法术。

  - 哨兵Sentinel:(需要等级4+,力量或敏捷13+)物品激活条件:任何近战武器+项链
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 守护者Guardian:当敌人穿过你的位置攻击你的队友时，你可以使用反应将敌人此次攻击强加劣势（每回合一次）。
    - 阻拦Halt:当你的借机攻击命中一名敌人时，该敌人下一回合先攻-2

  - 影界触碰Shadow Touched:(需要等级4+)物品激活条件:任何护手/披风+法器
    - 智力/感知/魅力择一提升1，最多提升到20(Ability Score Increse)。
    - 影界魔法Shadow Magic：选择一道幻术或死灵学派的一环法术。你始终准备着你选择的这道法术和隐形术，一次长休前可以无需法术位施展它们。
  施法属性是你以此专长提升的属性，你可以使用合适环阶的法术位来施展它们。

  - 神射手Sharpshooter:(需要等级4+,敏捷13+)物品激活条件:远程武器+任何腰带
    - 敏捷提升1，最多提升到20(Ability Score Increse)。
    - 绕过掩体Bypass Cover:当你使用远程武器攻击时，无视目标的掩体（如半掩体/全掩体）
    - 抵近射击Firing in Melee:当你使用远程武器攻击一个手持近战武器威胁到你的目标时，攻击不受劣势影响。

  - 盾牌大师Shield Master:(需要等级4+,盾牌熟练)物品激活条件:盾牌+任何单手近战简易武器
    - 力量提升1，最多提升到20(Ability Score Increse)。
    - 盾击Shield Bash:当你的主手武器命中目标时，如果你装备着盾牌，你可以立即使用一个附赠动作发动一次额外盾牌攻击，迫使目标进行一次力量豁免
  (DC=8+你的熟练加值+你通过这个专长选择提升的属性总调整值)，失败则陷入倒地状态，每回合你只能使用一次本能力。
    - 介入盾牌Interpose Shield:当你受某效应影响而需用敏捷豁免来判定”只受一半伤害"时，若你豁免成功且装备着盾牌，可以使用反应动作来完全免伤。

  - 潜伏者Skulker:(需要等级4+,敏捷13+)物品激活条件:鞋子+任何披风
    - 敏捷提升1，最多提升到20(Ability Score Increse)。
    - 战争迷雾Fog of War:你使用“潜行"动作时进行的隐匿检定具有优势。
    - 狙击手Sniper:当你在潜行状态时使用攻击时，即使攻击没命中，你依然可以保持潜行状态

  - 劈砍者Slasher:(需要等级4+)物品激活条件:挥砍类型近战武器+任何头盔
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 伤筋Hamstring:每回合一次，当你使用挥砍武器伤害到一名敌人时，可使其下一回合先攻-2
    - 强化重击Enhanced Critical:当你使用挥砍伤害的武器重击时，该敌人的所有攻击检定都会具有劣势，持续到你的下一回合。

  - 法术射手Spell Sniper:(需要等级4+,施法者或契约魔法特性)物品激活条件:法袍/法器+任何远程武器
    - 智力/感知/魅力选择一项提升1，最多提升到20(Ability Score Increse)。
    - 抵近施法Casting in Melee:当你使用具有攻击词条的法术攻击一个手持近战武器威胁到你的目标时，攻击不受劣势影响。
    - 绕过掩体Bypass Cover:当你使用一个具有攻击词条的法术攻击时，无视目标的掩体（如半掩体/全掩体）。

  - 念动力Telekinetic:(需要等级4+)物品激活条件:任何头盔+任何护手
    - 智力/感知/魅力选择一项提升1，最多提升到20(Ability Score Increse)。
    - 次级心灵遥控Minor Telekinesis:你获得特殊戏法法师之手的高阶版本，可以使用附赠动作让这只灵体手投掷你背包中的一把轻型投掷词条武器

  - 战地施法者War Caster:(需要等级4+,施法者或契约魔法特性)物品激活条件:法器+任何近战武器
    - 智力/感知/魅力选择一项提升1，最多提升到20(Ability Score Increse)。
    - 专注施法Concentration:当你进行维持专注的体质豁免检定时，你可以获得优势。
    - 姿势成分Somatic Components:当你使用一个具有Somatic成分的法术时，即使你的双手被占用（如持盾或双持武器），你也可以正常施法。
    - 反应施法Opportunity Casting:当一个敌人使用近战武器攻击你时，你可以使用反应来施展一个具有攻击词条的接触法术（必须有可用的法术位或戏法）。

  - 武器大师Weapon Master:(需要等级4+)物品激活条件：任何近战或远程武器+戒指
    - 力量或敏捷提升1，最多提升到20(Ability Score Increse)。
    - 精通词条Mastery Property:你自选一种你已经熟练的武器，获得该武器的精通（必须是简单或军用武器）。

-战斗风格专长
- **战斗风格专长**：
  - 战士/游侠可选：防御、双武器、重武器、远程。
  - 盗贼/法师/牧师不提供战斗风格专长，强调通用专长的搭配自由度。
  - 战斗风格专长提供更明显的机制强化（如双武器专精的回合首击额外攻击），但仍受行/列微增幅约束，保持整体平衡。

战斗风格专长清单（整合版，5e战斗风格本地化）：
  - 防御(Defense)：穿甲/重甲时获得微增幅（+1AC）。
  - 箭术(Archery)：远程攻击获得增幅（+2攻击加值）。
  - 盲斗(Blind Fighting)：可以无视“潜行”状态的目标无法选择的机制从而进行选取攻击，但对此类目标受到-2攻击减值。
  - 双武器(Two-Weapon Fighting)：当你副手持有轻型武器时AC+1,且使用副手发动追加攻击时，伤害可以增加你的攻击属性调整值。
  - 巨武器(Great Weapon Fighting)：使用双手近战武器时，伤害骰的任何1和2都视作3
  - 对决(Dueling)：当你使用单手近战武器时且副手空着时，伤害增加+2。
  - 拦截(Interception)：当队友被敌人近战攻击时候，可以使用反应来减少攻击造成的伤害，减少量等于你当前的熟练加值（每回合一次）。
必须手持一块盾牌或者一把简易/军用武器来使用这个专长。
  - 守护(Protection):当队友被敌人远程攻击时候，如果你正手持一面盾牌，你可以使用反应将此次敌人攻击强加劣势（每回合一次）。
  - 投掷(Thrown Weapon Fighting)：当你使用投掷词条武器攻击命中时，伤害骰+2加值。
  - 徒手(Unarmed Fighting)：当你使用徒手攻击命中时,伤害骰变成1d6+力量调整值。如果你副手不持任何武器和盾，则1d8+力量调整值。


