# AGENTS.md · 仓库组织规范

> 本文件是**项目结构与文件归属的唯一规范**，面向在本仓库工作的开发者与 AI 代理。
> 与 README 分工不同：README 讲"怎么构建与使用"，本文件讲"东西放在哪里、为什么"。

---

## 1. 顶层布局

```
E:\ddd-wpf-hmi\
├─ HmiSolution.slnx              解决方案（唯一入口）
├─ .editorconfig                 代码风格（解决方案级）
├─ Directory.Build.props         全解决方案共享元数据：版本单点、语言基线
├─ Directory.Packages.props      中央包管理（CPM）：所有包版本在此
├─ nuget.config                  包源配置（仅 nuget.org）
├─ .gitignore
├─ README.md                     使用说明（构建 / 运行 / 配置 / 测试）
├─ AGENTS.md                     本文件：组织规范
├─ src/                          源码项目（可交付产物）
├─ tests/                        测试项目（不发布）
├─ docs/                         需求、计划、验收与报告
└─ .github/workflows/            CI（build + test）
```

**Solution Items = 解决方案级文件**：不属于任何单个项目、但整个解决方案共享的文件，必须留在仓库根目录（MSBuild 的 `Directory.Build.props` / `Directory.Packages.props` 只在目录树向上查找时生效，挪走即失效）。

| 归入 Solution Items | 理由 |
|---|---|
| `.editorconfig` | 对所有项目生效的代码风格 |
| `README.md` / `AGENTS.md` | 仓库级说明，与具体项目无关 |
| `Directory.Build.props` / `Directory.Packages.props` | 共享编译元数据与包版本 |
| `nuget.config` | 仓库级包源配置，保证 CI 与本地一致 |
| `HmiSolution.slnx` | 解决方案自身 |

---

## 2. 代码目录归属规则

### 2.1 `src/` — 源码项目

依赖方向严格单向，禁止反向引用：

```
Hmi.Wpf ──► Hmi.ViewModels ──► Hmi.Application ──► Hmi.Domain
   │              │                  ▲                ▲
   └──────────────┴──► Hmi.Infrastructure ────────────┘
```

| 项目 | 目标框架 | 职责 | 禁止 |
|---|---|---|---|
| `Hmi.Domain` | `net10.0` | 设备 / 告警 / 工单 / 配方四上下文的实体、领域事件、仓储契约 | 引用任何 UI、DI 或序列化框架类型；MediatR 契约除外 |
| `Hmi.Application` | `net10.0` | CQRS 命令 / 查询 / 处理器、配置契约、UI 消息接口 | 引用 WPF / Windows 类型 |
| `Hmi.Infrastructure` | `net10.0` | 采集循环、模拟数据源、内存仓储、Outbox、配置服务、DI 注册 | 承载业务规则（应下沉到 Domain / Application） |
| `Hmi.ViewModels` | `net10.0-windows` | 页面视图模型、导航服务、UI 线程封送 | 承载领域规则或直接读写配置文件 |
| `Hmi.Wpf` | `net10.0-windows`（WinExe） | 主窗口、页面、样式、原生绘图控件、启动装配 | 承载可复用领域逻辑 |

**归属判定顺序**（新代码放哪里）：
1. 业务规则 / 状态机 / 领域事件 → `Domain`；
2. 用例编排、命令查询、UI 消息契约 → `Application`；
3. 设备协议、存储、后台服务、配置加载 → `Infrastructure`；
4. 只服务某个页面的状态 → `ViewModels`（必须依赖 WPF 时留 `ViewModels`，否则下沉 `Application`）；
5. 页面 / 控件 / 样式 / 资源 → `Wpf`。

### 2.2 `tests/` — 测试项目

命名固定 `Hmi.<被测层>.Tests`，一个被测项目对应一个测试工程。

| 工程 | 覆盖 |
|---|---|
| `Hmi.Domain.Tests` | 告警状态机、工单流转、设备聚合与越限判定 |

规则：测试工程不得被 `src/` 引用；测试数据不写仓库；涉及 WPF 的用例须遵守 STA 宿主约定。

### 2.3 `docs/` — 文档

放需求规格、实施计划、验收清单、性能 / 稳定性报告与作业手册；不放代码与构建产物。

---

## 3. 命名与格式约定

以根目录 `.editorconfig` 为准，补充以下本仓库惯例：

- 命名空间与目录一致，使用文件作用域声明（`namespace Hmi.Domain.Alarms;`）；
- 私有 / 内部字段使用 `_camelCase`，静态只读字段使用 PascalCase，常量使用 PascalCase；
- `using` 位于命名空间外，`System.*` 置顶，其余按字母序；
- 所有 `if` / `foreach` 一律使用大括号，即使只有一条语句；
- 公开类型与方法使用中文 `/// <summary>` 注释，说明"为什么"而不只是"是什么"；
- 领域事件以 `Event` 结尾，UI 消息以 `Message` 结尾，命令以 `Command` 结尾，查询以 `Query` 结尾；
- WPF 视图模型的可观察字段使用 `_camelCase`（CommunityToolkit 生成同名 PascalCase 属性）；
- 避免 `Application` 简写：在 `Hmi.*` 命名空间下会与 `Hmi.Application` 冲突，须写 `System.Windows.Application`。

## 4. 配置与版本

- **包版本只在 `Directory.Packages.props` 维护**，各 `csproj` 的 `PackageReference` 只写 `Include`；
- **版本号只在 `Directory.Build.props` 维护**（当前 `0.1.0`），同时作用于程序集与包；
- 新增包先加到 CPM 的对应 `ItemGroup`，再到项目中引用；禁止在项目里写 `Version` 或 `VersionOverride`；
- 运行时配置放 `src/Hmi.Wpf/appsettings.json`，本地凭据类文件（如 `config.json`）已被 `.gitignore` 排除。

## 5. 解决方案分组

`HmiSolution.slnx` 分组固定为 `Solution Items` / `src` / `tests`，与磁盘结构一致。新增项目必须同时改两处：磁盘目录 + `slnx` 对应分组；漏改会导致 CI 不编译该项目。

---

## 6. 新增文件 / 项目的检查清单

- [ ] 位置符合 §2 归属规则，命名符合 `Hmi.<层>[.<子域>]`；
- [ ] 目标框架与所在层一致（`Domain`/`Application`/`Infrastructure` 为 `net10.0`，`ViewModels`/`Wpf` 为 `net10.0-windows`）；
- [ ] 依赖方向单向，无反向引用；
- [ ] `HmiSolution.slnx` 已加入对应分组；
- [ ] 有对应测试（领域规则强制）；
- [ ] `dotnet build HmiSolution.slnx -c Release` 0 警告 0 错误、`dotnet test HmiSolution.slnx` 全绿；
- [ ] 文档与代码同批提交，版本号只改 `Directory.Build.props` 一处；
- [ ] 提交信息用中文 Conventional Commits，尾注版本号（如 `refactor(core): 统一代码风格 (0.1.0)`）。
