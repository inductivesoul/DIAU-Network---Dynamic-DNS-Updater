DIAU Namecheap Dynamic DNS Updater (v1.0.0 Stable)

A robust, fault-tolerant Windows utility designed to keep your Namecheap Dynamic DNS records perfectly synced with your public IP address. Designed for reliability, this app functions as a "set-it-and-forget-it" background service with an ultra-lightweight footprint.
🚀 Key Features
🧠 Smart Update Logic

    IP Intelligence: Uses Cloudflare’s icanhazip.com to detect your external public IP via secure HTTPS.

    Pre-Flight DNS Verification: Automatically compares your detected IP against the live DNS record. If they already match, the app skips the update to save system resources and prevent unnecessary API calls.

    Automatic "Safety Kick": On startup, the app checks if a scheduled update was missed (e.g., due to a power outage) and forces an immediate sync if necessary.

🛡️ Spam & Abuse Protection

    Rate Limiting: Enforces a strict cooldown window for all updates to stay in compliance with registrar policies.

    Smart Bypass: Manual updates are only allowed within the cooldown period if a DNS mismatch is detected, ensuring you can fix issues instantly without triggering Namecheap's spam filters.

🔌 Failover & Fault Tolerance

    Recursive Failover: If the primary Cloudflare IP lookup fails, the app alerts you with high-visibility, color-coded log entries.

    Requester Auto-detect: In the event of a third-party API failure, the app falls back to Namecheap’s internal IP detection logic to ensure your records stay updated regardless of service outages.

🖥️ Professional UI/UX

    Resource Efficient: Optimized for high-uptime environments (like game servers). Runs at ~26MB RAM and 0% idle CPU.

    System Tray Integration: Full "Minimize to Tray" support with a dynamic, color-coded status icon:

        🟢 Green: All systems synced and running.

        🟡 Yellow: Update failure or mismatch detected.

        🔴 Red: Periodic updates disabled.

    Live Dashboard: Features a real-time countdown timer, IP status trackers, and a color-coded terminal log.

    Theme Persistence: Fully themed interface (Dark/Light modes) that remembers your preference across restarts.

🛠️ How It Works

    Detection: Every X minutes, the app pings a secure endpoint to find your true external IP.

    Comparison: It performs a live DNS lookup on your configured domains.

    Decision Engine:

        If IPs match: Logs "Verified (No Change)" and returns to idle.

        If IPs differ: Sends an authenticated update request to Namecheap's Dynamic DNS API.

        If lookup fails: Notifies the user via the UI/Tray and forces an update using Namecheap's requester auto-detection as a safety net.

📥 Installation & Setup

    1. Download: Download the latest Release package from the GitHub repository.
        https://github.com/inductivesoul/DIAU-Network---Dynamic-DNS-Updater/releases
            Dependency Note: .NET Desktop Runtime 10.0 is required to run this application.
                https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.5/windowsdesktop-runtime-10.0.5-win-x64.exe

    2. Extract: Unzip the files to the directory you wish to run it in.

    3. Launch: Run the app: DIAU Network - Dynamic DNS Updater.exe.

    4. Configure Records: Open the app and enter your Host (e.g., @, *, or valheim), Domain, and the Dynamic DNS Password provided in your Namecheap dashboard.

    5. Set & Forget:

        Check "Enable periodic updates."

        Check "Run at Startup" to ensure continuous protection after a reboot.

        Close the window to let it run silently in the system tray.


> [!IMPORTANT]
> **Important Security Note**
> The app generates configuration files to store your records. Do not share these files or push them to public repositories, as they contain your Dynamic DNS passwords.
> 
> It is highly recommended to add these to your `.gitignore`:
> ```text
> diau_ddns_config.json
> diau_ui_config.json
> ```

> [!CAUTION]
> **Protect Your Configuration**
> Ensure you protect and do not share the `diau_ddns_config.json` file from your install directory. This file contains your DNS update passwords. Anyone who gets hold of this file can use it to maliciously change your DNS records.

📄 License

Distributed under the MIT License. See LICENSE for more information.
