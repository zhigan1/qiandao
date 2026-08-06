# 夸克网盘自动签到 (.NET 版)

基于 [Quark_Auot_Check_In](https://github.com/zhigan1/Quark_Auot_Check_In) 的 Python 脚本重写为 .NET 8 控制台应用，签到接口与行为保持一致，并内置 GitHub Actions 工作流。

> 本项目仅供学习交流，请勿用于非法用途。

## 功能

- 每日自动签到夸克网盘，领取成长奖励空间
- 支持多账号，账号之间用换行或 `&&` 分隔
- 今日已签到自动跳过，避免重复请求
- GitHub Actions 定时执行（北京时间 08:00 和 13:00），支持手动触发
- 随机延迟，模拟人工操作
- 自动清理旧的 workflow 运行记录，保持仓库活跃

## 目录结构

```text
.
├── .github/workflows/
│   ├── quark-signin.yml            # 每日签到工作流
│   └── empty-commit-keepalive.yml  # 每月空提交保活
├── src/QuarkCheckIn/
│   ├── Program.cs                  # 入口：读取环境变量、遍历账号
│   ├── QuarkAccount.cs             # 账号解析
│   └── QuarkClient.cs              # 夸克签到 API 客户端
├── QuarkNetCheckIn.sln
└── README.md
```

## 快速开始（GitHub Actions）

1. Fork 或上传本仓库到自己的 GitHub。
2. 在仓库 `Settings -> Secrets and variables -> Actions` 中新增 `COOKIE_QUARK` Secret。
3. 在 `Settings -> Actions -> General` 中将 Workflow permissions 设置为 **Read and write permissions**。
4. 进入 Actions 页面，手动运行 `Quark 签到 (.NET)` 验证配置。

`COOKIE_QUARK` 格式（单账号）：

```text
user=张三; kps=abcdefg; sign=hijklmn; vcode=111111111;
```

多账号用换行或 `&&` 分隔：

```text
user=张三; kps=abc; sign=def; vcode=111;
&&
user=李四; kps=ghi; sign=jkl; vcode=222;
```

`user` 可随意填写，用于日志区分；`kps`、`sign`、`vcode` 从夸克网盘签到接口请求中抓取。

## 本地运行

需要 .NET 8 SDK 或更高版本。

```powershell
$env:COOKIE_QUARK = "user=张三; kps=abc; sign=def; vcode=111;"
dotnet run --project src/QuarkCheckIn
```

Linux / macOS：

```bash
export COOKIE_QUARK="user=张三; kps=abc; sign=def; vcode=111;"
dotnet run --project src/QuarkCheckIn
```

## 上传到 GitHub

```bash
git init
git add .
git commit -m "init: Quark .NET check-in"
git branch -M main
git remote add origin https://github.com/<你的用户名>/<你的仓库名>.git
git push -u origin main
```

## 注意事项

- Cookie 或 `kps`、`sign`、`vcode` 过期后需要重新抓取并更新 Secret。
- 频繁手动触发可能被目标服务限流，请谨慎操作。
- 本项目在 MIT 协议下发布，重写自 MIT 协议的 [Quark_Auot_Check_In](https://github.com/zhigan1/Quark_Auot_Check_In)，保留原作者版权声明。
