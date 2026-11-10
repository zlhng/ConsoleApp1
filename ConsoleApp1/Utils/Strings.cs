using System.Collections.Generic;

namespace DeviceScanner.Utils
{
    /// <summary>
    /// Every fixed text in the prompts and the scan report: key -> { English, 简体中文 }.
    /// {0}, {1} ... are filled in by L.T(key, args). Empty Chinese = fall back to English.
    /// To add a language later, add a third column and a Lang value.
    /// </summary>
    internal static class Strings
    {
        public static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // ----- status tags -----
            ["tag.pass"] = new[] { "PASS", "通过" },
            ["tag.fail"] = new[] { "FAIL", "失败" },
            ["tag.warn"] = new[] { "WARN", "警告" },
            ["tag.notRun"] = new[] { "NOT RUN", "未运行" },
            ["tag.busy"] = new[] { "BUSY", "占用" },
            ["tag.na"] = new[] { "N/A", "不适用" },
            ["tag.fixed"] = new[] { "FIXED", "已修正" },

            // ----- dialog buttons -----
            ["btn.yes"]    = new[] { "Yes", "是" },
            ["btn.no"]     = new[] { "No", "否" },
            ["btn.ok"]     = new[] { "OK", "确定" },
            ["btn.cancel"] = new[] { "Cancel", "取消" },

            // ----- prompts (Program) -----
            ["prompt.username"]  = new[] { "Who is running this scan? Enter your name (it is printed in the report).",
                                           "请输入执行本次检测的人员姓名（将显示在报告中）。" },
            ["prompt.spec"]      = new[] { "Run Windows spec check (read only; the Chinese file-name scan can take a few minutes)?",
                                           "运行 Windows 规格检查？（只读；中文文件名扫描可能需要几分钟）" },
            ["prompt.discovery"] = new[] { "Also run full discovery (ping sweep + COM scan, slow)? This PC's network and COM port addresses are set to match the profile; a camera or ZMC at a wrong address is moved only after you confirm.",
                                           "同时运行完整设备发现？（Ping 扫描 + COM 扫描，较慢）本机网卡地址和 COM 口号将按配置文件修正；地址不对的相机或 ZMC 需您确认后才会修改。" },
            ["confirm.deviceIp"] = new[] { "Change the {0} IP address?\n\n{1}\n{2}  ->  {3} (profile)\n\nOther programs using the old address lose the connection.",
                                           "修改{0} IP 地址？\n\n{1}\n{2}  ->  {3}（配置文件）\n\n使用旧地址的其他程序将断开连接。" },
            ["dev.camera"]       = new[] { "camera", "相机" },
            ["dev.zmc"]          = new[] { "ZMC controller", "ZMC 控制器" },
            ["prompt.home"]      = new[] { "Home gantry ({0})? Make sure the gantry is clear.",
                                           "龙门回原点（{0}）？请确认龙门周围无障碍物。" },
            ["prompt.io"]        = new[] { "Run {0} discrete IO tests? Outputs will really switch (lifters, stoppers, vacuum...). Clear the machine first.",
                                           "运行 {0} 项离散 IO 测试？输出会真实动作（升降、阻挡、真空等），请先清空机台。" },

            // ----- messages (Program) -----
            ["msg.noHome"]       = new[] { "Profile has no home.programs, skipping homing.", "配置文件没有 home.programs，跳过回原点。" },
            ["msg.noIo"]         = new[] { "Profile has no io tests, skipping.", "配置文件没有 IO 测试，跳过。" },
            ["msg.zmcOpenFail"]  = new[] { "Cannot open ZMC at {0}.", "无法连接 ZMC：{0}。" },
            ["msg.zmcNotInit"]   = new[] { "Warning: controller not initialized; IO readings may differ from the main app.",
                                           "警告：控制器未初始化，IO 读数可能与主程序不同。" },
            ["msg.error"]        = new[] { "The scan stopped because of an unexpected error:\n{0}", "检测因意外错误而中止：\n{0}" },

            // ----- admin rights -----
            ["admin.declined"]   = new[] { "Administrator rights were not granted.\n\nContinue without them? Some Windows checks will show WARN, and Windows may ask to allow network access during the scan.",
                                           "未获得管理员权限。\n\n是否在没有管理员权限的情况下继续？部分 Windows 检查将显示警告，检测期间 Windows 可能会询问是否允许网络访问。" },

