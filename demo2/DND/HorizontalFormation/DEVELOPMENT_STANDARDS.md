# DND5- **✅ 强制要求**：与用户的所有对话必须使用**中文**

- **✅ 代码注释**：所有代码注释必须使用中文
- **✅ 变量命名**：使用标准英文命名规范（驼峰命名法等）
- **✅ 文档内容**：所有.md 文档内容使用中文
- **❌ 严禁行为**：因为用户点击英文按钮选项而切换成英文回复
- **❌ 严禁行为**：在任何情况下使用英文与用户交流（除非是代码本身的关键字）系统 - 工程编码规范

## 📋 核心开发原则

### �️ **文档保护和自动化管理规范**

- **🔒 绝对禁止删除本文档**：DEVELOPMENT_STANDARDS.md 是项目核心规范，严禁删除或移动
- **🔄 VSCode 启动自动执行**：每次 VSCode 开启后必须自动读取本文档内容
- **🧹 自动清理机制**：基于本文档规范自动删除所有弃用文件和空文件
- **📚 唯一文档原则**：所有业务逻辑规则、Unity 配置说明、手动设置指南均记录在本文档中
- **❌ 严禁创建其他文档**：不允许创建任何其他.md 技术文档或设置指南
- **✅ 文档集中管理**：本文档作为项目唯一技术标准和操作手册

### �🗣️ **对话语言规范**

- **✅ 强制要求**：与用户的所有对话必须使用**中文**
- **✅ 代码注释**：所有代码注释必须使用中文
- **✅ 变量命名**：使用英文命名规范（驼峰命名法等）
- **✅ 文档内容**：所有.md 文档内容使用中文
- **❌ 严禁行为**：因为用户点击英文按钮选项而切换成英文回复
- **❌ 严禁行为**：在任何情况下使用英文与用户交#### **1. 创建核心 GameObject#### **1. 创建核心 GameObject (挂机模式推荐)\*\*

````
Hierarchy结构:
├── IdleGameSystem ```csharp
// IdleGameManager Inspector 强制引用设置

[挂机模式设置]
✅ idleModeEnabled = false (启动时需手动开启)
✅ encounterInterval =## 📝 **总结**

**核心原则：严格手动挂载、强关联、单一路径、中文、文档保护**

- 🎯 **强制手动挂载** - 所有组件引用必须手动设置，禁止自动查找
- 💪 **强关联关系** - IdleGameManager 必须引用 FormationManager 和 AutoBattleAI
- 🗑️ **删除弃用逻辑** - 移除 CombatManager、FindObjectOfType 等过时代码
- 📋 **单一配置路径** - 每个配置只有一种标准实现方式
- ✅ **质量第一** - 确保每个脚本都编译无误且功能完整
- 🗣️ **中文交流** - 所有对话、注释、文档均使用中文
- 🔒 **文档保护** - 绝对禁止删除DEVELOPMENT_STANDARDS.md，自动读取和清理

**🔒 文档保护要求（新增核心规范）**:

1. **绝对保护**：DEVELOPMENT_STANDARDS.md 文件在任何情况下不得删除或移动
2. **自动读取**：VSCode启动时必须自动读取本文档并应用所有规范
3. **自动清理**：根据本文档违规检查清单自动删除弃用文件和空文件
4. **唯一标准**：所有技术说明、配置指南、业务逻辑均记录在本文档中
5. **禁止分散**：严禁创建其他技术文档，维护文档的统一性和权威性

**关键要求**:

1. IdleGameManager + HorizontalBattleFormationManager + AutoBattleAI 三位一体
2. 手动拖入所有组件引用，禁止代码自动查找
3. 删除所有不使用的弃用逻辑和组件
4. 确保强关联关系，避免空引用错误
5. **保护DEVELOPMENT_STANDARDS.md文档完整性和唯一性**

**记住：统一手动挂载，强关联设计，删除弃用逻辑，保护核心文档。**battleSpeed = 1.0f (战斗速度倍率)

[探索设置]
✅ currentStage = 1 (当前关卡)
✅ currentWave = 1 (当前波次)
✅ stageProgressPercent = 0f (关卡进度百分比)

[队伍配置]
✅ playerParty: 手动拖入场景中的玩家角色 (CharacterStats列表)

[预制体配置]
✅ playerPrefabs: 手动拖入玩家角色预制体列表
✅ enemyPrefabs: 手动拖入敌人角色预制体列表

[队伍生成设置]
✅ playerPartySize = 3 (玩家队伍人数，推荐1-6人)
✅ usePlayerPrefabs = false (是否使用预制体生成，建议设为true)

[系统组件 - 强制手动引用]
✅ formationManager: 手动拖入同场景中的HorizontalBattleFormationManager组件
✅ autoBattleAI: 手动拖入同场景中的AutoBattleAI组件
```ct)
│   ├── IdleGameManager (添加IdleGameManager脚本)
│   ├── HorizontalBattleFormationManager (添加HorizontalBattleFormationManager脚本)
│   └── AutoBattleAI (添加AutoBattleAI脚本)
├── Characters (空GameObject，用于角色容器)
│   ├── PlayerParty (空GameObject，运行时生成的玩家角色父对象)
│   └── EnemyParty (空GameObject，运行时生成的敌人角色父对象)
├── Environment (空GameObject，用于背景等)
│   └── Background (背景图片或滚动脚本)
└── [可选] UI (如果需要UI显示)
    ├── Canvas
    └── 其他UI元素
````

**重要说明**:

