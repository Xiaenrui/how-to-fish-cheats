# 渔力全开 / How to Fish 作弊菜单

游戏自带一个隐藏的开发者作弊菜单(`CheatsEnabled`),正常游玩是锁死的——`SteamManager.Awake` 里硬编码了 6 个开发者的 SteamID,不在名单上就永远是 false。本体还是 Mono 版 .NET 程序集,直接改两处 getter 就能解锁。

Steam AppID: `4001890`(Dazed Games)

## 解锁了什么

改了 `Assembly-CSharp.dll` 的三处:

1. `ClientSettings.get_CheatsEnabled()` -> 恒 `true`(作弊系统生效)
2. `SteamManager.get_IsDev()` -> 恒 `true`(隐藏按钮可触发)
3. `ButtonManager.Start()` -> 强制显示 `_cheatText`(作弊提示文本)

## 权限要求

- 聊天指令**只有房主能发**。`DazedCommands.IsServerCommand` 的逻辑:消息需以 `/` 开头,CheatsEnabled 为 true,`Server.Instance` 存在且 `IsServerInitialized`,否则分别提示 "Only dev is allowed to type commands" / "Only host is allowed to type commands"
- 单人模式也可以(房主就是你自己)

## 快捷键(进岛后直接按)

| 键 | 作用 |
|---|---|
| `M` | +99999 金币 |
| `N` | -99999 金币 |
| `O` | 传送全员 + 前往下一岛 |
| `T` | 慢动作,1x -> 0.1x -> 0.01x 循环 |
| `G` | 测试扣血 |
| `H` | 回满血 + 回满饱食度 |
| `,` | 收起 / 显示教程 |

## 聊天指令

游戏内(不是主菜单)按 **Enter** 呼出聊天框,输入指令再按 **Enter** 发送。指令以 `/` 开头会被游戏内部处理,不会发到聊天屏。

| 指令 | 作用 |
|---|---|
| `/spawn 名字` | 在面前约 2 米生成对应生物/物品。名字自动去空格、转小写,例如 `/spawn pufferfish` |
| `/spawndead 名字` | 生成即死(尸体状态) |
| `/spawndrip 名字` | 生成 Drip 变种(特殊款) |
| `/spawndripdead 名字` | 生成 Drip 变种且即死 |
| `/grill` | 解锁烤炉 |
| `/boat` | 解锁船 |
| `/addmoney` | 自己 +9999 金币(注意:不是 99999,要暴富按 `M`) |
| `/removemoney` | 自己 -9999 金币 |
| `/nextisland` | 传送到下一岛 |
| `/previsland` | 传送到上一岛 |
| `/godmode` | 切换无敌(再发一次关闭) |
| `/oneshot` | 切换一击必杀(服务器级设置) |
| `/killallcreatures` | 全部普通生物标记为已击杀 |
| `/killalldripcreatures` | 全部 Drip 生物标记为已击杀 |
| `/resetallcreatures` | 重置普通生物的击杀状态 |
| `/resetalldripcreatures` | 重置 Drip 生物的击杀状态 |
| `/slots 物品名 序号` | 给老虎机设置作弊皮肤,序号从 0 开始 |
| `/killboss` | 对 BOSS 来一发 999999 伤害,直接秒 |
| `/allskins` | 解锁全部皮肤(装备 + 船) |
| `/noskins` | 锁回全部皮肤 |
| `/showkillscores` | 把击杀分数加成打到聊天里 |
| `/finishgame` | 直接触发通关结算 |
| `/unlockachievements` | 解锁全部 Steam 成就 |
| `/lockachievements` | 锁回全部 Steam 成就 |

## 其他发现

- 主菜单场景(level0)里有 3 个 UI 元素绑定了 `CheatsButton`,按下次数为奇数时执行 `ToggleCheats(true)` 并切换 `_cheatText` 显示/颜色——这就是开发者平时用的入口
- 作弊开启后还有几处副作用:无存档时初始资金 99999;解锁全部岛;BOSS 战可直接跳过剧情动画
- `MainMenuManager.CrashAnimation` 在作弊开启时直接 `InstantCrash()` 跳过演出

## 怎么打补丁

工具:`Mono.Cecil 0.11.6`(NuGet 下载,拿 `lib\net40\Mono.Cecil.dll`),脚本见 `tools/patch_cheats.cs`。游戏要先完全退出。

```powershell
# Mono.Cecil.dll 放在 lib 目录下
Add-Type -Path 'tools\patch_cheats.cs' -ReferencedAssemblies "$pwd\lib\Mono.Cecil.dll"
[CecilPatch]::Run('D:\SteamLibrary\steamapps\common\How to Fish\How to Fish\How to Fish_Data\Managed\Assembly-CSharp.dll')
```

脚本自动把原文件备份成 `Assembly-CSharp.dll.orig`,然后在原位写回补丁后的程序集。从 Steam 启动游戏即可。

## 还原

用备份的 `.orig` 覆盖回去,或者 Steam 右键游戏 -> 属性 -> 已安装文件 -> 验证游戏文件完整性。

**注意**:游戏更新、Steam 校验文件都会覆盖补丁,重新打一次就行。

## 免责声明

仅供自己和朋友在私人房间使用。虽然聊天指令有房主校验、服务器也不会踢人,但带去公共服务器还是别想了,蹲黑屋事小,被举报事大。
