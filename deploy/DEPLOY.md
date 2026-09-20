# Putting Learning Desk on the internet

One always-on Linux server runs everything with Docker: the app, PostgreSQL, Caddy (HTTPS
certificates, automatic) and a nightly database dump. A Blazor Server app keeps a live connection
open for every visitor, so it can't run on a "scale to zero" host; a small VM is the right shape.

**What you need:** a GitHub repo with this code pushed, an Oracle Cloud account (or any Linux VM),
and ideally a domain name. About an hour, most of it waiting for the first build.

Everything below marked ✔ was run against this exact setup before writing it. Cloud-console
menus change, so where a step is in Oracle's website, follow the intent if a button has moved.

---

## Part 1. Create the server (Oracle Cloud Always Free)

1. Sign up at cloud.oracle.com. Pick a **home region close to your users** (Mumbai or Hyderabad
   for India). The home region can't be changed later, and Always Free resources live in it.
   A card is needed for identity checks; Always Free resources aren't billed.
2. **Compute → Instances → Create instance**
   - Image: **Canonical Ubuntu 24.04** (choose the *aarch64* build for the Ampere shape).
   - Shape: **Ampere → VM.Standard.A1.Flex**, **2 OCPU, 12 GB** (inside the free allowance).
   - Networking: keep "Assign a public IPv4 address".
   - SSH keys: let Oracle generate a pair and **download the private key**, or paste your own public key.
   - If it says *"Out of capacity"*, that's common for the free A1 shape: try again later or another
     availability domain.
3. **Give it a permanent address.** The default public IP can change if the VM is recreated, which
   would break your DNS. In **Networking → IP management → Reserved public IPs**, create one and
   attach it to the instance's network interface. Note the address; below it is `SERVER_IP`.
4. **Open the web ports in Oracle's firewall.** *Networking → Virtual cloud networks → your VCN →
   Security Lists → Default → Add Ingress Rules*: source `0.0.0.0/0`, protocol TCP, destination port
   `80`, and another rule for `443`. (Port 22 for SSH is already open.) Do **not** open 5432: the
   database is never published to the internet.

## Part 2. Prepare the server

Connect (Windows: PowerShell has `ssh`; use the key you downloaded):

```bash
ssh -i path/to/oracle-key ubuntu@SERVER_IP
```

**Open the ports on the server's own firewall too.** Oracle's Ubuntu image blocks them a second time:

```bash
sudo iptables -I INPUT 1 -m state --state NEW -p tcp --dport 80  -j ACCEPT
sudo iptables -I INPUT 1 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save        # keeps the rules after a reboot
```

**Install Docker:**

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
exit                                   # log out and back in so the group applies
ssh -i path/to/oracle-key ubuntu@SERVER_IP
docker compose version                 # should print a version
```

Optional but sensible: `sudo apt-get update && sudo apt-get install -y unattended-upgrades` so
security updates install themselves.

## Part 3. Point a domain at the server

1. At your domain registrar's DNS page, add an **A record**: name `learn` (or `@` for the bare
   domain), value `SERVER_IP`. That gives you `learn.yourdomain.com`.
2. Check it from your own computer: `nslookup learn.yourdomain.com` should print `SERVER_IP`.
   DNS can take a few minutes, occasionally longer.

**No domain yet?** `sslip.io` gives any IP a working name, and Caddy can get a real certificate for
it. For `203.0.113.10` use `203-0-113-10.sslip.io`. Fine to start with; move to your own domain later
by changing one line in `.env` (`SITE_ADDRESS`) and restarting.

Caddy fetches the HTTPS certificate by itself the first time the site is visited, **but only if the
name already points at the server and ports 80 and 443 are open**. Do parts 1 to 3 before part 4.

## Part 4. Deploy

```bash
git clone https://github.com/YOUR-USER/GenericLearningApp.git
cd GenericLearningApp
cp .env.example .env
nano .env
```

(A **private** repo needs a login to clone: use a
[deploy key](https://docs.github.com/en/authentication/connecting-to-github-with-ssh/managing-deploy-keys)
or a personal access token.)

Fill in `.env`:

| Setting | What to put |
| --- | --- |
| `POSTGRES_PASSWORD` | A long random string. Make one: `openssl rand -base64 32` |
| `SITE_ADDRESS` | Your name, **without** `https://`, e.g. `learn.yourdomain.com` |
| `SUPERADMIN_EMAIL` | Your own email. This account can never be removed or demoted. |
| `SUPERADMIN_PASSWORD` | A strong password. It is used **once**, to create that account. |
| the four `GOOGLE_*` / `MICROSOFT_*` lines | Leave blank unless you set up those logins (see the Readme). |

Then start it:

```bash
docker compose up -d --build     # first build takes 5 to 10 minutes on the A1
docker compose ps                # every service "Up", the app "healthy"
docker compose logs -f app       # Ctrl+C to stop watching
```

The app creates its own database tables and your super admin account on first start. Open
`https://learn.yourdomain.com`.

