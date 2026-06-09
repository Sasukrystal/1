# 黑暗地牢

基于 Unity 2022.3 的 2D 俯视角 Roguelike 地牢游戏。

仓库：[Sasukrystal/1](https://github.com/Sasukrystal/1)

## 给同学的一键链接（推荐）

GitHub **不能**在网页里直接运行 `.exe`。要给 Windows 同学「点链接就能玩」，请用 **Releases 下载包**：

1. 在 Unity 菜单 **Modern Rogue → Build Windows Release** 生成 `Builds/Windows/`
2. 在项目根目录执行：

   ```powershell
   .\scripts\Package-WindowsRelease.ps1
   ```

3. 打开 [新建 Release](https://github.com/Sasukrystal/1/releases/new)
   - Tag：`v1.0`
   - 上传 `Builds/DarkDungeon-Windows.zip`
   - 发布

4. 把下面链接发给同学（**点开后下载 → 解压 → 双击 `黑暗地牢.exe`**）：

   **https://github.com/Sasukrystal/1/releases/latest**

> 当前 Windows 包约 1.5GB，首次下载较慢，属正常现象。

### 想在浏览器里直接玩？

需要另做 **WebGL 版本**，并开启 [GitHub Pages](https://pages.github.com/)，玩链类似：

`https://sasukrystal.github.io/1/`

WebGL 需单独打包、体积和兼容性要调试，本仓库默认提供 **Windows 下载版**。若你需要，我可以再帮你加 WebGL 打包流程。

## 开发者运行

1. 使用 Unity **2022.3 LTS** 打开项目根目录
2. 打开场景 `Assets/Scenes/Main.unity`
3. 点击 Play 运行

## 发布

菜单 **Modern Rogue → Build Windows Release** 生成 Windows 可执行文件（输出在 `Builds/Windows/`，该目录已被 gitignore）。

## 主要模块

- `Assets/Scripts/ModernRogue/` — 主玩法、UI、地牢生成、虫核、Meta 成长
- `Assets/Scripts/Enemy/` — 敌人与战斗
- `Assets/Scenes/Main.unity` — 唯一发布场景

## 操作摘要

- **WASD** 移动，**鼠标** 瞄准，**左键** 攻击
- **B / E / ESC** 打开背包与总面板（含设置）
- 大厅 **M** 打开武器铺（局外 Meta 升级）