            // ----- run mode -----
            ["mode.question"]    = new[] { "How do you want to run the checks?", "请选择检测方式：" },
            ["mode.all"]         = new[] { "Run all modules (no more questions)", "运行全部模块（不再逐项询问）" },
            ["mode.each"]        = new[] { "One by one (ask before each module)", "逐项运行（每个模块前询问）" },
            ["mode.allCaution"]  = new[] { "A full run will move the machine without asking again:", "全部运行将直接动作机台，不再询问：" },

            // ----- scan window -----
            ["ui.operator"]      = new[] { "Operator: {0}    Started: {1}", "操作员：{0}    开始时间：{1}" },
            ["ui.starting"]      = new[] { "Starting...", "正在启动……" },
            ["ui.checking"]      = new[] { "Checking {0}...", "正在检查：{0}……" },
            ["ui.waitingNext"]   = new[] { "Next module...", "下一个模块……" },
            ["ui.waiting"]       = new[] { "WAITING", "等待" },
            ["ui.running"]       = new[] { "RUNNING", "进行中" },
            ["ui.recheck"]       = new[] { "checked again after address changes", "地址修正后重新检查" },
            ["ui.more"]          = new[] { "... and {0} more (see the PDF report)", "……另有 {0} 项（见 PDF 报告）" },
            ["ui.showLog"]       = new[] { "Show log", "显示日志" },
            ["ui.hideLog"]       = new[] { "Hide log", "隐藏日志" },
            ["ui.openLog"]       = new[] { "Open log file", "打开日志文件" },
            ["ui.openPdf"]       = new[] { "Open PDF report", "打开 PDF 报告" },
            ["ui.close"]         = new[] { "Close", "关闭" },
            ["ui.rescan"]        = new[] { "Scan again", "重新检测" },
            ["ui.fix"]           = new[] { "Fix problems ({0})", "修复问题（{0}）" },
            ["ui.email"]         = new[] { "Email report to QC", "发送报告给 QC" },

            // ----- online only / QC email (lite edition) -----
            ["online.required"]  = new[] { "This PC is offline: the mail server {0}:{1} cannot be reached.\n\nThe scanner only runs online. Connect the PC to the network, then choose Yes to try again (No stops).",
                                           "电脑处于离线状态：无法连接邮件服务器 {0}:{1}。\n\n本程序只能在线运行。请连接网络后选择“是”重试（选择“否”停止）。" },
            ["online.offline"]   = new[] { "OFFLINE: {0}:{1} not reachable", "离线：无法连接 {0}:{1}" },
            ["online.lost"]      = new[] { "network connection lost", "网络连接已断开" },
            ["online.back"]      = new[] { "network connection back", "网络连接已恢复" },
            ["online.stopped"]   = new[] { "stopped: the PC went offline", "已停止：电脑离线" },
            ["mail.ask"]         = new[] { "Send the report to QC now?\n\nTo: {0}\nCc: {1}", "现在将报告发送给 QC？\n\n收件人：{0}\n抄送：{1}" },
            ["mail.from"]        = new[] { "Sender email address to show on the report email (replies come to it):",
                                           "请输入报告邮件上显示的发件人邮箱（回复将发到此邮箱）：" },
            ["mail.noPassword"]  = new[] { "mail.secret (the Gmail app password) is missing next to the exe", "程序目录下缺少 mail.secret（Gmail 应用专用密码）" },
            ["mail.badFrom"]     = new[] { "\"{0}\" is not an email address.", "“{0}”不是有效的邮箱地址。" },
            ["mail.sending"]     = new[] { "Emailing the report to {0}", "正在将报告发送至 {0}" },
            ["mail.sent"]        = new[] { "Report emailed to {0}", "报告已发送至 {0}" },
            ["mail.failed"]      = new[] { "The report was not emailed: {0}", "报告发送失败：{0}" },
            ["mail.subject"]     = new[] { "Device scan {0}: {1} ({2})", "设备检测 {0}：{1}（{2}）" },
            ["mail.body"]        = new[] { "Machine profile: {0}\nOperator: {1}\nStarted: {2}\nPC: {3}\n\nThe PDF report and the log are attached.",
                                           "机台配置：{0}\n操作员：{1}\n开始时间：{2}\n电脑：{3}\n\n附件为 PDF 报告和日志。" },