- **不要同时添加 CombatManager**: 挂机模式下不需要 CombatManager
- **避免系统冲突**: CombatManager 和 IdleGameManager 同时存在可能产生冲突
- **如果需要 CombatManager**: 建议使用 DND_BattleSceneSetup 自动创建，或手动创建单独的 GameObject

#### **1-备选. 传统战斗模式 Hierarchy (如果需要手动战斗)**

```
Hierarchy结构:
├── BattleSystem (空GameObject)
│   ├── CombatManager (添加CombatManager脚本)
│   ├── DND_BattleSceneSetup (添加DND_BattleSceneSetup脚本，可自动创建CombatManager)
│   └── HorizontalBattleFormationManager (添加HorizontalBattleFormationManager脚本)
├── Characters (预先放置的角色)
├── UI (战斗UI界面)
└── Environment
```

```
Hierarchy结构:
├── IdleGameSystem (空GameObject)
│   ├── IdleGameManager (添加IdleGameManager脚本)
│   ├── HorizontalBattleFormationManager (添加HorizontalBattleFormationManager脚本)
│   └── AutoBattleAI (添加AutoBattleAI脚本)
├── Characters (空GameObject，用于角色容器)
│   ├── PlayerParty (空GameObject，运行时生成的玩家角色父对象)
│   └── EnemyParty (空GameObject，运行时生成的敌人角色父对象)
├── Environment (空GameObject，用于背景等)
│   └── Background (背景图片或滚动脚本)
└── [可选] UI (如果需要UI显示)
    ├── Canvas
    └── 其他UI元素
```

**注意**:

- 如果只使用挂机模式，不需要添加 CombatManager 到场景
- 角色会由 IdleGameManager 在运行时自动生成
- AutoBattleAI 负责处理自动战斗逻辑

#### **2. IdleGameManager 配置**

**说明**：本项目为中文项目，所有交流、文档、注释均应保持中文一致性。

### ⚠️ **严格禁止项**

1. **禁止创建测试/自动化脚本**

   - ❌ 不创建任何形式的测试脚本
   - ❌ 不创建自动化设置工具
   - ❌ 不创建检查器或验证脚本
   - ❌ 不创建演示/Demo 脚本
   - ✅ 只创建核心功能脚本

2. **避免多余逻辑**

   - ❌ 不添加不必要的辅助功能
   - ❌ 不创建"便利工具"
   - ❌ 不添加过度的错误检查
   - ✅ 专注核心业务逻辑

3. **防止编译错误**

   - ❌ 不引入可能导致编译错误的额外依赖
   - ❌ 不使用实验性或不稳定的代码
   - ✅ 确保每个脚本都是必需且稳定的

4. **强制错误清理**

   - ❌ 严禁在 IDE 的 PROBLEMS 面板有任何报错的情况下声称功能完成
   - ❌ 所有编译错误、语法错误、引用错误必须完全解决
   - ✅ 只有在 0 错误、0 警告的状态下才能宣布功能实现完成
   - ✅ 每次代码修改后必须验证 PROBLEMS 面板清洁状态
   - 🔧 **工作流程**：功能实现 → 错误检查 → 修复所有问题 → 再次验证 → 确认完成

5. **全项目编译检查**

   - ❌ 严禁仅检查当前修改脚本的编译状态就声称完成
   - ❌ 即使当前脚本无编译错误，也必须检查 IDE 中所有其他脚本的编译错误
   - ✅ 必须解决项目中所有脚本的编译错误，不論是否与当前任务直接相關
   - ✅ 只有 IDE 的 PROBLEMS 面板显示"0 个问题"时才能向用户报告任务完成
   - ⚠️ **检查范围**：包括但不限于 Assets/、Plugins/、Editor/等所有目录下的 C#脚本
   - 🔧 **验证流程**：当前任务完成 → 全项目编译检查 → 修复所有发现的错误 → 再次全项目检查 → 确认零错误状态 → 报告完成

6. **全项目编译检查**

   - ❌ 严禁仅检查当前修改的脚本，必须检查整个项目的编译状态
   - ❌ 即使当前脚本无错误，如果项目中存在其他脚本的编译错误也不能声称完成
   - ❌ 跨脚本引用错误、依赖问题、命名冲突等问题必须全部解决
   - ✅ 必须确保整个项目的 PROBLEMS 面板显示 0 个编译错误
   - ✅ 所有脚本文件都能正常编译，无任何报错或警告
   - 🔧 **检查范围**：不仅限于当前修改的文件，包括所有相关的依赖文件和引用文件
   - 🔧 **完成标准**：只有当整个项目编译完全成功时才能告知用户功能完成

7. **配置方法单一化原则**

   - ❌ 严禁为同一配置创建多种优先级或多种实现途径
   - ❌ 不创建"优先级 1、优先级 2、优先级 3"这样的复杂配置逻辑
   - ❌ 不提供"手动配置 → 引用配置 → 自动查找"等多重备选方案
   - ✅ 每个配置项仅提供一种最直接、最可靠的配置方法
   - ✅ 选择最不容易出错的单一配置方式作为唯一实现
   - ✅ 避免代码复杂化，保持简洁单一的配置路径
   - ⚠️ **例外情况**：只有在用户明确告知有特殊需求时才考虑多种配置方式
   - 🔧 **实施原则**：一个配置 = 一种方法 = 一处设置 = 一个入口

   **说明**：避免过度工程化的配置系统，减少代码复杂度和维护成本。

