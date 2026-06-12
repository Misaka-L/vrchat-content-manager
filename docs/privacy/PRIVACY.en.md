# Privacy Policy

*Last updated: June 12, 2026*

**This is an English translation of the original Chinese Privacy Policy. It is provided for reference only. In case of any discrepancy or dispute, the Chinese version shall prevail.**

## Introduction

This Privacy Policy is published by the developer of VRChat Content Publisher ("we," "us," or "our") to explain how we access, collect, store, use, and share (collectively, "process") your data when you use the Software.

This policy applies to the **VRChat Content Publisher** desktop client (Windows / Linux, hereinafter referred to as "the Software"), including when you:

- Use the Software to upload Avatars or Worlds to VRChat
- Log into your VRChat account through the Software

In this policy, "you" refers to the user of the Software. For ease of reading, the following terms have specific meanings throughout this document:

- **Content**: The Avatars and Worlds you create and upload to VRChat
- **User Icon**: Your VRChat account profile picture, unrelated to uploaded Content

## 1. Important Notice: Unofficial Client and Platform Risks

The Software is a **third-party, unofficial client** for VRChat. We are not affiliated with VRChat Inc. in any way, and the Software **is not authorized, endorsed, or sponsored by VRChat**.

Using the Software may violate VRChat's Terms of Service. Any resulting consequences—including but not limited to warnings, restrictions, suspension, or permanent banning of your VRChat account—**are solely your responsibility**. The developer assumes no liability for such platform penalties or any losses arising therefrom.

## 2. What Data Do We Collect?

### 2.1 Data You Provide to Us

When you log in, you are required to provide:

- Your VRChat username
- Your VRChat password
- A TOTP verification code (if you have enabled two-factor authentication)

**Your password and verification code are used only for completing the current login session. They are discarded immediately after use and are never stored in any form within the Software or on your device.**

### 2.2 Data We Obtain from the VRChat Platform

After a successful login, the Software directly calls VRChat's server interfaces to obtain the following information. The documentation for these interfaces has been compiled by the community through reverse engineering (VRChat does not provide official API documentation), but all data requests are sent directly from your device to VRChat's servers:

**Basic User Information** (obtained at login, stored in `sessions.json`):

- User ID
- Username
- User Icon
- Display name

**Content Information** (obtained when initiating an upload task, stored in `database/app.db`):

- The name and ID of the Content you intend to upload

This information is used solely to provide the core functionality of the Software. Corresponding data is cleared upon logout or task deletion. See Section 5.2 for details.

VRChat's collection and processing of data on its platform is governed by its own privacy policy. For details, please refer to the [VRChat Privacy Policy](https://hello.vrchat.com/privacy).

### 2.3 Content Files You Designate for Upload

When you initiate an upload task, the Software reads the AssetBundle file you specify (built by the VRChat SDK) and temporarily caches a copy in the operating system's cache directory for uploading to VRChat's servers. Once the upload task is completed or you manually delete the task, the associated cache is cleared.

### 2.4 Data We Generate Automatically

To maintain normal operation, the Software automatically generates the following data on your device:

- **Login Cookies**: Used to maintain your login session with VRChat, stored in `sessions.json`
- **Application Settings**: Your preference configurations (including Sentry telemetry preferences), stored in `settings.json`
- **Task Records**: The status and progress of your upload tasks, stored in `database/app.db`
- **Local Debug Logs**: Used for troubleshooting software issues (see Section 2.5), stored in `logs/`

### 2.5 Telemetry Data and Local Debug Logs

The Software generates two types of records, which are handled very differently:

**Sentry Telemetry Data (Under Your Control)**

When you enable Sentry telemetry, crash reports and diagnostic data are sent directly to **Functional Software, Inc. (the operator of the Sentry service)** based on the mode you have selected (see Section 4.1). In the default "Privacy Mode," identifiers such as user IDs are sanitized before leaving your device (see Section 4.1). The original values are discarded immediately after hashing and are never uploaded.

**Local Debug Logs (Fully Under Your Control)**

