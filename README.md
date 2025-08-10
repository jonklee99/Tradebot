# Project PokeBot
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/hexbyt3/PokeBot/total?style=flat-square&logoColor=Red&color=red)

![image](https://github.com/user-attachments/assets/bee51b3d-92a0-482c-a4ad-7f9f08d21f51)

## Support Discord:

For support on setting up your own instance of PokeBot, feel free to join the discord!

[<img src="https://canary.discordapp.com/api/guilds/1369342739581505536/widget.png?style=banner2">](https://discord.gg/WRs22V6DgE)

[sys-botbase](https://github.com/olliz0r/sys-botbase) client for remote control automation of Nintendo Switch consoles.

# Screenshots
![sysbot](https://github.com/user-attachments/assets/dbc0f47f-c80b-4180-8918-6336ce0646c2)



# PokeBot Control Panel
- **Locally Hosted Control Panel** 
Control all of your bots with a simple to use control panel via `http://localhost:8080` on your host machine.

 ## Control Panel
<img width="1633" height="641" alt="image" src="https://github.com/user-attachments/assets/9e67d6d6-273a-4e2c-bf0e-ff7eb38b5ca8" />
<img width="1642" height="1095" alt="image" src="https://github.com/user-attachments/assets/762e41ce-0d66-4376-9019-9530a9360d80" />

 ## Remote Control
Control your switches right from the control center.  Simply open up the Remote Control window, select the IP of the switch you wish to control, and start clicking away on the remotes!
<img width="1405" height="1151" alt="image" src="https://github.com/user-attachments/assets/d92647c4-e177-4e19-97b2-34cfd26bb77e" />

 ## Log Viewer
View logs right from the control center!  Search for errors, users, and more!
<img width="1410" height="1160" alt="image" src="https://github.com/user-attachments/assets/aaf823a9-6709-49e8-8a82-52f6865cbf49" />

  ## Realtime feedback
 Control all of your programs with the click of a button!  Idle all, stop all, start all, turn on/off all your switch screens at once!
<img width="1037" height="640" alt="image" src="https://github.com/user-attachments/assets/42dd0998-a759-4739-b2c7-ba96d65124a9" />

- **Automatic Updates**
Update your bots with the click of a button to always stay current with latest PKHeX/ALM releases.
<img width="712" height="875" alt="image" src="https://github.com/user-attachments/assets/7fd0215b-c9a4-4d15-ac52-fb9d6a8de27c" />

# 📱 Access PokeBot from Any Device on Your Network

## Quick Setup

### 1. Enable Network Access (choose one):
- **Option A:** Right-click PokeBot.exe → Run as Administrator
- **Option B:** Run in admin cmd: `netsh http add urlacl url=http://+:8080/ user=Everyone`

### 2. Allow Through Firewall:
Run in admin cmd:
```cmd
netsh advfirewall firewall add rule name="PokeBot Web" dir=in action=allow protocol=TCP localport=8080
```

### 3. Connect From Your Phone:
- Get your PC's IP: `ipconfig` (look for IPv4 Address)
- On your phone: `http://YOUR-PC-IP:8080`
- Example: `http://192.168.1.100:8080`

## Requirements
- Same WiFi network
- Windows Firewall rule (step 2)
- Admin rights (first time only)

---

# Other Program Features

- Live Log Searching through the Log tab.  Search for anything and find results fast.

![image](https://github.com/user-attachments/assets/820d8892-ae52-4aa6-981a-cb57d1c32690)

- Tray Support - When you press X to close out of the program, it goes to the system tray.  Right click the PokeBot icon in the tray to exit or control the bot.

![image](https://github.com/user-attachments/assets/3a30b334-955c-4fb3-b7d8-60cd005a2e18)

# Pokémon Trading Bot Commands

## Core Trading Commands

| Command | Aliases | Description | Usage | Permissions |
|---------|---------|-------------|--------|-------------|
| `trade` | `t` | Trade a Pokémon from Showdown set or file | `.trade [code] <showdown_set>` or attach file | Trade Role |
| `hidetrade` | `ht` | Trade without showing embed details | `.hidetrade [code] <showdown_set>` or attach file | Trade Role |
| `batchTrade` | `bt` | Trade multiple Pokémon (max 3) | `.bt <sets_separated_by_--->` | Trade Role |
| `egg` | - | Trade an egg from provided Pokémon name | `.egg [code] <pokemon_name>` | Trade Role |

## Specialized Trading Commands

| Command | Aliases | Description | Usage | Permissions |
|---------|---------|-------------|--------|-------------|
| `dittoTrade` | `dt`, `ditto` | Trade Ditto with specific stats/nature | `.dt [code] <stats> <language> <nature>` | Public |
| `itemTrade` | `it`, `item` | Trade Pokémon holding requested item | `.it [code] <item_name>` | Public |
| `mysteryegg` | `me` | Trade random egg with perfect IVs | `.me [code]` | Public |
| `mysterymon` | `mm` | Trade random Pokémon with perfect stats | `.mm [code]` | Trade Role |

## Fix & Clone Commands

| Command | Aliases | Description | Usage | Permissions |
|---------|---------|-------------|--------|-------------|
| `fixOT` | `fix`, `f` | Fix OT/nickname if advert detected | `.fix [code]` | FixOT Role |
| `clone` | `c` | Clone the Pokémon you show | `.clone [code]` | Clone Role |
| `dump` | `d` | Dump the Pokémon you show | `.dump [code]` | Dump Role |

## Event & Battle-Ready Commands

| Command | Aliases | Description | Usage | Permissions |
|---------|---------|-------------|--------|-------------|
| `listevents` | `le` | List available event files | `.le [filter] [pageX]` | Public |
| `eventrequest` | `er` | Request specific event by index | `.er <index>` | Trade Role |
| `battlereadylist` | `brl` | List battle-ready files | `.brl [filter] [pageX]` | Public |
| `battlereadyrequest` | `brr`, `br` | Request battle-ready file by index | `.brr <index>` | Trade Role |
| `specialrequestpokemon` | `srp` | List/request wondercard events | `.srp <gen> [filter] [pageX]` or `.srp <gen> <index>` | Public/Trade Role |
| `geteventpokemon` | `gep` | Download event as pk file | `.gep <gen> <index> [language]` | Public |

## Queue & Status Commands

| Command | Aliases | Description | Usage | Permissions |
|---------|---------|-------------|--------|-------------|
| `tradeList` | `tl` | Show users in trade queue | `.tl` | Admin |
| `fixOTList` | `fl`, `fq` | Show users in FixOT queue | `.fl` | Admin |
| `cloneList` | `cl`, `cq` | Show users in clone queue | `.cl` | Admin |
| `dumpList` | `dl`, `dq` | Show users in dump queue | `.dl` | Admin |
| `medals` | `ml` | Show your trade count and medals | `.ml` | Public |

## Admin Commands

| Command | Aliases | Description | Usage | Permissions |
|---------|---------|-------------|--------|-------------|
| `tradeUser` | `tu`, `tradeOther` | Trade file to mentioned user | `.tu [code] @user` + attach file | Admin |

## Usage Notes

- **Code Parameter**: Optional trade code (8 digits). If not provided, a random code is generated.
- **Batch Trading**: Separate multiple sets with `---` in batch trades.
- **File Support**: Commands accept both Showdown sets and attached .pk files.
- **Permissions**: Different commands require different Discord roles for access.
- **Languages**: Supported languages for events include EN, JA, FR, DE, ES, IT, KO, ZH.

## Supported Games

- Sword/Shield (SWSH)
- Brilliant Diamond/Shining Pearl (BDSP) 
- Legends Arceus (PLA)
- Scarlet/Violet (SV)
- Let's Go Pikachu/Eevee (LGPE)
  

# License
Refer to the `License.md` for details regarding licensing.