8. **弃用代码强制删除原则**

   - ❌ 严禁保留任何标记为[System.Obsolete]的方法、类或属性
   - ❌ 严禁保留任何包含"弃用"、"已弃用"、"⚠️ 弃用"等标记的代码
   - ❌ 严禁保留任何弃用警告注释或弃用说明文档
   - ❌ 严禁保留整个弃用的脚本文件，必须直接删除
   - ✅ 一旦确定代码弃用，立即从项目中完全删除
   - ✅ 弃用的功能必须用新的标准实现完全替代
   - ✅ 删除弃用代码后确保编译成功且功能完整
   - ⚠️ **删除范围**：包括弃用的字段、方法、类、整个文件
   - 🔧 **执行标准**：弃用 = 立即删除，不保留任何痕迹

   **说明**：避免代码库中积累弃用内容，保持代码的现代性和清洁性。弃用的代码会增加维护成本和混淆性，必须彻底清理。

9. **定期文件清理原则**

   - ❌ 严禁保留空文件或仅包含注释的无效文件
   - ❌ 禁止保留以下后缀的文件：`_Fixed.cs`、`_Backup.cs`、`_Old.cs`
   - ❌ 严禁保留任何测试、演示、验证类型的脚本文件
   - ✅ 定期清理项目中的无效和违规文件
   - ✅ 确保每个文件都有明确的核心业务价值
   - ✅ 所有文件都必须符合命名规范和功能要求
   - ⚠️ **清理范围**：包括 git 恢复的旧文件、IDE 自动生成的备份文件
   - 🔧 **清理标准**：文件必须包含实际的核心功能代码，不能是空文件或纯注释文件

   **说明**：避免项目中积累无效文件，保持代码库的整洁性和专业性。

---

    - 🧹 **已删除的空文件/违规文件**：

      - ❌ `ActionPanelDebugTools.cs` - 空文件，违反调试工具禁令
      - ❌ `DND_BattleSceneSetupEditor.cs` - 空文件，违反编辑器工具禁令
      - ❌ `DND_BattleSceneSetupEditor_New.cs` - 空文件，含"\_New"后缀违规
      - ❌ `DND_SystemVerifier.cs` - 空文件，违反验证器工具禁令
      - ❌ `BattleFormationManager.cs` - 空文件，无实际功能价值
      - ❌ `DNDSceneUpgrader.cs` - 空文件，违反自动化工具禁令
      - ❌ `LinearFormationSceneIntegrator.cs` - 空文件，违反集成器工具禁令

    - 🔧 **已修复的代码规范问题**：

      - ✅ `DND_CharacterAdapter.cs` - 修复 11 处 var 关键字使用，改为明确类型声明
      - ✅ `AnimationController.cs` - 修复 6 处 var 关键字使用
      - ✅ `SpellEffects.cs` - 修复 2 处 var 关键字使用

    - 📊 **清理统计**：

      - 删除空文件：7 个
      - 修复 var 警告：19 处
      - 涉及文件：10 个
      - 清理时间：2025-08-21

    - ✅ **验证结果**：
      - 核心业务脚本编译状态：0 错误，0 警告
      - 项目结构：简洁，无冗余文件
      - 代码规范：符合明确类型声明要求

11. **紧急清理记录 (2025-08-26)**

- 🚨 **发现并删除的违规文件**：

  - ❌ `PhysicsMovementSetup.cs` - 设置助手工具，违反开发规范，包含 10 个编译错误
    - 引用已删除的`PhysicsMovementValidator`类 (8 处)
    - 使用 var 关键字 (2 处)
    - 违反"禁止设置工具"规范
    - 文件大小：约 300 行，包含复杂的物理设置逻辑

- � **重构的核心业务文件**：

  - ✅ `LevelData.cs` - 修复 9 个编译错误，重构为纯核心业务功能
    - 移除对已删除`PhysicsMovementValidator`类的依赖
    - 保留核心的位置验证和边界限制功能
    - 简化为基于矩形区域的检测逻辑
    - 维持完整的 API 兼容性

- �📊 **本次清理统计**：

  - 删除违规文件：1 个
  - 重构核心文件：1 个
  - 解决编译错误：19 个 (10 + 9)
  - 清理时间：2025-08-26

- ✅ **清理后验证**：
  - 项目编译状态：0 错误，0 警告
  - 违规文件检查：已全部清除
  - 核心功能：完全保持，API 兼容
  - 代码规范：100% 符合要求

11. **弃用代码清理记录 (2025-08-26)**

- 🚨 **删除的弃用脚本文件**：

  - ❌ `CombatManager.cs` - 弃用的战斗管理器，违反新架构规范
  - ❌ `SimpleEnemyAI.cs` - 弃用的敌人 AI，依赖于 CombatManager
  - ❌ `DND_BattleUI.cs` - 弃用的战斗 UI，挂机模式不需要 UI 交互
  - ❌ `HorizontalBattleIntegration.cs` - 弃用的集成器，违反"禁止集成器"规范

- 🧹 **清理的弃用代码段**：

  - ❌ `IdleGameManager.playerParty` - 弃用字段及其[Obsolete]标记
  - ❌ `FindPlayerPartyFromScene_DEPRECATED()` - 弃用方法完全删除
  - ❌ `GeneratePlayerParty()` - 弃用方法完全删除
  - ❌ `StartHorizontalBattle(参数版本)` - HorizontalBattleIntegration 中的弃用方法

- 📊 **弃用清理统计**：

  - 删除弃用文件：4 个
  - 清理弃用代码段：4 个
  - 移除弃用注释和标记：8+ 处
  - 清理时间：2025-08-26