Log files stored on your device may contain **unsanitized raw data** (such as complete user IDs, Content names, etc.). This is necessary for accurately identifying the root cause when troubleshooting complex issues.

**Local debug logs are never automatically uploaded.** The developer can only view these files if you actively choose to share them via GitHub Issues or email.

### 2.6 Data Collection Overview

| Data Category | Stored? | Storage Location | Uploaded? | Under Your Control? |
|--------------|---------|-----------------|-----------|---------------------|
| Password / TOTP | No | — | — | — |
| Cookies | Yes, plaintext | `sessions.json` (local) | No | Cleared upon logout |
| Basic User Information (User ID, Username, User Icon, Display Name) | Yes, plaintext | `sessions.json` (local) | No | Cleared upon logout |
| Content Information (Name, ID) | Yes | `database/app.db` (local) | No | Cleared upon task deletion |
| AssetBundle Files | Yes (temporarily cached during upload) | OS cache directory | Yes (to VRChat servers only) | Cache cleared upon task deletion |
| Sentry Telemetry Data | Yes | Stored on Sentry servers (Germany) | Depends on mode | Disable or switch modes at any time |
| Local Debug Logs | Yes, plaintext | `logs/` (local) | **Never automatically uploaded** | Fully under your control |

## 3. How Do We Use This Data?

### 3.1 Providing Core Functionality

The core functionality of the Software is to provide you with a VRChat Content upload service. To deliver this service, your data is used for:

- **Login Authentication**: Your password and TOTP verification code are used only for the current login session and are discarded immediately afterwards.
- **Session Persistence**: Cookies and basic user information are stored locally so you do not need to log in again each time you start the Software.
- **Performing Uploads**: Transferring the AssetBundle files you specify to the VRChat platform.
- **Resuming Upload Progress**: Information about your upload tasks is saved locally. If the Software exits unexpectedly during an upload, you can resume from where it left off upon restart.

### 3.2 Crash Diagnosis and Improvement

- **Sentry Telemetry**: When you have Sentry enabled, crash reports and diagnostic data are used to help the developer identify software defects and analyze performance issues. Sentry servers are located in Germany (EU).
- **Local Debug Logs**: Stored solely on your device. When you report an issue, the developer may request that you manually share relevant logs for further investigation. **Whether you share them is entirely your decision.**

## 4. How Do We Share Data With Third Parties?

We do not sell your data. Only in the following circumstances is some data transmitted directly to third-party services. Each transmission is governed by that third party's own privacy policy, and we are not responsible for their data handling practices.

### 4.1 Crash Diagnostic Service Provider: Sentry

When you enable Sentry telemetry, crash diagnostic data is sent directly to **Functional Software, Inc. (the operator of the Sentry service)** to help us identify and fix software issues. Data is stored on servers located in Germany. As an independent data processor, Sentry's use of this data is governed by its [Privacy Policy](https://sentry.io/privacy/).

**Three Modes**

You will be asked about your Sentry preference when you first start the Software, and you can change it at any time in the settings.

> Privacy Mode is enabled by default because it provides sufficient diagnostic information while protecting your privacy. Full Mode exists only for situations where the developer needs more detailed data to troubleshoot specific complex issues—you may choose to temporarily enable it at your own discretion.

| Mode | Data Sent |
|------|-----------|
| Disabled | None |
| Privacy Mode (Default) | Device information, crash stack traces, sanitized identifiers |
| Full Mode | All data from Privacy Mode + raw identifiers (unsanitized) + IP address |

**Sanitization Details**

In Privacy Mode, the following identifiers are sanitized before leaving your device:

- **Sanitized items**: User ID, World ID, Avatar ID
- **Sanitization method**: MD5 hash + salt + truncation to the first eight characters
- **Example**: `usr_c6806bef-b885-427d-acaf-e24a66acb829` → `usr_2af9d47c`
- **Usernames and Content names**: Replaced directly with placeholders; the original text is never uploaded.
- **Original data handling**: The original identifiers are discarded immediately after the hash computation is complete and never leave your device.

**Important reminder**: Regardless of the mode selected, once data has been uploaded to Sentry, it cannot be deleted from Sentry's servers.

