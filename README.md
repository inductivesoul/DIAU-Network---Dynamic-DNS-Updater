DIAU Namecheap Dynamic DNS Updater (v1.0.0 Stable)

A robust, fault-tolerant Windows utility designed to keep your Namecheap Dynamic DNS records perfectly synced with your public IP address. Designed for reliability, this app functions as a set-it-and-forget-it background service.
🚀 Key Features
🧠 Smart Update Logic

    IP Intelligence: Uses Cloudflare’s icanhazip.com to detect your external public IP.

    DNS Verification: Automatically compares your detected IP against the live DNS record. If they already match, the app skips the update to save resources and API calls.

    Automatic "Safety Kick": On startup, the app checks if a scheduled update was missed (e.g., due to power outage) and forces an immediate sync if necessary.

🛡️ Spam & Abuse Protection

    Rate Limiting: Enforces a strict 5-minute cooldown window for all updates.

    Smart Bypass: Manual updates are allowed within the cooldown period only if a DNS mismatch is detected, ensuring you can fix issues instantly without triggering Namecheap's spam filters.

🔌 Failover & Fault Tolerance

    Fallback Detection: If the primary Cloudflare IP lookup fails, the app alerts you with high-visibility, color-coded log entries.

    Auto-detect Mode: Automatically falls back to Namecheap’s internal IP detection logic to ensure your records stay updated even during third-party service outages.

🖥️ Professional UI/UX

    System Tray Integration: Minimize to tray support with a context menu to keep your taskbar clean while the app runs silently in the background.

    Live Dashboard: Real-time countdown timer, IP status trackers, and a color-coded terminal log.

    Dark & Light Modes: Fully themed interface that remembers your preference across restarts.

    Windows Startup: Integrated toggle to automatically launch the app when you log in.

🛠️ How It Works

    Detection: Every X minutes, the app pings Cloudflare to find your true external IP.

    Comparison: It performs a DNS lookup on your configured domains.

    Decision: * If IPs match: Logs "Verified (No Change)" and waits.

        If IPs differ: Sends an authenticated update request to Namecheap's Dynamic DNS API.

        If Cloudflare is down: Notifies the user and forces an update using Namecheap's requester auto-detection.

📥 Installation & Setup

    Clone the Repo:
    Bash

    git clone https://github.com/inductivesoul/DIAU-Network---Dynamic-DNS-Updater

    Configure Records:

        Open the app and enter your Host (e.g., @ or www), Domain, and the Dynamic DNS Password provided by Namecheap.

    Set & Forget:

        Check "Enable periodic updates".

        Check "Run at Startup" to ensure continuous protection.

        Close the window to let it run silently in the system tray.

⚠️ Important Security Note

The app generates a file named diau_ddns_config.json to store your records. Do not share this file or push it to public repositories, as it contains your Dynamic DNS passwords. It is recommended to add this file to your .gitignore.

📄 License

Distributed under the MIT License. See LICENSE for more information.
