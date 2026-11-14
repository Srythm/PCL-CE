**简体中文** | [English](README-EN.md) | [繁體中文](README-ZH_TW.md)

<div align="center">

<img src="Plain Craft Launcher 2/Images/icon.ico" alt="Logo" width="80" height="80">

# PCL CE · 个人分支

[![Stars](https://img.shields.io/github/stars/Srythm/PCL-CE?style=for-the-badge&labelColor=444444&color=eac54f)](https://github.com/Srythm/PCL-CE/)
![GitHub Release](https://img.shields.io/github/v/release/Srythm/PCL-CE?label=release&logo=github&style=for-the-badge)
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/Srythm/PCL-CE/total?style=for-the-badge)

[本分支下载](https://github.com/Srythm/PCL-CE/releases/latest) |
[上游社区版](https://github.com/PCL-Community/PCL-CE) |
[上游主仓库](https://github.com/Meloong-Git/PCL)

</div>

> **这不是官方发布。** 本仓库是 [PCL Community Edition](https://github.com/PCL-Community/PCL-CE) 的个人分支，
> 用于承载若干自用改动。仓库内代码与其许可证均来自上游，改动部分见下方「与上游的差异」。
>
> 若是使用中遇到的问题，请提到 [本仓库的 Issues](https://github.com/Srythm/PCL-CE/issues)，
> **不要占用上游社区的支持渠道**——这些问题与本分支的构建方式直接相关，上游无法复现。

当前基于上游 **2.15.1**（上游基线 `b4949a9`），版本号与上游不严格对应。

## 🧩 与上游的差异

| 改动 | 说明 |
|---|---|
| **移除下载功能的地区限制** | 上游 `RestrictedFeaturePolicy` 会按「系统时区 + 设备地区」拦截下载、补全、整合包安装、加载器安装等流程；本分支将判定入口改为恒行放行，拦截提示不再出现。`RegionPolicy` 本体与其单元测试保持原样。 |
| **实例信息显示累计游玩时长** | 实例总览页的「实例信息」在「启动次数」后新增「游玩时长」。计时随游戏进程启动开始，正常退出 / 崩溃 / 取消时结算，按实例累加到实例配置 `PlayTimeSeconds`，运行期间每 30 秒回写一次。展示为「不足 1 分钟 / N 分钟 / N 小时 M 分钟 / N 天 M 小时」。 |
| **未配置联机服务时移除联机入口** | 大厅服务器地址与大屏账户凭据由上游以密钥形式在编译期注入，本分支没有这些密钥，故在地址缺失时直接移除「工具 → 联机」入口，避免进入一个必然报错的页面。若构建时提供了地址，该入口会照常出现。 |

### 已知不可用

| 功能 | 原因 |
|---|---|
| **联机大厅 / Natayark 账户登录** | 依赖上游运营的服务器地址与 Natayark OAuth 客户端凭据（含 client secret），无自助申请通道，本分支不包含也不借用。需要联机时请使用官方构建，或改用 EasyTier / OpenFrp 等自行组网。 |
| **CurseForge 官方 API** | 接口要求 `x-api-key`。本分支的发行包已内置一份自备密钥；自行构建时若未提供，则会回落到镜像源，模组搜索与列表走 `mod.mcimirror.top`，文件下载不受影响。 |
| **崩溃上报** | 未配置 Sentry DSN，仅影响遥测，不影响功能。 |

### 微软账户登录

微软登录使用编译期写入的 Entra 应用（client id），该应用已通过 Mojang 的 App ID 审核。
自行构建时必须提供自己的 client id，否则登录会直接抛 `Microsoft OAuth client id is not configured.`。

## 💻 支持平台

| 操作系统 | 支持情况 | 环境要求 |
|---|---|---|
| Windows 10 1809 (17763) 或更高 | ✅ 完整支持 | [.NET 10 Desktop Runtime](https://get.dot.net/10) |
| Windows 8 - Windows 10 1809 (17763) | ⚠️ 理论能跑 | [.NET 10 Desktop Runtime](https://get.dot.net/10) |
| Windows 7 或更低版本 | ❌ 不支持 | / |
| macOS / Linux / 其他操作系统 | ⚠️ 仅交叉编译，无法直接运行 | [.NET 11 SDK](https://get.dot.net/11) |

发行包为**框架依赖**发布（`--no-self-contained`），目标机需安装 .NET 10 桌面运行时。

## ⬇️ 下载与运行

在 [Releases](https://github.com/Srythm/PCL-CE/releases/latest) 下载对应架构的单文件：

| 文件 | 适用 |
|---|---|
| `PCL2_CE_Release_x64.exe` | 绝大多数 Intel / AMD 平台 |
| `PCL2_CE_Release_ARM64.exe` | Windows on ARM（骁龙 X 系列等） |

单文件、免安装。首次运行会在 exe 同目录创建 `PCL` 数据文件夹；若此前已在使用上游构建，
把 exe 放到原目录即可继续沿用同一套数据与实例配置。

Release 页面附有每个文件的 SHA-256，可用 `certutil -hashfile <文件> SHA256` 校验。

## 🔨 从源码构建

需要 .NET 11 SDK（项目目标框架为 `net10.0-windows`，由 SDK 11 编译）。

```bash
export PCL_WRITE_SECRET=1
export PCL_MS_CLIENT_ID=<你的 Entra 应用 client id>
export PCL_CURSEFORGE_API_KEY=<你的 CurseForge API Key>   # 可省略，省略则走镜像源
export PCL_GITHUB_SHA=$(git rev-parse HEAD)

dotnet publish "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" \
  -p:Configuration=Release \
  -p:Platform=x64 \
  -p:DeleteExistingFiles=true \
  -o ./dist --no-self-contained
```

`-p:Platform` 可选 `x64` 或 `ARM64`，项目会据此映射 `win-x64` / `win-arm64`；
`PublishSingleFile` 已在项目文件中开启，产物即单个 exe。

> **注意**：上述密钥由源生成器在**编译期**写入程序集。只改环境变量再 `publish`，
> MSBuild 会判定源码未变而复用旧程序集，密钥不会生效。改动前先 `touch PCL.Core/App/Secrets.cs`
> 强制重编，或用下面的脚本。

仓库外的辅助脚本（不在版本库内）：`apply-cf-key.py`，完成「验证 key → 备份 → 强制重编 → 校验已写入」。
联机大厅所需的 `PCL_LINK_SERVER_ROOT`、`PCL_LOBBY_DEFAULT_SECRET`、`PCL_NAID_CLIENT_ID/_SECRET`
属上游运营资源，本分支不提供。

## 📁 主要目录

| 目录 | 内容 |
|---|---|
| `Plain Craft Launcher 2/` | WPF 主程序（界面、页面、控件） |
| `PCL.Launcher.Core/` | 启动、下载、实例与进程监视等核心逻辑 |
| `PCL.Core/` | 配置、日志、本地化、网络等基础服务 |
| `PCL.Core.Wpf/` | WPF 相关基础组件与多语言资源 |
| `PCL.Core.SourceGenerators/` | 源生成器（配置项、密钥注入等） |
| `PCL.Core.Test/` | 单元测试 |

## 🔒 许可证

沿用上游，不做变更：

- `Plain Craft Launcher 2/` 使用 [自定义许可证](https://github.com/PCL-Community/PCL-CE/blob/dev/Plain%20Craft%20Launcher%202/LICENCE)
- `其余所有目录` 使用 [Apache License 2.0](https://github.com/PCL-Community/PCL-CE/blob/dev/LICENSE)

## ❤️ 致谢

界面与绝大部分实现来自 [PCL](https://github.com/Meloong-Git/PCL) 与
[PCL Community Edition](https://github.com/PCL-Community/PCL-CE)，
本分支仅在其上做了少量改动。若这些改动用得上，欢迎直接向上游提交（游玩时长一项与其它改动无耦合，可独立合并）。
