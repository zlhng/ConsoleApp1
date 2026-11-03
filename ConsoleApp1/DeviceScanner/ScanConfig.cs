using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeviceScanner
{
    /// <summary>
    /// Tuning that is the same on every machine. Anything machine-specific (IPs, COM ports, programs,
    /// axes, IO numbers) lives in a .profile file instead.
    /// </summary>
    internal sealed class ScanConfig
    {
        // ----- device verification / discovery -----
        public int[] Subnets { get; } = { 0, 1 };            // discovery sweep: 192.168.<n>.1-254
        public int PingTimeoutMs { get; } = 500;
        public int PingParallelism { get; } = 64;
        public HashSet<string> MultiInstanceComDevices { get; } =
            new HashSet<string> { "ITV regulator", "OPT light", "LC18 light" };
        /// <summary>Every GigE camera must be reached through this NIC (Intel I350), else the camera check FAILS.</summary>
        public string CameraNicPnpId { get; } = @"PCI\VEN_8086&DEV_1521";
        /// <summary>Discovery gives a wired NIC this host number (192.168.x.200 /24) when a profile subnet has no NIC; next free one if taken.</summary>
        public int PcHostOctet { get; } = 200;

        // ----- controller init -----
        public int InitTimeoutMs { get; } = 10000;           // wait for sysstate == 1 after RUN <init program>

        // ----- homing -----
        public int MaxProbeAxes { get; } = 32;               // upper bound when auto-discovering axes (profile home.axes empty)
        public int[] ExcludeAxes { get; } = { };             // pumps / encoders / conveyors you don't want awaited
        public bool UseDoneFlag { get; } = false;            // true only if the home programs set VR(HomeDoneVr)=1 at the end
        public int HomeDoneVr { get; } = 100;
        public int StartWaitMs { get; } = 3000;              // max wait for first axis to move after RUN
        public int IdleQuietMs { get; } = 7000;              // all axes idle this long = homing finished
        public int HomeTimeoutMs { get; } = 60000;
        public int AxisSettleTimeoutMs { get; } = 10000;

        // ----- discrete IO test -----
        public int IoScanCount { get; } = 64;                // inputs 0..N-1 are read before/after each toggle
        public int IoSettleMs { get; } = 1000;               // used when a profile io line has no settle time
        public string Password = "ScanReport"; // password for PDF encryption
        public string ReportDir = "ScanReports"; // directory to save PDF reports

        // ----- Windows Firewall -----
        public string FirewallRuleName = "Device Scanner";   // inbound allow rule for this exe, so Windows never asks to allow network access

        // ----- lite edition: runs online only, the report is emailed to QC at the end -----
        public string QcEmailAddress = "zlhng@nswautomation.com";
        public string[] QcCc = { "zlhng@gmail.com", "huagulong1234@gmail.com" };
        // Sent through Gmail so it is not flagged as spam: the envelope From is the Gmail account itself,
        // the operator's own address (asked before sending) goes in the sender name and Reply-To.
        public string SmtpHost = "smtp.gmail.com";           // also the online check: this host must answer on SmtpPort
        public int SmtpPort = 587;
        public bool SmtpSsl = true;                          // STARTTLS
        public string SmtpUser = "6cillefong@gmail.com";                  // the Gmail address, also the From address
        // the Gmail app password is not kept in source: one line in mail.secret next to the exe (git-ignored)
        public string SmtpPassword => ReadSecret("mail.secret");
        public int SmtpTimeoutMs = 60000;
        public int OnlineTimeoutMs = 3000;

        /// <summary>First line of a file next to the exe, spaces removed; empty if the file is missing.</summary>
        private static string ReadSecret(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (!File.Exists(path)) return "";
            string line = File.ReadLines(path).FirstOrDefault() ?? "";
            return line.Replace(" ", "").Trim();
        }
    }
}