            // ----- installing missing drivers / VC++ runtime -----
            ["fix.needAdmin"]    = new[] { "Installing drivers, the VC++ runtime and changing network settings needs administrator rights.\n\nClose the scanner, start it again and choose Yes on the Windows prompt.",
                                           "安装驱动、VC++ 运行库和修改网卡设置需要管理员权限。\n\n请关闭检测程序后重新启动，并在 Windows 提示中选择“是”。" },
            ["fix.pickFile"]     = new[] { "{0} is not next to the scanner - select it", "检测程序目录下没有 {0}，请手动选择" },
            ["fix.confirm"]      = new[] { "Fix {0} item(s)?", "修复 {0} 个项目？" },
            ["fix.nicWarning"]   = new[] { "Network adapters drop out for a few seconds while their driver or settings change.",
                                           "安装网卡驱动或修改网卡设置时，网络会中断几秒钟。" },
            ["fix.running"]      = new[] { "Fixing...", "正在修复……" },
            ["fix.done"]         = new[] { "{0} of {1} item(s) fixed.", "{1} 个项目中，已修复 {0} 个。" },
            ["fix.notInPackage"] = new[] { "not in the driver package", "驱动包中没有此驱动" },
            ["fix.notAdded"]     = new[] { "still not in the driver store", "仍未出现在驱动库中" },
            ["fix.reboot"]       = new[] { "Restart the PC to finish the installation.", "请重启电脑以完成安装。" },
            ["fix.rescan"]       = new[] { "Run the scan again so the report shows the new state.", "请重新运行检测，以便报告显示最新状态。" },
            ["ui.quitRunning"]   = new[] { "A check is still running and cannot be stopped half way.\nQuit anyway? No report will be saved.",
                                           "检测仍在进行，无法中途停止。\n仍要退出吗？将不会保存报告。" },

            // ----- profile selection -----
            ["profile.loaded"]   = new[] { "Profile: {0}  ({1})", "配置文件：{0}  （{1}）" },
            ["profile.error"]    = new[] { "Profile error: {0}", "配置文件错误：{0}" },
            ["profile.none"]     = new[] { "No profiles found. Wrote an example to {0}", "未找到配置文件，已写入示例：{0}" },
            ["profile.editHint"] = new[] { "Edit it for this machine (or copy it for another one), then run again.",
                                           "请按本机修改（或复制给其他机台），然后重新运行。" },
            ["profile.choose"]   = new[] { "Choose the profile for this machine:", "请选择本机的配置文件：" },

            // ----- report header -----
            ["sum.expected"]     = new[] { "(expected {0})", "（应为 {0}）" },

