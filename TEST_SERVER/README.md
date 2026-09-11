# Local CatosHoverInspector test server

`start_catoshoverinspector_test.bat` mirrors the CatosChestViewer test harness
and the shared `world-setup.md` contract.

The launcher uses the existing Valheim 1.0 world directory:

```text
World source: C:\Users\magni\Downloads\Dedicated
Save root:   C:\Users\magni\Downloads
World name:  Dedicated
World mount: C:\Users\magni\Downloads\worlds_local\Dedicated
```

Valheim resolves worlds under `<savedir>\worlds_local\<world>`, so the
launcher exposes the source through a directory junction. It does not copy,
regenerate, repair, delete, or modify the world source. This is the same
arrangement used by CatosChestViewer.

The launcher:

- rebuilds the client-only DLL on every launch;
- deploys it only to the `CatosHoverInspector` r2modman client profile;
- never deploys the DLL to the dedicated server;
- copies the seed admin list to the active save root;
- seeds the client config only if it does not already exist;
- refuses to run while Valheim is open;
- checks for a stale dedicated-server assembly before launch.

Default connection:

```text
127.0.0.1:2462
Password: 696969
Public: 0
```

Override paths with these environment variables before launching:

```powershell
$env:CHI_SERVER_INSTALL = 'C:\path\to\Valheim dedicated server'
$env:CHI_CLIENT_PROFILE = 'C:\path\to\r2modman\profiles\CatosHoverInspector'
$env:CHI_WORLD_DIR = 'C:\Users\magni\Downloads\Dedicated'
$env:CHI_SAVE_DIR = 'C:\Users\magni\Downloads'
```

The admin list is only test-harness setup. CatosHoverInspector does not read it
and has no admin-only behavior.