- ✅ **清理后验证**：
  - 项目中无任何[System.Obsolete]标记
  - 无任何"弃用"相关注释或警告
  - 所有弃用功能已用新架构替代
  - 编译状态：0 错误，0 警告

**说明**：持续监控和清理违规文件，确保项目始终保持专业标准。

---

## 🎯 **开发响应模式**

### 当用户询问 Unity 设置时：

#### ✅ **正确做法：**

- 提供**纯手动设置指南**
- 详细说明 Hierarchy 结构
- 明确组件配置步骤
- 给出具体的 GameObject 设置
- 提供代码集成方法

#### ❌ **错误做法：**

- 创建自动化设置脚本
- 制作设置向导工具
- 开发检查器组件
- 编写测试管理器

---

## 📁 **文件组织规范**

### **核心脚本类型**（允许创建）

```
✅ 系统管理器脚本      (如: BattleFormationManager.cs)
✅ 游戏逻辑组件        (如: HorizontalStealthComponent.cs)
✅ 数据结构定义        (如: HorizontalFormationTypes.cs)
✅ 业务规则系统        (如: HorizontalCombatRules.cs)
✅ AI决策系统          (如: AutoBattleAI.cs)
✅ 集成适配器          (如: HorizontalBattleIntegration.cs)
```

### **禁止脚本类型**（严禁创建和保留）

```
❌ XXXTester.cs         - 测试脚本
❌ XXXDemo.cs           - 演示脚本
❌ XXXSetupWizard.cs    - 设置向导
❌ XXXChecker.cs        - 检查器
❌ XXXValidator.cs      - 验证器
❌ QuickSetupXXX.cs     - 快速设置
❌ AutoXXX.cs           - 自动化工具
❌ XXXTestManager.cs    - 测试管理器
❌ XXXTestScene.cs      - 测试场景
❌ XXXTestTrigger.cs    - 测试触发器
❌ XXX_Fixed.cs         - 修复版本文件
❌ XXX_Backup.cs        - 备份文件
❌ XXX_Old.cs           - 旧版本文件
❌ 空文件或仅包含注释的文件
```

---

## 🛠️ **代码质量标准**

### **编译要求**

- ✅ 所有脚本必须 0 编译错误
- ✅ 所有脚本必须 0 编译警告
- ✅ **整个项目**必须能完全编译成功，不仅仅是当前修改的脚本
- ✅ 必须检查并解决项目中所有文件的编译错误
- ✅ 跨文件引用错误、命名冲突、依赖问题必须全部修复
- ✅ 避免使用`var`关键字，使用显式类型声明
- ✅ 确保所有引用的类型和方法存在
- 🔧 **完成验证流程**：修改代码 → 检查当前文件 → 检查整个项目 → 修复所有错误 → 确认 PROBLEMS 面板为空 → 告知用户完成

### **代码结构**

- ✅ 每个脚本专注单一职责
- ✅ 清晰的类和方法命名
- ✅ 适当的注释和文档
- ✅ 合理的访问修饰符使用

### **依赖管理**

- ✅ 最小化外部依赖
- ✅ 使用现有的 DND5E 系统组件
- ✅ 避免循环依赖
- ✅ 明确的组件引用关系

---

## 📖 **文档规范**

### **🔒 文档保护和自动化原则（核心规范）**

- **✅ 文档不可删除**：`DEVELOPMENT_STANDARDS.md` - 项目生命周期内绝对禁止删除
- **✅ VSCode 自动读取**：每次 VSCode 启动必须自动加载本文档内容到工作环境
- **✅ 自动清理执行**：基于本文档违规检查清单自动删除弃用文件和空文件
- **✅ 唯一技术标准**：本文档是项目唯一的技术规范、配置指南、业务逻辑说明
- **❌ 绝对禁止分散**：严禁创建任何其他技术文档、设置指南、配置说明

### **唯一文档原则**

- **✅ 唯一文档**: `DEVELOPMENT_STANDARDS.md` - 项目的唯一技术文档
- **✅ 集中管理**: 所有游戏逻辑、设置指南、开发规范集中在此文档
- **❌ 严格禁止**: 创建任何其他.md 格式的文档文件
- **❌ 严格禁止**: 分散的指南、手册、说明文档

### **文档内容范围**

此文档包含以下所有内容：

```
✅ 开发编码规范          - 代码质量和结构要求
✅ 游戏系统架构          - 核心游戏逻辑和组件说明
✅ Unity手动设置指南     - 完整的场景和组件配置方法
✅ 横版2D布局规范        - 角色位置和阵型布局标准
✅ 动画系统文档          - 角色动画状态和播放控制
✅ API参考和使用示例     - 核心系统的编程接口
✅ 性能优化和最佳实践    - 开发建议和注意事项
```

### **严格禁止的文档类型**

```
❌ 任何新的.md文件       - 无论是指南、手册还是说明
❌ 分散的设置指南        - 所有设置指南已集中在此文档
❌ 独立的API文档         - API说明已整合在系统架构章节
❌ 单独的故障排除文档    - 问题解决方案在相应章节中说明
❌ 临时的开发笔记文档    - 开发信息记录在相应的规范章节
```

### **⚠️ 文档维护原则**

- **集中更新**: 所有技术信息更新都在此文档中进行
- **版本统一**: 避免多个文档版本不一致的问题
- **查找便利**: 使用章节标题和搜索功能快速定位信息
- **内容完整**: 确保此文档包含开发所需的所有技术信息

---

## 🎮 **Unity 集成原则**

### **Hierarchy 设置方式**