            // ----- modules (one PASS / WARN / FAIL / NOT RUN each) -----
            ["mod.windowsspec"]  = new[] { "Windows spec", "Windows 规格" },
            ["mod.drivers"]      = new[] { "Drivers", "驱动程序" },
            ["mod.devices"]      = new[] { "Devices", "设备" },
            ["mod.homing"]       = new[] { "Gantry homing", "龙门回原点" },
            ["mod.io"]           = new[] { "Discrete IO", "离散 IO" },
            ["dev.cameraNic"]    = new[] { "Camera NIC (Intel I350)", "相机网卡（Intel I350）" },
            ["dev.cameraNicMissing"] = new[] { "no network adapter with this hardware ID on this PC", "本机没有此硬件 ID 的网卡" },
            ["dev.cameraNicPorts"] = new[] { "{0} port(s), need at least {1} (one per camera IP):", "{0} 个端口，至少需要 {1} 个（每个相机 IP 一个）：" },
            ["dev.cameraRoute"]  = new[] { "Camera NIC route", "相机网卡路由" },
            ["dev.routeUnknown"] = new[] { "camera answers but its local NIC could not be identified (required {0})", "相机有响应，但无法识别其本机网卡（要求 {0}）" },
            ["dev.wrongNic"]     = new[] { "camera on the wrong NIC: {0}, required {1}", "相机连接在错误的网卡上：{0}，要求 {1}" },
            ["dev.nicDown"]      = new[] { "not connected", "未连接" },
            ["mod.burnrun"]      = new[] { "Conveyor burn run", "输送带空跑" },
            ["burn.laneCycles"]  = new[] { "{0}: {1} cycle(s)", "{0}：{1} 次" },
            ["burn.summary"]     = new[] { "{0}   {1:F1} min", "{0}   {1:F1} 分钟" },
            ["burn.programs"]    = new[] { "programs: {0}", "程序：{0}" },
            ["burn.stopped"]     = new[] { "stopped by the operator", "操作员已停止" },
            ["burn.done"]        = new[] { "ran the set time ({0})", "已运行设定时间（{0}）" },
            ["burn.early"]       = new[] { "stopped after {0} of the set {1}", "设定 {1}，仅运行了 {0}" },
            ["burn.planned"]     = new[] { "set {0}", "设定 {0}" },
            ["burn.untilStop"]   = new[] { "until Stop", "直到停止" },
            ["burn.speedPct"]    = new[] { "speed {0:0.#} %", "速度 {0:0.#} %" },
            ["prompt.burnTime"]  = new[] { "How long should the burn run go?\n\nIt stops by itself when the time is up (the lane in progress finishes its cycle first). All zero = until you press Stop burn run.",
                                           "空跑要运行多长时间？\n\n时间到后自动停止（通道会先完成当前循环）。全部为 0 = 一直运行，直到按下“停止空跑”。" },
            ["burn.cancelled"]   = new[] { "Run time not set, burn run skipped.", "未设定运行时间，已跳过空跑。" },
            ["lbl.hours"]        = new[] { "Hours", "小时" },
            ["lbl.minutes"]      = new[] { "Minutes", "分钟" },
            ["lbl.seconds"]      = new[] { "Seconds", "秒" },
            ["burn.noCycle"]     = new[] { "stopped before any lane finished a cycle", "在任何通道完成一个循环前已停止" },
            ["burn.needHome"]    = new[] { "not run: homing did not pass", "未运行：回原点未通过" },
            ["msg.noBurn"]       = new[] { "Profile has no burn.lane lines, skipping.", "配置文件没有 burn.lane，跳过。" },
            ["prompt.burn"]      = new[] { "Homing done. Run the conveyor burn run ({0}) now?\n\nNo test board is needed (unless the profile sets burn.sensors = on). Conveyors, stoppers, lifters and the gantry move at {1:0.#} % of their original speed; keep hands clear.\nYou set the run time next; Stop burn run ends it early.",
                                           "回原点完成。现在运行输送带空跑（{0}）？\n\n无需放置测试板（配置文件设 burn.sensors = on 时除外）。输送带、挡板、顶升和龙门会以原始参数 {1:0.#}% 的速度动作，请勿将手伸入。\n下一步设定运行时间；按“停止空跑”可提前结束。" },
            ["ui.stopBurn"]      = new[] { "Stop burn run", "停止空跑" },
            ["ui.stoppingBurn"]  = new[] { "Stopping the burn run...", "正在停止空跑……" },
            ["col.lane"]         = new[] { "Lane", "通道" },
            ["col.cycles"]       = new[] { "Cycles", "循环次数" },
            ["mod.discovery"]    = new[] { "Discovery", "设备发现" },
            ["mod.counts"]       = new[] { "{0} passed, {1} failed, {2} warning(s) of {3}", "共 {3} 项，通过 {0}，失败 {1}，警告 {2}" },
            ["mod.skipped"]      = new[] { "skipped by the operator", "操作员选择跳过" },
            ["mod.homeSummary"]  = new[] { "{0}   axes: {1}   {2:F1} s", "{0}   轴：{1}   {2:F1} 秒" },
            ["mod.fixSummary"]   = new[] { "{0} address(es) set to the profile", "已按配置文件修正 {0} 个地址" },
            ["mod.discoverySummary"] = new[] { "{0} IP host(s), {1} COM device(s) found (not limited to the profile)",
                                               "找到 {0} 个 IP 主机、{1} 个 COM 设备（不限于配置文件）" },