✔ Tested: the whole stack builds and runs from this compose file, sign-in, the admin area
(a live WebSocket, through Caddy), creating a subject and publishing settings all work, and a
full container rebuild keeps you signed in with your data intact.

**Right after the first sign-in:**

1. Sign in with `SUPERADMIN_EMAIL` and `SUPERADMIN_PASSWORD`.
2. Account menu → **Password**: change it to one only you know.
3. Delete the `SUPERADMIN_PASSWORD` value from `.env` and run `docker compose up -d`. The account
   stays; the password is no longer sitting in a file.
4. Open `/admin` and look around.

### Updating later

```bash
cd GenericLearningApp
git pull
docker compose up -d --build
```

Database changes apply by themselves on start, and people stay signed in. Before an update that
changes the database, take a manual backup (part 5) first.

### If something doesn't work

| Symptom | Look at |
| --- | --- |
| Browser says the site can't be reached | Both firewalls (part 1 step 4, part 2), and `docker compose ps` |
| Browser warns about the certificate | DNS not pointing at `SERVER_IP` yet, or ports 80/443 closed: `docker compose logs caddy` |
| Page loads but buttons do nothing | The app didn't start cleanly: `docker compose logs app` |
| `POSTGRES_PASSWORD` / `SUPERADMIN_EMAIL` "is required" | `.env` is missing that line |

---

## Part 5. Backups (do not skip)

**What already happens:** a `backup` container writes a database dump every 24 hours (and one on
start) into a Docker volume and deletes dumps older than 14 days. The dump also contains the login
keys, so a restored site keeps working.

**The gap:** that volume is on the *same server*. If the server or disk is lost, the backups go with
it. So copy them somewhere else.

### 5a. Copy backups off the server

`deploy/offsite-backup.sh` does this using `rclone` inside Docker (nothing to install). ✔ Tested
against a local folder. The step that depends on your own cloud account can only be checked by you (step 3).

1. Choose somewhere to keep them. Any of these work with rclone: Oracle Object Storage (Always Free
   includes 20 GB, S3-compatible), Backblaze B2, Google Drive, or another server over SFTP.
2. One-time, create the connection ("remote"). Answer its questions for your provider:

   ```bash
   docker run --rm -it -v ~/.config/rclone:/config/rclone rclone/rclone config
   ```

   Name the remote `gla-backups`. rclone has a guide per provider at rclone.org/docs.
3. Try it by hand:

   ```bash
   RCLONE_REMOTE=gla-backups:my-bucket/genericlearningapp ./deploy/offsite-backup.sh
   ```

   It lists the dumps it copied. Check they really are in your bucket or folder.
4. Make it automatic, nightly at 03:30 (after the app's own dump):

   ```bash
   crontab -e
   ```

   add this line (adjust the path and remote):

   ```text
   30 3 * * * cd /home/ubuntu/GenericLearningApp && RCLONE_REMOTE=gla-backups:my-bucket/genericlearningapp ./deploy/offsite-backup.sh >> /home/ubuntu/offsite-backup.log 2>&1
   ```

   Off-site copies are kept 60 days (`REMOTE_RETENTION_DAYS` to change).

### 5b. Test that a backup restores

```bash
./deploy/restore-test.sh
```

✔ Tested. It restores the newest dump into a throwaway database, prints how many people, subjects
and so on it holds, and deletes the throwaway. Live data is never touched. Do this once now and
then monthly. A backup you have never restored is only a hope.

### 5c. Take a backup by hand (before risky changes)

```bash
docker compose exec backup sh -c 'pg_dump -h db -U gla -d genericlearningapp -Fc -f /backups/gla-manual-$(date -u +%Y%m%dT%H%M%SZ).dump'
```

### Recovering from a disaster

New server, or the data is damaged. Get the latest dump from your off-site storage first.
✔ Tested by emptying a running site's database, then running steps 3 and 4 from a dump: the same
login, settings and data came back.

1. Do parts 1 to 4 on the new server, **using the same `POSTGRES_PASSWORD`, `SUPERADMIN_EMAIL`**.
   Stop the app: `docker compose stop app`.
2. Put the dump file into the backups volume, e.g.
   `docker cp gla-XXXX.dump $(docker compose ps -q backup):/backups/`.
3. Replace the empty database with it:

   ```bash
   docker compose exec backup sh -c '
     dropdb   -h db -U gla genericlearningapp &&
     createdb -h db -U gla genericlearningapp &&
     pg_restore -h db -U gla -d genericlearningapp --no-owner /backups/gla-XXXX.dump'
   ```

4. `docker compose up -d`. Sign in: everyone's accounts, subjects, notes and settings are back.

---

## Known limits at launch

- **No email yet.** Sign-up can't verify addresses (the confirmation link is shown on screen),
  "Forgot password" can't send mail, and admin **invites** give the admin a link to pass on by hand.
  Wire up an email service before you rely on either.
- **Guest accounts are never cleaned up.** Fine at first; the table grows.
- **Google / Microsoft sign-in** stays hidden until you add credentials.
- **One server, one instance.** A rebuild takes the site down for a minute or so.
