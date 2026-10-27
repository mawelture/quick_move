# 快捷移动（QuickMove）

## 背景

现在能用的 AI 工具已经很多，真正拿去处理的材料却常常不在一个地方。别人发来的文件落在下载目录或聊天记录里，自己随手保存的又散在桌面和各个文件夹。每次让 AI 看一批材料，都要先找出来，再复制或拖进同一个目录。

AI 更适合对着一个固定的工作目录工作。文件一开始可以先随便放，难的是事后整理。快捷移动把这一步收成按键：在资源管理器里选中这些文件，按对应的快捷键，它们就进入事先定好的目录。这个目录可以一直作为 AI 的工作空间，不用每次重新收拾。

最多可以设 5 组快捷键，按用途分开。例如 `Ctrl+1` 把待处理的材料收进工作目录，`Ctrl+2` 把已经看过的文件收进归档目录。

## 使用

启动后只在通知区域显示图标，不占任务栏。右键图标可以打开设置或退出。关掉设置窗口不会退出程序。再次启动不会开第二个实例，只会打开设置。

设置里固定 5 行。每行有启用开关、快捷键和目标目录。快捷键框获得焦点后，按下带 `Ctrl`、`Alt`、`Shift` 或 `Win` 的组合即可录入，`Backspace` 清空。没启用、没设快捷键或没选目录的行不生效。两行不能用同一个快捷键。

按下快捷键时：

- 桌面和所有打开的资源管理器里，当前选中的文件和文件夹都会移动。后台窗口里仍保持选中的项也算。
- 同时选中了某个文件夹和它里面的内容时，只移动外面的文件夹。
- 已经直接在目标目录里的项会跳过。目标目录的子目录中的文件会移到目标目录这一层。
- 目标里已有同名项时自动改名，例如 `报告.pdf` 变成 `报告 (1).pdf`。原有的同名项保留。
- 目标目录不存在时不自动创建，只提示失败。
- 完成后用一条托盘提示汇总移动、改名、跳过和失败的数量。

配置写在 `%AppData%\QuickMove\config.json`。

## 环境

- Windows 10 或 Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 构建

在本目录执行：

```powershell
dotnet test QuickMove.sln
dotnet run --project src/QuickMove/QuickMove.csproj
```

发布一个独立的可执行文件：

```powershell
dotnet publish src/QuickMove/QuickMove.csproj -c Release -r win-x64 --self-contained false
```

输出在 `src/QuickMove/bin/Release/net8.0-windows/win-x64/publish/`。目标机器需要已安装 .NET 8 桌面运行时。