            // ----- discovery details -----
            ["sum.nothing"]      = new[] { "NOTHING MATCHED", "未识别" },
            ["sum.zmcElsewhere"] = new[] { "  {0} ZMC found at {1} but the profile says {2}", "  {0} 在 {1} 发现 ZMC，但配置文件为 {2}" },
            ["sum.itvCount"]     = new[] { "  COM  ITV count: {0}", "  COM  ITV 数量：{0}" },
            ["sum.comBusy"]      = new[] { "  COM  in use (device unknown): {0}", "  COM  被占用（设备未知）：{0}" },

            // ----- verdict -----
            ["sum.result"]       = new[] { "===== RESULT =====", "===== 结果 =====" },
            ["sum.overallPass"]  = new[] { "OVERALL: PASS", "总体：通过" },
            ["sum.overallFail"]  = new[] { "OVERALL: FAIL ({0} failure(s))", "总体：失败（{0} 项失败）" },
            ["sum.warnings"]     = new[] { "  {0} warning(s)", "  {0} 项警告" },
            ["sum.notRunList"]   = new[] { "  not run: {0}", "  未运行：{0}" },

            // ----- PDF -----
            ["sum.pdfSaved"]     = new[] { "Summary PDF saved to: {0}", "报告 PDF 已保存：{0}" },
            ["sum.pdfMissing"]   = new[] { "ERROR: PDF was not created: {0}", "错误：未生成 PDF：{0}" },
            ["sum.pdfError"]     = new[] { "ERROR creating PDF: {0}", "生成 PDF 出错：{0}" },
            ["sum.logSaved"]     = new[] { "Full log saved to: {0}", "完整日志已保存：{0}" },
            ["sum.logError"]     = new[] { "ERROR saving the log: {0}", "保存日志出错：{0}" },

            // ----- PDF layout -----
            ["pdf.title"]        = new[] { "SCAN REPORT", "检测报告" },
            ["pdf.profile"]      = new[] { "Profile: {0}", "配置文件：{0}" },
            ["pdf.overall"]      = new[] { "OVERALL", "总体" },
            ["pdf.user"]         = new[] { "Username", "用户名" },
            ["pdf.started"]      = new[] { "Started", "开始时间" },
            ["pdf.finished"]     = new[] { "Finished", "结束时间" },
            ["pdf.profileFile"]  = new[] { "Profile file", "配置文件路径" },
            ["pdf.failures"]     = new[] { "Failures: {0}", "失败：{0}" },
            ["pdf.warnings"]     = new[] { "Warnings: {0}", "警告：{0}" },
            ["pdf.notRun"]       = new[] { "Not run: {0}", "未运行：{0}" },
            ["pdf.none"]         = new[] { "none", "无" },
            ["pdf.modules"]      = new[] { "Module results", "模块结果" },
            ["pdf.result"]       = new[] { "Result", "结果" },
            ["pdf.file"]         = new[] { "Report file: {0}", "报告文件：{0}" },
            ["pdf.page"]         = new[] { "Page {0} of {1}", "第 {0} 页，共 {1} 页" },
            ["col.status"]       = new[] { "Status", "状态" },
            ["col.module"]       = new[] { "Module", "模块" },
            ["col.result"]       = new[] { "Result", "结果" },
            ["col.duration"]     = new[] { "Time", "耗时" },
            ["col.group"]        = new[] { "Group", "分组" },
            ["col.check"]        = new[] { "Check", "检查项" },
            ["col.actual"]       = new[] { "Actual", "实际值" },
            ["col.expected"]     = new[] { "Expected", "应为" },
            ["col.detail"]       = new[] { "Detail", "说明" },
            ["col.kind"]         = new[] { "Kind", "类型" },
            ["col.address"]      = new[] { "Address", "地址" },
            ["col.device"]       = new[] { "Device", "设备" },
            ["col.programs"]     = new[] { "Programs", "回原点程序" },
            ["col.axes"]         = new[] { "Axes", "轴" },
            ["col.time"]         = new[] { "Time", "耗时" },
            ["col.op"]           = new[] { "OP", "输出" },
            ["col.in"]           = new[] { "IN", "输入" },
            ["col.name"]         = new[] { "Name", "名称" },
        };
    }
}