### 4.2 VRChat Platform

The Software operates as a client. Network requests generated by your use of the Software are sent directly from your device to VRChat's servers. VRChat's processing of data received from your device is governed by its [Privacy Policy](https://hello.vrchat.com/privacy). There is no independent data-sharing relationship between us and VRChat.

### 4.3 Other Data

Apart from the above circumstances, we do not sell, rent, trade, or share your data with any other third parties.

## 5. How Is Your Data Stored? Is It Secure?

### 5.1 Storage Location

All local data is stored on your device:

- **Windows**: `%LOCALAPPDATA%\vrchat-content-manager-81b7bca3\`
- **Linux**: `$XDG_CONFIG_HOME/vrchat-content-manager-81b7bca3/` (on most distributions, this resolves to `$HOME/.config`)

### 5.2 Storage Format

All files are stored in **plaintext (unencrypted)** format:

- `sessions.json`: Login cookies, basic user information (username, user ID, User Icon, display name, etc.)
- `settings.json`: Application settings (including Sentry preferences)
- `database/app.db`: Task records (upload task information)
- `logs/`: Debug log files (plain text)
- OS cache directory: AssetBundle files temporarily cached during upload; automatically cleared upon task completion or deletion

### ⚠️ Important Security Notice

**Cookies are stored in plaintext on your device. Anyone with access to your device's file system, or any malicious software, may be able to read these cookies and use them to log into your VRChat account.** We recommend that you:

- Do not share your computer user account with others.
- Log out promptly after using the Software on public or shared devices.
- Keep your operating system up to date with security patches.

### 5.3 Data Residue After Uninstallation

Uninstalling the Software does not automatically delete the data folders mentioned above. To completely remove all local data, please manually delete the `vrchat-content-manager-81b7bca3` directory at the corresponding path. We plan to offer a cleanup option during uninstallation in a future version.

## 6. What Choices and Rights Do You Have?

- **Log Out**: Clicking "Log Out" will immediately clear the cookies and basic user information stored in `sessions.json`.
- **Complete Data Removal**: Manually deleting the configuration directory (see Section 5.1 for the path) will remove all locally stored data.
- **Control Sentry Telemetry**: You can switch between "Disabled," "Privacy Mode," and "Full Mode" at any time in the settings. Selecting "Disabled" will stop all telemetry data uploads.
- **Manage Local Debug Logs**: Logs are fully under your control. You may view, delete, or choose not to share these files at any time. The Software will never automatically upload them.

**Limitations to note**:

- Once telemetry data has been uploaded to Sentry, we cannot assist you in deleting it from Sentry's servers. Disabling telemetry will prevent future uploads, but does not affect data that has already been sent.
- VRChat's platform does not offer a "revoke other login sessions" feature. Therefore, you cannot use the Software to forcibly log out VRChat sessions on other devices.

## 7. Note Regarding Minors

The Software has no means of knowing your actual age and does not actively collect age-related information. If you are a minor, we recommend that you obtain consent from a parent or legal guardian before using the Software. For VRChat's specific policies regarding minors, please refer to the [VRChat Terms of Service](https://hello.vrchat.com/legal).

## 8. Privacy Policy Updates

We may update this Privacy Policy from time to time. Material changes (such as new types of data collection, changes to data processing methods, or new sharing partners) will be notified to you via an in-app popup when the Software starts. The revision history of this policy can be reviewed through the commit records of the [GitHub repository](to be filled in).

## 9. Contact Information

- **Email**: `lipww1234@foxmail.com`

The Software is maintained by an individual developer in their spare time. While we cannot commit to a specific response timeframe, we will make reasonable efforts to reply to your inquiries as soon as possible.

## 10. Language Versions

The Chinese version of this Privacy Policy is the original version. This English translation is provided for reference only. In case of any discrepancy or dispute, the Chinese version shall prevail.

---

*This Privacy Policy has been drafted based on the actual data processing practices of the Software (VRChat Content Publisher). The source code is fully open source (GPLv3) and open to audit.*