1. **纯手动创建**

   - 详细的 GameObject 创建步骤
   - 明确的 Transform 坐标设置
   - 清晰的组件添加说明
   - 具体的 Inspector 配置

2. **引用配置**

   - 手动拖拽组件引用
   - 明确的数组设置步骤
   - 详细的参数配置说明

3. **代码集成**
   - 提供集成代码示例
   - 说明调用方法和时机
   - 明确参数传递方式

### **禁止的集成方式**

- ❌ MenuItem 自动化工具
- ❌ EditorWindow 设置界面
- ❌ 运行时自动检查器
- ❌ ScriptableWizard 向导

---

## 💡 **最佳实践**

### **响应用户需求时**

1. **理解真实需求** - 用户要的是功能实现，不是开发工具
2. **提供直接方案** - 直接告诉如何手动设置
3. **专注核心功能** - 只关注业务逻辑实现
4. **避免过度工程** - 不要为了"方便"而增加复杂性

### **代码编写时**

1. **先思考后编码** - 确保代码必要且稳定
2. **测试编译状态** - 每次修改后验证编译
3. **保持简洁性** - 避免不必要的抽象和工具
4. **专注业务价值** - 每行代码都应服务于核心功能
5. **中文优先原则** - 注释和文档使用中文，变量命名使用标准英文规范

### **与用户交流时**

1. **语言一致性** - 无论用户如何提问或选择，始终用中文回复
2. **专业术语** - 使用中文解释技术概念，英文术语需附中文说明
3. **避免语言切换** - 不因界面按钮或选项为英文而改变回复语言
4. **保持项目风格** - 与代码注释、文档的中文风格保持一致

---

## 🚨 **违规检查清单**

在创建任何新文件前，检查以下问题：

- [ ] 这个文件是否为测试、演示或自动化目的？
- [ ] 这个文件是否会增加编译复杂性？
- [ ] 这个文件是否真的是核心功能必需的？
- [ ] 是否可以通过手动设置指南替代？
- [ ] 这个文件是否可能被误认为是"工具"？
- [ ] 是否要创建新的.md 文档？（除非用户主动要求，否则禁止）
- [ ] 是否与用户使用了中文交流？（必须保持中文）
- [ ] 是否为同一配置创建了多种实现途径？（违反单一化原则）
- [ ] 是否存在空文件或无效的测试/演示文件？（需要清理）
- [ ] 是否包含任何弃用代码、[Obsolete]标记或弃用注释？（必须删除）

**🔒 文档保护检查（新增）：**

- [ ] 是否试图删除或移动 DEVELOPMENT_STANDARDS.md 文件？（绝对禁止）
- [ ] 是否创建了重复的技术文档或设置指南？（违反唯一文档原则）
- [ ] VSCode 启动时是否自动读取了本文档？（必须自动执行）
- [ ] 是否按照本文档规范自动清理了弃用文件？（必须自动清理）
- [ ] 新增的业务逻辑或配置说明是否记录在本文档中？（必须集中管理）

**在完成任何功能前，必须检查以下编译状态：**

- [ ] 当前修改的脚本是否有编译错误？
- [ ] 整个项目的 PROBLEMS 面板是否显示 0 个错误？
- [ ] 是否存在跨脚本的引用错误或依赖问题？
- [ ] 所有相关脚本是否都能正常编译？
- [ ] 是否检查了命名冲突和类型定义问题？

**在设计配置方案前，必须检查以下配置原则：**

- [ ] 是否为每个配置项选择了唯一的实现方法？
- [ ] 是否避免了多重优先级的复杂配置逻辑？
- [ ] 是否选择了最不容易出错的配置方式？
- [ ] 配置路径是否足够简洁明确？c

**在提交代码前，必须检查以下清理要求：**

- [ ] 是否清理了所有空文件和无效文件？
- [ ] 是否删除了所有测试、演示、验证类型的脚本？
- [ ] 是否移除了以\_Fixed、\_Backup、\_Old 等结尾的文件？
- [ ] 项目中是否只保留核心业务功能文件？

**在提交代码前，必须检查以下弃用代码清理要求：**

- [ ] 是否删除了所有[System.Obsolete]标记的代码？
- [ ] 是否清理了所有包含"弃用"、"已弃用"字样的注释？
- [ ] 是否删除了所有弃用的方法、字段、类？
- [ ] 是否删除了整个弃用的脚本文件？
- [ ] 弃用功能是否已用新的标准实现完全替代？

**如果任何一项答案是"是"（对于前 10 项）或"否"（对于后 19 项检查），则不应该声称功能完成。**

---

## � **游戏系统架构**

### **横版 2D DND5E 战斗系统**

本项目是一个 Unity 横版 2D 战斗游戏，实现了完整的 DND5E 规则系统和自动挂机模式。

#### **核心游戏模式**

- **挂机模式**: 全自动探索、遭遇、战斗循环
- **横版布局**: 玩家在左侧，敌人在右侧的经典对战布局
- **DND5E 规则**: 完整的角色属性、技能、法术系统
- **动画系统**: 基于 Spine 的角色动画，支持走路、空闲、攻击等状态

#### **🎮 核心玩法逻辑（横版过关系统）**

##### **1. 横版滚动探索**
- **背景滚动**: 2D横版背景从右往左位移，营造前进探索的视觉效果
- **玩家进入**: 玩家小队阵型从屏幕左侧进入摄影机范围
- **行走动画**: 玩家队伍保持行走动画状态，表现探索移动
- **探索时长**: 经过一定时间后触发遭遇事件

