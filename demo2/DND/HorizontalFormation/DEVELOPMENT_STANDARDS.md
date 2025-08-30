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

### **代码质量要求**
- **强制错误清理**：严禁在 IDE 的 PROBLEMS 面板有任何报错的情况下声称功能完成
- **全项目编译检查**：必须解决项目中所有脚本的编译错误
- **弃用代码强制删除**：严禁保留任何标记为[System.Obsolete]的方法、类或属性

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
玩家方(左侧)          敌人方(右侧)
前排: [-1, +2] [镜像] [+1, +2] :前排
      [-1,  0] [镜像] [+1,  0] 
      [-1, -2] [镜像] [+1, -2] 
后排: [-2, +2] [镜像] [+2, +2] :后排
      [-2,  0] [镜像] [+2,  0] 
      [-2, -2] [镜像] [+2, -2] 
```

**战斗行为模式：**
- **近战职业** → 移动到敌人面前攻击后返回原位
- **远程职业** → 原地攻击，支持跨位攻击后排
- **先攻系统** → 按DND5E规则进行先攻检定排序
- **回合制** → 每回合6秒，严格按先攻顺序执行

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
