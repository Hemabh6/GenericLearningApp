# Running Learning Desk on a spare Windows laptop

For a spare laptop running Windows with Docker Desktop: no USB stick, no reinstalling. Written for an HP
Pavilion 15 with a 256 GB SSD and a 1 TB hard disk, but it applies to any laptop with a similar layout.

Commands are for **Command Prompt** (`cmd`) unless it says otherwise. "Run as administrator" means:
Start menu → type `cmd` → right-click → *Run as administrator*.

## Is this a good idea?

**Good for:** staging, a demo, or a small private site (you and a few people). It costs nothing.

**Not good for:** a public site people depend on. A home machine has power cuts, a home internet
connection, Windows restarting for updates and an ageing laptop. For real production, a small paid
server (about $5 a month) is more reliable. See `DEPLOY.md`.

The best long-term setup for this laptop is **Ubuntu Server** on the SSD with the hard disk for backups.
It's more reliable than Windows for 24/7 use. That needs a USB stick to install, so this guide gets you
running today without one.

---

## 1. Check the laptop (5 minutes)

| Check | How | What you want |
| --- | --- | --- |
| Windows version | `Win+R`, type `winver` | Windows 10 version 22H2, or Windows 11. Docker Desktop won't install on old versions like 8.1. |
| Virtualization | Task Manager (`Ctrl+Shift+Esc`) → Performance → CPU | "Virtualization: **Enabled**". If Disabled, turn it on in BIOS: restart and press **Esc**, then **F10**, then find *Virtualization Technology*. |
| Memory | Task Manager → Performance → Memory | 8 GB is enough. 16 GB is comfortable. |
| Which disk is which | `powershell -NoProfile -Command "Get-PhysicalDisk \| Format-Table FriendlyName,MediaType,Size"` | Note which is the `SSD` and which is the `HDD` |
| Free space on the SSD | `powershell -NoProfile -Command "Get-Volume \| Format-Table DriveLetter,FileSystemLabel,Size,SizeRemaining"` | At least **40 GB free** on the drive Windows uses (usually `C:`) |
| Drive health | Install **CrystalDiskInfo** (free) and look for "Good" | Both drives should say **Good**. If either says Caution or Bad, replace it before trusting it with data. |

**About Windows 10:** as far as Microsoft has announced, free security updates for Windows 10 ended in
October 2025, and Windows 11 officially needs a newer processor than a 5th-generation Intel Core. If your
laptop is on Windows 10 it is running an OS that no longer gets free security fixes. That's tolerable for a
private staging site behind a tunnel (section 9). It is another reason to install Ubuntu when you get a USB
stick, and not to expose this laptop directly to the internet.

## 2. Install Docker Desktop and Git

1. Docker Desktop from docker.com. Keep **Use WSL 2** ticked, finish the install and restart if asked.
2. Git for Windows from git-scm.com (the defaults are fine).
3. Open Docker Desktop once. **Settings → General**: tick **Start Docker Desktop when you sign in**.

**Limit how much memory Docker takes**, or it can starve Windows. In Command Prompt:

```
notepad %UserProfile%\.wslconfig
```

Put this in (use `memory=4GB` on an 8 GB laptop, `memory=6GB` on 16 GB), save, then apply:

```
[wsl2]
memory=4GB
processors=2
swap=2GB
```

```
wsl --shutdown
```

Restart Docker Desktop.

## 3. Keep the laptop awake (run as administrator)

```
powercfg /change standby-timeout-ac 0
powercfg /change standby-timeout-dc 0
powercfg /change hibernate-timeout-ac 0
powercfg /change hibernate-timeout-dc 0
powercfg /h off
powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0
powercfg /setdcvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0
powercfg /setactive SCHEME_CURRENT
```

That means: never sleep or hibernate, and closing the lid does nothing. (`/h off` also disables Fast Startup.)

Also:

- **Keep it plugged in,** and prefer an **Ethernet cable** to Wi-Fi.
- **Windows Update:** Settings → Windows Update → Advanced options → set **Active hours** to when you
  use it. You can't switch updates off safely, so expect an occasional restart. Section 6 makes it come back by itself.
- **Automatic sign-in,** so the laptop returns to a working state after a restart with nobody there:
  `Win+R`, run `netplwiz`, untick *Users must enter a user name and password to use this computer*.
  The trade-off is that anyone with physical access can use it, so keep it somewhere safe.
- **Laptops get hot.** Keep the vents clear, put it on a hard surface, and check the temperature now and then.

## 4. Get the code and configure it

Pick the branch for this machine: `stg` for a staging server, `master` for production.

```
cd C:\
git clone --branch stg https://github.com/Hemabh6/GenericLearningApp.git gla
cd gla
copy .env.example .env
notepad .env
```

In `.env` set:

| Setting | Value |
| --- | --- |
| `POSTGRES_PASSWORD` | A long random string (this database stays on your machine) |
| `SITE_ADDRESS` | `:80` (plain web address on your own network; the tunnel in section 9 adds HTTPS) |
| `SUPERADMIN_EMAIL` | Your email |
| `SUPERADMIN_PASSWORD` | A strong password (used once, to create the account; see `DEPLOY.md`) |

Leave the Google, Microsoft and Cloudflare lines blank.

## 5. Start it

```
cd C:\gla
docker compose up -d --build
docker compose ps
```

The first build takes a while on a dual-core laptop (10 to 20 minutes is normal). Wait until `docker compose ps`
shows the app as **healthy**. Then open **http://localhost** and sign in with the super-admin account.

- **From your phone or another PC on the same Wi-Fi:** run `ipconfig`, find this laptop's *IPv4 Address*
  (something like `192.168.1.20`) and open `http://192.168.1.20`. If Windows asks whether to allow Docker on
  the network, allow it on **Private** networks only.
- **"port is already allocated" or "Only one usage of each socket address":** something else uses port 80.
  Find it with `netstat -ano | findstr :80`, or stop the Windows *World Wide Web Publishing Service* if it's running.

## 6. Make sure it comes back after a restart

Restart the laptop and touch nothing. After about three minutes it should sign in on its own, Docker Desktop
should start, and `http://localhost` should work again. The containers restart themselves (`restart: unless-stopped`
is already set), so this is only about Windows and Docker starting. If it doesn't work, check auto sign-in and the
Docker Desktop "start when you sign in" setting.

## 7. Backups to the hard disk

The app already writes a database dump every night into a Docker volume, which lives on the **same SSD** as the
database. `deploy\backup-to-folder.ps1` copies the dumps to a folder on the hard disk. If one drive dies, the other
still has your data. ✔ Tested on Windows PowerShell 5.1 with fake dumps, including paths with spaces, cleanup and re-runs.

First find the hard disk's drive letter (it's the ~931 GB one): `powershell -NoProfile -Command "Get-Volume"`.
Say it's `D:`. Try it by hand:

```
powershell -NoProfile -ExecutionPolicy Bypass -File C:\gla\deploy\backup-to-folder.ps1 -Destination D:\gla-backups
```

Then make it automatic, every night at 03:30:

```
schtasks /create /tn "Learning Desk backup copy" /sc daily /st 03:30 /rl highest /tr "powershell -NoProfile -ExecutionPolicy Bypass -File C:\gla\deploy\backup-to-folder.ps1 -Destination D:\gla-backups"
```

Copies older than 60 days are deleted (`-KeepDays` changes that; `0` keeps everything).

**Test a restore** (it uses a throwaway database and never touches the live one). In Git Bash, or from
Command Prompt like this:

```
cd C:\gla
"C:\Program Files\Git\bin\bash.exe" -c "MSYS_NO_PATHCONV=1 ./deploy/restore-test.sh"
```

**A second disk in the same laptop doesn't protect against theft, fire or a power surge.** Also copy the
newest dump to a USB drive or cloud storage now and then. `offsite-backup.sh` in `DEPLOY.md` does this for cloud storage.

## 8. Updating

```
cd C:\gla
git pull
docker compose up -d --build
```

Database changes apply by themselves, and people stay signed in. Take a manual backup first (see `DEPLOY.md`, part 5c).

## 9. Reaching it from the internet (optional)

At home you usually can't open ports (many Indian ISPs share addresses between customers), and you shouldn't
expose a laptop anyway. Use a **Cloudflare Tunnel** instead: the laptop makes an *outgoing* connection to
Cloudflare and visitors come through it. It's free, but you need a **domain name** in Cloudflare (roughly $10 a year).

The steps and the optional `compose.tunnel.yaml` are described at the top of that file. In short: create the tunnel
in Cloudflare's dashboard, point your hostname at `http://app:8080`, put the token in `.env`, then:

```
docker compose -f compose.yaml -f compose.tunnel.yaml up -d --build db app backup tunnel
```

**This part is untested.** I checked that the file is valid, but not against a real Cloudflare account, and I haven't
confirmed here that Blazor's live connections pass through the tunnel cleanly. Try it with staging first.

## 10. What was tested and what wasn't

| Tested | Not tested |
| --- | --- |
| The whole stack builds and runs under Docker Desktop on Windows, and passed browser tests through Caddy | The power, sleep and lid commands on **your** laptop |
| The backup-to-hard-disk script (cleanup, re-runs, paths with spaces) | The `.wslconfig` memory limits |
| The restore test and full disaster recovery | Automatic start after a restart on your laptop |
| The tunnel file is valid Docker Compose | The tunnel against a real Cloudflare account |

## 11. When you get a USB stick

Install **Ubuntu Server 24.04 LTS** on the SSD and use the hard disk for backups (mount it at `/mnt/backups`).
Then follow `DEPLOY.md` from Part 4, skipping the cloud steps. Docker on Linux is the more dependable way to run
this 24/7.