##### **2. 敌人遭遇机制**
- **遭遇触发**: 探索阶段持续特定时间后自动触发战斗
- **敌人进入**: 怪物小队从屏幕右侧进入摄影机范围
- **阵型对峙**: 玩家队伍（左侧）与敌人队伍（右侧）面对面站立
- **安全距离**: 两队之间保持适当空间，形成战斗前的对峙状态

##### **3. 回合制战斗系统**
- **先攻判定**: 双方队伍进入阵型后，系统自动计算先攻顺序
- **自动战斗**: 开始回合制战斗，完全自动计算战斗结果
- **回合时长**: 每个角色行动回合持续6秒
- **战斗循环**: 按先攻顺序依次执行角色行动，直到战斗结束

##### **4. 攻击行为模式**
- **近战角色**: 
  - 行走到目标敌人位置
  - 执行攻击动画
  - 攻击完成后返回自己的阵型位置
- **远程角色**: 
  - 保持在原阵型位置不移动
  - 直接播放远程攻击动画
  - 攻击效果投射到目标位置

##### **5. 循环流程**
```
探索阶段（背景滚动，走路动画）
      ↓
   遭遇触发
      ↓
敌人进入（右侧进入，形成对峙）
      ↓
先攻计算（自动排序行动顺序）
      ↓
回合战斗（6秒/回合，自动执行）
      ↓
战斗结束（胜利后继续探索循环）
```

**核心设计原则**:
- **全自动化**: 玩家无需手动操作，完全挂机体验
- **视觉连贯**: 从探索到战斗的动画过渡自然流畅
- **节奏控制**: 探索和战斗的时间比例保持平衡
- **经典感受**: 传统横版过关游戏的视觉体验

#### **挂机游戏系统组件 (强关联架构)**

```
✅ IdleGameManager.cs          - 挂机游戏主管理器 (核心)
✅ HorizontalBattleFormationManager.cs - 横版阵型管理器 (核心)
✅ AutoBattleAI.cs             - 自动战斗AI系统 (核心)
✅ CharacterStats.cs           - 角色属性系统
✅ Role.cs                     - 角色动画控制组件
```

**强关联关系**:

- IdleGameManager 必须引用 HorizontalBattleFormationManager
- IdleGameManager 必须引用 AutoBattleAI
- 三个核心组件必须在同一场景中手动挂载
- 不使用任何自动创建或查找逻辑

```
✅ IdleGameManager.cs          - 挂机游戏主管理器 (核心)
✅ HorizontalBattleFormationManager.cs - 横版阵型管理器 (核心)
✅ AutoBattleAI.cs             - 自动战斗AI系统 (挂机模式专用)
✅ CharacterStats.cs           - 角色属性系统
✅ Role.cs                     - 角色动画控制组件
❓ CombatManager.cs            - 传统战斗管理器 (挂机模式不必需)
```

**说明**:

- CombatManager 主要用于手动战斗模式和 UI 交互
- 挂机模式使用 IdleGameManager + AutoBattleAI 的简化架构
- 如果只使用挂机模式，CombatManager 可以不添加到场景中

### **游戏流程设计**

#### **挂机循环流程**

1. **探索阶段**: 背景横向滚动，角色播放走路动画
2. **遭遇触发**: 定时或随机遭遇敌人
3. **敌人进入**: 敌人从右侧屏幕外进入战场
4. **战斗阶段**: 角色切换空闲动画，执行自动战斗
5. **奖励结算**: 获得经验值、金币等奖励
6. **循环继续**: 返回探索阶段

#### **角色位置系统**

- **玩家阵型**: 左侧固定阵型 (X: -2 到 -5)
- **敌人阵型**: 右侧固定阵型 (X: 2 到 5)
- **楔形布局**: 前排中锋向对方突出，形成攻击楔形
- **深度层次**: 前后排分离，增加战术深度

---

## 🛠️ **Unity 手动挂载设置指南**

### **唯一 Hierarchy 结构 (强关联挂载)**

```
Hierarchy结构:
├── IdleGameSystem (空GameObject)
│   ├── IdleGameManager (手动添加IdleGameManager脚本)
│   ├── HorizontalBattleFormationManager (手动添加HorizontalBattleFormationManager脚本)
│   └── AutoBattleAI (手动添加AutoBattleAI脚本)
├── Characters (空GameObject，角色容器)
│   ├── PlayerParty (空GameObject，运行时角色父对象)
│   └── EnemyParty (空GameObject，运行时角色父对象)
└── Environment (空GameObject，背景容器)
    └── Background (背景图片或滚动脚本)
```

### **强关联引用配置 (必须手动拖拽)**

#### **1. IdleGameManager 强制引用设置**

│ └── EnemyParty (空 GameObject)
└── Environment (空 GameObject，用于背景等)

````

#### **1. IdleGameManager 配置 (简化)**

```csharp
// IdleGameManager Inspector 强制引用设置

[挂机模式设置]
✅ idleModeEnabled = false (启动时需手动开启)
✅ encounterInterval = 10.0f (遭遇间隔时间)
✅ battleSpeed = 1.0f (战斗速度倍率)

[探索设置]
✅ currentStage = 1 (当前关卡)
✅ currentWave = 1 (当前波次)
✅ stageProgressPercent = 0f (关卡进度百分比)

[队伍生成设置]
✅ useFormationManager = true (使用阵型管理器生成队伍)
✅ playerPartySize = 3 (玩家队伍人数上限，推荐1-6人)

[系统组件 - 强制手动引用]
✅ formationManager: 手动拖入同场景中的HorizontalBattleFormationManager组件
✅ autoBattleAI: 手动拖入同场景中的AutoBattleAI组件
```

