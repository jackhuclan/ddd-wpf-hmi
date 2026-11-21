# HMI · 工控设备监控上位机

基于微信文章《DDD + WPF 工控上位机》提炼的需求实现的可运行样例：.NET 10 WPF，DDD 四限界上下文（设备 / 告警 / 工单 / 配方），还原原文的深色导航 + 白色内容区 + 状态栏布局与四个页面。

## 快速开始

```bash
# 还原、构建（集中包管理，版本见 Directory.Packages.props）
dotnet restore HmiSolution.slnx
dotnet build HmiSolution.slnx -c Release --no-restore

# 测试（领域规则单测）
dotnet test HmiSolution.slnx -c Release --no-build

# 运行
dotnet run --project src/Hmi.Wpf/Hmi.Wpf.csproj
```

启动后默认进入「设备监控」页。点击「开始采集」启动 `PeriodicTimer` 采集循环，内存模拟源持续产生温度与压力数据并推进曲线；压力周期性越限，可在「告警中心」完成「确认 → 清除」 lifecycle。

## 项目结构

```
HmiSolution.slnx              解决方案（唯一入口，Solution Items / src / tests 分组）
├─ src/
│  ├─ Hmi.Domain             领域层：实体、值对象、领域事件、仓储契约（net10.0）
│  ├─ Hmi.Application        应用层：CQRS 命令 / 查询 / 处理器、配置契约、UI 消息（net10.0）
│  ├─ Hmi.Infrastructure     基础设施层：采集、模拟数据源、内存仓储、Outbox、DI（net10.0）
│  ├─ Hmi.ViewModels         MVVM 视图模型与导航服务（net10.0-windows）
│  └─ Hmi.Wpf                WPF 主程序、页面、样式与图表控件（net10.0-windows, WinExe）
├─ tests/
│  └─ Hmi.Domain.Tests       领域规则单测（告警状态机 / 工单流转 / 设备状态）
└─ docs/
   └─ 需求规格说明书.md       从原文提炼的需求，含假设 A1–A13
```

依赖方向严格单向：`Wpf → ViewModels → Application → Domain`，`Infrastructure → Application → Domain`。

## 配置

运行配置位于 `src/Hmi.Wpf/appsettings.json`，由 `ConfigurationBuilder` 加载并支持 `reloadOnChange: true`。主要节点：

| 节点 | 说明 |
|---|---|
| `Devices` | 设备 → 通信模块 → 点位三级模型，含协议、阈值与模拟相位 |
| `Recipes` | 配方编号 / 名称 / 版本 / 点位目标值 |
| `AlarmThresholds` | 按点位编号配置的上下限与级别 |
| `Collection` | 心跳周期、曲线最大点数、温度 / 压力基准值 |
| `Sync` | Outbox 后台同步周期（秒） |

修改后可在「工单管理」或「配方管理」页点击刷新触发 `ReloadConfigCommand` 热加载。

## 页面与交互

| 页面 | 主要功能 |
|---|---|
| 设备监控 | 采集启停、温度 / 压力实时值、双曲线滚动显示（WPF 原生 Canvas 绘制） |
| 告警中心 | 活动告警确认与清除、历史告警按级别 / 状态 / 时间筛选与分页 |
| 工单管理 | 工单创建、设备指派与配方绑定、开始执行、完成归档、配置刷新 |
| 配方管理 | 配方列表、启用配方切换下发、配置刷新 |

关键领域规则：告警确认幂等、恢复与确认独立、**未确认不得清除**；工单状态单向流转；工单开始时按最新配方版本与设备当前版本比对决定是否下发。

## 测试矩阵

| 工程 | 覆盖 |
|---|---|
| `Hmi.Domain.Tests` | 告警状态机（6 项）、工单流转（5 项）、设备状态与越限（6 项），共 17 项 |

## 工程规范

参见 [`AGENTS.md`](AGENTS.md)。要点：CPM 集中管理包版本；`.editorconfig` 统一风格（文件作用域命名空间、`_camelCase` 私有字段、System using 前置、中文 XML 注释、if 一律大括号）；提交信息使用中文 Conventional Commits 并附版本号尾注。