#### **2. HorizontalBattleFormationManager 配置 (核心！阵型预制体配置)**

```csharp
// HorizontalBattleFormationManager Inspector设置

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
// AutoBattleAI Inspector设置
✅ enableAutoBattle = true
✅ attackDamage = 15
✅ attackDelay = 1.0f
```

### **强关联验证检查 (更新)**

- [ ] IdleGameManager.formationManager 引用不为空
- [ ] IdleGameManager.autoBattleAI 引用不为空
- [ ] IdleGameManager.useFormationManager 设为 true
- [ ] HorizontalBattleFormationManager 中玩家阵型预制体已配置
- [ ] HorizontalBattleFormationManager 中敌人阵型预制体已配置
- [ ] 三个脚本都挂载在同一场景中
- [ ] idleModeEnabled 在运行时设为 true
- [ ] 没有使用 FindObjectOfType 等自动查找逻辑

### **角色预制体要求**

#### **必需组件清单**

```
✅ GameObject (角色根对象)
├── Transform (位置控制)
├── SpriteRenderer 或 MeshRenderer (显示组件)
├── CharacterStats (角色属性，可运行时自动添加)
├── Role (动画控制，可运行时自动添加)
└── SkeletonAnimation (Spine动画，如果使用Spine)
```

#### **角色标签设置**

```
✅ 玩家角色标签: "Player"
✅ 敌人角色标签: "Enemy"
✅ 队友角色标签: "Ally"
```

### **横版 2D 布局规范**

#### **坐标系统说明**

- **X 轴**: 左右对抗方向 (玩家在负值，敌人在正值)
- **Y 轴**: 排内左中右分布 (左翼 Y 大，右翼 Y 小)
- **Z 轴**: 固定为 0 (2D 游戏)

#### **标准阵型位置**

**玩家方(左侧)坐标:**

```
前排左翼: (-2.0, -2.0, 0)  // 前排，左翼
前排中锋: (-3.5, -4.0, 0)  // 前排，中路，向敌人突出
前排右翼: (-2.0, 0.0, 0)   // 前排，右翼

后排左翼: (-5.0, -2.0, 0)  // 后排，左翼
后排中路: (-5.0, -4.0, 0)  // 后排，中路
后排右翼: (-5.0, 0.0, 0)   // 后排，右翼
```

**敌人方(右侧)坐标:**

```
前排左翼: (2.0, -2.0, 0)   // 前排，左翼
前排中锋: (3.5, -4.0, 0)   // 前排，中路，向玩家突出
前排右翼: (2.0, 0.0, 0)    // 前排，右翼

后排左翼: (5.0, -2.0, 0)   // 后排，左翼
后排中路: (5.0, -4.0, 0)   // 后排，中路
后排右翼: (5.0, 0.0, 0)    // 后排，右翼
```

### **动画状态管理**

#### **Role 组件动画状态**

```csharp
Role.ActState.IDLE    - 空闲动画 (战斗时使用)
Role.ActState.MOVE    - 走路动画 (探索时使用)
Role.ActState.ATTACK  - 攻击动画 (战斗行动时)
Role.ActState.HIT     - 受击动画 (被攻击时)
Role.ActState.DEAD    - 死亡动画 (角色死亡时)
```

#### **动画播放 API**

```csharp
// 播放指定动画
Role roleComponent = character.GetComponent<Role>();
roleComponent.playAct(Role.ActState.MOVE, "walk");  // 播放走路动画
roleComponent.playAct(Role.ActState.IDLE);          // 播放空闲动画
```

### **探索到战斗动画系统**

#### **完整动画流程**

1. **探索阶段**: 玩家队伍播放走路动画，背景滚动
2. **遭遇触发**: 系统检测到遭遇条件
3. **敌人进入**: 敌人从右侧屏幕外(X+15)移动到阵型位置
4. **阵型就位**: 双方角色到达指定战斗位置
5. **动画切换**: 所有角色从走路动画切换到空闲动画
6. **战斗开始**: 进入回合制战斗流程

#### **核心 API 调用**

```csharp
// 启动完整的探索到战斗流程
List<CharacterStats> playerTeam = GetPlayerParty();
List<CharacterStats> enemyTeam = GenerateEnemyEncounter();

// 方法1: 使用CombatManager (如果有接战动画系统)
CombatManager.Instance.StartExplorationToCombatSequence(playerTeam, enemyTeam);

// 方法2: 使用IdleGameManager (挂机模式)
IdleGameManager idleManager = FindObjectOfType<IdleGameManager>();
// 自动处理，无需手动调用
```

### **CombatManager 挂载方式说明**

#### **自动创建方式 (传统战斗模式)**

```
✅ 使用 DND_BattleSceneSetup 脚本自动创建
   - 该脚本会在运行时检查是否存在 CombatManager
   - 如果不存在，会自动创建名为 "CombatManager" 的 GameObject
   - 自动添加 CombatManager 组件
```

#### **手动创建方式**

```
1. 创建空 GameObject，命名为 "CombatManager"
2. 添加 CombatManager 脚本组件
3. 确保该 GameObject 在场景中保持激活状态
```

#### **挂机模式建议 (推荐)**

```
❌ 不推荐：添加 CombatManager 到挂机游戏场景
✅ 推荐：只使用 IdleGameManager + AutoBattleAI 架构
✅ 好处：避免系统冲突，减少内存占用，简化调试过程
```

**重要提醒**:

- 如果场景中同时存在 CombatManager 和 IdleGameManager，可能会产生冲突
- 挂机模式下建议移除所有 CombatManager 相关的组件和脚本

### **CombatManager vs IdleGameManager 选择指南**

#### **CombatManager 使用场景**

```
✅ 需要手动回合制战斗
✅ 需要UI交互 (DND_BattleUI)
✅ 需要复杂的战斗规则和状态管理
✅ 需要玩家手动选择行动
✅ 需要先攻顺序和详细的战斗流程
```

#### **IdleGameManager 使用场景 (推荐)**

```
✅ 全自动挂机模式
✅ 简化的战斗流程
✅ 不需要UI交互
✅ 连续的探索-战斗循环
✅ 适合放置类游戏
```

### **组件依赖关系 (严格强关联)**

#### **IdleGameManager 模式 (唯一标准)**

```
强制架构:
IdleGameManager (主控制器)
├── formationManager: HorizontalBattleFormationManager (手动引用)
├── autoBattleAI: AutoBattleAI (手动引用)
└── playerPrefabs: 角色预制体列表 (手动配置)
```

**强关联规则**:

- ✅ 所有组件引用必须手动设置
- ✅ 三个脚本必须在同一场景中
- ❌ 禁止使用 FindObjectOfType 等自动查找
- ❌ 禁止可选或备选的配置方式
- ❌ 禁止 CombatManager 等过时组件

### **配置强制化原则**

#### **唯一配置方式**

```csharp
// IdleGameManager Inspector - 强制手动设置
[Header("强关联组件")]
public HorizontalBattleFormationManager formationManager;  // 必须手动拖入
public AutoBattleAI autoBattleAI;                          // 必须手动拖入

[Header("角色配置")]
public GameObject[] playerPrefabs;                         // 必须手动配置
```

#### **删除的弃用逻辑**

```
✅ 已完成移除/标记弃用:
❌ CombatManager 及其相关脚本 (已标记为[Obsolete])
❌ SimpleEnemyAI (已标记为[Obsolete])
❌ DND_BattleUI (已标记为[Obsolete])
❌ IdleGameManager中的FindObjectOfType逻辑 (已移除)
❌ FindPlayerPartyFromScene方法 (已标记为弃用)
❌ 自动查找组件的代码逻辑 (已移除)
❌ 手动/自动选择的双重配置 (已简化)
❌ FindObjectOfType 等自动发现机制 (已移除)
❌ 可选配置和后备方案 (已移除)
```

### **强关联验证检查**

#### **运行前验证清单**

- [ ] IdleGameManager.formationManager 不为 null
- [ ] IdleGameManager.autoBattleAI 不为 null
- [ ] playerPrefabs 数组已配置且不为空 (如果 usePlayerPrefabs=true)
- [ ] playerParty 手动配置完成 (如果 usePlayerPrefabs=false)
- [ ] enemyPrefabs 数组已配置且不为空
- [ ] idleModeEnabled 启动后设为 true
- [ ] usePlayerPrefabs 设置与实际使用方式匹配
- [ ] 三个组件在同一场景的 GameObject 上
- [ ] 没有残留的 CombatManager 或其他弃用组件
- [ ] 脚本中的 FindObjectOfType 逻辑已删除

#### **故障排除指南**

1. **NullReferenceException**: 检查手动引用是否丢失
2. **角色不生成**:
   - 验证 playerPrefabs 是否正确配置 (如果 usePlayerPrefabs=true)
   - 验证 playerParty 是否手动配置 (如果 usePlayerPrefabs=false)
3. **挂机模式不启动**: 确认 idleModeEnabled 设为 true
4. **敌人不生成**: 检查 enemyPrefabs 列表是否为空
5. **位置错误**: 确认 formationManager 引用正确
6. **战斗不启动**: 检查 autoBattleAI 引用和配置
7. **参数不匹配**: 确保 Inspector 设置与脚本默认值一致
8. **FindObjectOfType 错误**: 需要从脚本中移除自动查找逻辑

### **性能优化建议**

#### **队伍规模限制**

- **推荐队伍大小**: 单方 1-6 人
- **最优配置**: 玩家 3 人，敌人 2-4 人
- **性能考虑**: 超过 6 人可能影响动画流畅度

#### **内存管理**

- **对象池**: 敌人角色建议使用对象池机制
- **及时清理**: 战斗结束后清理不需要的敌人对象
- **预制体缓存**: 避免重复加载相同的角色预制体

---

## 📝 **总结**

**核心原则：严格手动挂载、强关联、单一路径、中文**

- 🎯 **强制手动挂载** - 所有组件引用必须手动设置，禁止自动查找
- � **强关联关系** - IdleGameManager 必须引用 FormationManager 和 AutoBattleAI
- � **删除弃用逻辑** - 移除 CombatManager、FindObjectOfType 等过时代码
- 📋 **单一配置路径** - 每个配置只有一种标准实现方式
- ✅ **质量第一** - 确保每个脚本都编译无误且功能完整
- 🗣️ **中文交流** - 所有对话、注释、文档均使用中文

**关键要求**:

1. IdleGameManager + HorizontalBattleFormationManager + AutoBattleAI 三位一体
2. 手动拖入所有组件引用，禁止代码自动查找
3. 删除所有不使用的弃用逻辑和组件
4. 确保强关联关系，避免空引用错误

**记住：统一手动挂载，强关联设计，删除弃用逻辑。**

---

_此规范文档已更新为强制手动挂载标准，指导所有后续开发工作。_
````
